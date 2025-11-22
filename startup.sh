#!/bin/bash
# Enhanced startup script for Azure App Service with Tesseract OCR and native libraries
# This script ensures all required dependencies are installed and properly configured

echo "=== Starting Tesseract OCR and Dependencies Setup ==="

# Set error handling
set -e  # Exit immediately if a command exits with a non-zero status
set -o pipefail  # Ensure pipeline errors are caught

# Update package list and install dependencies
echo "Updating package list and installing dependencies..."
apt-get update

# Install all required packages in a single command for better dependency resolution
DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    tesseract-ocr \
    tesseract-ocr-eng \
    tesseract-ocr-fil \
    libleptonica-dev \
    libtesseract-dev \
    liblept5 \
    libtesseract4 \
    libopencv-dev \
    libopencv-core-dev \
    libopencv-highgui-dev \
    libopencv-imgproc-dev \
    libgdiplus \
    libc6-dev

# Create necessary symlinks for Tesseract .NET
echo "Creating required symlinks..."

# Create symlinks for Leptonica
find /usr/lib -name "liblept.so*" -exec ls -la {} \;
find /usr/lib -name "libleptonica.so*" -exec ls -la {} \;

# Create symlinks for Leptonica (Tesseract.NET looks for specific filenames)
ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /usr/lib/x86_64-linux-gnu/liblept.so
ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /usr/lib/x86_64-linux-gnu/libleptonica-1.82.0.so
ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /usr/lib/libleptonica-1.82.0.so

# Create symlinks for Tesseract
ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.4 /usr/lib/x86_64-linux-gnu/libtesseract.so
ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.4 /usr/lib/libtesseract.so

# Set TESSDATA_PREFIX
export TESSDATA_PREFIX=/usr/share/tesseract-ocr/4.00/tessdata
mkdir -p $TESSDATA_PREFIX

# Verify installations
echo -e "\n=== Verifying Installations ==="

# Verify Tesseract installation
echo -e "\nTesseract version:"
tesseract --version || echo "Tesseract not found!"

# Verify Leptonica is linked correctly
echo -e "\nLeptonica library check:"
ldd $(which tesseract) | grep -i lept || echo "Leptonica not linked to Tesseract!"

# Verify OpenCV
echo -e "\nOpenCV check:"
pkg-config --modversion opencv4 || echo "OpenCV not found or pkg-config not available"

# Set library paths
export LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu:/usr/lib:$LD_LIBRARY_PATH"
echo -e "\nLD_LIBRARY_PATH: $LD_LIBRARY_PATH"

# List installed libraries for debugging
echo -e "\nInstalled libraries:"
ls -la /usr/lib/x86_64-linux-gnu/liblept* /usr/lib/x86_64-linux-gnu/libtesseract* || echo "No libraries found in /usr/lib/x86_64-linux-gnu/"

# Check for required .so files
echo -e "\nChecking for required .so files:"
REQUIRED_LIBS=(
    "/usr/lib/x86_64-linux-gnu/liblept.so.5"
    "/usr/lib/x86_64-linux-gnu/libtesseract.so.4"
    "/usr/lib/x86_64-linux-gnu/libopencv_core.so"
)

for lib in "${REQUIRED_LIBS[@]}"; do
    if [ -f "$lib" ]; then
        echo "✓ Found: $lib"
    else
        echo "✗ Missing: $lib"
    fi
done

# Start the application
echo -e "\n=== Starting Application ==="
dotnet Barangay.dll
