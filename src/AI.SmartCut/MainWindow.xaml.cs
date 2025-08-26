using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AI.SmartCut
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TxtStatus.Text = "Ready";
        }

        private async void RemoveBackground_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                // NOTE: removed *.webp from filter to avoid runtime NotSupportedException unless ImageSharp.Webp is added.
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    TxtStatus.Text = "Processing...";
                    BtnRemoveBg.IsEnabled = false; // Disable button during processing

                    using var img = Image.Load<Rgba32>(dialog.FileName);
                    ImgOriginal.Source = ToBitmapImage(img);

                    // Run background removal on a background thread to avoid UI freezing
                    var cut = await Task.Run(() => BackgroundRemover.RemoveBackground(img));
                    using (cut)
                    {
                        ImgCutout.Source = ToBitmapImage(cut);

                    // Save beside original
                    var savePath = Path.Combine(
                        Path.GetDirectoryName(dialog.FileName)!,
                        Path.GetFileNameWithoutExtension(dialog.FileName) + "_nobg.png"
                    );

                    var encoder = new PngEncoder
                    {
                        ColorType = PngColorType.RgbWithAlpha
                    };

                        cut.Save(savePath, encoder);
                        TxtStatus.Text = $"Saved: {savePath}";
                    }
                }
                catch (FileNotFoundException ex)
                {
                    TxtStatus.Text = "Model file missing";
                    MessageBox.Show($"Model Error: {ex.Message}\n\nPlease check the models directory and ensure the U2Net ONNX model is properly installed.", 
                        "AI SmartCut - Model Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (InvalidDataException ex)
                {
                    TxtStatus.Text = "Invalid model file";
                    MessageBox.Show($"Model Error: {ex.Message}\n\nThe model file appears to be corrupted or is a Git LFS pointer file.", 
                        "AI SmartCut - Invalid Model", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (InvalidOperationException ex)
                {
                    TxtStatus.Text = "Model loading failed";
                    MessageBox.Show($"ONNX Runtime Error: {ex.Message}\n\nPlease verify the model file is compatible with ONNX Runtime.", 
                        "AI SmartCut - Model Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    TxtStatus.Text = "Processing error";
                    MessageBox.Show($"Unexpected Error: {ex.Message}\n\nPlease try again or check the image file format.", 
                        "AI SmartCut - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    BtnRemoveBg.IsEnabled = true; // Re-enable button after processing
                }
            }
        }

        private static BitmapImage ToBitmapImage(Image<Rgba32> image)
        {
            using var ms = new MemoryStream();
            image.SaveAsPng(ms); // preserves alpha by default
            ms.Position = 0;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}
