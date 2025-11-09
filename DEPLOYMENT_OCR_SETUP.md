# Local OCR Deployment Guide

## Overview
The application now uses **Tesseract OCR** (local AI) instead of Azure OCR API. This means **no API keys or external services** are needed, but Tesseract OCR must be installed on the deployment server.

## Deployment Options

### Option 1: Azure App Service (Linux) - Using Startup Script

If you're deploying to Azure App Service on Linux:

1. **Create a startup script** (already created: `startup.sh`)
2. **Configure Azure App Service** to use the startup script:
   - Go to Azure Portal → Your App Service
   - Go to **Configuration** → **General settings**
   - Set **Startup Command** to: `bash startup.sh`
   - Or use: `apt-get update && apt-get install -y tesseract-ocr tesseract-ocr-eng && dotnet Barangay.dll`

3. **Alternative: Use App Service Extension**
   - Go to **Extensions** in Azure Portal
   - Add **Tesseract OCR** extension if available

### Option 2: Docker Deployment

If using Docker (Dockerfile is already updated):

The Dockerfile now automatically installs Tesseract OCR during the build process. Just build and deploy:

```bash
docker build -t bhcare-app .
docker run -p 8080:80 bhcare-app
```

### Option 3: Manual Installation on Server

If deploying to a Linux server manually:

```bash
# Install Tesseract OCR
sudo apt-get update
sudo apt-get install -y tesseract-ocr tesseract-ocr-eng

# Verify installation
tesseract --version

# Deploy your application
dotnet publish -c Release
# Copy published files to server
```

### Option 4: Windows Server

If deploying to Windows Server:

1. **Download and install Tesseract OCR:**
   - Download from: https://github.com/UB-Mannheim/tesseract/wiki
   - Install to: `C:\Program Files\Tesseract-OCR`
   - The application will automatically find it

2. **Or include tessdata folder:**
   - Copy `tessdata` folder to your application directory
   - The application will use it automatically

## Verification

After deployment, check the logs to verify Tesseract is working:

1. **Check application logs** for:
   ```
   Tesseract data path: /usr/share/tesseract-ocr/tessdata
   === LOCAL OCR ANALYSIS START ===
   ```

2. **Test OCR functionality:**
   - Go to Sign Up page
   - Upload an ID image
   - Check if OCR processes successfully

## Troubleshooting

### Error: "Tesseract data path not found"

**Solution:** Tesseract OCR is not installed or tessdata folder is missing.

1. **For Linux:**
   ```bash
   sudo apt-get install -y tesseract-ocr tesseract-ocr-eng
   ```

2. **For Windows:**
   - Install Tesseract from: https://github.com/UB-Mannheim/tesseract/wiki
   - Or copy tessdata folder to application directory

### Error: "Unable to load library 'tesseract'"

**Solution:** The Tesseract native library is not available.

1. **For Linux:** Ensure Tesseract is installed: `sudo apt-get install tesseract-ocr`
2. **For Windows:** Install Tesseract and ensure it's in PATH

### Azure App Service: Startup script not running

**Solution:** Configure the startup command in Azure Portal:

1. Go to **Configuration** → **General settings**
2. Set **Startup Command** to: `bash startup.sh`
3. Or use: `apt-get update && apt-get install -y tesseract-ocr tesseract-ocr-eng && dotnet Barangay.dll`

## Benefits of Local OCR

✅ **No API costs** - Free to use  
✅ **No external dependencies** - Works offline  
✅ **No API keys** - No configuration needed  
✅ **Faster processing** - No network latency  
✅ **Privacy** - Data stays on your server  
✅ **No rate limits** - Process unlimited images  

## Notes

- The application automatically detects Tesseract installation location
- Supports both Windows and Linux deployments
- English language data is included by default
- For other languages, install additional language packs: `tesseract-ocr-[lang]`

