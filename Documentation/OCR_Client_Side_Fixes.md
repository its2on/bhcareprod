# OCR Client-Side Validation Fixes

## Overview
Frontend JavaScript validation to prevent uploading invalid images and provide immediate user feedback.

---

## Fix 1: Image Quality Validation Functions

Add these functions to `Pages/Account/SignUp.cshtml` before the scan button handler (around line 2200):

```javascript
/**
 * Validate image quality before uploading to server
 * Returns validation results with errors and warnings
 */
async function validateImageQuality(file) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        const reader = new FileReader();
        
        reader.onload = function(e) {
            img.onload = function() {
                const results = {
                    isValid: true,
                    errors: [],
                    warnings: [],
                    metrics: {}
                };
                
                // Metric 1: Resolution check
                results.metrics.width = img.width;
                results.metrics.height = img.height;
                
                if (img.width < 600 || img.height < 400) {
                    results.isValid = false;
                    results.errors.push(
                        `Resolution too low (${img.width}x${img.height}). Minimum: 600x400 pixels.`
                    );
                }
                
                // Metric 2: Aspect ratio (IDs are typically 1.4:1 to 1.8:1)
                const aspectRatio = img.width / img.height;
                results.metrics.aspectRatio = aspectRatio.toFixed(2);
                
                if (aspectRatio < 1.2 || aspectRatio > 2.0) {
                    results.warnings.push(
                        `Unusual aspect ratio (${aspectRatio.toFixed(2)}:1). Ensure entire ID is visible.`
                    );
                }
                
                // Metric 3: File size check
                results.metrics.fileSizeKB = Math.round(file.size / 1024);
                
                if (file.size < 50000) { // Less than 50KB
                    results.warnings.push(
                        'Image file very small (may indicate low quality).'
                    );
                } else if (file.size > 5 * 1024 * 1024) { // More than 5MB
                    results.warnings.push(
                        'Large file size. Upload may take longer.'
                    );
                }
                
                // Metric 4: Brightness and blur analysis
                const canvas = document.createElement('canvas');
                canvas.width = Math.min(img.width, 800); // Limit processing size
                canvas.height = Math.min(img.height, 600);
                const ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                
                try {
                    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                    
                    // Calculate brightness
                    const brightness = calculateAverageBrightness(imageData);
                    results.metrics.brightness = Math.round(brightness);
                    
                    if (brightness < 40) {
                        results.isValid = false;
                        results.errors.push('Image too dark. Please retake in better lighting.');
                    } else if (brightness < 70) {
                        results.warnings.push('Image somewhat dark. Consider retaking in brighter light.');
                    } else if (brightness > 230) {
                        results.warnings.push('Image may be overexposed. Text might be hard to read.');
                    }
                    
                    // Estimate blur
                    const blurScore = estimateBlur(imageData);
                    results.metrics.blurScore = Math.round(blurScore);
                    
                    if (blurScore < 15) {
                        results.isValid = false;
                        results.errors.push('Image too blurry. Please retake with steady hand.');
                    } else if (blurScore < 25) {
                        results.warnings.push('Image may be slightly blurry. Consider retaking.');
                    }
                } catch (e) {
                    console.warn('Advanced validation failed:', e);
                    // Don't fail validation if advanced checks fail
                }
                
                console.log('Image validation results:', results);
                resolve(results);
            };
            
            img.onerror = function() {
                reject(new Error('Failed to load image. File may be corrupted.'));
            };
            
            img.src = e.target.result;
        };
        
        reader.onerror = function() {
            reject(new Error('Failed to read file.'));
        };
        
        reader.readAsDataURL(file);
    });
}

/**
 * Calculate average brightness of image (0-255)
 */
function calculateAverageBrightness(imageData) {
    const data = imageData.data;
    let sum = 0;
    let count = 0;
    
    // Sample every 4th pixel for performance
    for (let i = 0; i < data.length; i += 16) {
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];
        
        // Calculate luminance using standard formula
        const brightness = 0.299 * r + 0.587 * g + 0.114 * b;
        sum += brightness;
        count++;
    }
    
    return sum / count;
}

/**
 * Estimate blur using edge detection
 * Higher score = sharper image
 */
function estimateBlur(imageData) {
    const data = imageData.data;
    const width = imageData.width;
    const height = imageData.height;
    let edgeStrength = 0;
    let count = 0;
    
    // Sample every 8th pixel for performance
    for (let y = 1; y < height - 1; y += 8) {
        for (let x = 1; x < width - 1; x += 8) {
            const idx = (y * width + x) * 4;
            
            // Get grayscale value
            const center = 0.299 * data[idx] + 0.587 * data[idx + 1] + 0.114 * data[idx + 2];
            
            // Compare with adjacent pixels
            const rightIdx = idx + 4;
            const bottomIdx = ((y + 1) * width + x) * 4;
            
            const right = 0.299 * data[rightIdx] + 0.587 * data[rightIdx + 1] + 0.114 * data[rightIdx + 2];
            const bottom = 0.299 * data[bottomIdx] + 0.587 * data[bottomIdx + 1] + 0.114 * data[bottomIdx + 2];
            
            // Calculate edge strength
            const dx = Math.abs(right - center);
            const dy = Math.abs(bottom - center);
            
            edgeStrength += dx + dy;
            count++;
        }
    }
    
    // Normalize
    return edgeStrength / count;
}

/**
 * Display validation results to user
 */
function showValidationResults(results) {
    const scannerResult = document.getElementById('scannerResult');
    
    if (!results.isValid) {
        let errorHtml = '<div class="alert alert-danger"><strong><i class="fas fa-exclamation-circle"></i> Image Quality Issues:</strong><ul class="mb-0 mt-2">';
        results.errors.forEach(error => {
            errorHtml += `<li>${error}</li>`;
        });
        errorHtml += '</ul></div>';
        
        scannerResult.innerHTML = errorHtml;
        scannerResult.classList.remove('d-none');
        return false;
    }
    
    if (results.warnings.length > 0) {
        let warningHtml = '<div class="alert alert-warning"><strong><i class="fas fa-exclamation-triangle"></i> Image Quality Warnings:</strong><ul class="mb-0 mt-2">';
        results.warnings.forEach(warning => {
            warningHtml += `<li>${warning}</li>`;
        });
        warningHtml += '</ul><small class="d-block mt-2">You can proceed, but results may be less accurate.</small></div>';
        
        scannerResult.innerHTML = warningHtml;
        scannerResult.classList.remove('d-none');
    }
    
    return true;
}
```

---

## Fix 2: Update Scan Button Handler with Validation

Replace the existing scan button handler (around line 2218) with this enhanced version:

```javascript
// Handle scan button click with comprehensive validation
scanIdButton.addEventListener('click', async function() {
    // Step 1: Check file selection
    if (!idScannerInput.files || !idScannerInput.files[0]) {
        idScannerInput.click();
        return;
    }
    
    const file = idScannerInput.files[0];
    const scannerStatus = document.getElementById('scannerStatus');
    const scannerResult = document.getElementById('scannerResult');
    const scanStatusText = document.getElementById('scanStatusText');
    const progressBar = document.getElementById('scanProgressBar');
    
    // Step 2: Basic file type validation
    const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/bmp', 'image/webp'];
    if (!validTypes.includes(file.type.toLowerCase())) {
        scannerResult.innerHTML = `
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-circle me-2"></i> 
                <strong>Invalid file type: ${file.type}</strong>
                <p class="mb-0 mt-2">Please upload a JPG, PNG, BMP, or WebP image.</p>
            </div>`;
        scannerResult.classList.remove('d-none');
        return;
    }
    
    // Step 3: File size validation (max 10MB)
    if (file.size > 10 * 1024 * 1024) {
        scannerResult.innerHTML = `
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-circle me-2"></i> 
                <strong>File too large (${(file.size / 1024 / 1024).toFixed(1)}MB)</strong>
                <p class="mb-0 mt-2">Maximum file size is 10MB. Please use a smaller image.</p>
            </div>`;
        scannerResult.classList.remove('d-none');
        return;
    }
    
    // Show processing status
    scannerStatus.classList.remove('d-none');
    scannerResult.classList.add('d-none');
    scanIdButton.disabled = true;
    
    scanStatusText.textContent = 'Validating image quality...';
    progressBar.style.width = '10%';
    
    try {
        // Step 4: Image quality validation
        const validation = await validateImageQuality(file);
        
        if (!validation.isValid) {
            // Show validation errors
            showValidationResults(validation);
            throw new Error('Image quality validation failed');
        }
        
        // Show warnings if any (but continue)
        if (validation.warnings.length > 0) {
            showValidationResults(validation);
        }
        
        progressBar.style.width = '25%';
        scanStatusText.textContent = 'Preparing image for upload...';
        
        // Step 5: Prepare form data
        const enhancedMode = document.getElementById('enhancedModeCheckbox')?.checked || false;
        const idType = 'NationalID';
        
        const formData = new FormData();
        formData.append('file', file);
        
        const options = {
            enhancedMode: enhancedMode,
            brightness: 0,
            contrast: 0,
            sharpness: 0,
            idType: idType
        };
        
        formData.append('options', JSON.stringify(options));
        
        progressBar.style.width = '40%';
        scanStatusText.textContent = 'Uploading to server...';
        
        console.log('Uploading image:', {
            name: file.name,
            type: file.type,
            size: file.size,
            validation: validation.metrics
        });
        
        // Step 6: Send to server with timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 60000); // 60 second timeout
        
        const response = await fetch('/api/IdScanner/process', {
            method: 'POST',
            body: formData,
            signal: controller.signal
        });
        
        clearTimeout(timeoutId);
        
        progressBar.style.width = '70%';
        scanStatusText.textContent = 'Processing OCR results...';
        
        // Step 7: Handle response
        if (!response.ok) {
            let errorMessage = `Server error (${response.status})`;
            let errorDetails = '';
            
            try {
                const errorData = await response.json();
                errorMessage = errorData.message || errorData.Message || errorMessage;
                errorDetails = errorData.errorDetails || errorData.ErrorDetails || '';
            } catch (e) {
                try {
                    errorMessage = await response.text();
                } catch (e2) {
                    // Use default error
                }
            }
            
            throw new Error(errorDetails ? `${errorMessage}<br><small>${errorDetails}</small>` : errorMessage);
        }
        
        const result = await response.json();
        console.log('OCR API response:', result);
        
        progressBar.style.width = '90%';
        scanStatusText.textContent = 'Filling form fields...';
        
        if (result.success && result.data) {
            // Update processed image preview
            if (result.processedImageUrl) {
                const imagePreview = document.getElementById('imagePreview');
                if (imagePreview) {
                    imagePreview.src = result.processedImageUrl;
                }
            }
            
            // Fill form fields
            fillFormFields(result.data);
            
            // Show success message with confidence
            let confidenceClass = 'success';
            let confidenceIcon = 'check-circle';
            let confidenceText = '';
            
            if (result.confidence < 0.5) {
                confidenceClass = 'danger';
                confidenceIcon = 'exclamation-triangle';
                confidenceText = '<div class="alert alert-warning mt-2 py-2"><i class="fas fa-exclamation-triangle"></i> <strong>Low confidence.</strong> Please verify all fields carefully.</div>';
            } else if (result.confidence < 0.7) {
                confidenceClass = 'warning';
                confidenceIcon = 'info-circle';
                confidenceText = '<div class="small text-warning mt-2"><i class="fas fa-info-circle"></i> Some fields may need correction.</div>';
            }
            
            // List extracted fields
            const extractedFields = [];
            if (result.data.FirstName) extractedFields.push('First Name');
            if (result.data.MiddleName) extractedFields.push('Middle Name');
            if (result.data.LastName) extractedFields.push('Last Name');
            if (result.data.BirthDate) extractedFields.push('Birth Date');
            if (result.data.Address) extractedFields.push('Address');
            if (result.data.Barangay) extractedFields.push('Barangay');
            if (result.data.Gender) extractedFields.push('Gender');
            if (result.data.ContactNumber) extractedFields.push('Contact Number');
            
            const fieldsHtml = extractedFields.length > 0 
                ? `<div class="small mt-2"><strong>Extracted:</strong> ${extractedFields.join(', ')}</div>` 
                : '<div class="small mt-2 text-muted">No fields extracted. Please fill manually.</div>';
            
            scannerResult.innerHTML = `
                <div class="alert alert-${confidenceClass}">
                    <i class="fas fa-${confidenceIcon} me-2"></i> 
                    <strong>ID processed successfully!</strong>
                    ${confidenceText}
                    ${fieldsHtml}
                    <div class="small mt-2 text-muted"><i class="fas fa-check"></i> Please verify all information before submitting.</div>
                </div>`;
        } else {
            throw new Error(result.message || result.Message || 'No data extracted from image');
        }
        
    } catch (error) {
        console.error('Error processing ID:', error);
        
        // Categorize errors for user-friendly messages
        let errorTitle = 'Error Processing ID';
        let errorMessage = 'Please try again or fill the form manually.';
        let errorTips = '';
        
        if (error.name === 'AbortError') {
            errorTitle = 'Request Timeout';
            errorMessage = 'The server took too long to respond.';
            errorTips = '• Try disabling enhanced mode<br>• Use a smaller image file<br>• Check your internet connection';
        } else if (error.message?.includes('network') || error.message?.includes('Failed to fetch')) {
            errorTitle = 'Connection Error';
            errorMessage = 'Could not connect to the server.';
            errorTips = '• Check your internet connection<br>• Try again in a few moments<br>• Contact support if problem persists';
        } else if (error.message?.includes('quality') || error.message?.includes('validation')) {
            errorTitle = 'Image Quality Issue';
            errorMessage = error.message;
            errorTips = '• Ensure good lighting<br>• Hold camera steady<br>• Make sure entire ID is visible';
        } else if (error.message) {
            errorMessage = error.message;
        }
        
        scannerResult.innerHTML = `
            <div class="alert alert-danger">
                <strong><i class="fas fa-exclamation-circle me-2"></i> ${errorTitle}</strong>
                <p class="mt-2 mb-0">${errorMessage}</p>
                ${errorTips ? `<div class="small mt-2 pt-2 border-top">${errorTips}</div>` : ''}
                <div class="mt-3">
                    <button type="button" class="btn btn-sm btn-outline-primary me-2" 
                            onclick="document.getElementById('idScannerInput').click()">
                        <i class="fas fa-redo me-1"></i> Try Different Image
                    </button>
                    <button type="button" class="btn btn-sm btn-outline-secondary" 
                            onclick="document.getElementById('scannerResult').classList.add('d-none')">
                        <i class="fas fa-times me-1"></i> Dismiss
                    </button>
                </div>
            </div>`;
        
    } finally {
        // Reset UI
        progressBar.style.width = '100%';
        setTimeout(() => {
            scannerStatus.classList.add('d-none');
            scannerResult.classList.remove('d-none');
            scanIdButton.disabled = false;
            progressBar.style.width = '0%';
        }, 500);
    }
});
```

---

## Fix 3: Image Preview Enhancement

Add visual feedback for image selection (around line 2186):

```javascript
// Enhanced file selection handler
idScannerInput.addEventListener('change', async function() {
    const scanIdButton = document.getElementById('scanIdButton');
    const scannerResult = document.getElementById('scannerResult');
    
    if (this.files && this.files[0]) {
        const file = this.files[0];
        
        // Update button state
        scanIdButton.disabled = false;
        scanIdButton.innerHTML = '<i class="fas fa-camera me-2"></i> Process Selected Image';
        
        // Show image preview
        const previewContainer = document.getElementById('previewContainer');
        const imagePreview = document.getElementById('imagePreview');
        
        if (previewContainer && imagePreview) {
            const reader = new FileReader();
            reader.onload = function(e) {
                imagePreview.src = e.target.result;
                previewContainer.classList.remove('d-none');
                
                // Show file info
                const fileInfo = `${file.name} (${(file.size / 1024).toFixed(0)}KB)`;
                let infoHtml = `<small class="text-muted d-block mt-2"><i class="fas fa-file-image"></i> ${fileInfo}</small>`;
                
                // Quick validation hint
                if (file.size < 50000) {
                    infoHtml += '<small class="text-warning d-block"><i class="fas fa-exclamation-triangle"></i> File size very small - may be low quality</small>';
                } else if (file.size > 5 * 1024 * 1024) {
                    infoHtml += '<small class="text-info d-block"><i class="fas fa-info-circle"></i> Large file - upload may take time</small>';
                }
                
                scannerResult.innerHTML = infoHtml;
                scannerResult.classList.remove('d-none');
            };
            reader.readAsDataURL(file);
        }
    } else {
        // Reset button
        scanIdButton.disabled = true;
        scanIdButton.innerHTML = '<i class="fas fa-camera me-2"></i> Scan ID';
        
        // Hide preview
        const previewContainer = document.getElementById('previewContainer');
        if (previewContainer) {
            previewContainer.classList.add('d-none');
        }
        
        scannerResult.classList.add('d-none');
    }
});
```

---

## Testing

### Test Validation Functions in Browser Console:

```javascript
// Test blur detection
const canvas = document.createElement('canvas');
const ctx = canvas.getContext('2d');
const img = document.querySelector('#imagePreview');
canvas.width = img.width;
canvas.height = img.height;
ctx.drawImage(img, 0, 0);
const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
console.log('Blur score:', estimateBlur(imageData));
console.log('Brightness:', calculateAverageBrightness(imageData));
```

---

## Summary

These client-side fixes provide:
- ✅ Pre-upload validation (saves API calls)
- ✅ Immediate user feedback
- ✅ Better error messages
- ✅ Image quality metrics
- ✅ Retry functionality

Next: See `OCR_Azure_Integration.md` for Azure Computer Vision setup.
