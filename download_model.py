#!/usr/bin/env python3
"""
Script to download the U2Net ONNX model for AI SmartCut
"""

import os
import sys
import urllib.request
import hashlib

def download_file(url, filename, expected_size=None):
    """Download a file with progress indication"""
    print(f"Downloading {filename} from {url}")
    print("This may take several minutes depending on your internet connection...")
    
    try:
        urllib.request.urlretrieve(url, filename, reporthook=show_progress)
        print(f"\nDownload completed: {filename}")
        
        # Verify file size if expected_size is provided
        if expected_size:
            actual_size = os.path.getsize(filename)
            if actual_size != expected_size:
                print(f"Warning: Expected size {expected_size}, got {actual_size}")
            else:
                print("File size verification passed")
                
    except Exception as e:
        print(f"Download failed: {e}")
        return False
    
    return True

def show_progress(block_num, block_size, total_size):
    """Show download progress"""
    if total_size > 0:
        percent = min(100, (block_num * block_size * 100) // total_size)
        print(f"\rProgress: {percent}%", end="", flush=True)

def main():
    # Model URLs (multiple sources for redundancy)
    model_urls = [
        "https://github.com/xuebinqin/U-2-Net/releases/download/v1.0/u2net.onnx",
        "https://huggingface.co/danielgatis/rembg/resolve/main/u2net.onnx"
    ]
    
    # Expected file size (approximately 176MB)
    expected_size = 176 * 1024 * 1024  # 176MB in bytes
    
    # Target directory and filename
    target_dir = "src/AI.SmartCut/Assets/Models"
    filename = "u2net.onnx"
    target_path = os.path.join(target_dir, filename)
    
    # Create target directory if it doesn't exist
    os.makedirs(target_dir, exist_ok=True)
    
    # Check if file already exists
    if os.path.exists(target_path):
        print(f"Model file already exists at {target_path}")
        
        # Check if it's a Git LFS pointer
        with open(target_path, 'r') as f:
            first_line = f.readline().strip()
            if first_line.startswith("version https://git-lfs.github.com/spec/v1"):
                print("Warning: Existing file is a Git LFS pointer, not the actual model")
                overwrite = input("Do you want to overwrite it? (y/N): ").lower().strip()
                if overwrite != 'y':
                    print("Download cancelled")
                    return
            else:
                print("Model file appears to be valid")
                return
    
    # Try downloading from each URL
    for i, url in enumerate(model_urls):
        print(f"\nAttempting download from source {i+1}/{len(model_urls)}")
        
        if download_file(url, target_path, expected_size):
            print(f"Successfully downloaded model to {target_path}")
            print("You can now run the AI SmartCut application!")
            return
        else:
            print(f"Failed to download from {url}")
            if i < len(model_urls) - 1:
                print("Trying next source...")
    
    print("\nAll download attempts failed.")
    print("Please manually download the u2net.onnx file from:")
    print("https://github.com/xuebinqin/U-2-Net/releases")
    print(f"And place it in: {target_path}")

if __name__ == "__main__":
    main()