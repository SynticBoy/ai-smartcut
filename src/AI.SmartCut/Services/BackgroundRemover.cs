using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AI.SmartCut.Services
{
    public sealed class BackgroundRemover : IDisposable
    {
        private readonly string _modelPath;
        private InferenceSession? _session;
        private string? _inputName;
        private string? _outputName;
        private int _inH = 320, _inW = 320; 

        public bool IsReady => _session != null;

        public BackgroundRemover(string modelPath)
        {
            _modelPath = modelPath;
            if (!File.Exists(_modelPath)) return;

            var opts = new SessionOptions();
            _session = new InferenceSession(_modelPath, opts);

            _inputName = _session.InputMetadata.Keys.FirstOrDefault();
            _outputName = _session.OutputMetadata.Keys.FirstOrDefault();

            if (_inputName != null)
            {
                var meta = _session.InputMetadata[_inputName];
                var dims = meta.Dimensions;
                if (dims != null && dims.Length == 4)
                {
                    if (dims[2] > 0) _inH = dims[2].Value;
                    if (dims[3] > 0) _inW = dims[3].Value;
                }
            }
        }

        public async Task<byte[]> RemoveBackgroundAsync(string imagePath)
        {
            if (!IsReady) throw new InvalidOperationException("Model not loaded.");

            using var src = await Image.LoadAsync<Rgba32>(imagePath).ConfigureAwait(false);

            using var resized = src.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(_inW, _inH),
                Mode = ResizeMode.Stretch,
                Sampler = KnownResamplers.Bicubic
            }));

            var inputTensor = new DenseTensor<float>(new[] { 1, 3, _inH, _inW });
            for (int y = 0; y < _inH; y++)
            {
                var row = resized.GetPixelRowSpan(y);
                for (int x = 0; x < _inW; x++)
                {
                    int idx = y * _inW + x;
                    var p = row[x];
                    inputTensor[0, 0, y, x] = p.R / 255f;
                    inputTensor[0, 1, y, x] = p.G / 255f;
                    inputTensor[0, 2, y, x] = p.B / 255f;
                }
            }

            var feeds = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName ?? "input", inputTensor)
            };

            float[] mask1d;
            int mH = _inH, mW = _inW;

            using (var results = _session!.Run(feeds))
            {
                var first = _outputName != null ? results.First(v => v.Name == _outputName)
                                                : results.First();

                var t = first.AsTensor<float>();
                var dims = t.Dimensions.ToArray();

                if (dims.Length == 4)
                {
                    mH = dims[2];
                    mW = dims[3];
                }
                else if (dims.Length == 3)
                {
                    mH = dims[1];
                    mW = dims[2];
                }
                else if (dims.Length == 2)
                {
                    mH = dims[0];
                    mW = dims[1];
                }

                mask1d = t.ToArray();
            }

            using var maskImg = new Image<L8>(mW, mH);
            for (int y = 0; y < mH; y++)
            {
                var row = maskImg.GetPixelRowSpan(y);
                for (int x = 0; x < mW; x++)
                {
                    int idx = y * mW + x;
                    var v = idx < mask1d.Length ? mask1d[idx] : 0f;
                    var m = Math.Clamp(v, 0f, 1f);
                    row[x] = new L8((byte)(m * 255));
                }
            }

            using var maskResized = maskImg.Clone(ctx => ctx.Resize(src.Width, src.Height, KnownResamplers.Bicubic));

            using var output = new Image<Rgba32>(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            {
                var sRow = src.GetPixelRowSpan(y);
                var mRow = maskResized.GetPixelRowSpan(y);
                var oRow = output.GetPixelRowSpan(y);
                for (int x = 0; x < src.Width; x++)
                {
                    var p = sRow[x];
                    var a = mRow[x].PackedValue; // 0..255
                    oRow[x] = new Rgba32(p.R, p.G, p.B, a);
                }
            }

            using var ms = new MemoryStream();
            await output.SaveAsPngAsync(ms).ConfigureAwait(false);
            return ms.ToArray();
        }

        public void Dispose() => _session?.Dispose();
    }
}
