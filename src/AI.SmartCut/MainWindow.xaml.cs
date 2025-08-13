using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace AI.SmartCut
{
    public partial class MainWindow : Window
    {
        private string? _currentImagePath;
        private byte[]? _resultPngBytes;

        private readonly Services.BackgroundRemover _bg;

        public MainWindow()
        {
            InitializeComponent();

            // مسیر مدل: src/AI.SmartCut/Assets/Models/u2net.onnx
            var modelPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Assets", "Models", "u2net.onnx");

            _bg = new Services.BackgroundRemover(modelPath);

            if (!_bg.IsReady)
                TxtInfo.Text = "Model not found: Assets/Models/u2net.onnx (upload the ONNX model)";
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Images|*.png;*.jpg;*.jpeg;*.webp;*.bmp",
                Title = "Open an image"
            };
            if (ofd.ShowDialog() == true)
            {
                _currentImagePath = ofd.FileName;
                ImgOriginal.Source = new BitmapImage(new Uri(_currentImagePath));
                ImgResult.Source = null;
                _resultPngBytes = null;

                BtnRemove.IsEnabled = _bg.IsReady;
                BtnExport.IsEnabled = false;
                TxtInfo.Text = _bg.IsReady ? "Ready to remove background." : "Model missing.";
            }
        }

        private async void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentImagePath) || !_bg.IsReady)
            {
                TxtInfo.Text = "Open an image and ensure model exists first.";
                return;
            }

            try
            {
                BtnRemove.IsEnabled = false;
                TxtInfo.Text = "Processing…";

                var pngBytes = await _bg.RemoveBackgroundAsync(_currentImagePath);
                _resultPngBytes = pngBytes;

                using var ms = new MemoryStream(pngBytes);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                ImgResult.Source = bmp;

                BtnExport.IsEnabled = true;
                TxtInfo.Text = "Done.";
            }
            catch (Exception ex)
            {
                TxtInfo.Text = "Error: " + ex.Message;
                BtnExport.IsEnabled = false;
            }
            finally
            {
                BtnRemove.IsEnabled = true;
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (_resultPngBytes == null)
            {
                TxtInfo.Text = "Nothing to export.";
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = "AI_SmartCut.png"
            };
            if (sfd.ShowDialog() == true)
            {
                File.WriteAllBytes(sfd.FileName, _resultPngBytes);
                TxtInfo.Text = "Saved: " + sfd.FileName;
            }
        }
    }
}
