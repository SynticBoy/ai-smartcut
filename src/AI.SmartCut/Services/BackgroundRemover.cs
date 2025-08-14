using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace AI.SmartCut
{
    public static class BackgroundRemover
    {
        private static InferenceSession _session;
        private static readonly object _lock = new();

        static BackgroundRemover()
        {
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Models", "u2net.onnx");

            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model file not found at {modelPath}");

            _session = new InferenceSession(modelPath);
        }

        public static Image<Rgba32> RemoveBackground(Image<Rgba32> inputImage)
        {
            lock (_lock)
            {
                // Resize to model input
                var resized = inputImage.Clone(ctx => ctx.Resize(320, 320));
                var inputTensor = ImageToTensor(resized);

                // Run model
                var inputs = new List<NamedOnnxValue> {
                    NamedOnnxValue.CreateFromTensor("input", inputTensor)
                };
                using var results = _session.Run(inputs);
                var output = results.First().AsTensor<float>();

                // Convert mask to image
                var mask = TensorToImage(output, resized.Width, resized.Height);

                // Apply mask to original image
                return ApplyMask(inputImage, mask);
            }
        }

        private static DenseTensor<float> ImageToTensor(Image<Rgba32> image)
        {
            var tensor = new DenseTensor<float>(new[] { 1, 3, image.Height, image.Width });
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    tensor[0, 0, y, x] = pixel.R / 255f;
                    tensor[0, 1, y, x] = pixel.G / 255f;
                    tensor[0, 2, y, x] = pixel.B / 255f;
                }
            }
            return tensor;
        }

        private static Image<Rgba32> TensorToImage(Tensor<float> tensor, int width, int height)
        {
            var mask = new Image<Rgba32>(width, height);
            var min = tensor.Min();
            var max = tensor.Max();
            var range = max - min;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var value = (tensor[0, 0, y, x] - min) / range;
                    byte intensity = (byte)(value * 255);
                    mask[x, y] = new Rgba32(intensity, intensity, intensity, 255);
                }
            }
            return mask;
        }

        private static Image<Rgba32> ApplyMask(Image<Rgba32> original, Image<Rgba32> mask)
        {
            var result = new Image<Rgba32>(original.Width, original.Height);
            var resizedMask = mask.Clone(ctx => ctx.Resize(original.Width, original.Height));

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    var pixel = original[x, y];
                    var alpha = resizedMask[x, y].R / 255f;
                    result[x, y] = new Rgba32(pixel.R, pixel.G, pixel.B, (byte)(alpha * 255));
                }
            }
            return result;
        }
    }
}
