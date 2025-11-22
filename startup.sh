#!/bin/bash
# ===================================================================
# Enhanced Tesseract OCR and Dependencies Setup for Azure App Service
# This script ensures all required dependencies are installed and properly configured
# with comprehensive error handling and logging
# ===================================================================

echo "=== Tesseract OCR and Dependencies Setup (v2) ==="

# ==============================
# 1. Environment Setup
# ==============================
echo "[1/6] Setting up environment..."

# Set error handling
set -e  # Exit on error
set -o pipefail  # Catch pipeline errors
export DEBIAN_FRONTEND=noninteractive

# Create log file
LOG_FILE="/tmp/tesseract_setup.log"
echo "Logging to: $LOG_FILE"
exec > >(tee -a "$LOG_FILE") 2>&1

# Function to log messages
log() {
    echo "[$(date +'%Y-%m-%d %H:%M:%S')] $1"
}

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to install a package with error handling
install_package() {
    log "Installing package: $1"
    if ! apt-get install -y --no-install-recommends "$1"; then
        log "ERROR: Failed to install $1"
        return 1
    fi
}

# ==============================
# 2. System Update
# ==============================
log "[2/6] Updating package lists..."
apt-get update || { log "WARNING: Failed to update package lists"; }

# ==============================
# 3. Install Dependencies
# ==============================
log "[3/6] Installing required packages..."

# Install base dependencies
BASE_PKGS=(
    tesseract-ocr
    tesseract-ocr-eng
    tesseract-ocr-fil
    libleptonica-dev
    libtesseract-dev
    liblept5
    libtesseract4
    libopencv-dev
    libopencv-core-dev
    libopencv-highgui-dev
    libopencv-imgproc-dev
    libgdiplus
    libc6-dev
    pkg-config
    wget
    unzip
)

for pkg in "${BASE_PKGS[@]}"; do
    if ! install_package "$pkg"; then
        log "WARNING: Package $pkg installation failed, but continuing..."
    fi
done

# ==============================
# 4. Manual Library Installation (Fallback)
# ==============================
log "[4/6] Setting up libraries..."

# Create necessary directories
mkdir -p /usr/local/lib

# Function to download and install a library
install_library() {
    local lib_name="$1"
    local lib_url="$2"
    local target_dir="/usr/local/lib"
    
    log "Downloading $lib_name..."
    if wget -q "$lib_url" -O "/tmp/$lib_name"; then
        log "Installing $lib_name..."
        cp "/tmp/$lib_name" "$target_dir/"
        chmod 755 "$target_dir/$lib_name"
        ldconfig
        return 0
    else
        log "WARNING: Failed to download $lib_name"
        return 1
    fi
}

# Install Leptonica from source if not found
if [ ! -f "/usr/lib/x86_64-linux-gnu/liblept.so.5" ]; then
    log "Leptonica not found, installing from source..."
    cd /tmp
    wget http://www.leptonica.org/source/leptonica-1.82.0.tar.gz
    tar -xzf leptonica-1.82.0.tar.gz
    cd leptonica-1.82.0
    ./configure
    make
    make install
    ldconfig
fi

# ==============================
# 5. Create Symlinks
# ==============================
log "[5/6] Creating symlinks..."

# Create symlinks for Leptonica
for lib in /usr/lib/x86_64-linux-gnu/liblept* /usr/local/lib/liblept*; do
    if [ -f "$lib" ]; then
        ln -sf "$lib" /usr/lib/x86_64-linux-gnu/
        ln -sf "$lib" /usr/lib/
    fi
done

# Create specific symlinks that Tesseract.NET looks for
ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /usr/lib/x86_64-linux-gnu/liblept.so || true
ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /usr/lib/x86_64-linux-gnu/libleptonica-1.82.0.so || true
ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /usr/lib/libleptonica-1.82.0.so || true

# Create symlinks for Tesseract
ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.4 /usr/lib/x86_64-linux-gnu/libtesseract.so || true
ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.4 /usr/lib/libtesseract.so || true

# Set TESSDATA_PREFIX
export TESSDATA_PREFIX="/usr/share/tesseract-ocr/4.00/tessdata"
mkdir -p "$TESSDATA_PREFIX"

# ==============================
# 6. Verify Installation
# ==============================
log "[6/6] Verifying installation..."

# Set library paths
export LD_LIBRARY_PATH="/usr/local/lib:/usr/lib/x86_64-linux-gnu:/usr/lib:$LD_LIBRARY_PATH"
ldconfig

# Verify Tesseract
log "Tesseract version:"
tesseract --version || log "WARNING: Tesseract not found!"

# Verify Leptonica
log "\nLeptonica check:"
ldd $(which tesseract) | grep -i lept || log "WARNING: Leptonica not linked to Tesseract!"

# List installed libraries
log "\nInstalled libraries:"
ls -la /usr/lib/x86_64-linux-gnu/liblept* /usr/lib/x86_64-linux-gnu/libtesseract* /usr/local/lib/liblept* 2>/dev/null || true

# Check for required .so files
log "\nChecking for required libraries:"
for lib in \
    "/usr/lib/x86_64-linux-gnu/liblept.so.5" \
    "/usr/lib/x86_64-linux-gnu/libtesseract.so.4" \
    "/usr/lib/x86_64-linux-gnu/libopencv_core.so" \
    "/usr/local/lib/liblept.so.5"; do
    if [ -f "$lib" ]; then
        log "✓ Found: $lib"
    else
        log "✗ Missing: $lib"
    fi
done

# ==============================
# 7. Start Application
# ==============================
log "\n=== Starting Application ==="
log "LD_LIBRARY_PATH: $LD_LIBRARY_PATH"
log "TESSDATA_PREFIX: $TESSDATA_PREFIX"
log "Current directory: $(pwd)"
log "Environment variables:"
printenv | sort

# Start the application
exec dotnet Barangay.dll
