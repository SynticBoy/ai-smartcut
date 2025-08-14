using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Windows;

namespace AI.SmartCut
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RemoveBackground_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() == true)
            {
                using var image = Image.Load<Rgba32>(dialog.FileName);
                var result = BackgroundRemover.RemoveBackground(image);

                var savePath = Path.Combine(
                    Path.GetDirectoryName(dialog.FileName),
                    Path.GetFileNameWithoutExtension(dialog.FileName) + "_nobg.png"
                );

                result.Save(savePath);
                MessageBox.Show($"Saved without background at:\n{savePath}", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
