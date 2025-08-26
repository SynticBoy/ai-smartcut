using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AI.SmartCut
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Check model status and update UI accordingly
            var (isReady, errorMessage) = BackgroundRemover.GetModelStatus();
            if (isReady)
            {
                TxtStatus.Text = "Ready - Model loaded successfully";
                BtnRemoveBg.IsEnabled = true;
            }
            else
            {
                TxtStatus.Text = "Error - Model not available";
                BtnRemoveBg.IsEnabled = false;
                
                // Show error details in a more user-friendly way
                var errorText = errorMessage ?? "Unknown error";
                if (errorText.Contains("Git LFS pointer"))
                {
                    TxtStatus.Text = "Error - Model file not downloaded";
                }
                else if (errorText.Contains("not found"))
                {
                    TxtStatus.Text = "Error - Model file missing";
                }
            }
        }

        private void RemoveBackground_Click(object sender, RoutedEventArgs e)
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

                    // Validate input image
                    if (!File.Exists(dialog.FileName))
                    {
                        MessageBox.Show("Selected file does not exist.", "AI SmartCut", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtStatus.Text = "Ready";
                        return;
                    }

                    // Check file size to prevent memory issues
                    var fileInfo = new FileInfo(dialog.FileName);
                    if (fileInfo.Length > 100 * 1024 * 1024) // 100MB limit
                    {
                        MessageBox.Show("File is too large. Please use images smaller than 100MB.", "AI SmartCut", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtStatus.Text = "Ready";
                        return;
                    }

                    using var img = Image.Load<Rgba32>(dialog.FileName);
                    
                    // Check image dimensions
                    if (img.Width > 4096 || img.Height > 4096)
                    {
                        var result = MessageBox.Show(
                            $"Image dimensions ({img.Width}x{img.Height}) are very large. This may cause memory issues or slow processing. Continue anyway?",
                            "AI SmartCut", 
                            MessageBoxButton.YesNo, 
                            MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.No)
                        {
                            TxtStatus.Text = "Ready";
                            return;
                        }
                    }

                    ImgOriginal.Source = ToBitmapImage(img);

                    // Check if model is available before processing
                    try
                    {
                        using var cut = BackgroundRemover.RemoveBackground(img);
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
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Model not initialized") || ex.Message.Contains("Model file is a Git LFS pointer"))
                    {
                        TxtStatus.Text = "Model Error";
                        MessageBox.Show($"Background removal failed: {ex.Message}\n\nPlease ensure the u2net.onnx model file is properly downloaded and placed in the Assets/Models folder.", 
                            "AI SmartCut", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (OutOfMemoryException)
                    {
                        TxtStatus.Text = "Memory Error";
                        MessageBox.Show("Processing failed due to insufficient memory. Try using a smaller image or closing other applications.", 
                            "AI SmartCut", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    TxtStatus.Text = "Error";
                    MessageBox.Show($"An unexpected error occurred: {ex.Message}", "AI SmartCut", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static BitmapImage ToBitmapImage(Image<Rgba32> image)
        {
            using var ms = new MemoryStream();
            image.SaveAsPng(ms); // preserves alpha by default
            ms.Position = 0;

            // Create a copy of the stream data to avoid disposal issues
            var imageData = ms.ToArray();
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = new MemoryStream(imageData);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}
