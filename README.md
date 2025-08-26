# AI SmartCut

A WPF application for AI-powered background removal using the U²-Net model.

## Features

- **AI Background Removal**: Uses U²-Net ONNX model for high-quality background removal
- **Modern UI**: Clean, dark-themed interface with real-time preview
- **Multiple Formats**: Supports PNG, JPG, JPEG, and BMP input formats
- **Alpha Channel**: Outputs PNG with proper alpha channel for transparent backgrounds
- **Batch Ready**: Architecture supports future batch processing capabilities

## Current Issue

⚠️ **The application currently has a critical bug**: The `u2net.onnx` model file is missing and only a Git LFS pointer exists.

### What This Means
- The application will show "Error - Model file not downloaded" on startup
- Background removal functionality will not work
- The remove background button will be disabled

### How to Fix

1. **Download the U²-Net model**:
   - Visit: https://github.com/xuebinqin/U-2-Net/releases
   - Download the `u2net.onnx` file (approximately 176MB)
   - Place it in: `src/AI.SmartCut/Assets/Models/u2net.onnx`

2. **Alternative download sources**:
   - Hugging Face: https://huggingface.co/danielgatis/rembg/resolve/main/u2net.onnx
   - Direct link: https://github.com/xuebinqin/U-2-Net/releases/download/v1.0/u2net.onnx

3. **Verify the file**:
   - The file should be approximately 176MB
   - It should NOT start with "version https://git-lfs.github.com/spec/v1"
   - It should be a binary ONNX file

## Requirements

- .NET 8.0 or later
- Windows 10/11
- At least 2GB RAM (4GB recommended)
- U²-Net ONNX model file (see above)

## Building

```bash
dotnet restore
dotnet build
dotnet run
```

## Usage

1. Launch the application
2. Click "Remove Background" button
3. Select an image file (PNG, JPG, JPEG, or BMP)
4. Wait for processing to complete
5. The processed image will be saved with "_nobg.png" suffix

## Technical Details

- **Framework**: .NET 8.0 WPF
- **AI Model**: U²-Net (U²-Net: Going Deeper with Nested U-Structure for Salient Object Detection)
- **Image Processing**: SixLabors.ImageSharp
- **ONNX Runtime**: Microsoft.ML.OnnxRuntime
- **Architecture**: MVVM-ready with service layer

## Troubleshooting

### Common Issues

1. **"Model not found" error**:
   - Ensure the `u2net.onnx` file is in the correct location
   - Check file permissions

2. **"Git LFS pointer" error**:
   - The model file wasn't downloaded properly
   - Re-download the model file

3. **Out of memory errors**:
   - Close other applications
   - Use smaller images
   - Ensure sufficient RAM

4. **Slow processing**:
   - The model requires significant computational resources
   - Consider using smaller images for testing

## Contributing

This is an open-source project. Contributions are welcome!

## License

[Add your license information here]