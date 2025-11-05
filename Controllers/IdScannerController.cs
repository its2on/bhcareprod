using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Linq;
using BHCARE.Extensions;
using OpenCvSharp;

namespace BHCARE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IdScannerController : ControllerBase
    {
        private readonly ILogger<IdScannerController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _clientFactory;

        public IdScannerController(
            ILogger<IdScannerController> logger,
            IWebHostEnvironment environment,
            IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _environment = environment;
            _clientFactory = clientFactory;
        }

        public class IdScannerOptions
        {
            public bool EnhancedMode { get; set; } = false;
            public float Brightness { get; set; } = 0;
            public float Contrast { get; set; } = 0;
            public float Sharpness { get; set; } = 0;
            public string IdType { get; set; } = "NationalID";
        }

        public class IdScannerResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public IdData Data { get; set; }
            public float Confidence { get; set; }
            public string ProcessedImageUrl { get; set; }
            public string ErrorDetails { get; set; }
        }

        public class IdData
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string MiddleName { get; set; }
            public string Suffix { get; set; }
            public string BirthDate { get; set; }
            public string Address { get; set; }
            public string ContactNumber { get; set; }
            public string IdNumber { get; set; }
            public string Gender { get; set; }
            public string Barangay { get; set; }
        }

        [HttpPost("process")]
        public async Task<ActionResult<IdScannerResponse>> ProcessId(IFormFile file, [FromForm] string options)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new IdScannerResponse
                {
                    Success = false,
                    Message = "No file uploaded"
                });
            }

            _logger.LogInformation($"Processing ID image: {file.FileName}, {file.ContentType}, {file.Length} bytes");
            
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

            try
            {
                // Create a unique filename for this session
                string fileId = Guid.NewGuid().ToString("N");
                string fileExtension = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(fileExtension))
                {
                    fileExtension = ".png";
                }
                
                string tempPath = Path.Combine(_environment.WebRootPath, "temp");
                Directory.CreateDirectory(tempPath);
                
                string originalFilePath = Path.Combine(tempPath, $"{fileId}_original{fileExtension}");
                string processedFilePath = Path.Combine(tempPath, $"{fileId}_processed{fileExtension}");
                
                // Save the original file
                using (var stream = new FileStream(originalFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Process the image with OpenCvSharp preprocessing for better OCR, especially address section
                using (var image = await Image.LoadAsync(originalFilePath))
                {
                    // Apply preprocessing
                    if (scannerOptions.EnhancedMode)
                    {
                        // More aggressive preprocessing for enhanced mode
                        image.Mutate(x => x
                            .BrightenSaturation(scannerOptions.Brightness > 0 ? scannerOptions.Brightness : 0.15f)
                            .AdjustContrast(scannerOptions.Contrast > 0 ? scannerOptions.Contrast : 0.2f)
                            .CustomSharpen(scannerOptions.Sharpness > 0 ? scannerOptions.Sharpness : 3f)
                            .Grayscale()
                            .AutoOrient()
                            .Resize(new ResizeOptions
                            {
                                Size = new SixLabors.ImageSharp.Size(1800, 0),
                                Mode = ResizeMode.Max
                            }));
                    }
                    else
                    {
                        // Standard preprocessing
                        image.Mutate(x => x
                            .BrightenSaturation(scannerOptions.Brightness)
                            .AdjustContrast(scannerOptions.Contrast)
                            .CustomSharpen(scannerOptions.Sharpness)
                            .AutoOrient()
                            .Resize(new ResizeOptions
                            {
                                Size = new SixLabors.ImageSharp.Size(1200, 0),
                                Mode = ResizeMode.Max
                            }));
                    }
                    
                    // Save the processed image
                    await image.SaveAsync(processedFilePath);
                }
                
                // Apply OpenCvSharp preprocessing specifically for address section (bottom part of ID)
                PreprocessImageForAddressExtraction(processedFilePath);

                // Send to OCR service
                var ocrResult = await PerformOcr(processedFilePath, scannerOptions.EnhancedMode);
                
                // LOG THE RAW OCR OUTPUT FOR DEBUGGING
                _logger.LogInformation("===========================================");
                _logger.LogInformation("RAW OCR OUTPUT (for debugging):");
                _logger.LogInformation("===========================================");
                _logger.LogInformation(ocrResult ?? "(null or empty)");
                _logger.LogInformation("===========================================");
                
                if (ocrResult == null || string.IsNullOrWhiteSpace(ocrResult))
                {
                    return StatusCode(500, new IdScannerResponse
                    {
                        Success = false,
                        Message = "Failed to extract text from image",
                        ErrorDetails = "OCR process returned no results. The image may be too blurry, too dark, or improperly formatted."
                    });
                }

                // Auto-detect ID type from OCR text if not explicitly set
                string detectedIdType = scannerOptions.IdType;
                if (string.IsNullOrWhiteSpace(detectedIdType) || detectedIdType.ToUpper() == "NATIONALID")
                {
                    // Check if it's a Driver's License
                    if (Regex.IsMatch(ocrResult, @"DRIVER'?S?\s+LICENSE", RegexOptions.IgnoreCase))
                    {
                        detectedIdType = "DriverLicense";
                        _logger.LogInformation("Auto-detected ID type as Driver's License based on OCR text");
                    }
                    // Check if it's a National ID
                    else if (Regex.IsMatch(ocrResult, @"(REPUBLIKA\s+NG\s+PILIPINAS|PHILIPPINE\s+IDENTIFICATION|MGA\s+PANGALAN)", RegexOptions.IgnoreCase))
                    {
                        detectedIdType = "NationalID";
                        _logger.LogInformation("Auto-detected ID type as National ID based on OCR text");
                    }
                }
                
                // Process the OCR result to extract structured data based on ID type
                var idData = ExtractIdData(ocrResult, scannerOptions.EnhancedMode, detectedIdType);
                
                // Generate a URL for the processed image
                string processedImageUrl = $"/temp/{Path.GetFileName(processedFilePath)}";

                // Calculate confidence based on filled fields AND field quality
                float confidence = CalculateEnhancedConfidence(idData, ocrResult);
                
                _logger.LogInformation($"Overall extraction confidence: {confidence:P1}");
                
                // Provide user guidance based on confidence level
                string message = "ID processed successfully";
                if (confidence < 0.5f)
                {
                    message = "ID scanned with low confidence. Please verify all fields carefully.";
                }
                else if (confidence < 0.7f)
                {
                    message = "ID scanned with moderate confidence. Please review the extracted information.";
                }
                else if (confidence >= 0.9f)
                {
                    message = "ID scanned with high confidence!";
                }
                
                return Ok(new IdScannerResponse
                {
                    Success = true,
                    Message = message,
                    Data = idData,
                    Confidence = confidence,
                    ProcessedImageUrl = processedImageUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ID");
                return StatusCode(500, new IdScannerResponse
                {
                    Success = false,
                    Message = GetUserFriendlyErrorMessage(ex),
                    ErrorDetails = ex.ToString()
                });
            }
        }

        private async Task<string> PerformOcr(string imagePath, bool enhancedMode)
        {
            try
            {
                // For this implementation, we'll use a Cloud OCR service
                // You can replace this with Tesseract server-side or other OCR services
                
                // Example using Google Cloud Vision API
                var client = _clientFactory.CreateClient("GoogleVision");
                
                // Read image as base64
                byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
                string base64Image = Convert.ToBase64String(imageBytes);
                
                // Create the request body
                var requestBody = new
                {
                    requests = new[]
                    {
                        new
                        {
                            image = new { content = base64Image },
                            features = new[]
                            {
                                new { type = "TEXT_DETECTION" }
                            },
                            imageContext = new
                            {
                                languageHints = new[] { "en" }
                            }
                        }
                    }
                };
                
                // For demonstration purposes, we'll simulate the response
                // In a real implementation, you would uncomment the HTTP request below
                
                /* 
                var response = await client.PostAsJsonAsync("", requestBody);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<dynamic>();
                var extractedText = result.responses[0].fullTextAnnotation.text;
                return extractedText;
                */
                
                // Perform actual OCR on the image
                return PerformOcrOnImage(imagePath, enhancedMode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR processing error");
                throw new Exception("OCR service error: " + ex.Message, ex);
            }
        }
        
        private void PreprocessImageForAddressExtraction(string imagePath)
        {
            try
            {
                _logger.LogInformation("Applying OpenCvSharp preprocessing for address section extraction");
                
                // Load image using OpenCvSharp
                using (var src = Cv2.ImRead(imagePath, ImreadModes.Color))
                {
                    if (src.Empty())
                    {
                        _logger.LogWarning("Failed to load image with OpenCvSharp, skipping preprocessing");
                        return;
                    }
                    
                    // Convert to grayscale
                    Mat gray = new Mat();
                    Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                    
                    // Get image dimensions
                    int height = gray.Height;
                    int width = gray.Width;
                    
                    // Focus on bottom 40% of image where address typically appears
                    int bottomStartY = (int)(height * 0.6);
                    Rect bottomRegion = new Rect(0, bottomStartY, width, height - bottomStartY);
                    Mat bottomSection = new Mat(gray, bottomRegion);
                    
                    // Apply aggressive preprocessing to bottom section
                    Mat processedBottom = new Mat();
                    
                    // 1. Apply CLAHE (Contrast Limited Adaptive Histogram Equalization) for better contrast
                    using (var clahe = Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8)))
                    {
                        clahe.Apply(bottomSection, processedBottom);
                    }
                    
                    // 2. Apply sharpening using unsharp masking
                    Mat blurred = new Mat();
                    Cv2.GaussianBlur(processedBottom, blurred, new OpenCvSharp.Size(0, 0), 3);
                    Mat sharpened = new Mat();
                    Cv2.AddWeighted(processedBottom, 1.5, blurred, -0.5, 0, sharpened);
                    
                    // 3. Apply adaptive thresholding for better text clarity
                    Mat thresholded = new Mat();
                    Cv2.AdaptiveThreshold(sharpened, thresholded, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
                    
                    // 4. Apply morphological operations to clean up noise
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
                    Mat cleaned = new Mat();
                    Cv2.MorphologyEx(thresholded, cleaned, MorphTypes.Close, kernel);
                    
                    // 5. Copy processed bottom section back to original image
                    Mat processedGray = gray.Clone();
                    using (var roi = new Mat(processedGray, bottomRegion))
                    {
                        cleaned.CopyTo(roi);
                    }
                    
                    // 6. Apply overall denoising to the entire image
                    Mat denoised = new Mat();
                    Cv2.FastNlMeansDenoising(processedGray, denoised, 10, 7, 21);
                    
                    // Convert back to color and save
                    Mat final = new Mat();
                    Cv2.CvtColor(denoised, final, ColorConversionCodes.GRAY2BGR);
                    
                    // Save the preprocessed image
                    Cv2.ImWrite(imagePath, final);
                    
                    _logger.LogInformation("Successfully applied OpenCvSharp preprocessing for address extraction");
                    
                    // Cleanup
                    gray.Dispose();
                    bottomSection.Dispose();
                    processedBottom.Dispose();
                    blurred.Dispose();
                    sharpened.Dispose();
                    thresholded.Dispose();
                    kernel.Dispose();
                    cleaned.Dispose();
                    processedGray.Dispose();
                    denoised.Dispose();
                    final.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenCvSharp preprocessing failed, continuing with original image. Error: {0}", ex.Message);
                // Don't throw - continue with original image if preprocessing fails
            }
        }
        
        private string PerformOcrOnImage(string imagePath, bool enhancedMode)
        {
            try
            {
                // Initialize Tesseract with the trained data files path
                var tessDataPath = Path.Combine(_environment.ContentRootPath, "tessdata");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(tessDataPath))
                {
                    Directory.CreateDirectory(tessDataPath);
                    _logger.LogWarning($"Tesseract data directory created at {tessDataPath}. Please add language files there.");
                }
                
                // Set the TESSDATA_PREFIX environment variable to help Tesseract find the language files
                Environment.SetEnvironmentVariable("TESSDATA_PREFIX", tessDataPath);
                _logger.LogInformation($"Set TESSDATA_PREFIX to {tessDataPath}");
                
                // Verify if eng.traineddata exists
                string engDataFile = Path.Combine(tessDataPath, "eng.traineddata");
                if (!System.IO.File.Exists(engDataFile))
                {
                    _logger.LogWarning($"English language data file not found at {engDataFile}, attempting to download it");
                    
                    try
                    {
                        // Try to download the file
                        using (var client = new System.Net.WebClient())
                        {
                            _logger.LogInformation("Downloading Tesseract language data file from GitHub...");
                            client.DownloadFile(
                                "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata",
                                engDataFile);
                            _logger.LogInformation($"Successfully downloaded language data file to {engDataFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to download language data file");
                        throw new Exception("Tesseract language data file not found and could not be downloaded automatically. Please add eng.traineddata to the tessdata directory manually.");
                    }
                }
                
                if (!System.IO.File.Exists(engDataFile))
                {
                    _logger.LogError("Language file still not found after download attempt");
                    throw new Exception("Tesseract language data file not found. Please make sure eng.traineddata exists in the tessdata directory.");
                }
                
                _logger.LogInformation($"Found language data file at {engDataFile}");
                
                // Verify if fil.traineddata exists for Filipino language support
                string filDataFile = Path.Combine(tessDataPath, "fil.traineddata");
                if (!System.IO.File.Exists(filDataFile))
                {
                    _logger.LogWarning($"Filipino language data file not found at {filDataFile}, attempting to download it");
                    
                    try
                    {
                        // Try to download the Filipino language file
                        using (var client = new System.Net.WebClient())
                        {
                            _logger.LogInformation("Downloading Filipino (fil) language data file from GitHub...");
                            try
                            {
                                // Try the best version first (better accuracy)
                                client.DownloadFile(
                                    "https://github.com/tesseract-ocr/tessdata_best/raw/main/fil.traineddata",
                                    filDataFile);
                                _logger.LogInformation($"Successfully downloaded Filipino language data file (best) to {filDataFile}");
                            }
                            catch
                            {
                                // Fallback to standard version
                                _logger.LogInformation("Best version not available, trying standard version...");
                                client.DownloadFile(
                                    "https://github.com/tesseract-ocr/tessdata/raw/main/fil.traineddata",
                                    filDataFile);
                                _logger.LogInformation($"Successfully downloaded Filipino language data file (standard) to {filDataFile}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to download Filipino language data file. Will use English only. Error: {ex.Message}");
                        // Don't throw - we can still use English only, but address extraction may be less accurate
                    }
                }
                
                if (System.IO.File.Exists(filDataFile))
                {
                    _logger.LogInformation($"Found Filipino language data file at {filDataFile}");
                }
                else
                {
                    _logger.LogWarning("Filipino language data file not available. Address extraction accuracy may be reduced.");
                }

                // Try with absolute path and no directory separator at the end
                string absoluteTessDataPath = Path.GetFullPath(tessDataPath).TrimEnd(Path.DirectorySeparatorChar);
                _logger.LogInformation($"Using absolute tessdata path: {absoluteTessDataPath}");
                
                // Try multiple PSM modes to capture all text, especially addresses at the bottom
                // Mode 3: Fully automatic page segmentation
                // Mode 4: Single column of variable-sized text (good for ID cards)
                // Mode 6: Uniform block of text (best for ID cards)
                // Mode 11: Sparse text (can help with addresses)
                // Mode 12: Sparse text with OSD (can help find all text regions)
                // Mode 13: Raw line - treat image as single text line (helps with bottom address text)
                var psmModes = new[] { "6", "3", "4", "13", "11", "12" };
                var allTexts = new List<string>();
                
                // Determine language string - use eng+fil if available, otherwise just eng
                string language = System.IO.File.Exists(filDataFile) ? "eng+fil" : "eng";
                _logger.LogInformation($"Using Tesseract language: {language}");
                
                foreach (var psmMode in psmModes)
                {
                    try
                    {
                        using (var engine = new Tesseract.TesseractEngine(absoluteTessDataPath, language, enhancedMode ? 
                            Tesseract.EngineMode.Default : Tesseract.EngineMode.LstmOnly))
                        {
                            // Configure engine parameters for ID card recognition
                            // Removed character whitelist to allow all characters (including special chars that might be in addresses)
                            engine.SetVariable("tessedit_pageseg_mode", psmMode);
                            
                            // Set DPI to 300 for better accuracy
                            engine.SetVariable("user_defined_dpi", "300");
                            
                            using (var img = Tesseract.Pix.LoadFromFile(imagePath))
                            {
                                using (var page = engine.Process(img))
                                {
                                    var text = page.GetText();
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        allTexts.Add(text);
                                        var lineCount = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                                        _logger.LogInformation($"OCR PSM mode {psmMode} ({language}) confidence: {page.GetMeanConfidence()}, extracted {text.Length} characters, {lineCount} lines");
                                        _logger.LogDebug($"PSM mode {psmMode} extracted text:\n{text}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"OCR with PSM mode {psmMode} failed: {ex.Message}");
                    }
                }
                
                // Combine all OCR results, merging all unique lines from all PSM modes
                string combinedText = "";
                if (allTexts.Count > 0)
                {
                    // Merge unique lines from all PSM mode results
                    // Use a case-insensitive comparison but preserve original casing from longest match
                    var lineDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    
                    // Process texts in order of length (longest first) to preserve best quality text
                    foreach (var text in allTexts.OrderByDescending(t => t.Length))
                    {
                        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .Where(l => !string.IsNullOrWhiteSpace(l));
                        
                        foreach (var line in lines)
                        {
                            // Use case-insensitive key to avoid duplicates, but keep the version with better casing
                            var key = line.ToUpperInvariant();
                            if (!lineDict.ContainsKey(key) || line.Length > lineDict[key].Length)
                            {
                                lineDict[key] = line;
                            }
                        }
                    }
                    
                    // Combine all unique lines in order they appear in the longest text first, then append others
                    var longestText = allTexts.OrderByDescending(t => t.Length).First();
                    var longestLines = longestText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim().ToUpperInvariant())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                    
                    var orderedLines = new List<string>();
                    var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    
                    // First, add lines from longest text in order
                    foreach (var key in longestLines)
                    {
                        if (lineDict.ContainsKey(key) && !processedKeys.Contains(key))
                        {
                            orderedLines.Add(lineDict[key]);
                            processedKeys.Add(key);
                        }
                    }
                    
                    // Then add any remaining unique lines
                    foreach (var kvp in lineDict)
                    {
                        if (!processedKeys.Contains(kvp.Key))
                        {
                            orderedLines.Add(kvp.Value);
                            processedKeys.Add(kvp.Key);
                        }
                    }
                    
                    combinedText = string.Join("\n", orderedLines);
                    _logger.LogInformation($"Combined OCR text from {allTexts.Count} PSM modes: {combinedText.Length} characters, {orderedLines.Count} unique lines");
                }
                
                if (string.IsNullOrWhiteSpace(combinedText))
                {
                    _logger.LogWarning("OCR returned empty text from all PSM modes");
                    throw new Exception("OCR could not extract any text from the image");
                }
                
                _logger.LogInformation($"Final OCR extracted text:\n{combinedText}");
                return combinedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tesseract OCR failed");
                throw new Exception($"OCR processing failed: {ex.Message}", ex);
            }
        }

        private IdData ExtractIdData(string ocrText, bool enhancedMode, string idType = "NationalID")
        {
            var data = new IdData();
            
            try
            {
                _logger.LogInformation($"Extracting data from OCR text for ID type: {idType}");
                
                // Clean up the OCR text
                var cleanedText = ocrText.Replace("\r\n", "\n").Replace("\n+", "\n");
                
                _logger.LogInformation($"Cleaned OCR text for processing:\n{cleanedText}");
                
                // Route to specific extraction method based on ID type
                switch (idType.ToUpper())
                {
                    case "NATIONALID":
                        return ExtractNationalIdData(cleanedText, enhancedMode);
                    case "DRIVERLICENSE":
                        return ExtractDriverLicenseData(cleanedText, enhancedMode);
                    case "POSTALID":
                        return ExtractPostalIdData(cleanedText, enhancedMode);
                    case "PHILHEALTH":
                        return ExtractPhilHealthData(cleanedText, enhancedMode);
                    case "TIN":
                        return ExtractTinIdData(cleanedText, enhancedMode);
                    case "SSS":
                        return ExtractSssIdData(cleanedText, enhancedMode);
                    case "UMID":
                        return ExtractUmidIdData(cleanedText, enhancedMode);
                    case "PASSPORT":
                        return ExtractPassportData(cleanedText, enhancedMode);
                    default:
                        // Generic extraction for unknown types
                        _logger.LogWarning($"Unknown ID type: {idType}, using generic extraction");
                        break;
                }
                
                // Pattern 1: Look for labels in Tagalog/English format from Philippine National ID
                // Lastname/Apelyido
                var lastNameMatch = Regex.Match(cleanedText, @"(?:APELYIDO|LASTNAME|APELIYDO)[:\s]*([A-Z][A-Z\s]+)", RegexOptions.IgnoreCase);
                if (lastNameMatch.Success)
                {
                    data.LastName = CleanupExtractedText(lastNameMatch.Groups[1].Value);
                    _logger.LogInformation($"Extracted LastName: {data.LastName}");
                }
                
                // Given Name/Mga Pangalan (First + Middle names together)
                var givenNameMatch = Regex.Match(cleanedText, @"(?:MGA\s+PANGALAN|GIVEN\s+NAME(?:S)?)[:\s]*([A-Z][A-Z\s]+(?:[A-Z][A-Z\s]+)*)", RegexOptions.IgnoreCase);
                if (givenNameMatch.Success)
                {
                    var fullGivenName = givenNameMatch.Groups[1].Value.Trim();
                    var nameParts = fullGivenName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (nameParts.Length >= 2)
                    {
                        data.FirstName = CleanupExtractedText(nameParts[0]);
                        data.MiddleName = CleanupExtractedText(string.Join(" ", nameParts.Skip(1)));
                        _logger.LogInformation($"Extracted GivenName: First={data.FirstName}, Middle={data.MiddleName}");
                    }
                    else if (nameParts.Length == 1)
                    {
                        data.FirstName = CleanupExtractedText(nameParts[0]);
                    }
                }
                
                // Gitnang Apelyido (Middle Name in Tagalog)
                var middleNameMatchTagalog = Regex.Match(cleanedText, @"(?:GITNANG\s+APELYIDO|MIDDLE\s+SURNAME)[:\s]*([A-Z][A-Z\s]+)", RegexOptions.IgnoreCase);
                if (middleNameMatchTagalog.Success && string.IsNullOrWhiteSpace(data.MiddleName))
                {
                    data.MiddleName = CleanupExtractedText(middleNameMatchTagalog.Groups[1].Value);
                    _logger.LogInformation($"Extracted MiddleName (Gitnang Apelyido): {data.MiddleName}");
                }
                
                // Also try standard English labels
                var standardLastNameMatch = Regex.Match(cleanedText, @"LAST\s+NAME:?\s*([A-Z][A-Za-z\s\-\.]+)", RegexOptions.IgnoreCase);
                if (standardLastNameMatch.Success && string.IsNullOrWhiteSpace(data.LastName))
                {
                    data.LastName = CleanupExtractedText(standardLastNameMatch.Groups[1].Value);
                }
                
                var standardFirstNameMatch = Regex.Match(cleanedText, @"FIRST\s+NAME:?\s*([A-Z][A-Za-z\s\-\.]+)", RegexOptions.IgnoreCase);
                if (standardFirstNameMatch.Success && string.IsNullOrWhiteSpace(data.FirstName))
                {
                    data.FirstName = CleanupExtractedText(standardFirstNameMatch.Groups[1].Value);
                }
                
                var middleNameMatch = Regex.Match(cleanedText, @"MIDDLE\s+NAME:?\s*([A-Z][A-Za-z\s\-\.]+)", RegexOptions.IgnoreCase);
                if (middleNameMatch.Success && string.IsNullOrWhiteSpace(data.MiddleName))
                {
                    data.MiddleName = CleanupExtractedText(middleNameMatch.Groups[1].Value);
                }
                
                // Pattern 2: If no explicit labels found, try to extract name from common formats
                if (string.IsNullOrWhiteSpace(data.FirstName) && string.IsNullOrWhiteSpace(data.LastName))
                {
                    // Try "SURNAME, GIVEN NAME MIDDLE NAME" format
                    var surnameGivenMatch = Regex.Match(cleanedText, @"SURNAME:?\s*([A-Za-z\s]+)[,\s]+GIVEN\s+NAMES?:?\s*([A-Za-z\s]+)", RegexOptions.IgnoreCase);
                    if (surnameGivenMatch.Success)
                    {
                        data.LastName = CleanupExtractedText(surnameGivenMatch.Groups[1].Value);
                        var givenNameParts = surnameGivenMatch.Groups[2].Value.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (givenNameParts.Length > 0)
                        {
                            data.FirstName = CleanupExtractedText(givenNameParts[0]);
                            if (givenNameParts.Length > 1)
                            {
                                data.MiddleName = CleanupExtractedText(string.Join(" ", givenNameParts.Skip(1)));
                            }
                        }
                    }
                    
                    // Try patterns specific to Philippine National ID format
                    if (string.IsNullOrWhiteSpace(data.FirstName) && string.IsNullOrWhiteSpace(data.LastName))
                    {
                        // Pattern for: Line with REPUBLIC OF THE PHILIPPINES followed by name
                        // Looking for all-caps sequences that are likely names
                        var lines = cleanedText.Split('\n');
                        
                        for (int i = 0; i < lines.Length; i++)
                        {
                            var line = lines[i].Trim();
                            
                            // Look for lines after REPUBLIC or REPUBLIKA
                            if (Regex.IsMatch(line, @"REPUBL(?:IKA|IC)", RegexOptions.IgnoreCase) && i + 1 < lines.Length)
                            {
                                var nextLine = lines[i + 1].Trim();
                                // Check if next line contains all caps (likely the name)
                                if (Regex.IsMatch(nextLine, @"^[A-Z][A-Z\s]+$"))
                                {
                                    var nameParts = nextLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (nameParts.Length >= 2)
                                    {
                                        // Last name is typically first in Philippine format
                                        data.LastName = CleanupExtractedText(nameParts[0]);
                                        
                                        // Remaining names
                                        if (nameParts.Length >= 4)
                                        {
                                            // Likely: Last, First, Middle, Suffix
                                            data.FirstName = CleanupExtractedText(nameParts[1]);
                                            data.MiddleName = CleanupExtractedText(nameParts[2]);
                                            data.Suffix = CleanupExtractedText(nameParts[3]);
                                        }
                                        else if (nameParts.Length == 3)
                                        {
                                            // Likely: Last, First, Middle
                                            data.FirstName = CleanupExtractedText(nameParts[1]);
                                            data.MiddleName = CleanupExtractedText(nameParts[2]);
                                        }
                                        else if (nameParts.Length == 2)
                                        {
                                            // Last, First
                                            data.FirstName = CleanupExtractedText(nameParts[1]);
                                        }
                                        break;
                                    }
                                }
                            }
                            
                            // Try to find a line with multiple all-caps words that looks like a name
                            if (Regex.IsMatch(line, @"^[A-Z][A-Z\s]{2,}$") && line.Split(' ').Length >= 2 && line.Split(' ').Length <= 5)
                            {
                                var nameParts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                // Skip if it's a date or ID number
                                if (!Regex.IsMatch(line, @"\d") && nameParts.All(p => p.Length > 1))
                                {
                                    // Philippine format: Last, First, Middle, LastPart
                                    data.LastName = CleanupExtractedText(nameParts[0]);
                                    
                                    if (nameParts.Length >= 4)
                                    {
                                        // Format: Last First Middle Suffix
                                        data.FirstName = CleanupExtractedText(nameParts[1]);
                                        data.MiddleName = CleanupExtractedText(nameParts[2]);
                                        data.Suffix = CleanupExtractedText(nameParts[3]);
                                    }
                                    else if (nameParts.Length == 3)
                                    {
                                        // Format: Last First Middle
                                        data.FirstName = CleanupExtractedText(nameParts[1]);
                                        data.MiddleName = CleanupExtractedText(nameParts[2]);
                                    }
                                    else if (nameParts.Length == 2)
                                    {
                                        // Format: Last First
                                        data.FirstName = CleanupExtractedText(nameParts[1]);
                                    }
                                    break;
                                }
                            }
                            
                            // Another pattern: Look for lines with mixed case (Title Case) that might be names
                            // Skip common words and look for name-like patterns
                            if (Regex.IsMatch(line, @"^[A-Z][a-z]+\s+[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*$"))
                            {
                                var nameParts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                // Check if it's not a common word like "Republic" or "Philippines"
                                if (!Regex.IsMatch(line, @"\b(REPUBLIC|REPUBLIKA|PHILIPPINES|PILIPINAS|DRIVER|LICENSE|CARD)\b", RegexOptions.IgnoreCase) && 
                                    nameParts.All(p => p.Length > 1))
                                {
                                    if (string.IsNullOrWhiteSpace(data.LastName))
                                    {
                                        data.LastName = CleanupExtractedText(nameParts[0]);
                                        
                                        if (nameParts.Length >= 4)
                                        {
                                            data.FirstName = CleanupExtractedText(nameParts[1]);
                                            data.MiddleName = CleanupExtractedText(nameParts[2]);
                                            data.Suffix = CleanupExtractedText(nameParts[3]);
                                        }
                                        else if (nameParts.Length == 3)
                                        {
                                            data.FirstName = CleanupExtractedText(nameParts[1]);
                                            data.MiddleName = CleanupExtractedText(nameParts[2]);
                                        }
                                        else if (nameParts.Length == 2)
                                        {
                                            data.FirstName = CleanupExtractedText(nameParts[1]);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Extract birth date - multiple formats including Tagalog label
                var birthDatePatterns = new[]
                {
                    @"(?:PETSA\s+NG\s+KAPANGANAKAN|DATE\s+OF\s+BIRTH|BIRTH\s+DATE|DOB|BORN)[:\s]*([A-Za-z]+\s+\d{1,2},?\s+\d{4})",
                    @"(?:PETSA\s+NG\s+KAPANGANAKAN|DATE\s+OF\s+BIRTH|BIRTH\s+DATE|DOB|BORN)[:\s]*(\d{1,2}[/\-]\d{1,2}[/\-]\d{2,4})",
                    @"\b(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER)\s+(\d{1,2}),?\s+(\d{4})\b"
                };
                
                foreach (var pattern in birthDatePatterns)
                {
                    var match = Regex.Match(cleanedText, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        try
                        {
                            string dateStr = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                            
                            // Handle month name format
                            if (match.Groups.Count == 4 && match.Groups[1].Success && match.Groups[2].Success && match.Groups[3].Success)
                            {
                                var month = match.Groups[1].Value;
                                var day = match.Groups[2].Value.PadLeft(2, '0');
                                var year = match.Groups[3].Value;
                                data.BirthDate = $"{year}-{GetMonthNumber(month)}-{day}";
                            }
                            else if (dateStr.Contains("/") || dateStr.Contains("-"))
                            {
                                // Handle MM/DD/YYYY or DD/MM/YYYY format
                                var parts = dateStr.Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length == 3)
                                {
                                    var month = parts[0].PadLeft(2, '0');
                                    var day = parts[1].PadLeft(2, '0');
                                    var year = parts[2];
                                    if (year.Length == 2)
                                    {
                                        year = int.Parse(year) > 50 ? "19" + year : "20" + year;
                                    }
                                    data.BirthDate = $"{year}-{month}-{day}";
                                }
                            }
                            else
                            {
                                // Try to parse as natural date
                                var date = DateTime.Parse(dateStr);
                                data.BirthDate = date.ToString("yyyy-MM-dd");
                            }
                            break;
                        }
                        catch
                        {
                            // Continue to next pattern
                        }
                    }
                }
                
                // Extract address
                var addressPatterns = new[]
                {
                    @"ADDRESS:?\s*(.+?)(?:\n\n|\r\n\r\n|\n[A-Z]+\s*:|$)",
                    @"RESIDENCE:?\s*(.+?)(?:\n\n|\r\n\r\n|\n[A-Z]+\s*:|$)"
                };
                
                foreach (var pattern in addressPatterns)
                {
                    var match = Regex.Match(cleanedText, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        data.Address = CleanupExtractedText(match.Groups[1].Value);
                        break;
                    }
                }
                
                // Extract contact number
                var contactPatterns = new[]
                {
                    @"(?:MOBILE|PHONE|CONTACT|TEL|CELL)[\s#:\.\-]+([0-9\+\-\(\)\s]{7,})",
                    @"\b((?:09|\+?639)\d{9})\b"
                };
                
                foreach (var pattern in contactPatterns)
                {
                    var match = Regex.Match(cleanedText, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        data.ContactNumber = Regex.Replace(match.Groups[1].Value, @"[^\d\+]", "");
                        break;
                    }
                }
                
                // Extract ID number
                var idPatterns = new[]
                {
                    @"(?:LICENSE\s+(?:NO|NUMBER)|ID\s+(?:NO|NUMBER)):?\s*([A-Z0-9\-]+)",
                    @"\b([0-9]{4}[\-\s]?[0-9]{4}[\-\s]?[0-9]{4}[\-\s]?[0-9]{4})\b"
                };
                
                foreach (var pattern in idPatterns)
                {
                    var match = Regex.Match(cleanedText, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        data.IdNumber = match.Groups[1].Value;
                        break;
                    }
                }
                
                // Extract gender
                var genderMatch = Regex.Match(cleanedText, @"(?:SEX|GENDER):?\s*([MF]|MALE|FEMALE)", RegexOptions.IgnoreCase);
                if (genderMatch.Success)
                {
                    var genderValue = genderMatch.Groups[1].Value.Trim().ToUpper();
                    if (genderValue == "M" || genderValue == "MALE")
                    {
                        data.Gender = "Male";
                    }
                    else if (genderValue == "F" || genderValue == "FEMALE")
                    {
                        data.Gender = "Female";
                    }
                }
                
                _logger.LogInformation($"Extracted data - FirstName: {data.FirstName}, LastName: {data.LastName}, BirthDate: {data.BirthDate}, ContactNumber: {data.ContactNumber}");
                
                // Apply additional processing for enhanced mode
                if (enhancedMode)
                {
                    ApplyFuzzyCorrections(data, cleanedText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting data from OCR text");
                // Continue with partial data
            }
            
            return data;
        }
        
        private string GetMonthNumber(string monthName)
        {
            var months = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"JANUARY", "01"}, {"JAN", "01"},
                {"FEBRUARY", "02"}, {"FEB", "02"},
                {"MARCH", "03"}, {"MAR", "03"},
                {"APRIL", "04"}, {"APR", "04"},
                {"MAY", "05"},
                {"JUNE", "06"}, {"JUN", "06"},
                {"JULY", "07"}, {"JUL", "07"},
                {"AUGUST", "08"}, {"AUG", "08"},
                {"SEPTEMBER", "09"}, {"SEP", "09"},
                {"OCTOBER", "10"}, {"OCT", "10"},
                {"NOVEMBER", "11"}, {"NOV", "11"},
                {"DECEMBER", "12"}, {"DEC", "12"}
            };
            
            if (months.TryGetValue(monthName.ToUpper(), out var monthNum))
            {
                return monthNum;
            }
            return "01";
        }
        
        // Philippine National ID specific extraction - ENHANCED VERSION
        private IdData ExtractNationalIdData(string cleanedText, bool enhancedMode)
        {
            var data = new IdData();
            
            _logger.LogInformation("=== ENHANCED EXTRACTION: Philippine National ID ===");
            _logger.LogInformation($"Raw OCR Text Length: {cleanedText.Length} characters");
            
            // Apply OCR error corrections FIRST
            var correctedText = CorrectOcrErrors(cleanedText);
            _logger.LogInformation("Applied OCR error corrections");
            
            // Split text into lines for better parsing
            var lines = correctedText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();
            
            _logger.LogInformation($"Processing {lines.Length} text lines");
            
            // === LAST NAME EXTRACTION (with fuzzy matching) ===
            var lastNameLabels = new[] { "APELYIDO", "APELIYDO", "APELLIDO", "LASTNAME", "LAST NAME", "SURNAME" };
            var lastNameText = ExtractTextAfterLabel(correctedText, lastNameLabels, maxDistance: 2, maxWordsToCapture: 2);
            if (!string.IsNullOrWhiteSpace(lastNameText))
            {
                data.LastName = CleanupExtractedText(lastNameText);
                _logger.LogInformation($"✓ Extracted LastName from label (fuzzy): {data.LastName}");
            }
            
            // Find "Mga Pangalan" or "Mga Pangalar" (common OCR misspelling) label
            int mgaPangalanIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], @"MGA\s+PANGAL(AN|AR)", RegexOptions.IgnoreCase))
                {
                    mgaPangalanIndex = i;
                    _logger.LogInformation($"Found 'Mga Pangalan' label at line {i}: {lines[i]}");
                    break;
                }
            }
            
            // FIRST: Look for last name BEFORE "Mga Pangalan" (common PhilID format)
            // In Philippine National ID, the last name (Apelyido) typically appears before "Mga Pangalan"
            if (mgaPangalanIndex > 0)
            {
                // Check lines before "Mga Pangalan" for potential last name
                for (int i = mgaPangalanIndex - 1; i >= 0 && i >= mgaPangalanIndex - 3; i--)
                {
                    var line = lines[i].Trim();
                    // Skip if it's the ID number or other non-name content
                    if (Regex.IsMatch(line, @"\d{4}[\-\s]?\d{4}[\-\s]?\d{4}[\-\s]?\d{4}") ||
                        Regex.IsMatch(line, @"(REPUBLIKA|REPUBLIC|PILIPINAS|PHILIPPINES|PAMBANSANG|PAGKAKAKILANLAN)", RegexOptions.IgnoreCase))
                        continue;
                    
                    // If it's an all-cap word or short phrase, might be last name
                    if (Regex.IsMatch(line, @"^[A-Z][A-Z\s]+$") && line.Split(' ').Length <= 2 && !Regex.IsMatch(line, @"\d"))
                    {
                        var words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length > 0 && words.All(w => w.Length > 1 && w.All(c => char.IsLetter(c))))
                        {
                            data.LastName = CleanupExtractedText(line);
                            _logger.LogInformation($"Extracted LastName before 'Mga Pangalan': {data.LastName}");
                            break;
                        }
                    }
                }
            }
            
            // === GIVEN NAMES EXTRACTION (First Name ONLY from Mga Pangalan) ===
            // IMPORTANT: "Mga Pangalan" contains ONLY given/first names, NOT middle name
            // Middle name is in a separate "Gitnang Apelyido" field
            var givenNameLabels = new[] { "MGA PANGALAN", "MGAPANGALAN", "MGA PANGALAR", "GIVEN NAME", "GIVENNAME", "GIVEN NAMES" };
            var givenNameText = ExtractTextAfterLabel(correctedText, givenNameLabels, maxDistance: 2, maxWordsToCapture: 5);
            
            if (!string.IsNullOrWhiteSpace(givenNameText))
            {
                var nameParts = givenNameText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(p => p.Length > 1 && Regex.IsMatch(p, @"^[A-Z]+$"))
                    .Take(5)
                    .ToArray();
                
                if (nameParts.Length > 0)
                {
                    // Keep ALL words from "Mga Pangalan" as First Name
                    // Examples: "RHYLLE LANDER" -> First Name: "RHYLLE LANDER"
                    //           "JUAN PEDRO CARLOS" -> First Name: "JUAN PEDRO CARLOS"
                    data.FirstName = CleanupExtractedText(string.Join(" ", nameParts));
                    _logger.LogInformation($"✓ Extracted FirstName from 'Mga Pangalan' (fuzzy): {data.FirstName}");
                }
            }
            
            // === MIDDLE NAME EXTRACTION (from separate "Gitnang Apelyido" field) ===
            // Philippine IDs have a SEPARATE middle name field after given names
            var middleNameLabels = new[] { "GITNANG APELYIDO", "GITNANG APELIYDO", "G1TNANG APELYIDO", "MIDDLE NAME", "MIDDLENAME", "MIDDLE SURNAME" };
            var middleNameText = ExtractTextAfterLabel(correctedText, middleNameLabels, maxDistance: 2, maxWordsToCapture: 2);
            
            if (!string.IsNullOrWhiteSpace(middleNameText))
            {
                data.MiddleName = CleanupExtractedText(middleNameText);
                _logger.LogInformation($"✓ Extracted MiddleName from 'Gitnang Apelyido' (fuzzy): {data.MiddleName}");
            }
            
            // Fallback: SECOND method - Extract given names from lines AFTER "Mga Pangalan" label
            // Only if First Name is still missing
            if (string.IsNullOrWhiteSpace(data.FirstName) && mgaPangalanIndex >= 0 && mgaPangalanIndex + 1 < lines.Length)
            {
                var givenNameParts = new List<string>();
                
                // Collect all-cap words from lines after "Mga Pangalan" until we hit "Gitnang Apelyido" or other field
                for (int i = mgaPangalanIndex + 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    
                    // Stop if we hit middle name label, date, ID number, or other field indicators
                    if (Regex.IsMatch(line, @"GITNANG\s+APELYIDO|MIDDLE\s+NAME", RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(line, @"\d{4}[\-\s]?\d{4}[\-\s]?\d{4}[\-\s]?\d{4}") || // ID number pattern
                        Regex.IsMatch(line, @"(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER)") || // Date
                        Regex.IsMatch(line, @"(ADDRESS|RESIDENCE|BARANGAY|CITY|MUNICIPALITY|PROVINCE|TIRAHAN)", RegexOptions.IgnoreCase)) // Address field
                    {
                        break;
                    }
                    
                    // Check if line contains all-cap words (likely name parts)
                    if (Regex.IsMatch(line, @"^[A-Z][A-Z\s]+$") && !Regex.IsMatch(line, @"\d"))
                    {
                        var words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(w => w.Length > 1 && w.All(c => char.IsLetter(c)))
                            .ToArray();
                        givenNameParts.AddRange(words);
                    }
                    // Also check for single all-cap words that might be names
                    else if (Regex.IsMatch(line, @"^[A-Z]{2,}$") && line.All(c => char.IsLetter(c)))
                    {
                        givenNameParts.Add(line);
                    }
                    
                    // Limit to reasonable number of name parts
                    if (givenNameParts.Count >= 5)
                        break;
                }
                
                if (givenNameParts.Count > 0)
                {
                    // Keep ALL collected words as First Name (don't split off as middle name)
                    data.FirstName = CleanupExtractedText(string.Join(" ", givenNameParts));
                    _logger.LogInformation($"✓ Extracted FirstName from 'Mga Pangalan' section (fallback): {data.FirstName}");
                }
            }
            
            // Fallback: Look for middle name from lines after "Gitnang Apelyido" label
            if (string.IsNullOrWhiteSpace(data.MiddleName))
            {
                // Try to find "Gitnang Apelyido" label in lines
                for (int i = 0; i < lines.Length; i++)
                {
                    if (Regex.IsMatch(lines[i], @"GITNANG\s+APELYIDO|G1TNANG\s+APELYIDO|MIDDLE\s+NAME", RegexOptions.IgnoreCase))
                    {
                        _logger.LogInformation($"Found 'Gitnang Apelyido' label at line {i}: {lines[i]}");
                        
                        // Look at next line for the actual middle name
                        if (i + 1 < lines.Length)
                        {
                            var nextLine = lines[i + 1].Trim();
                            if (Regex.IsMatch(nextLine, @"^[A-Z][A-Z\s]+$") && !Regex.IsMatch(nextLine, @"\d") &&
                                !Regex.IsMatch(nextLine, @"(PETSA|DATE|BIRTH|KAPANGANAKAN|ADDRESS|TIRAHAN)", RegexOptions.IgnoreCase))
                            {
                                data.MiddleName = CleanupExtractedText(nextLine);
                                _logger.LogInformation($"✓ Extracted MiddleName from 'Gitnang Apelyido' section (fallback): {data.MiddleName}");
                                break;
                            }
                        }
                    }
                }
            }
            
            // === DATE OF BIRTH EXTRACTION with fuzzy matching ===
            var dobLabels = new[] { "BIRTH DATE", "BIRTHDATE", "PETSA NG KAPANGANAKAN", "KAPANGANAKAN", "DATE OF BIRTH", "DOB" };
            var dobText = ExtractTextAfterLabel(correctedText, dobLabels, maxDistance: 3, maxWordsToCapture: 5);
            
            if (!string.IsNullOrWhiteSpace(dobText))
            {
                data.BirthDate = ParseDateFromText(dobText);
                if (!string.IsNullOrWhiteSpace(data.BirthDate))
                {
                    _logger.LogInformation($"✓ Extracted BirthDate from label: {data.BirthDate}");
                }
            }
            
            // Fallback: Look for date patterns anywhere in text
            if (string.IsNullOrWhiteSpace(data.BirthDate))
            {
                var datePatterns = new[]
                {
                    @"(\d{1,2}[\s\-\/]\d{1,2}[\s\-\/]\d{4})",
                    @"(\d{4}[\s\-\/]\d{1,2}[\s\-\/]\d{1,2})",
                    @"((?:JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)[A-Z]*\s+\d{1,2},?\s+\d{4})"
                };
                
                foreach (var pattern in datePatterns)
                {
                    var match = Regex.Match(correctedText, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        data.BirthDate = ParseDateFromText(match.Groups[1].Value);
                        if (!string.IsNullOrWhiteSpace(data.BirthDate))
                        {
                            _logger.LogInformation($"✓ Extracted BirthDate from pattern: {data.BirthDate}");
                            break;
                        }
                    }
                }
            }
            
            // === ADDRESS EXTRACTION with fuzzy matching ===
            var addressLabels = new[] { "ADDRESS", "TIRAHAN", "RESIDENCE", "LUGAR", "PERMANENTTIRAHAN", "PERMANENT ADDRESS", "PERMANENTADDRESS" };
            var addressLines = new List<string>();
            
            bool foundAddressLabel = false;
            for (int i = 0; i < lines.Length; i++)
            {
                var lineUpper = lines[i].ToUpper().Replace(" ", "");
                
                // Check if this line contains an address label (with fuzzy matching)
                foreach (var label in addressLabels)
                {
                    if (LevenshteinDistance(lineUpper, label.Replace(" ", "")) <= 3)
                    {
                        foundAddressLabel = true;
                        _logger.LogInformation($"Found address label at line {i}: {lines[i]}");
                        
                        // Collect next few lines as address
                        for (int j = i + 1; j < Math.Min(lines.Length, i + 4); j++)
                        {
                            var addressLine = lines[j].Trim();
                            // Stop if we hit another field or card boundaries
                            if (!Regex.IsMatch(addressLine, @"(REPUBLIC|PILIPINAS|VALID|EXPIRES|ID\s*NO|PETSA|DATE)", RegexOptions.IgnoreCase) &&
                                addressLine.Length > 5 &&
                                !Regex.IsMatch(addressLine, @"^\d{4}[\-\s]?\d{4}")) // Not an ID number
                            {
                                addressLines.Add(addressLine);
                            }
                            else
                            {
                                break;
                            }
                        }
                        break;
                    }
                }
                
                if (foundAddressLabel)
                    break;
            }
            
            if (addressLines.Count > 0)
            {
                data.Address = CleanupExtractedText(string.Join(", ", addressLines));
                _logger.LogInformation($"✓ Extracted Address: {data.Address.Substring(0, Math.Min(100, data.Address.Length))}...");
            }
            
            // === GENDER EXTRACTION with fuzzy matching ===
            var genderLabels = new[] { "SEX", "KASARIAN", "GENDER" };
            var genderText = ExtractTextAfterLabel(correctedText, genderLabels, maxDistance: 1, maxWordsToCapture: 1);
            
            if (!string.IsNullOrWhiteSpace(genderText))
            {
                var genderUpper = genderText.ToUpper();
                if (genderUpper.Contains("M") || genderUpper.Contains("MALE") || genderUpper.Contains("LALAKI"))
                {
                    data.Gender = "Male";
                    _logger.LogInformation($"✓ Extracted Gender: Male");
                }
                else if (genderUpper.Contains("F") || genderUpper.Contains("FEMALE") || genderUpper.Contains("BABAE"))
                {
                    data.Gender = "Female";
                    _logger.LogInformation($"✓ Extracted Gender: Female");
                }
            }
            
            // === CONTACT NUMBER EXTRACTION ===
            var phonePattern = @"(\+?63|0)?[\s\-]?9\d{2}[\s\-]?\d{3}[\s\-]?\d{4}";
            var phoneMatch = Regex.Match(correctedText, phonePattern);
            if (phoneMatch.Success)
            {
                data.ContactNumber = phoneMatch.Value.Trim();
                _logger.LogInformation($"✓ Extracted ContactNumber: {data.ContactNumber}");
            }
            
            // === BARANGAY EXTRACTION from Address ===
            if (!string.IsNullOrWhiteSpace(data.Address))
            {
                data.Barangay = ExtractBarangayFromAddress(data.Address);
                if (!string.IsNullOrWhiteSpace(data.Barangay))
                {
                    _logger.LogInformation($"✓ Extracted Barangay from address: {data.Barangay}");
                }
            }
            
            _logger.LogInformation("=== EXTRACTION COMPLETE ===");
            _logger.LogInformation($"Final Results: FirstName={data.FirstName}, LastName={data.LastName}, Middle={data.MiddleName}, DOB={data.BirthDate}, Barangay={data.Barangay}");
            
            // Fallback: Look for name patterns if still missing
            if (string.IsNullOrWhiteSpace(data.FirstName) || string.IsNullOrWhiteSpace(data.LastName))
            {
                // Try to find names in all-cap lines that look like names
                // Allow up to 5 words for multiple given names
                foreach (var line in lines)
                {
                    if (Regex.IsMatch(line, @"^[A-Z][A-Z\s]{2,}$") && line.Split(' ').Length >= 2 && line.Split(' ').Length <= 6)
                    {
                        var words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (words.All(w => w.Length > 1 && w.All(c => char.IsLetter(c))) && !Regex.IsMatch(line, @"\d"))
                        {
                            // Skip common non-name phrases
                            if (!Regex.IsMatch(line, @"(REPUBLIKA|REPUBLIC|PILIPINAS|PHILIPPINES|PAMBANSANG|PAGKAKAKILANLAN|MGA\s+PANGAL)", RegexOptions.IgnoreCase))
                            {
                                if (string.IsNullOrWhiteSpace(data.FirstName))
                                {
                                    // If we don't have last name yet, assume last word is last name
                                    if (string.IsNullOrWhiteSpace(data.LastName) && words.Length > 1)
                                    {
                                        // Put all words except last one in First Name
                                        data.FirstName = CleanupExtractedText(string.Join(" ", words.Take(words.Length - 1)));
                                        data.LastName = CleanupExtractedText(words[words.Length - 1]);
                                    }
                                    else if (!string.IsNullOrWhiteSpace(data.LastName))
                                    {
                                        // Last name already found, split given names: first parts in First Name, last part in Middle Name
                                        if (words.Length >= 2)
                                        {
                                            data.FirstName = CleanupExtractedText(string.Join(" ", words.Take(words.Length - 1)));
                                            data.MiddleName = CleanupExtractedText(words[words.Length - 1]);
                                        }
                                        else
                                        {
                                            data.FirstName = CleanupExtractedText(words[0]);
                                        }
                                    }
                                    else
                                    {
                                        // Only one word and no last name yet - treat as first name
                                        data.FirstName = CleanupExtractedText(words[0]);
                                    }
                                    _logger.LogInformation($"Extracted names from fallback pattern: First={data.FirstName}, Middle={data.MiddleName}, Last={data.LastName}");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            
            // Extract address specifically for Philippine National ID - look for "Tirahan/Address" label
            if (string.IsNullOrWhiteSpace(data.Address))
            {
                // Look for "TIRAHAN" or "TIRAHAN/ADDRESS" label in lines
                int tirahanIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (Regex.IsMatch(lines[i], @"TIRAHAN|TI\s*RAHAN|T1RAHAN", RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(lines[i], @"TIRAHAN\s*[/\s]\s*ADDRESS|ADDRESS\s*[/\s]\s*TIRAHAN", RegexOptions.IgnoreCase))
                    {
                        tirahanIndex = i;
                        _logger.LogInformation($"Found 'Tirahan/Address' label at line {i}: {lines[i]}");
                        break;
                    }
                }
                
                // If "Tirahan" label found, extract address from following lines
                if (tirahanIndex >= 0 && tirahanIndex + 1 < lines.Length)
                {
                    var addressParts = new List<string>();
                    
                    // Collect up to 4 lines after "Tirahan" label (typical address format)
                    for (int i = tirahanIndex + 1; i < lines.Length && i < tirahanIndex + 5 && addressParts.Count < 4; i++)
                    {
                        var line = lines[i].Trim();
                        
                        // Stop if we hit another field label
                        if (Regex.IsMatch(line, @"^(TIRAHAN|ADDRESS|PHONE|MOBILE|CONTACT|EMAIL|REPUBLIKA|REPUBLIC|ID\s+NUMBER)", RegexOptions.IgnoreCase) ||
                            Regex.IsMatch(line, @"\d{4}[\-\s]?\d{4}[\-\s]?\d{4}[\-\s]?\d{4}")) // ID number
                            break;
                        
                        // Skip if it looks like a name (short all-caps, no numbers, no commas)
                        if (Regex.IsMatch(line, @"^[A-Z]{2,}\s+[A-Z]{2,}(\s+[A-Z]{2,})?$") && 
                            line.Split(' ').Length <= 3 && !line.Contains(",") && !Regex.IsMatch(line, @"\d"))
                            break;
                        
                        // Collect address lines (should have length > 5 and contain numbers/commas/keywords)
                        if (line.Length > 5)
                        {
                            addressParts.Add(line);
                            _logger.LogInformation($"Extracted address line {i} after Tirahan: {line}");
                        }
                        else if (line.Length < 3)
                        {
                            // Stop if line is too short
                            break;
                        }
                    }
                    
                    if (addressParts.Count > 0)
                    {
                        data.Address = CleanupExtractedText(string.Join(", ", addressParts));
                        _logger.LogInformation($"Extracted Address from 'Tirahan' label: {data.Address}");
                    }
                }
            }
            
            // Extract other fields using common methods
            ExtractCommonFields(cleanedText, enhancedMode, data);
            
            return data;
        }
        
        // Driver's License specific extraction
        private IdData ExtractDriverLicenseData(string cleanedText, bool enhancedMode)
        {
            var data = new IdData();
            
            _logger.LogInformation("Extracting Driver's License data");
            
            // Split text into lines for better parsing
            var lines = cleanedText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();
            
            // Driver's License format: "SURNAME, GIVEN NAME MIDDLE NAME" on one line
            // Example: "ALMARIO, RENIER BEN PEREZ"
            foreach (var line in lines)
            {
                // Look for "SURNAME, GIVEN NAME" pattern (comma separated)
                var nameMatch = Regex.Match(line, @"^([A-Z][A-Z\s]+),\s+([A-Z][A-Z\s]+)$", RegexOptions.IgnoreCase);
                if (nameMatch.Success && nameMatch.Groups.Count >= 3)
                {
                    // Skip if it contains common non-name words
                    if (Regex.IsMatch(line, @"(REPUBLIC|PHILIPPINES|DEPARTMENT|TRANSPORTATION|OFFICE|LICENSE)", RegexOptions.IgnoreCase))
                        continue;
                    
                    var surname = nameMatch.Groups[1].Value.Trim();
                    var givenNames = nameMatch.Groups[2].Value.Trim();
                    
                    // Extract last name
                    data.LastName = CleanupExtractedText(surname);
                    
                    // Extract first and middle names from given names
                    var givenNameParts = givenNames.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (givenNameParts.Length > 0)
                    {
                        data.FirstName = CleanupExtractedText(givenNameParts[0]);
                        if (givenNameParts.Length > 1)
                        {
                            data.MiddleName = CleanupExtractedText(string.Join(" ", givenNameParts.Skip(1)));
                        }
                    }
                    
                    _logger.LogInformation($"Extracted Driver's License name: Last={data.LastName}, First={data.FirstName}, Middle={data.MiddleName}");
                    break;
                }
            }
            
            // Fallback: Try to find name pattern if not found above
            if (string.IsNullOrWhiteSpace(data.LastName) || string.IsNullOrWhiteSpace(data.FirstName))
            {
                // Look for "SURNAME, GIVEN NAME" pattern anywhere in text
                var surnameGivenMatch = Regex.Match(cleanedText, @"([A-Z][A-Z\s]+),\s+([A-Z][A-Z\s]+)", RegexOptions.IgnoreCase);
                if (surnameGivenMatch.Success && surnameGivenMatch.Groups.Count >= 3)
                {
                    var surname = surnameGivenMatch.Groups[1].Value.Trim();
                    var givenNames = surnameGivenMatch.Groups[2].Value.Trim();
                    
                    // Skip if it's clearly not a name (contains common words)
                    if (!Regex.IsMatch(surname + " " + givenNames, @"(REPUBLIC|PHILIPPINES|DEPARTMENT|TRANSPORTATION|OFFICE|LICENSE)", RegexOptions.IgnoreCase))
                    {
                        data.LastName = CleanupExtractedText(surname);
                        var givenNameParts = givenNames.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (givenNameParts.Length > 0)
                        {
                            data.FirstName = CleanupExtractedText(givenNameParts[0]);
                            if (givenNameParts.Length > 1)
                            {
                                data.MiddleName = CleanupExtractedText(string.Join(" ", givenNameParts.Skip(1)));
                            }
                        }
                        _logger.LogInformation($"Extracted Driver's License name (fallback): Last={data.LastName}, First={data.FirstName}, Middle={data.MiddleName}");
                    }
                }
            }
            
            // Extract address specifically for Driver's License
            // Driver's License format: Address appears after the name, before license number
            // Look for lines that look like addresses (contain numbers, street names, city names)
            if (string.IsNullOrWhiteSpace(data.Address))
            {
                var addressKeywords = new[] { "STREET", "STR", "ROAD", "RD", "AVE", "AVENUE", "BRGY", "BARANGAY", "CITY", "CALOOCAN", "NCR", "DISTRICT", "DEPARO", "KABATUHAN" };
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    
                    // Skip header lines, names, dates
                    if (Regex.IsMatch(line, @"(REPUBLIC|PHILIPPINES|DEPARTMENT|TRANSPORTATION|OFFICE|DRIVER|LICENSE|ALMARIO|RENIER)", RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(line, @"^\d{4}[/-]\d{2}[/-]\d{2}$") || // Date format YYYY/MM/DD
                        Regex.IsMatch(line, @"^NO\s+\d+[-]\d+[-]\d+")) // License number format
                        continue;
                    
                    // Filter out garbage text - lines with too many special characters or very short
                    int specialCharCount = line.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
                    if (specialCharCount > line.Length * 0.3 || line.Length < 5 || Regex.IsMatch(line, @"^[""]\s*[so]\s*[,\d]")) // Filter patterns like "" s 2, o 7
                        continue;
                    
                    // Check if line looks like an address
                    // Look for partial matches of address keywords (for garbled OCR like "BATUoDA" -> "KABATUHAN", "CAN CITY" -> "CALOOCAN CITY")
                    bool hasAddressKeyword = addressKeywords.Any(keyword => 
                    {
                        // Exact match
                        if (Regex.IsMatch(line, @"\b" + Regex.Escape(keyword) + @"\b", RegexOptions.IgnoreCase))
                            return true;
                        // Partial match for garbled OCR (at least 50% of keyword matches)
                        if (keyword.Length >= 4 && line.IndexOf(keyword.Substring(0, Math.Min(4, keyword.Length)), StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                        return false;
                    });
                    
                    bool hasNumberAndWords = Regex.IsMatch(line, @"^\d+[.\s]+[A-Z]") && line.Split(' ').Length >= 3;
                    bool hasMultipleWords = line.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Length >= 3; // Reduced from 4 to 3
                    bool containsCommas = line.Count(c => c == ',') >= 1;
                    bool hasValidWordLength = line.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Any(w => w.Length >= 3); // At least one word with 3+ chars
                    
                    if (hasAddressKeyword || (hasNumberAndWords && hasMultipleWords && hasValidWordLength) || (containsCommas && hasMultipleWords && hasValidWordLength))
                        {
                            // Collect this line and following lines that look like address continuation
                            var addressParts = new List<string> { line };
                            
                            for (int j = i + 1; j < lines.Length && j < i + 4 && addressParts.Count < 4; j++)
                            {
                                var nextLine = lines[j].Trim();
                                
                                // Stop if we hit license number or date
                                if (Regex.IsMatch(nextLine, @"^NO\s+\d+[-]\d+[-]\d+|^\d{4}[/-]\d{2}[/-]\d{2}$"))
                                    break;
                                
                                // Filter garbage text
                                int nextSpecialCharCount = nextLine.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
                                if (nextSpecialCharCount > nextLine.Length * 0.3 || nextLine.Length < 5)
                                {
                                    if (nextLine.Length < 5) break;
                                    continue;
                                }
                                
                                // Check for address keywords (exact or partial match)
                                bool hasKeyword = addressKeywords.Any(k => 
                                {
                                    if (Regex.IsMatch(nextLine, @"\b" + Regex.Escape(k) + @"\b", RegexOptions.IgnoreCase))
                                        return true;
                                    if (k.Length >= 4 && nextLine.IndexOf(k.Substring(0, Math.Min(4, k.Length)), StringComparison.OrdinalIgnoreCase) >= 0)
                                        return true;
                                    return false;
                                });
                                
                                // Add if it looks like address continuation
                                if (nextLine.Length > 5 && (nextLine.Contains(",") || hasKeyword || 
                                    (nextLine.Split(' ').Length >= 2 && nextLine.Any(c => char.IsDigit(c)))))
                                {
                                    addressParts.Add(nextLine);
                                }
                                else if (nextLine.Length < 5)
                                {
                                    break;
                                }
                            }
                            
                            // Only use if we have at least one valid address line (not just garbage)
                            if (addressParts.Count > 0 && addressParts.Any(part => 
                                part.Length >= 8 && part.Split(' ').Any(w => w.Length >= 3)))
                            {
                                data.Address = CleanupExtractedText(string.Join(", ", addressParts));
                                _logger.LogInformation($"Extracted Driver's License address: {data.Address}");
                                break;
                            }
                        }
                }
            }
            
            // Extract license number (Driver's License format: "NO 2-23-003719" or "N02-23-003719")
            if (string.IsNullOrWhiteSpace(data.IdNumber))
            {
                var licenseNumberPatterns = new[]
                {
                    @"(?:LICENSE\s+(?:NO|NUMBER)|NO)[:\s]*([A-Z]?\d+[-]\d+[-]\d+)",
                    @"\b([A-Z]?\d+[-]\d+[-]\d+)\b"
                };
                
                foreach (var pattern in licenseNumberPatterns)
                {
                    var match = Regex.Match(cleanedText, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        // Skip if it looks like a date (YYYY-MM-DD or similar)
                        var candidate = match.Groups[1].Value;
                        if (!Regex.IsMatch(candidate, @"^\d{4}[-]\d{2}[-]\d{2}$"))
                        {
                            data.IdNumber = candidate;
                            _logger.LogInformation($"Extracted Driver's License number: {data.IdNumber}");
                            break;
                        }
                    }
                }
            }
            
            ExtractCommonFields(cleanedText, enhancedMode, data);
            
            return data;
        }
        
        // Postal ID specific extraction
        private IdData ExtractPostalIdData(string cleanedText, bool enhancedMode)
        {
            var data = new IdData();
            
            _logger.LogInformation("Extracting Postal ID data");
            ExtractCommonFields(cleanedText, enhancedMode, data);
            
            return data;
        }
        
        // PhilHealth ID specific extraction
        private IdData ExtractPhilHealthData(string cleanedText, bool enhancedMode)
        {
            var data = new IdData();
            
            _logger.LogInformation("Extracting PhilHealth ID data");
            ExtractCommonFields(cleanedText, enhancedMode, data);
            
            return data;
        }
        
        // TIN ID specific extraction
        private IdData ExtractTinIdData(string cleanedText, bool enhancedMode)
        {
            var data = new IdData();
            
            _logger.LogInformation("Extracting TIN ID data");
            ExtractCommonFields(cleanedText, enhancedMode, data);
            
            return data;
        }
        
        // SSS ID specific extraction
        private IdData ExtractSssIdData(string cleanedText, bool enhancedMode)
        {
            var data = new IdData();
            
            _logger.LogInformation("Extracting SSS ID data");
            ExtractCommonFields(cleanedText, enhancedMode, data);
            
            return data;
        }
        
        // UMID specific extraction
        private IdData ExtractUmidIdData(string cleanedText, bool enhancedMode)
        {
            var data = new IdData();
            
            _logger.LogInformation("Extracting UMID data");
            ExtractCommonFields(cleanedText, enhancedMode, data);
            
            return data;
        }
        
        // Passport specific extraction
        private IdData ExtractPassportData(string cleanedText, bool enhancedMode)
        {
            var data = new IdData();
            
            _logger.LogInformation("Extracting Passport data");
            
            // Passport uses different format
            var lastNameMatch = Regex.Match(cleanedText, @"(?:SURNAME|LAST\s+NAME)[:\s]*([A-Z][A-Z\s]+)", RegexOptions.IgnoreCase);
            if (lastNameMatch.Success)
            {
                data.LastName = CleanupExtractedText(lastNameMatch.Groups[1].Value);
            }
            
            var firstNameMatch = Regex.Match(cleanedText, @"(?:GIVEN\s+NAME(?:S)?)[:\s]*([A-Z][A-Z\s]+)", RegexOptions.IgnoreCase);
            if (firstNameMatch.Success)
            {
                var fullName = firstNameMatch.Groups[1].Value.Trim();
                var nameParts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (nameParts.Length >= 1)
                {
                    data.FirstName = CleanupExtractedText(nameParts[0]);
                    if (nameParts.Length > 1)
                    {
                        data.MiddleName = CleanupExtractedText(string.Join(" ", nameParts.Skip(1)));
                    }
                }
            }
            
            ExtractCommonFields(cleanedText, enhancedMode, data);
            
            return data;
        }
        
        // Common field extraction used by all ID types
        private void ExtractCommonFields(string cleanedText, bool enhancedMode, IdData data)
        {
            // Extract birth date - prioritize labeled dates and filter out future dates
            var birthDatePatterns = new[]
            {
                // YYYY/MM/DD format with label (Driver's License format) - HIGHEST PRIORITY
                @"(?:DATE\s+OF\s+BIRTH|BIRTH\s+DATE|DOB|BORN)[:\s]*(\d{4}[/\-]\d{1,2}[/\-]\d{1,2})",
                // Standard date formats with label
                @"(?:PETSA\s+NG\s+KAPANGANAKAN|DATE\s+OF\s+BIRTH|BIRTH\s+DATE|DOB|BORN)[:\s]*([A-Za-z]+\s+\d{1,2},?\s+\d{4})",
                @"(?:PETSA\s+NG\s+KAPANGANAKAN|DATE\s+OF\s+BIRTH|BIRTH\s+DATE|DOB|BORN)[:\s]*(\d{1,2}[/\-]\d{1,2}[/\-]\d{2,4})",
                // Month name format
                @"\b(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER)\s+(\d{1,2}),?\s+(\d{4})\b"
            };
            
            // First, try labeled dates (these are most reliable)
            foreach (var pattern in birthDatePatterns)
            {
                var match = Regex.Match(cleanedText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    try
                    {
                        string dateStr = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                        
                        if (match.Groups.Count == 4 && match.Groups[1].Success && match.Groups[2].Success && match.Groups[3].Success)
                        {
                            var month = match.Groups[1].Value;
                            var day = match.Groups[2].Value.PadLeft(2, '0');
                            var year = match.Groups[3].Value;
                            
                            // Validate: birth date should be in the past (not future)
                            var parsedYear = int.Parse(year);
                            if (parsedYear <= DateTime.Now.Year)
                            {
                                data.BirthDate = $"{year}-{GetMonthNumber(month)}-{day}";
                                break;
                            }
                        }
                        else if (dateStr.Contains("/") || dateStr.Contains("-"))
                        {
                            var parts = dateStr.Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 3)
                            {
                                string year, month, day;
                                
                                // Check if it's YYYY/MM/DD format (Driver's License) or MM/DD/YYYY format
                                if (parts[0].Length == 4)
                                {
                                    // YYYY/MM/DD format
                                    year = parts[0];
                                    month = parts[1].PadLeft(2, '0');
                                    day = parts[2].PadLeft(2, '0');
                                }
                                else
                                {
                                    // MM/DD/YYYY or DD/MM/YYYY format
                                    month = parts[0].PadLeft(2, '0');
                                    day = parts[1].PadLeft(2, '0');
                                    year = parts[2];
                                    if (year.Length == 2)
                                    {
                                        year = int.Parse(year) > 50 ? "19" + year : "20" + year;
                                    }
                                }
                                
                                // Validate: birth date should be in the past (not future)
                                var parsedYear = int.Parse(year);
                                if (parsedYear <= DateTime.Now.Year && parsedYear >= 1900)
                                {
                                    data.BirthDate = $"{year}-{month}-{day}";
                                    break;
                                }
                            }
                        }
                        else
                        {
                            var date = DateTime.Parse(dateStr);
                            // Validate: birth date should be in the past
                            if (date <= DateTime.Now && date.Year >= 1900)
                            {
                                data.BirthDate = date.ToString("yyyy-MM-dd");
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }
            
            // If no labeled date found, try standalone dates but filter out future dates
            if (string.IsNullOrWhiteSpace(data.BirthDate))
            {
                var standaloneDatePattern = @"\b(\d{4})[/\-](\d{1,2})[/\-](\d{1,2})\b";
                var matches = Regex.Matches(cleanedText, standaloneDatePattern);
                foreach (Match match in matches)
                {
                    try
                    {
                        var year = match.Groups[1].Value;
                        var month = match.Groups[2].Value.PadLeft(2, '0');
                        var day = match.Groups[3].Value.PadLeft(2, '0');
                        var parsedYear = int.Parse(year);
                        
                        // Filter: birth date should be in the past and reasonable (1900-now)
                        if (parsedYear <= DateTime.Now.Year && parsedYear >= 1900)
                        {
                            // Skip if it looks like an expiration date (very recent year like 2027)
                            // Birth dates are typically 18+ years ago
                            if (parsedYear <= DateTime.Now.Year - 18)
                            {
                                data.BirthDate = $"{year}-{month}-{day}";
                                _logger.LogInformation($"Extracted standalone birth date (filtered future dates): {data.BirthDate}");
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }
            
            // Extract contact number
            var contactPatterns = new[]
            {
                @"(?:MOBILE|PHONE|CONTACT|TEL|CELL)[\s#:\.\-]+([0-9\+\-\(\)\s]{7,})",
                @"\b((?:09|\+?639)\d{9})\b"
            };
            
            foreach (var pattern in contactPatterns)
            {
                var match = Regex.Match(cleanedText, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    data.ContactNumber = Regex.Replace(match.Groups[1].Value, @"[^\d\+]", "");
                    break;
                }
            }
            
            // Extract address - multiple patterns for Philippine ID formats
            var addressPatterns = new[]
            {
                // Tagalog/English combined label with slash (e.g., "TIRAHAN/ADDRESS")
                @"(?:TIRAHAN|ADDRESS)[\/]?(?:\s*\/\s*)?(?:TIRAHAN|ADDRESS)[:\s]*(.+?)(?:\n\n|\r\n\r\n|\n[A-Z]{2,}[:\/]|$)",
                // Tagalog labels - handle common OCR misspellings (TI RAHAN, T1RAHAN, etc.)
                @"(?:TI\s*RAHAN|TIRAHAN|T1RAHAN|ADDRESS|RESIDENCE|LUGAR)[:\s]+(.+?)(?:\n\n|\r\n\r\n|\n[A-Z]{2,}[:\/]|$)",
                // English labels
                @"(?:ADDRESS|RESIDENCE)[:\s]+(.+?)(?:\n\n|\r\n\r\n|\n[A-Z]{2,}[:\/]|$)",
                // Pattern for multi-line addresses after address label - capture all following lines
                @"(?:TIRAHAN|TI\s*RAHAN|ADDRESS)[\/]?(?:\s*\/\s*)?(?:TIRAHAN|ADDRESS)[:\s]*\n(.+?)(?:\n\n|\r\n\r\n|\n[A-Z]{2,}[:\/]|$)",
                @"(?:ADDRESS|TIRAHAN|TI\s*RAHAN|RESIDENCE)[:\s]*\n(.+?)(?:\n\n|\r\n\r\n|\n[A-Z]{2,}[:\/]|$)",
                // Pattern for addresses on separate lines (common in PhilID) - up to 3 lines
                @"(?:TIRAHAN|TI\s*RAHAN|ADDRESS)[\/]?(?:\s*\/\s*)?(?:TIRAHAN|ADDRESS)[:\s]*\n(.+)\n(.+?)(?:\n[A-Z]{2,}[:\/]|$)",
                @"(?:ADDRESS|TIRAHAN|TI\s*RAHAN|RESIDENCE)[:\s]*\n(.+)\n(.+?)(?:\n[A-Z]{2,}[:\/]|$)",
                // Pattern to capture address after date (address often comes after birth date)
                // This captures lines immediately following the date pattern
                @"(?:JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER)\s+\d{1,2},?\s+\d{4}\s*\n\s*([^\n]+(?:\n[^\n]+)*?)(?:\n\n|\r\n\r\n|\n(?:[A-Z]{2,}[:\/]|TIRAHAN|ADDRESS|PHONE|MOBILE|CONTACT|EMAIL|\d{4}[\-\s]?\d{4})|$)",
                // More flexible pattern: address starting with number after date (common format: "391 ALPHA HOMES...")
                @"(?:JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER)\s+\d{1,2},?\s+\d{4}.*?\n\s*(\d+\s+[A-Z][^\n]+(?:\n[^\n]*?)?)(?:\n\n|\r\n\r\n|\n(?:[A-Z]{2,}[:\/]|TIRAHAN|ADDRESS|PHONE|MOBILE|CONTACT|EMAIL|\d{4}[\-\s]?\d{4})|$)"
            };
            
            foreach (var pattern in addressPatterns)
            {
                var match = Regex.Match(cleanedText, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);
                if (match.Success && match.Groups.Count > 1)
                {
                    // Combine multiple groups if present (for multi-line addresses)
                    var addressParts = new List<string>();
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        if (match.Groups[i].Success && !string.IsNullOrWhiteSpace(match.Groups[i].Value))
                        {
                            addressParts.Add(match.Groups[i].Value.Trim());
                        }
                    }
                    if (addressParts.Count > 0)
                    {
                        data.Address = CleanupExtractedText(string.Join(", ", addressParts));
                        _logger.LogInformation($"Extracted Address: {data.Address}");
                        break;
                    }
                }
            }
            
            // If address still not found, try looking for address-like patterns in lines
            if (string.IsNullOrWhiteSpace(data.Address))
            {
                var lines = cleanedText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToArray();
                
                // Look for lines that might be addresses (contain common address keywords)
                var addressKeywords = new[] { "STREET", "STR", "ROAD", "RD", "AVE", "AVENUE", "BRGY", "BARANGAY", "CITY", "BAYAN", "MUNICIPALITY", "LUNGSOD", "PROVINCE", "PROBINSYA", "SUBD", "SUBDIVISION", "HOMES", "VILLE", "DISTRICT", "NCR", "CALOOCAN" };
                
                // Find line index after date (address usually comes after birth date)
                int dateLineIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (Regex.IsMatch(lines[i], @"(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER)\s+\d{1,2},?\s+\d{4}", RegexOptions.IgnoreCase))
                    {
                        dateLineIndex = i;
                        _logger.LogInformation($"Found birth date at line {i}: {lines[i]}");
                        break;
                    }
                }
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    
                    // Skip if it's clearly a name, date, or ID number
                    if (Regex.IsMatch(line, @"\d{4}[\-\s]?\d{4}[\-\s]?\d{4}[\-\s]?\d{4}") || // ID number
                        Regex.IsMatch(line, @"^(REPUBLIKA|REPUBLIC|PILIPINAS|PHILIPPINES|PAMBANSANG|PAGKAKAKILANLAN|MGA\s+PANGAL|TIRAHAN|ADDRESS)", RegexOptions.IgnoreCase) || // Header/label text
                        Regex.IsMatch(line, @"^(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER)\s+\d{1,2},?\s+\d{4}$", RegexOptions.IgnoreCase) || // Date pattern (e.g., "JUNE 12, 2003")
                        (Regex.IsMatch(line, @"^[A-Z]{2,}\s+[A-Z]{2,}$") && line.Split(' ').All(w => w.Length >= 2 && w.All(c => char.IsLetter(c))) && 
                         !line.Contains(",") && !Regex.IsMatch(line, @"\d"))) // Likely a name (no numbers, no commas)
                        continue;
                    
                    // Prioritize lines after date - these are more likely to be addresses
                    bool isAfterDate = dateLineIndex >= 0 && i > dateLineIndex && i <= dateLineIndex + 10; // Increased range to 10 lines
                    
                    // Check if line contains address-like patterns
                    bool hasAddressKeyword = addressKeywords.Any(keyword => Regex.IsMatch(line, @"\b" + keyword + @"\b", RegexOptions.IgnoreCase));
                    bool hasNumberAndWords = Regex.IsMatch(line, @"^\d+\s+[A-Z]") && line.Split(' ').Length >= 2; // Starts with number followed by words (reduced min words to 2)
                    bool hasCommaAndLength = line.Contains(",") && line.Length > 10 && line.Split(' ').Length >= 2; // Reduced threshold
                    bool hasMultipleCommas = line.Count(c => c == ',') >= 1; // Reduced to 1 comma
                    bool hasOF = Regex.IsMatch(line, @"\bOF\b", RegexOptions.IgnoreCase) && line.Length > 10; // "CITY OF CALOOCAN" pattern
                    bool hasLongLineAfterDate = isAfterDate && line.Length > 20 && line.Split(' ').Length >= 3; // Long lines after date
                    
                    // Log potential address lines for debugging
                    if (isAfterDate || hasAddressKeyword || hasNumberAndWords || hasCommaAndLength || hasMultipleCommas)
                    {
                        _logger.LogInformation($"Evaluating line {i} after date (index {dateLineIndex}): {line}");
                        _logger.LogInformation($"  - isAfterDate: {isAfterDate}, hasKeyword: {hasAddressKeyword}, hasNumber: {hasNumberAndWords}, hasComma: {hasCommaAndLength}, multipleCommas: {hasMultipleCommas}, hasOF: {hasOF}, longLine: {hasLongLineAfterDate}");
                    }
                    
                    if (hasAddressKeyword || hasNumberAndWords || hasCommaAndLength || hasMultipleCommas || hasOF || hasLongLineAfterDate || isAfterDate)
                    {
                        // Collect this line and following lines that look like address parts
                        var addressLines = new List<string> { line };
                        
                        // Check next few lines for continuation of address (up to 5 more lines for multi-line addresses)
                        for (int j = i + 1; j < lines.Length && j < i + 6 && addressLines.Count < 5; j++)
                        {
                            var nextLine = lines[j].Trim();
                            
                            // Stop if we hit another field (date, ID number, or new label)
                            if (Regex.IsMatch(nextLine, @"\d{4}[\-\s]?\d{4}[\-\s]?\d{4}[\-\s]?\d{4}") ||
                                Regex.IsMatch(nextLine, @"^(TIRAHAN|ADDRESS|RESIDENCE|BARANGAY|CITY|PHONE|MOBILE|CONTACT|EMAIL|REPUBLIKA|REPUBLIC)", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(nextLine, @"^(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER)\s+\d{1,2},?\s+\d{4}$", RegexOptions.IgnoreCase)) // Skip date lines
                                break;
                            
                            // Skip if it looks like a name (all caps, 2-3 words, no numbers, no commas)
                            if (Regex.IsMatch(nextLine, @"^[A-Z]{2,}\s+[A-Z]{2,}(\s+[A-Z]{2,})?$") && 
                                !nextLine.Contains(",") && !Regex.IsMatch(nextLine, @"\d"))
                                break;
                            
                            // Add if it looks like address continuation (has comma, long enough, has address keywords, or has "OF")
                            bool isAddressContinuation = (nextLine.Contains(",") && nextLine.Length > 5) ||
                                (nextLine.Length > 15 && nextLine.Split(' ').Length >= 2) ||
                                addressKeywords.Any(keyword => Regex.IsMatch(nextLine, @"\b" + keyword + @"\b", RegexOptions.IgnoreCase)) ||
                                Regex.IsMatch(nextLine, @"\bOF\b", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(nextLine, @"^\d+\s+[A-Z]"); // Starts with number
                            
                            if (isAddressContinuation)
                            {
                                addressLines.Add(nextLine);
                                _logger.LogInformation($"  Added address continuation line {j}: {nextLine}");
                            }
                            else if (nextLine.Length > 8 && j <= i + 2)
                            {
                                // Add if reasonably long and close to first line (might be part of address)
                                addressLines.Add(nextLine);
                                _logger.LogInformation($"  Added potential address continuation line {j}: {nextLine}");
                            }
                            else if (nextLine.Length < 5)
                            {
                                // Stop if line is too short
                                break;
                            }
                        }
                        
                        if (addressLines.Count > 0)
                        {
                            data.Address = CleanupExtractedText(string.Join(", ", addressLines));
                            _logger.LogInformation($"Extracted Address from fallback pattern: {data.Address}");
                            break;
                        }
                    }
                }
            }
            
            // Extract ID number
            var idPatterns = new[]
            {
                @"(?:LICENSE\s+(?:NO|NUMBER)|ID\s+(?:NO|NUMBER)):?\s*([A-Z0-9\-]+)",
                @"\b([0-9]{4}[\-\s]?[0-9]{4}[\-\s]?[0-9]{4}[\-\s]?[0-9]{4})\b"
            };
            
            foreach (var pattern in idPatterns)
            {
                var match = Regex.Match(cleanedText, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    data.IdNumber = match.Groups[1].Value;
                    break;
                }
            }
            
            // Extract gender
            var genderMatch = Regex.Match(cleanedText, @"(?:SEX|GENDER):?\s*([MF]|MALE|FEMALE)", RegexOptions.IgnoreCase);
            if (genderMatch.Success)
            {
                var genderValue = genderMatch.Groups[1].Value.Trim().ToUpper();
                if (genderValue == "M" || genderValue == "MALE")
                {
                    data.Gender = "Male";
                }
                else if (genderValue == "F" || genderValue == "FEMALE")
                {
                    data.Gender = "Female";
                }
            }
        }
        
        private void ApplyFuzzyCorrections(IdData data, string fullText)
        {
            // Apply more aggressive fuzzy matching for enhanced mode
            // This would contain more sophisticated algorithms for extracting data
            // from potentially low-quality OCR results
            
            // Example: If we couldn't find a structured name field, try to infer it
            if (string.IsNullOrWhiteSpace(data.FirstName) && 
                string.IsNullOrWhiteSpace(data.LastName))
            {
                // Look for name patterns in the full text
                var namePattern = Regex.Match(fullText, @"(?:NAME|LICENSED TO):?\s*([A-Z][a-z]+\s+[A-Z][a-z]+)");
                if (namePattern.Success)
                {
                    var fullName = namePattern.Groups[1].Value.Trim();
                    var parts = fullName.Split(' ');
                    if (parts.Length >= 2)
                    {
                        data.FirstName = parts[0];
                        data.LastName = parts[parts.Length - 1];
                        
                        if (parts.Length > 2)
                        {
                            data.MiddleName = string.Join(" ", parts.Skip(1).Take(parts.Length - 2));
                        }
                    }
                }
            }
        }
        
        private string CleanupExtractedText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
                
            // Basic cleanup
            var cleaned = text.Trim()
                .Replace("  ", " ")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
                
            return cleaned;
        }

        private float CalculateConfidence(IdData data)
        {
            // Calculate a confidence score based on how many fields were successfully extracted
            int totalFields = 8; // Total number of fields we're trying to extract
            int filledFields = 0;
            
            if (!string.IsNullOrWhiteSpace(data.FirstName)) filledFields++;
            if (!string.IsNullOrWhiteSpace(data.LastName)) filledFields++;
            if (!string.IsNullOrWhiteSpace(data.MiddleName)) filledFields++;
            if (!string.IsNullOrWhiteSpace(data.BirthDate)) filledFields++;
            if (!string.IsNullOrWhiteSpace(data.Address)) filledFields++;
            if (!string.IsNullOrWhiteSpace(data.ContactNumber)) filledFields++;
            if (!string.IsNullOrWhiteSpace(data.IdNumber)) filledFields++;
            if (!string.IsNullOrWhiteSpace(data.Gender)) filledFields++;
            
            return (float)filledFields / totalFields;
        }

        private string GetUserFriendlyErrorMessage(Exception ex)
        {
            // Map common exceptions to user-friendly messages
            if (ex is UnauthorizedAccessException)
            {
                return "Permission error while processing image.";
            }
            else if (ex is InvalidOperationException)
            {
                return "Invalid operation during image processing. The image may be corrupted.";
            }
            else if (ex is IOException ioEx && ioEx.Message.Contains("disk"))
            {
                return "Server storage error. Please try again later.";
            }
            else if (ex is ArgumentException)
            {
                return "Invalid image format. Please use a supported image format.";
            }
            else
            {
                return "Error processing ID. Please try again or fill the form manually.";
            }
        }

        #region Enhanced Fuzzy Matching and OCR Error Correction

        /// <summary>
        /// Calculate Levenshtein distance between two strings for fuzzy matching
        /// </summary>
        private int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
                return string.IsNullOrEmpty(target) ? 0 : target.Length;
            if (string.IsNullOrEmpty(target))
                return source.Length;

            int n = source.Length;
            int m = target.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;
            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Correct common OCR errors in text
        /// </summary>
        private string CorrectOcrErrors(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var corrected = text;
            
            // Fix common OCR character confusions in NAME sections (all-caps sequences)
            corrected = Regex.Replace(corrected, @"\b[A-Z0-9@|€£¥]{2,}\b", match =>
            {
                var result = match.Value;
                // Only correct if it looks like a name (not a date or ID number)
                if (!Regex.IsMatch(result, @"^\d+$") && !Regex.IsMatch(result, @"\d{4}"))
                {
                    result = result.Replace("0", "O")
                                   .Replace("1", "I")
                                   .Replace("5", "S")
                                   .Replace("8", "B")
                                   .Replace("|", "I")
                                   .Replace("@", "A");
                }
                return result;
            });

            return corrected;
        }

        /// <summary>
        /// Extract text after a fuzzy-matched label with better OCR tolerance
        /// </summary>
        private string ExtractTextAfterLabel(string ocrText, string[] possibleLabels, int maxDistance = 2, int maxWordsToCapture = 3)
        {
            var lines = ocrText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var lineUpper = line.ToUpper();
                foreach (var label in possibleLabels)
                {
                    var labelUpper = label.ToUpper();
                    
                    // Try exact match with colon/space separator
                    var exactMatch = Regex.Match(line, $@"{Regex.Escape(labelUpper)}[\s:]+(.+)", RegexOptions.IgnoreCase);
                    if (exactMatch.Success)
                    {
                        return exactMatch.Groups[1].Value.Trim();
                    }
                    
                    // Try fuzzy match on individual words
                    var words = lineUpper.Split(new[] { ' ', ':', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < words.Length; i++)
                    {
                        if (LevenshteinDistance(words[i], labelUpper) <= maxDistance ||
                            LevenshteinDistance(words[i].Replace(" ", ""), labelUpper.Replace(" ", "")) <= maxDistance)
                        {
                            // Found match, extract remaining words
                            var originalWords = line.Split(new[] { ' ', ':', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            var remainingWords = originalWords.Skip(i + 1).Take(maxWordsToCapture).ToArray();
                            if (remainingWords.Length > 0)
                            {
                                return string.Join(" ", remainingWords);
                            }
                        }
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Parse date from various text formats with better error tolerance
        /// </summary>
        private string ParseDateFromText(string dateText)
        {
            if (string.IsNullOrWhiteSpace(dateText))
                return null;
            
            try
            {
                dateText = dateText.Trim();
                
                // Try standard DateTime parsing first
                if (DateTime.TryParse(dateText, out DateTime parsedDate))
                {
                    return parsedDate.ToString("yyyy-MM-dd");
                }
                
                // Try parsing numeric dates: DD/MM/YYYY, MM/DD/YYYY, DD-MM-YYYY, etc.
                var parts = Regex.Split(dateText, @"[\s\-\/\.]");
                if (parts.Length >= 3)
                {
                    if (int.TryParse(parts[0], out int part1) && 
                        int.TryParse(parts[1], out int part2) && 
                        int.TryParse(parts[2], out int year))
                    {
                        // Normalize year
                        if (year < 100)
                            year += (year > 50) ? 1900 : 2000;
                        
                        // Try both interpretations
                        var dates = new List<DateTime?>();
                        
                        // DD/MM/YYYY (Philippine format)
                        if (part1 >= 1 && part1 <= 31 && part2 >= 1 && part2 <= 12)
                            dates.Add(new DateTime(year, part2, part1));
                        
                        // MM/DD/YYYY (US format)
                        if (part2 >= 1 && part2 <= 31 && part1 >= 1 && part1 <= 12)
                            dates.Add(new DateTime(year, part1, part2));
                        
                        // Return first valid date
                        foreach (var date in dates.Where(d => d.HasValue))
                        {
                            return date.Value.ToString("yyyy-MM-dd");
                        }
                    }
                }
                
                // Try month name format: "JANUARY 15, 1990" or "15 JANUARY 1990"
                var monthPattern = @"(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)[A-Z]*[\s,]+(\d{1,2})[\s,]+(\d{4})";
                var monthMatch = Regex.Match(dateText, monthPattern, RegexOptions.IgnoreCase);
                if (!monthMatch.Success)
                {
                    // Try reverse: "15 JANUARY 1990"
                    monthPattern = @"(\d{1,2})[\s,]+(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)[A-Z]*[\s,]+(\d{4})";
                    monthMatch = Regex.Match(dateText, monthPattern, RegexOptions.IgnoreCase);
                }
                
                if (monthMatch.Success)
                {
                    var monthMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                    {
                        {"JAN", 1}, {"FEB", 2}, {"MAR", 3}, {"APR", 4},
                        {"MAY", 5}, {"JUN", 6}, {"JUL", 7}, {"AUG", 8},
                        {"SEP", 9}, {"OCT", 10}, {"NOV", 11}, {"DEC", 12}
                    };
                    
                    string monthStr = monthMatch.Groups[1].Value;
                    string dayStr = monthMatch.Groups[2].Value;
                    string yearStr = monthMatch.Groups[3].Value;
                    
                    // Check if order is day-month-year or month-day-year
                    if (!monthMap.ContainsKey(monthStr.Substring(0, Math.Min(3, monthStr.Length))))
                    {
                        // Swap if first group is not a month
                        (monthStr, dayStr) = (dayStr, monthStr);
                    }
                    
                    if (monthMap.TryGetValue(monthStr.Substring(0, 3).ToUpper(), out int month) &&
                        int.TryParse(dayStr, out int day) &&
                        int.TryParse(yearStr, out int year))
                    {
                        return new DateTime(year, month, day).ToString("yyyy-MM-dd");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to parse date '{dateText}': {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Extract barangay number from address string
        /// </summary>
        private string ExtractBarangayFromAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;
            
            var addressUpper = address.ToUpper();
            
            // Look for patterns like "BARANGAY 158", "BRGY 159", "BRGY. 160", etc.
            var patterns = new[]
            {
                @"(?:BARANGAY|BRGY\.?|BRG\.?)\s*(158|159|160|161)",
                @"\b(158|159|160|161)\b"  // Just the number itself
            };
            
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(addressUpper, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var barangayNum = match.Groups[1].Value;
                    _logger.LogInformation($"Found barangay '{barangayNum}' in address using pattern: {pattern}");
                    return barangayNum;
                }
            }
            
            _logger.LogInformation("No barangay number found in address");
            return null;
        }

        /// <summary>
        /// Calculate confidence for a single field based on various factors
        /// </summary>
        private float GetFieldConfidence(string fieldValue, string ocrText)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return 0;
            
            float confidence = 0.5f; // Base confidence for having a value
            
            // Check if value appears in OCR text (higher confidence)
            if (ocrText.Contains(fieldValue, StringComparison.OrdinalIgnoreCase))
            {
                confidence += 0.4f;
            }
            else if (ocrText.Contains(fieldValue.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
            {
                // Found without spaces
                confidence += 0.3f;
            }
            
            // Length check (reasonable length = higher confidence)
            if (fieldValue.Length >= 2 && fieldValue.Length <= 50)
            {
                confidence += 0.1f;
            }
            
            return Math.Min(confidence, 1.0f);
        }

        /// <summary>
        /// Improved confidence calculation with field-level scoring
        /// </summary>
        private float CalculateEnhancedConfidence(IdData data, string ocrText)
        {
            float totalScore = 0;
            float maxScore = 0;
            
            // Name fields (higher weight)
            maxScore += 30; // FirstName
            if (!string.IsNullOrWhiteSpace(data.FirstName))
            {
                totalScore += 30 * GetFieldConfidence(data.FirstName, ocrText);
            }
            
            maxScore += 30; // LastName
            if (!string.IsNullOrWhiteSpace(data.LastName))
            {
                totalScore += 30 * GetFieldConfidence(data.LastName, ocrText);
            }
            
            maxScore += 10; // MiddleName (lower weight, often missing)
            if (!string.IsNullOrWhiteSpace(data.MiddleName))
            {
                totalScore += 10 * GetFieldConfidence(data.MiddleName, ocrText);
            }
            
            // Birth date (high weight)
            maxScore += 20;
            if (!string.IsNullOrWhiteSpace(data.BirthDate))
            {
                totalScore += 20 * GetFieldConfidence(data.BirthDate, ocrText);
            }
            
            // Address (medium weight)
            maxScore += 15;
            if (!string.IsNullOrWhiteSpace(data.Address))
            {
                totalScore += 15 * GetFieldConfidence(data.Address, ocrText);
            }
            
            // Gender (low weight)
            maxScore += 10;
            if (!string.IsNullOrWhiteSpace(data.Gender))
            {
                totalScore += 10;
            }
            
            // Contact number (bonus)
            maxScore += 5;
            if (!string.IsNullOrWhiteSpace(data.ContactNumber))
            {
                totalScore += 5;
            }
            
            return maxScore > 0 ? totalScore / maxScore : 0;
        }

        #endregion
    }
}
