using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AI.SmartCut
{
    public static class BackgroundRemover
    {
        // Model expects 320x320 by default for vanilla U2Net
        private const int ModelW = 320;
        private const int ModelH = 320;

        private static readonly object _lock = new();
        private static InferenceSession? _session;
        private static string? _inputName;
        private static string? _outputName;
        private static bool _initialized = false;
        private static string? _initializationError;

        static BackgroundRemover()
        {
            try
            {
                InitializeModel();
                _initialized = true;
            }
            catch (Exception ex)
            {
                _initializationError = ex.Message;
                // Don't throw from static constructor - let the calling method handle it
            }
        }

        private static void InitializeModel()
        {
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Models", "u2net.onnx");
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model not found at: {modelPath}");

            // Check if file is actually a Git LFS pointer
            var firstLine = File.ReadLines(modelPath).FirstOrDefault();
            if (firstLine?.StartsWith("version https://git-lfs.github.com/spec/v1") == true)
                throw new FileNotFoundException($"Model file is a Git LFS pointer. Please download the actual u2net.onnx model file.");

            // Create session once
            _session = new InferenceSession(modelPath);

            // Detect IO names robustly
            var inputs = _session.InputMetadata.Keys.ToList();
            var outputs = _session.OutputMetadata.Keys.ToList();

            if (!inputs.Any())
                throw new InvalidOperationException("No input nodes found in ONNX model");

            if (!outputs.Any())
                throw new InvalidOperationException("No output nodes found in ONNX model");

            _inputName = inputs.FirstOrDefault(n =>
                string.Equals(n, "input", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "images", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "input_0", StringComparison.OrdinalIgnoreCase)
            ) ?? inputs.First();

            _outputName = outputs.FirstOrDefault(n =>
                string.Equals(n, "output", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "sigmoid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "mask", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "pred", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "1704", StringComparison.OrdinalIgnoreCase) // sometimes unnamed layers
            ) ?? outputs.First();
        }

        /// <summary>
        /// Checks if the background removal model is properly initialized and ready to use.
        /// </summary>
        /// <returns>True if the model is ready, false otherwise.</returns>
        public static bool IsModelReady()
        {
            return _initialized && _session != null;
        }

        /// <summary>
        /// Gets the current initialization status and any error messages.
        /// </summary>
        /// <returns>A tuple containing initialization status and error message if any.</returns>
        public static (bool IsReady, string? ErrorMessage) GetModelStatus()
        {
            if (_initialized)
                return (true, null);
            
            return (false, _initializationError ?? "Model initialization failed");
        }

        /// <summary>
        /// Removes background via U^2-Net ONNX (returns RGBA image with alpha from mask).
        /// </summary>
        public static Image<Rgba32> RemoveBackground(Image<Rgba32> inputImage)
        {
            if (inputImage == null)
                throw new ArgumentNullException(nameof(inputImage), "Input image cannot be null");

            if (inputImage.Width <= 0 || inputImage.Height <= 0)
                throw new ArgumentException("Input image dimensions must be greater than zero", nameof(inputImage));

            if (!_initialized)
            {
                if (!string.IsNullOrEmpty(_initializationError))
                    throw new InvalidOperationException($"Model not initialized: {_initializationError}");
                throw new InvalidOperationException("Model initialization failed");
            }

            if (_session is null) 
                throw new InvalidOperationException("ONNX session not initialized.");

            lock (_lock)
            {
                try
                {
                    // Keep original size for final output
                    int ow = inputImage.Width;
                    int oh = inputImage.Height;

                    // Prepare model input
                    using var resized = inputImage.Clone(ctx => ctx.Resize(ModelW, ModelH));
                    var input = ImageToCHW(resized); // 1x3xH xW

                    // Run inference
                    using var results = _session.Run(new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor(_inputName!, input)
                    });

                    // Read mask tensor (assume NCHW 1x1xH xW or NHWC variants -> handled below)
                    var maskTensor = ExtractMask(results, ModelH, ModelW);

                    // Convert tensor to grayscale mask [0..255]
                    using var maskImg = TensorToGray(maskTensor, ModelW, ModelH, normalize: true);

                    // Resize mask to original size
                    using var finalMask = maskImg.Clone(ctx => ctx.Resize(ow, oh));

                    // Apply to original (create cut-out with alpha)
                    return ApplyAlpha(inputImage, finalMask);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Background removal failed: {ex.Message}", ex);
                }
            }
        }

        private static DenseTensor<float> ImageToCHW(Image<Rgba32> img)
        {
            var t = new DenseTensor<float>(new[] { 1, 3, img.Height, img.Width });
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var p = img[x, y];
                    t[0, 0, y, x] = p.R / 255f;
                    t[0, 1, y, x] = p.G / 255f;
                    t[0, 2, y, x] = p.B / 255f;
                }
            }
            return t;
        }

        private static Tensor<float> ExtractMask(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results, int h, int w)
        {
            // Pick the resolved output name
            var mv = results.FirstOrDefault(r => r.Name == _outputName) ?? results.First();
            var tensor = mv.AsTensor<float>();

            // Handle common shapes:
            // 1) [1,1,H,W] -> NCHW mask
            // 2) [1,H,W,1] -> NHWC mask
            // 3) [1,H,W]   -> CHW collapsed
            // 4) [1,7, H,W] -> multi-stage U2Net; usually last map or average
            var dims = tensor.Dimensions.ToArray();

            if (dims.Length == 4)
            {
                // [N,C,H,W] or [N,H,W,C]
                if (dims[1] == 1 && dims[2] == h && dims[3] == w)
                {
                    // NCHW single channel
                    return tensor;
                }
                if (dims[3] == 1 && dims[1] == h && dims[2] == w)
                {
                    // NHWC single channel -> convert to NCHW-like view
                    var nchw = new DenseTensor<float>(new[] { 1, 1, h, w });
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            nchw[0, 0, y, x] = tensor[0, y, x, 0];
                    return nchw;
                }
                if (dims[1] > 1 && dims[2] == h && dims[3] == w)
                {
                    // Multi-map (e.g., 7 side outputs). Take the last one.
                    int c = dims[1];
                    var last = new DenseTensor<float>(new[] { 1, 1, h, w });
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            last[0, 0, y, x] = tensor[0, c - 1, y, x];
                    return last;
                }
            }
            else if (dims.Length == 3 && dims[1] == h && dims[2] == w)
            {
                // [1,H,W] -> expand channel dim
                var nchw = new DenseTensor<float>(new[] { 1, 1, h, w });
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        nchw[0, 0, y, x] = tensor[0, y, x];
                return nchw;
            }

            // Fallback: try flatten interpretation
            var flat = tensor.ToArray();
            if (flat.Length == h * w)
            {
                var nchw = new DenseTensor<float>(new[] { 1, 1, h, w });
                for (int i = 0; i < flat.Length; i++)
                {
                    int y = i / w;
                    int x = i % w;
                    nchw[0, 0, y, x] = flat[i];
                }
                return nchw;
            }

            throw new InvalidDataException($"Unexpected mask tensor shape: [{string.Join(",", dims)}]");
        }

        private static Image<Rgba32> TensorToGray(Tensor<float> tensor, int w, int h, bool normalize)
        {
            // Expecting [1,1,H,W]
            var img = new Image<Rgba32>(w, h);

            float min = float.MaxValue, max = float.MinValue;
            if (normalize)
            {
                // find min/max for normalization
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        var v = tensor[0, 0, y, x];
                        if (v < min) min = v;
                        if (v > max) max = v;
                    }
                if (Math.Abs(max - min) < 1e-8f) { min = 0f; max = 1f; }
            }
            else { min = 0f; max = 1f; }

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var v = tensor[0, 0, y, x];
                    // optional sigmoid if model outputs logits (uncomment if needed)
                    // v = 1f / (1f + MathF.Exp(-v));

                    if (normalize) v = (v - min) / (max - min);
                    byte g = (byte)Math.Clamp(v * 255f, 0f, 255f);
                    img[x, y] = new Rgba32(g, g, g, 255);
                }

            return img;
        }

        private static Image<Rgba32> ApplyAlpha(Image<Rgba32> original, Image<Rgba32> grayMask)
        {
            var result = new Image<Rgba32>(original.Width, original.Height);

            // Ensure same size
            using var mask = grayMask.Clone(ctx => ctx.Resize(original.Width, original.Height));

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    var p = original[x, y];
                    byte a = mask[x, y].R; // grayscale -> alpha
                    result[x, y] = new Rgba32(p.R, p.G, p.B, a);
                }
            }
            return result;
        }
    }
}
