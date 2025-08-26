# AI SmartCut Models

## Required Model File

This application requires the U2Net ONNX model file:
- **File**: `u2net.onnx`
- **Size**: ~176MB
- **Purpose**: Background removal AI model

## How to Get the Model

The actual model file is stored using Git LFS (Large File Storage). To download it:

1. Install Git LFS: `git lfs install`
2. Pull LFS files: `git lfs pull`

Alternatively, you can download the U2Net ONNX model from:
- [U2Net official repository](https://github.com/xuebinqin/U-2-Net)
- [ONNX Model Zoo](https://github.com/onnx/models)

## Model Information

- **Input**: RGB image (resized to 320x320)
- **Output**: Segmentation mask for background removal
- **Framework**: ONNX Runtime
- **License**: Check original U2Net repository for licensing terms

## Troubleshooting

If you see a "Model not found" error:
1. Ensure the `u2net.onnx` file is in this directory
2. Check that the file is not a Git LFS pointer file (should be ~176MB, not ~134 bytes)
3. Verify file permissions allow read access