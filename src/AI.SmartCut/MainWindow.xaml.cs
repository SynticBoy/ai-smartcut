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
            TxtStatus.Text = "Ready";
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

                    using var img = Image.Load<Rgba32>(dialog.FileName);
                    ImgOriginal.Source = ToBitmapImage(img);

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
                catch (Exception ex)
                {
                    TxtStatus.Text = "Error";
                    MessageBox.Show(ex.Message, "AI SmartCut", MessageBoxButton.OK, MessageBoxImage.Error);
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
