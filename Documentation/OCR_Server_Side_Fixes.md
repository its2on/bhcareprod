# OCR Server-Side Fixes

## Overview
This document provides backend controller improvements for better OCR error handling, validation, and user feedback.

---

## Fix 1: Add Blur Detection Method

Add this new method to `Controllers/IdScannerController.cs`:

```csharp
/// <summary>
/// Calculate image blurriness using Laplacian variance
/// Higher values = sharper image
/// </summary>
private double CalculateImageBlurriness(string imagePath)
{
    try
    {
        using (var src = Cv2.ImRead(imagePath, ImreadModes.Grayscale))
        {
            if (src.Empty())
            {
                _logger.LogWarning("Failed to load image for blur detection");
                return 100; // Assume acceptable
            }
            
            // Use Laplacian variance to detect blur
            using (var laplacian = new Mat())
            {
                Cv2.Laplacian(src, laplacian, MatType.CV_64F);
                Cv2.MeanStdDev(laplacian, out Scalar mean, out Scalar stddev);
                
                // Variance (stddev squared) indicates sharpness
                double variance = stddev.Val0 * stddev.Val0;
                _logger.LogInformation($"Image blur score: {variance:F2}");
                return variance;
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to calculate image blurriness");
        return 100; // Assume acceptable if check fails
    }
}
```

---

## Fix 2: Enhanced Image Validation

Update the `PerformOcrOnImage` method (starting at line 395) to add validation at the beginning:

```csharp
private string PerformOcrOnImage(string imagePath, bool enhancedMode)
{
    try
    {
        // ========== VALIDATION PHASE ==========
        
        // Validate: File exists
        if (!System.IO.File.Exists(imagePath))
        {
            _logger.LogError($"Image file not found: {imagePath}");
            throw new Exception("Image file not found. Please try uploading again.");
        }

        // Validate: Image is readable and has minimum dimensions
        try
        {
            using (var testImage = Image.Load(imagePath))
            {
                _logger.LogInformation($"Image dimensions: {testImage.Width}x{testImage.Height}");
                
                // Check minimum resolution
                if (testImage.Width < 600 || testImage.Height < 400)
                {
                    throw new Exception(
                        $"Image resolution too low ({testImage.Width}x{testImage.Height}). " +
                        "Minimum required: 600x400 pixels. Please use a higher quality image."
                    );
                }
                
                // Check maximum resolution (prevent memory issues)
                if (testImage.Width > 4000 || testImage.Height > 4000)
                {
                    _logger.LogWarning($"Image resolution very high: {testImage.Width}x{testImage.Height}");
                }
            }
        }
        catch (Exception imgEx)
        {
            _logger.LogError(imgEx, "Image validation failed");
            
            if (imgEx.Message.Contains("resolution"))
            {
                throw; // Re-throw resolution errors as-is
            }
            
            throw new Exception($"Invalid or corrupted image file: {imgEx.Message}");
        }
        
        // Validate: Check for blur
        var blurScore = CalculateImageBlurriness(imagePath);
        if (blurScore < 50) // Threshold for blurry images
        {
            throw new Exception(
                "Image appears to be blurry or out of focus. " +
                $"Quality score: {blurScore:F0}/100. " +
                "Please retake the photo with a steady hand in good lighting."
            );
        }
        
        // ========== PROCEED WITH OCR ==========
        
        // ... rest of existing Tesseract OCR code ...
```

---

## Fix 3: Better Empty OCR Result Handling

Update lines 607-621 with improved error messages:

```csharp
if (string.IsNullOrWhiteSpace(combinedText))
{
    _logger.LogWarning("OCR returned empty text from all PSM modes");
    
    // Build a detailed error message
    var errorMessage = new StringBuilder();
    errorMessage.AppendLine("No readable text could be extracted from the image.");
    errorMessage.AppendLine();
    errorMessage.AppendLine("Common causes:");
    errorMessage.AppendLine("• Image does not contain a valid ID document");
    errorMessage.AppendLine("• Lighting is too dark or creates glare");
    errorMessage.AppendLine("• Text is blurry, small, or illegible");
    errorMessage.AppendLine("• Document is partially visible or cut off");
    errorMessage.AppendLine();
    errorMessage.AppendLine("Solutions:");
    errorMessage.AppendLine("• Ensure the entire ID is visible in the frame");
    errorMessage.AppendLine("• Use bright, even lighting without glare");
    errorMessage.AppendLine("• Hold the camera steady to avoid blur");
    errorMessage.AppendLine("• Try enabling 'Enhanced Mode' for better accuracy");
    
    throw new Exception(errorMessage.ToString());
}
```

---

## Fix 4: Add Request Validation to API Endpoint

Update the `ProcessId` method (line 74) to add comprehensive validation:

```csharp
[HttpPost("process")]
public async Task<ActionResult<IdScannerResponse>> ProcessId(IFormFile file, [FromForm] string options)
{
    // === VALIDATION PHASE ===
    
    // Validate: File presence
    if (file == null || file.Length == 0)
    {
        return BadRequest(new IdScannerResponse
        {
            Success = false,
            Message = "No file uploaded",
            ErrorDetails = "Please select an ID image to upload."
        });
    }
    
    // Validate: File size (max 10MB)
    const long maxFileSize = 10 * 1024 * 1024; // 10 MB
    if (file.Length > maxFileSize)
    {
        return BadRequest(new IdScannerResponse
        {
            Success = false,
            Message = "File too large",
            ErrorDetails = $"File size ({file.Length / 1024 / 1024}MB) exceeds maximum allowed (10MB). Please upload a smaller image."
        });
    }
    
    // Validate: File type
    var allowedContentTypes = new[] 
    { 
        "image/jpeg", 
        "image/jpg", 
        "image/png", 
        "image/bmp",
        "image/webp"
    };
    
    if (!allowedContentTypes.Contains(file.ContentType.ToLower()))
    {
        return BadRequest(new IdScannerResponse
        {
            Success = false,
            Message = "Unsupported file type",
            ErrorDetails = $"File type '{file.ContentType}' is not supported. Please upload JPG, PNG, BMP, or WebP images."
        });
    }
    
    // Validate: File extension matches content type
    var extension = Path.GetExtension(file.FileName)?.ToLower();
    var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
    
    if (string.IsNullOrEmpty(extension) || !validExtensions.Contains(extension))
    {
        return BadRequest(new IdScannerResponse
        {
            Success = false,
            Message = "Invalid file extension",
            ErrorDetails = $"File extension '{extension}' is not allowed. Use: .jpg, .jpeg, .png, .bmp, or .webp"
        });
    }
    
    _logger.LogInformation($"Processing ID - File: {file.FileName}, Type: {file.ContentType}, Size: {file.Length} bytes");
    
    // === PROCEED WITH PROCESSING ===
    
    // Parse options
    IdScannerOptions scannerOptions = null;
    try
    {
        scannerOptions = JsonConvert.DeserializeObject<IdScannerOptions>(options ?? "{}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to parse scanner options");
        scannerOptions = new IdScannerOptions();
    }
    
    // ... rest of existing code ...
}
```

---

## Fix 5: Improved Error Response in Main Try-Catch

Update lines 235-245 with detailed error responses:

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing ID");
    
    // Parse error message for user-friendly response
    string userMessage = "Failed to process ID image";
    string errorDetails = ex.Message;
    
    // Categorize errors for better user feedback
    if (ex.Message.Contains("resolution") || ex.Message.Contains("dimension"))
    {
        userMessage = "Image quality issue";
    }
    else if (ex.Message.Contains("blurry") || ex.Message.Contains("blur"))
    {
        userMessage = "Image too blurry";
    }
    else if (ex.Message.Contains("text") && ex.Message.Contains("extract"))
    {
        userMessage = "No text found in image";
    }
    else if (ex.Message.Contains("traineddata") || ex.Message.Contains("Tesseract"))
    {
        userMessage = "OCR service configuration error";
        errorDetails = "The OCR service is not properly configured. Please contact support.";
    }
    else if (ex.Message.Contains("file") || ex.Message.Contains("path"))
    {
        userMessage = "File processing error";
    }
    
    return StatusCode(500, new IdScannerResponse
    {
        Success = false,
        Message = userMessage,
        ErrorDetails = errorDetails,
        ProcessedImageUrl = null
    });
}
```

---

## Fix 6: Add Configuration Validation on Startup

Add this to `Program.cs` (after line 490):

```csharp
// Validate Tesseract configuration at startup
var tessDataPath = Path.Combine(builder.Environment.ContentRootPath, "tessdata");
if (!Directory.Exists(tessDataPath))
{
    Console.WriteLine($"WARNING: Tesseract data directory not found at: {tessDataPath}");
    Console.WriteLine("Creating directory. Please add language files (eng.traineddata, fil.traineddata).");
    Directory.CreateDirectory(tessDataPath);
}
else
{
    var engFile = Path.Combine(tessDataPath, "eng.traineddata");
    var filFile = Path.Combine(tessDataPath, "fil.traineddata");
    
    if (!File.Exists(engFile))
    {
        Console.WriteLine($"WARNING: English language file not found: {engFile}");
    }
    
    if (!File.Exists(filFile))
    {
        Console.WriteLine($"INFO: Filipino language file not found: {filFile}");
        Console.WriteLine("Filipino language support will not be available.");
    }
}
```

---

## Testing the Fixes

### Test Case 1: Invalid Image (No Text)
```bash
curl -X POST http://localhost:5000/api/IdScanner/process \
  -F "file=@decorative-image.png" \
  -F 'options={"enhancedMode":true}'
  
# Expected: 500 with detailed message about no text found
```

### Test Case 2: Blurry Image
```bash
curl -X POST http://localhost:5000/api/IdScanner/process \
  -F "file=@blurry-id.jpg" \
  -F 'options={"enhancedMode":true}'
  
# Expected: 500 with message about image being blurry
```

### Test Case 3: Low Resolution
```bash
curl -X POST http://localhost:5000/api/IdScanner/process \
  -F "file=@small-id.jpg" \
  -F 'options={"enhancedMode":true}'
  
# Expected: 400 Bad Request with resolution message
```

### Test Case 4: Invalid File Type
```bash
curl -X POST http://localhost:5000/api/IdScanner/process \
  -F "file=@document.pdf" \
  -F 'options={"enhancedMode":true}'
  
# Expected: 400 Bad Request with file type message
```

---

## Deployment Checklist

- [ ] Install Tesseract language files to `tessdata/` directory
- [ ] Verify `eng.traineddata` exists
- [ ] Verify `fil.traineddata` exists (optional but recommended)
- [ ] Test with various image qualities
- [ ] Monitor logs for errors
- [ ] Set up alerts for OCR failure rate > 10%

---

## Next Steps

See companion documents:
- `OCR_Client_Side_Fixes.md` - Frontend validation
- `OCR_Azure_Integration.md` - Azure Computer Vision setup
- `OCR_Retry_Logic.md` - Fallback and retry strategies
