# AI SmartCut

A Windows desktop application that uses AI-powered background removal to automatically cut out objects from images.

## Features

- **AI Background Removal**: Uses U²-Net deep learning model for accurate background removal
- **Multiple Image Formats**: Supports PNG, JPG, JPEG, and BMP input files  
- **Transparent Output**: Saves results as PNG files with transparent backgrounds
- **Modern UI**: Clean, dark-themed interface with real-time preview
- **Batch Processing Ready**: Foundation for future batch processing features

## Technology Stack

- **Framework**: .NET 8.0 with WPF (Windows Presentation Foundation)
- **AI Model**: U²-Net ONNX model for semantic segmentation
- **Image Processing**: SixLabors.ImageSharp library
- **ML Runtime**: Microsoft.ML.OnnxRuntime for model inference

## Prerequisites

- Windows 10/11 (x64)
- .NET 8.0 Runtime (included in self-contained builds)
- U²-Net ONNX model file (~176MB)

## Installation & Setup

### 1. Clone the Repository
```bash
git clone <repository-url>
cd ai-smartcut
```

### 2. Download the AI Model
The U²-Net ONNX model is required for background removal:

**Option A: Git LFS (Recommended)**
```bash
git lfs install
git lfs pull
```

**Option B: Manual Download**
1. Download the U²-Net ONNX model from [U²-Net repository](https://github.com/xuebinqin/U-2-Net)
2. Place the `u2net.onnx` file in `src/AI.SmartCut/Assets/Models/`
3. Ensure the file size is ~176MB (not a Git LFS pointer file)

### 3. Build the Application
```bash
dotnet build AI.SmartCut.sln --configuration Release
```

### 4. Run the Application
```bash
dotnet run --project src/AI.SmartCut/AI.SmartCut.csproj
```

## Usage

1. **Launch the Application**: Run AI SmartCut
2. **Select Image**: Click "Remove Background" and choose an image file
3. **Processing**: The AI will automatically remove the background
4. **Preview**: View original and processed images side-by-side
5. **Auto-Save**: Processed image is automatically saved with `_nobg.png` suffix

## Recent Bug Fixes

The following critical bugs have been identified and fixed:

### ✅ Fixed Issues

1. **Solution File Project References**
   - **Problem**: Solution file contained references to non-existent project GUID
   - **Fix**: Removed orphaned project configuration entries
   - **Impact**: Clean solution builds without warnings

2. **Model Path Resolution**
   - **Problem**: Mismatch between model path in code (`Assets/Models`) and build configuration (`models`)
   - **Fix**: Updated project file to properly copy Assets to models directory
   - **Impact**: Application can now locate the ONNX model file at runtime

3. **Git LFS Model File Handling**
   - **Problem**: Git LFS pointer file (~134 bytes) instead of actual model (~176MB)
   - **Fix**: Added validation to detect LFS pointer files with helpful error messages
   - **Impact**: Clear error messages guide users to download the actual model

4. **Error Handling & User Experience**
   - **Problem**: Generic error messages provided poor user guidance
   - **Fix**: Implemented specific exception handling for different error scenarios
   - **Impact**: Users receive actionable error messages for model, file, and processing issues

5. **UI Responsiveness**
   - **Problem**: UI would freeze during background processing
   - **Fix**: Added async processing with Task.Run for CPU-intensive operations
   - **Impact**: UI remains responsive during background removal processing

## Project Structure

```
src/
├── AI.SmartCut/                 # Main WPF application
│   ├── Assets/
│   │   ├── Models/              # AI model files
│   │   │   ├── u2net.onnx      # U²-Net ONNX model (Git LFS)
│   │   │   └── README.md       # Model documentation
│   │   └── Images/             # Application assets
│   ├── Services/
│   │   └── BackgroundRemover.cs # AI background removal logic
│   ├── MainWindow.xaml         # Main UI layout
│   ├── MainWindow.xaml.cs      # Main UI code-behind
│   ├── App.xaml                # Application configuration
│   └── AI.SmartCut.csproj      # Project file
└── AI.SmartCut.Package/        # UWP packaging (future)
```

## Troubleshooting

### Model Not Found Error
```
Model not found at: [...]\models\u2net.onnx
```
**Solution**: Ensure the U²-Net ONNX model is downloaded (see Installation step 2)

### Git LFS Pointer File Error
```
Model file appears to be a Git LFS pointer file
```
**Solution**: Run `git lfs pull` to download actual model files

### ONNX Runtime Error
```
Failed to load ONNX model
```
**Solutions**:
- Verify model file is not corrupted (should be ~176MB)
- Ensure you have the x64 version of the application
- Check that ONNX Runtime dependencies are installed

### Image Format Not Supported
**Supported formats**: PNG, JPG, JPEG, BMP
**Note**: WebP support requires additional ImageSharp.Webp package

## Development

### Building from Source
```bash
# Debug build
dotnet build AI.SmartCut.sln

# Release build
dotnet build AI.SmartCut.sln --configuration Release

# Self-contained executable
dotnet publish src/AI.SmartCut/AI.SmartCut.csproj -c Release -r win-x64 --self-contained
```

### Key Dependencies
- `SixLabors.ImageSharp` - Image processing and format support
- `Microsoft.ML.OnnxRuntime` - AI model inference
- `System.Numerics.Tensors` - Tensor operations for ML

## Future Enhancements

- [ ] Batch processing for multiple images
- [ ] Manual editing tools for fine-tuning results
- [ ] Additional visual effects and filters
- [ ] WebP format support
- [ ] GPU acceleration for faster processing
- [ ] UWP Store packaging

## License

This project uses the U²-Net model for background removal. Please check the [original U²-Net repository](https://github.com/xuebinqin/U-2-Net) for licensing terms regarding the AI model.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Support

For issues and questions:
1. Check the [Troubleshooting](#troubleshooting) section
2. Review closed issues in the repository
3. Open a new issue with detailed error information and system specs