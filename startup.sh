#!/bin/bash
# Startup script for Azure App Service to install Tesseract OCR and native libraries

echo "Installing Tesseract OCR and native dependencies..."

# Update package list
apt-get update

# Install Tesseract OCR and English language data
apt-get install -y tesseract-ocr tesseract-ocr-eng

# Install Leptonica library (required by Tesseract .NET wrapper)
# Install both dev and runtime packages to ensure all libraries are available
apt-get install -y libleptonica-dev libtesseract-dev libleptonica5 libtesseract4

# Install OpenCV libraries (required by OpenCvSharp)
apt-get install -y libopencv-dev libopencv4

# Install additional dependencies that might be needed
apt-get install -y libgdiplus libc6-dev

# Verify installations
echo "Verifying Tesseract installation..."
tesseract --version

echo "Verifying Leptonica installation..."
# Find the actual Leptonica library (look for .so files, not symlinks first)
LEPTONICA_LIB=$(find /usr/lib -name "liblept.so.*" -o -name "libleptonica.so.*" 2>/dev/null | grep -v ".so$" | head -1)
if [ -z "$LEPTONICA_LIB" ]; then
    LEPTONICA_LIB=$(find /usr/lib -name "liblept.so" -o -name "libleptonica.so" 2>/dev/null | head -1)
fi
if [ -z "$LEPTONICA_LIB" ]; then
    LEPTONICA_LIB=$(find /usr/lib -name "liblept*.so*" -o -name "libleptonica*.so*" 2>/dev/null | head -1)
fi

if [ -n "$LEPTONICA_LIB" ]; then
    echo "✓ Leptonica library found: $LEPTONICA_LIB"
    # Get the real path (resolve symlinks)
    LEPTONICA_REAL=$(readlink -f "$LEPTONICA_LIB" 2>/dev/null || echo "$LEPTONICA_LIB")
    LEPTONICA_DIR=$(dirname "$LEPTONICA_REAL")
    LEPTONICA_FILE=$(basename "$LEPTONICA_REAL")
    
    # Common library directories
    for LIB_DIR in /usr/lib/x86_64-linux-gnu /usr/lib /usr/local/lib; do
        if [ -d "$LIB_DIR" ]; then
            # Create symlink for libleptonica-1.82.0.so (Tesseract.NET looks for this)
            if [ ! -f "$LIB_DIR/libleptonica-1.82.0.so" ] && [ -w "$LIB_DIR" ] 2>/dev/null; then
                echo "Creating symlink: $LIB_DIR/libleptonica-1.82.0.so -> $LEPTONICA_REAL"
                ln -sf "$LEPTONICA_REAL" "$LIB_DIR/libleptonica-1.82.0.so" 2>/dev/null || echo "  (symlink creation skipped - may need root)"
            fi
        fi
    done
else
    echo "⚠ Warning: Leptonica library not found in expected locations"
    find /usr -name "liblept*.so*" -o -name "libleptonica*.so*" 2>/dev/null | head -5
fi

echo "Verifying OpenCV installation..."
if [ -f /usr/lib/x86_64-linux-gnu/libopencv_core.so ] || [ -f /usr/lib/libopencv_core.so ]; then
    echo "✓ OpenCV library found"
else
    echo "⚠ Warning: OpenCV library not found in expected locations"
    find /usr -name "libopencv*.so" 2>/dev/null | head -5
fi

echo "Installation complete."

# Set library paths to help .NET find native libraries
export LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu:/usr/lib:$LD_LIBRARY_PATH"
echo "LD_LIBRARY_PATH set to: $LD_LIBRARY_PATH"

# Start the application
dotnet Barangay.dll

