#!/bin/bash
# Startup script for Azure App Service to install Tesseract OCR and native libraries

echo "Installing Tesseract OCR and native dependencies..."

# Update package list
apt-get update

# Install Tesseract OCR and English language data
apt-get install -y tesseract-ocr tesseract-ocr-eng

# Install Leptonica library (required by Tesseract .NET wrapper)
apt-get install -y libleptonica-dev libtesseract-dev

# Install OpenCV libraries (required by OpenCvSharp)
apt-get install -y libopencv-dev libopencv4

# Install additional dependencies that might be needed
apt-get install -y libgdiplus libc6-dev

# Verify installations
echo "Verifying Tesseract installation..."
tesseract --version

echo "Verifying Leptonica installation..."
if [ -f /usr/lib/x86_64-linux-gnu/liblept.so ] || [ -f /usr/lib/liblept.so ]; then
    echo "✓ Leptonica library found"
else
    echo "⚠ Warning: Leptonica library not found in expected locations"
    find /usr -name "liblept*.so" 2>/dev/null | head -5
fi

echo "Verifying OpenCV installation..."
if [ -f /usr/lib/x86_64-linux-gnu/libopencv_core.so ] || [ -f /usr/lib/libopencv_core.so ]; then
    echo "✓ OpenCV library found"
else
    echo "⚠ Warning: OpenCV library not found in expected locations"
    find /usr -name "libopencv*.so" 2>/dev/null | head -5
fi

echo "Installation complete."

# Start the application
dotnet Barangay.dll

