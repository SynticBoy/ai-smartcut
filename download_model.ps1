# PowerShell script to download the U2Net ONNX model for AI SmartCut

param(
    [switch]$Force
)

# Model URLs (multiple sources for redundancy)
$modelUrls = @(
    "https://github.com/xuebinqin/U-2-Net/releases/download/v1.0/u2net.onnx",
    "https://huggingface.co/danielgatis/rembg/resolve/main/u2net.onnx"
)

# Expected file size (approximately 176MB)
$expectedSize = 176MB

# Target directory and filename
$targetDir = "src\AI.SmartCut\Assets\Models"
$filename = "u2net.onnx"
$targetPath = Join-Path $targetDir $filename

# Create target directory if it doesn't exist
if (!(Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Write-Host "Created directory: $targetDir"
}

# Check if file already exists
if (Test-Path $targetPath) {
    Write-Host "Model file already exists at $targetPath"
    
    # Check if it's a Git LFS pointer
    $firstLine = Get-Content $targetPath -First 1
    if ($firstLine -and $firstLine.StartsWith("version https://git-lfs.github.com/spec/v1")) {
        Write-Host "Warning: Existing file is a Git LFS pointer, not the actual model" -ForegroundColor Yellow
        
        if (!$Force) {
            $overwrite = Read-Host "Do you want to overwrite it? (y/N)"
            if ($overwrite -ne "y" -and $overwrite -ne "Y") {
                Write-Host "Download cancelled"
                exit
            }
        }
    } else {
        Write-Host "Model file appears to be valid" -ForegroundColor Green
        exit
    }
}

# Function to download file with progress
function Download-FileWithProgress {
    param(
        [string]$Url,
        [string]$OutFile
    )
    
    try {
        Write-Host "Downloading from $Url" -ForegroundColor Cyan
        Write-Host "This may take several minutes depending on your internet connection..."
        
        $webClient = New-Object System.Net.WebClient
        
        # Register progress event
        $webClient.DownloadProgressChanged = {
            param($sender, $e)
            $percent = [math]::Round($e.ProgressPercentage)
            Write-Progress -Activity "Downloading u2net.onnx" -Status "$percent% Complete" -PercentComplete $percent
        }
        
        # Register completion event
        $webClient.DownloadFileCompleted = {
            param($sender, $e)
            if ($e.Cancelled) {
                Write-Host "Download cancelled" -ForegroundColor Yellow
            } elseif ($e.Error) {
                Write-Host "Download failed: $($e.Error.Message)" -ForegroundColor Red
            } else {
                Write-Host "Download completed successfully!" -ForegroundColor Green
            }
        }
        
        # Start download
        $webClient.DownloadFileAsync($Url, $OutFile)
        
        # Wait for completion
        while ($webClient.IsBusy) {
            Start-Sleep -Milliseconds 100
        }
        
        # Check if file was downloaded successfully
        if (Test-Path $OutFile) {
            $actualSize = (Get-Item $OutFile).Length
            if ($actualSize -gt 100MB) {  # Basic size check
                Write-Host "File size verification passed: $([math]::Round($actualSize / 1MB, 2)) MB" -ForegroundColor Green
                return $true
            } else {
                Write-Host "Warning: Downloaded file seems too small" -ForegroundColor Yellow
                return $false
            }
        }
        
        return $false
    }
    catch {
        Write-Host "Download failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
    finally {
        if ($webClient) {
            $webClient.Dispose()
        }
    }
}

# Try downloading from each URL
for ($i = 0; $i -lt $modelUrls.Count; $i++) {
    $url = $modelUrls[$i]
    Write-Host "`nAttempting download from source $($i + 1)/$($modelUrls.Count)" -ForegroundColor Cyan
    
    if (Download-FileWithProgress -Url $url -OutFile $targetPath) {
        Write-Host "`nSuccessfully downloaded model to $targetPath" -ForegroundColor Green
        Write-Host "You can now run the AI SmartCut application!" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "Failed to download from $url" -ForegroundColor Red
        if ($i -lt $modelUrls.Count - 1) {
            Write-Host "Trying next source..." -ForegroundColor Yellow
        }
    }
}

Write-Host "`nAll download attempts failed." -ForegroundColor Red
Write-Host "Please manually download the u2net.onnx file from:" -ForegroundColor Yellow
Write-Host "https://github.com/xuebinqin/U-2-Net/releases" -ForegroundColor Yellow
Write-Host "And place it in: $targetPath" -ForegroundColor Yellow
exit 1