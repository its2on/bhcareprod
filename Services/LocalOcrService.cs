using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tesseract;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using OpenCvSharp;

namespace Barangay.Services
{
    public class LocalOcrService
    {
        private readonly ILogger<LocalOcrService> _logger;
        private readonly string _tesseractDataPath;
        private readonly HttpClient _httpClient;
        private List<(string path, string method)> _preprocessedImages;

        public LocalOcrService(ILogger<LocalOcrService> logger, IHttpClientFactory httpClientFactory = null)
        {
            _logger = logger;
            _httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();
            
            // Use application directory for tessdata (we'll download files here if needed)
            var appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            
            // Try to find Tesseract data directory
            // Common locations: current directory, tessdata subfolder, or system installation
            // Supports both Windows and Linux paths
            var possiblePaths = new List<string>
            {
                appDataPath, // Prefer application directory (we can download files here)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "tessdata"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "tessdata")
            };

            // Windows paths
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                possiblePaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Tesseract-OCR", "tessdata"));
                possiblePaths.Add(Path.Combine("C:", "Program Files", "Tesseract-OCR", "tessdata"));
                possiblePaths.Add(Path.Combine("C:", "Program Files (x86)", "Tesseract-OCR", "tessdata"));
            }
            else
            {
                // Linux paths (for Azure App Service)
                possiblePaths.Add("/usr/share/tesseract-ocr/5/tessdata");
                possiblePaths.Add("/usr/share/tesseract-ocr/4.00/tessdata");
                possiblePaths.Add("/usr/share/tesseract-ocr/tessdata");
                possiblePaths.Add("/usr/local/share/tessdata");
                possiblePaths.Add("/opt/tesseract/tessdata");
            }

            // Find existing tessdata directory or use application directory
            var existingPath = possiblePaths.FirstOrDefault(Directory.Exists);
            _tesseractDataPath = existingPath ?? appDataPath;
            
            // Use absolute path
            _tesseractDataPath = Path.GetFullPath(_tesseractDataPath).TrimEnd(Path.DirectorySeparatorChar);
            
            // Create tessdata directory if it doesn't exist (for downloading files)
            if (!Directory.Exists(_tesseractDataPath))
            {
                try
                {
                    Directory.CreateDirectory(_tesseractDataPath);
                    _logger.LogInformation("Created tessdata directory at: {Path}", _tesseractDataPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not create tessdata directory at {Path}, will try to use existing paths", _tesseractDataPath);
                }
            }
            
            // Set TESSDATA_PREFIX environment variable
            Environment.SetEnvironmentVariable("TESSDATA_PREFIX", _tesseractDataPath);
            
            _logger.LogInformation("Tesseract data path: {Path}", _tesseractDataPath);
            
            // Test if Tesseract native libraries are available (only on Linux)
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                try
                {
                    // Try to create a TesseractEngine instance to test if native libraries are available
                    using (var testEngine = new TesseractEngine(_tesseractDataPath, "eng", EngineMode.Default))
                    {
                        _logger.LogInformation("✓ Tesseract native libraries are available");
                    }
                }
                catch (DllNotFoundException dllEx)
                {
                    _logger.LogError(dllEx, "❌ Tesseract native libraries (Leptonica) not found. Local OCR will not work. " +
                        "Please ensure Leptonica is installed: apt-get install -y libleptonica-dev libtesseract-dev");
                }
                catch (Exception ex)
                {
                    // Other exceptions (like missing language files) are OK - we'll handle those later
                    _logger.LogWarning(ex, "Could not initialize Tesseract engine during startup check (this may be OK if language files are missing)");
                }
            }
            
            // Ensure language files exist (download if needed) - wait for it to complete
            try
            {
                EnsureLanguageFilesExist().Wait(TimeSpan.FromSeconds(30)); // Wait up to 30 seconds
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not download language files during initialization, will try again when needed");
            }
        }

        /// <summary>
        /// Preprocesses image using ImageSharp as fallback
        /// </summary>
        private async Task<byte[]> PreprocessWithImageSharp(byte[] imageBytes)
        {
            using var image = Image.Load(imageBytes);
            
            // Scale image to higher resolution for better OCR
            var originalWidth = image.Width;
            var originalHeight = image.Height;
            
            if (image.Width < 1200 || image.Height < 1600)
            {
                var scaleFactor = Math.Max(1200.0 / image.Width, 1600.0 / image.Height);
                var newWidth = (int)(image.Width * scaleFactor);
                var newHeight = (int)(image.Height * scaleFactor);
                
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(newWidth, newHeight),
                    Mode = ResizeMode.BoxPad
                }));
                
                _logger.LogInformation("Scaled image from {OldWidth}x{OldHeight} to {NewWidth}x{NewHeight}", 
                    originalWidth, originalHeight, newWidth, newHeight);
            }
            
            // Enhance image quality for OCR
            image.Mutate(x => x
                .Grayscale()
                .Contrast(1.8f)
                .Brightness(1.3f)
                .GaussianSharpen(2.0f)
                .AutoOrient()
            );

            // Convert back to byte array
            using var processedStream = new MemoryStream();
            await image.SaveAsPngAsync(processedStream);
            return processedStream.ToArray();
        }

        /// <summary>
        /// Ensures that the required Tesseract language data files exist, downloading them if necessary
        /// </summary>
        private async Task EnsureLanguageFilesExist()
        {
            var engDataFile = Path.Combine(_tesseractDataPath, "eng.traineddata");
            
            if (!File.Exists(engDataFile))
            {
                _logger.LogWarning("English language data file not found at {Path}, attempting to download", engDataFile);
                
                try
                {
                    _logger.LogInformation("Downloading eng.traineddata from GitHub...");
                    var url = "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata";
                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    
                    var data = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(engDataFile, data);
                    
                    _logger.LogInformation("Successfully downloaded eng.traineddata to {Path}", engDataFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download eng.traineddata. OCR may not work until this file is manually added.");
                }
            }
            else
            {
                _logger.LogInformation("Found eng.traineddata at {Path}", engDataFile);
            }
        }

        /// <summary>
        /// Analyzes a document and extracts barangay number (158, 159, 160, or 161)
        /// </summary>
        public async Task<OcrResult> AnalyzeResidencyDocumentAsync(Stream documentStream, string fileName)
        {
            try
            {
                _logger.LogInformation("=== LOCAL OCR ANALYSIS START ===");
                _logger.LogInformation("File: {FileName}", fileName);

                // Convert stream to byte array
                using var memoryStream = new MemoryStream();
                await documentStream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                // Ensure language files exist before using Tesseract
                var engDataFile = Path.Combine(_tesseractDataPath, "eng.traineddata");
                if (!File.Exists(engDataFile))
                {
                    _logger.LogWarning("eng.traineddata not found, attempting to download...");
                    await EnsureLanguageFilesExist();
                    
                    // Check again after download attempt
                    if (!File.Exists(engDataFile))
                    {
                        return new OcrResult
                        {
                            Success = false,
                            Message = "Tesseract language data file not found. Please ensure eng.traineddata exists in the tessdata directory."
                        };
                    }
                }

                // Preprocess image for better OCR results using OpenCvSharp for advanced processing
                byte[] processedBytes;
                
                try
                {
                    // Save image to temp file for OpenCvSharp processing
                    var tempImagePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
                    await File.WriteAllBytesAsync(tempImagePath, imageBytes);
                    
                    try
                    {
                        // Use OpenCvSharp for advanced preprocessing
                        using (var src = Cv2.ImRead(tempImagePath, ImreadModes.Color))
                        {
                            if (!src.Empty())
                            {
                                _logger.LogInformation("Applying OpenCvSharp preprocessing for better OCR");
                                
                                // Convert to grayscale
                                Mat gray = new Mat();
                                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                                
                                // Scale up if too small
                                if (gray.Width < 1200 || gray.Height < 1600)
                                {
                                    var scaleFactor = Math.Max(1200.0 / gray.Width, 1600.0 / gray.Height);
                                    var newWidth = (int)(gray.Width * scaleFactor);
                                    var newHeight = (int)(gray.Height * scaleFactor);
                                    Mat scaled = new Mat();
                                    Cv2.Resize(gray, scaled, new OpenCvSharp.Size(newWidth, newHeight), 0, 0, InterpolationFlags.Cubic);
                                    gray.Dispose();
                                    gray = scaled;
                                    _logger.LogInformation("Scaled image to {Width}x{Height} for better OCR", newWidth, newHeight);
                                }
                                
                                // Try multiple preprocessing approaches and use the best one
                                // Approach 1: CLAHE + Adaptive Thresholding (best for glare)
                                Mat claheResult = new Mat();
                                using (var clahe = Cv2.CreateCLAHE(3.0, new OpenCvSharp.Size(8, 8)))
                                {
                                    clahe.Apply(gray, claheResult);
                                }
                                
                                // Apply adaptive thresholding to handle glare better
                                Mat adaptive = new Mat();
                                Cv2.AdaptiveThreshold(claheResult, adaptive, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
                                
                                // Approach 2: Otsu thresholding
                                Mat otsu = new Mat();
                                Cv2.Threshold(claheResult, otsu, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                                
                                // Approach 3: Morphological operations to reduce glare
                                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                                Mat morphed = new Mat();
                                Cv2.MorphologyEx(claheResult, morphed, MorphTypes.Close, kernel);
                                
                                // Apply sharpening to all approaches
                                Mat sharpened1 = new Mat();
                                Mat blurred1 = new Mat();
                                Cv2.GaussianBlur(adaptive, blurred1, new OpenCvSharp.Size(0, 0), 2);
                                Cv2.AddWeighted(adaptive, 2.0, blurred1, -1.0, 0, sharpened1);
                                
                                Mat sharpened2 = new Mat();
                                Mat blurred2 = new Mat();
                                Cv2.GaussianBlur(otsu, blurred2, new OpenCvSharp.Size(0, 0), 2);
                                Cv2.AddWeighted(otsu, 2.0, blurred2, -1.0, 0, sharpened2);
                                
                                Mat sharpened3 = new Mat();
                                Mat blurred3 = new Mat();
                                Cv2.GaussianBlur(morphed, blurred3, new OpenCvSharp.Size(0, 0), 2);
                                Cv2.AddWeighted(morphed, 2.0, blurred3, -1.0, 0, sharpened3);
                                
                                // Apply denoising
                                Mat denoised1 = new Mat();
                                Mat denoised2 = new Mat();
                                Mat denoised3 = new Mat();
                                Cv2.FastNlMeansDenoising(sharpened1, denoised1, 10, 7, 21);
                                Cv2.FastNlMeansDenoising(sharpened2, denoised2, 10, 7, 21);
                                Cv2.FastNlMeansDenoising(sharpened3, denoised3, 10, 7, 21);
                                
                                // Save all three versions for OCR testing
                                var tempPath1 = tempImagePath.Replace(".jpg", "_1.jpg");
                                var tempPath2 = tempImagePath.Replace(".jpg", "_2.jpg");
                                var tempPath3 = tempImagePath.Replace(".jpg", "_3.jpg");
                                
                                Cv2.ImWrite(tempPath1, denoised1);
                                Cv2.ImWrite(tempPath2, denoised2);
                                Cv2.ImWrite(tempPath3, denoised3);
                                
                                // Store all three versions for OCR testing
                                _preprocessedImages = new List<(string path, string method)>
                                {
                                    (tempPath1, "AdaptiveThreshold"),
                                    (tempPath2, "OtsuThreshold"),
                                    (tempPath3, "Morphological")
                                };
                                
                                // Use the first approach by default (adaptive thresholding is best for glare)
                                processedBytes = await File.ReadAllBytesAsync(tempPath1);
                                
                                // Cleanup
                                gray.Dispose();
                                claheResult.Dispose();
                                adaptive.Dispose();
                                otsu.Dispose();
                                kernel.Dispose();
                                morphed.Dispose();
                                blurred1.Dispose();
                                blurred2.Dispose();
                                blurred3.Dispose();
                                sharpened1.Dispose();
                                sharpened2.Dispose();
                                sharpened3.Dispose();
                                denoised1.Dispose();
                                denoised2.Dispose();
                                denoised3.Dispose();
                                
                                _logger.LogInformation("OpenCvSharp preprocessing completed successfully - created 3 versions for OCR testing");
                            }
                            else
                            {
                                _logger.LogWarning("OpenCvSharp failed to load image, using ImageSharp preprocessing");
                                processedBytes = await PreprocessWithImageSharp(imageBytes);
                            }
                        }
                    }
                    finally
                    {
                        // Clean up temp files after OCR is done (will be cleaned up later)
                        // Keep them for now so we can try OCR on all versions
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenCvSharp preprocessing failed, falling back to ImageSharp");
                    processedBytes = await PreprocessWithImageSharp(imageBytes);
                }

                // Perform OCR using Tesseract with multiple PSM modes and combine results
                var allTexts = new List<string>();
                
                // Try different Page Segmentation Modes (PSM) for better results
                // PSM 6: Uniform block of text (best for ID cards)
                // PSM 3: Fully automatic page segmentation
                // PSM 4: Single column of variable-sized text
                // PSM 11: Sparse text
                // PSM 12: Sparse text with OSD
                var psmModes = new[] { "6", "3", "4", "11", "12" };
                
                // Try both Default and LSTM engine modes for better accuracy
                var engineModes = new[] { EngineMode.Default, EngineMode.LstmOnly };
                
                // Try OCR on all preprocessed versions if available
                var imagesToProcess = new List<(byte[] bytes, string method)>();
                imagesToProcess.Add((processedBytes, "Primary"));
                
                if (_preprocessedImages != null && _preprocessedImages.Count > 0)
                {
                    foreach (var (path, method) in _preprocessedImages)
                    {
                        if (File.Exists(path))
                        {
                            try
                            {
                                var bytes = await File.ReadAllBytesAsync(path);
                                imagesToProcess.Add((bytes, method));
                                _logger.LogInformation("Added preprocessed image for OCR: {Method}", method);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to read preprocessed image: {Path}", path);
                            }
                        }
                    }
                }
                
                // Also try the original image without preprocessing as a fallback
                imagesToProcess.Add((imageBytes, "Original"));
                _logger.LogInformation("Will try OCR on {Count} image versions", imagesToProcess.Count);
                
                foreach (var (imgBytes, method) in imagesToProcess)
                {
                    foreach (var engineMode in engineModes)
                    {
                        foreach (var psmMode in psmModes)
                        {
                            try
                            {
                                using (var engine = new TesseractEngine(_tesseractDataPath, "eng", engineMode))
                                {
                                    // Configure engine parameters for ID card recognition
                                    // REMOVED character whitelist to allow all characters (including special chars)
                                    engine.SetVariable("tessedit_pageseg_mode", psmMode);
                                    
                                    // Set DPI to 300 for better accuracy
                                    engine.SetVariable("user_defined_dpi", "300");
                                    
                                    // Additional settings for better OCR
                                    engine.SetVariable("tessedit_char_whitelist", ""); // No whitelist - allow all characters
                                    engine.SetVariable("preserve_interword_spaces", "1"); // Preserve spaces
                                    
                                    using var img = Pix.LoadFromMemory(imgBytes);
                                    using var page = engine.Process(img);
                                    var text = page.GetText();
                                    var confidence = page.GetMeanConfidence();
                                    
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        allTexts.Add(text);
                                        _logger.LogInformation("Method: {Method}, Engine: {EngineMode}, PSM {PSM} - Confidence: {Confidence}, Text length: {Length}", 
                                            method, engineMode, psmMode, confidence, text.Length);
                                    }
                                }
                            }
                            catch (DllNotFoundException dllEx)
                            {
                                // Native library missing - this is a critical error that should be reported clearly
                                _logger.LogError(dllEx, "❌ Tesseract native libraries (Leptonica) not found. " +
                                    "Local OCR cannot function. Please install: apt-get install -y libleptonica-dev libtesseract-dev");
                                
                                // Return early with a clear error message
                                return new OcrResult
                                {
                                    Success = false,
                                    Message = "Tesseract OCR native libraries are not installed on the server. " +
                                        "Please contact the administrator. Error: " + dllEx.Message
                                };
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to process with Method: {Method}, Engine: {EngineMode}, PSM {PSM}", method, engineMode, psmMode);
                                
                                // Check if this is a native library issue
                                if (ex.Message.Contains("libleptonica") || ex.Message.Contains("DllNotFoundException") || 
                                    ex.InnerException?.Message?.Contains("libleptonica") == true)
                                {
                                    _logger.LogError(ex, "❌ Native library error detected. Local OCR cannot function.");
                                    return new OcrResult
                                    {
                                        Success = false,
                                        Message = "Tesseract OCR native libraries are not available. " +
                                            "Please contact the administrator. Error: " + ex.Message
                                    };
                                }
                            }
                        }
                    }
                }
                
                // Combine all OCR results, merging unique lines from all PSM modes
                string extractedText = "";
                if (allTexts.Count > 0)
                {
                    // Merge unique lines from all PSM mode results
                    var lineDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    
                    // Process texts in order of length (longest first) to preserve best quality text
                    foreach (var text in allTexts.OrderByDescending(t => t.Length))
                    {
                        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .Where(l => !string.IsNullOrWhiteSpace(l));
                        
                        foreach (var line in lines)
                        {
                            var key = line.ToUpperInvariant();
                            if (!lineDict.ContainsKey(key) || line.Length > lineDict[key].Length)
                            {
                                lineDict[key] = line;
                            }
                        }
                    }
                    
                    // Combine all unique lines
                    extractedText = string.Join("\n", lineDict.Values);
                    _logger.LogInformation("Combined OCR text from {Count} PSM modes: {Length} characters, {Lines} unique lines", 
                        allTexts.Count, extractedText.Length, lineDict.Count);
                }

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogWarning("No text extracted from document");
                    return new OcrResult
                    {
                        Success = false,
                        Message = "No text could be extracted from the document. Please ensure the document is clear and readable."
                    };
                }

                _logger.LogInformation("Extracted text length: {Length} characters", extractedText.Length);
                _logger.LogInformation("Extracted text preview: {Preview}", extractedText.Substring(0, Math.Min(200, extractedText.Length)));

                // Clean up temp files after OCR is done
                if (_preprocessedImages != null)
                {
                    foreach (var (path, _) in _preprocessedImages)
                    {
                        try
                        {
                            if (File.Exists(path)) File.Delete(path);
                        }
                        catch { }
                    }
                    _preprocessedImages = null;
                }

                // Step 3: Search for Barangay number
                var barangayResult = ExtractBarangayNumber(extractedText);

                return barangayResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OCR analysis");
                return new OcrResult
                {
                    Success = false,
                    Message = $"OCR analysis error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Validates that the extracted text is from an actual Philippine ID document
        /// Rejects plain text, screenshots, or documents without ID markers
        /// STRICT VALIDATION: Requires actual ID document markers, not just address fields
        /// </summary>
        private bool IsValidPhilippineIdDocument(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var upperText = text.ToUpper();
            
            // CRITICAL: Check for screenshot indicators in text (screenshots often have UI elements)
            var screenshotIndicators = new[] { "SCREENSHOT", "SCREEN SHOT", "CAPTURE", "SNAP", "WINDOWS", "MACOS", "ANDROID", "IOS" };
            if (screenshotIndicators.Any(indicator => upperText.Contains(indicator)))
            {
                _logger.LogWarning("⚠️ Document validation failed: Screenshot indicators found in text");
                return false;
            }
            
            // Required Philippine ID markers - document MUST contain at least one STRONG ID marker
            // These are specific to actual ID documents, not just any document with an address
            var strongIdMarkers = new[]
            {
                // Republic of the Philippines markers (REQUIRED for most IDs)
                "REPUBLIC OF THE PHILIPPINES",
                "REPUBLIKA NG PILIPINAS",
                "REPUBLIC OF THE PHILIPPINE",
                
                // Driver's License markers (REQUIRED)
                "DRIVER'S LICENSE",
                "DRIVERS LICENSE",
                "DRIVER LICENSE",
                "LICENSE TO DRIVE",
                "LAND TRANSPORTATION OFFICE",
                "LTO",
                "DEPARTMENT OF TRANSPORTATION",
                
                // National ID markers (REQUIRED)
                "PHILSYS",
                "PHILIPPINE IDENTIFICATION SYSTEM",
                "PHILIPPINE NATIONAL ID",
                "NATIONAL ID",
                "PAMBANSANG PAGKAKAKILANLAN",
                "PHILIPPINE IDENTIFICATION CARD",
                
                // PhilHealth markers (REQUIRED)
                "PHILHEALTH",
                "PHIL-HEALTH",
                "PHILIPPINE HEALTH INSURANCE",
                "MEMBER ID",
                
                // UMID/SSS markers (REQUIRED)
                "UMID",
                "UNIFIED MULTI-PURPOSE ID",
                "GSIS",
                "SSS",
                "SOCIAL SECURITY",
                
                // Postal ID markers (REQUIRED)
                "POSTAL ID",
                "PHILIPPINE POSTAL",
                "PHLPOST",
                "POST OFFICE",
                
                // Passport markers (REQUIRED)
                "PASSPORT",
                "REPUBLIC OF THE PHILIPPINES PASSPORT",
                
                // TIN ID markers (REQUIRED)
                "TIN",
                "TAX IDENTIFICATION NUMBER",
                "BIR",
                "BUREAU OF INTERNAL REVENUE"
            };
            
            // Check for STRONG ID markers first (these are required for legitimate IDs)
            bool hasStrongIdMarker = strongIdMarkers.Any(marker => upperText.Contains(marker));
            
            // Also check for partial matches of strong markers (handle OCR errors)
            if (!hasStrongIdMarker)
            {
                hasStrongIdMarker = 
                    upperText.Contains("REPUBLIC") && (upperText.Contains("PHILIPPINES") || upperText.Contains("PHILIPPINE")) ||
                    (upperText.Contains("DRIVER") || upperText.Contains("DRIVERS")) && upperText.Contains("LICENSE") ||
                    upperText.Contains("PHILSYS") ||
                    upperText.Contains("PHILHEALTH") ||
                    upperText.Contains("UMID") ||
                    upperText.Contains("POSTAL ID") ||
                    upperText.Contains("PASSPORT") ||
                    (upperText.Contains("TAX") || upperText.Contains("TIN")) && upperText.Contains("IDENTIFICATION");
            }

            // CRITICAL: Must have a STRONG ID marker - screenshots won't have these
            if (!hasStrongIdMarker)
            {
                _logger.LogWarning("⚠️ Document validation failed: No strong Philippine ID markers found");
                _logger.LogWarning("Text preview: {Preview}", text.Substring(0, Math.Min(500, text.Length)));
                _logger.LogWarning("Screenshots and non-ID documents are rejected. Please upload an actual Philippine ID document.");
                return false;
            }

            // Additional validation: Check for ID-specific fields (using very lenient partial matches)
            var idFields = new[]
            {
                "LAST NAME", "SURNAME", "APELYIDO", "APELLIDO", "LAST", "NAME",
                "FIRST NAME", "GIVEN NAME", "MGA PANGALAN", "FIRST", "GIVEN",
                "DATE OF BIRTH", "BIRTH DATE", "KAPANGANAKAN", "BIRTH", "DATE",
                "ADDRESS", "TIRAHAN", "BARANGAY", "BARANG", "BRGY", "CITY",
                "SEX", "GENDER", "KASARIAN", "NATIONALITY", "NATL"
            };

            // Document should have at least 2 ID fields (name + address or birth date)
            // Use very lenient partial matching to handle OCR errors
            int fieldCount = idFields.Count(field => 
                upperText.Contains(field) || 
                upperText.Contains(field.Replace(" ", "")) ||
                // Handle common OCR errors - very lenient
                (field.Contains("BARANGAY") && (upperText.Contains("BARANG") || upperText.Contains("BRGY") || upperText.Contains("BAR"))) ||
                (field.Contains("ADDRESS") && (upperText.Contains("ADDR") || upperText.Contains("CITY") || upperText.Contains("ADD"))) ||
                (field.Contains("NAME") && (upperText.Contains("NAM") || upperText.Contains("SURNAME") || upperText.Contains("SUR"))) ||
                (field.Contains("NATIONALITY") && (upperText.Contains("NATL") || upperText.Contains("NATIO") || upperText.Contains("NAT")))
            );
            
            // Also check for patterns that indicate ID fields even if garbled
            // Check for name pattern (Last, First format)
            if (Regex.IsMatch(upperText, @"[A-Z]{2,},\s*[A-Z]{2,}"))
            {
                fieldCount++; // Found name pattern
            }
            
            // Check for license number pattern (N10-22-300176)
            if (Regex.IsMatch(upperText, @"\b\d{2,3}-\d{2}-\d{6}\b") || Regex.IsMatch(upperText, @"[A-Z]?\d{2,3}-\d{2}-\d{6}"))
            {
                fieldCount++; // Found license number
            }
            
            // Check if we have barangay number or address indicators
            var hasBarangay = Regex.IsMatch(upperText, @"\b(15[89]|1[6-9][0-9]|1[6-9][0-9])\b") || 
                             upperText.Contains("BARANG") || upperText.Contains("BRGY") ||
                             upperText.Contains("LT") || upperText.Contains("BLK"); // LT5 BLK1 indicates address
            
            if (hasBarangay)
            {
                fieldCount++; // Count barangay/address as a field
            }
            
            // Check for address indicators
            if (upperText.Contains("ADDRESS") || upperText.Contains("ADDR") || 
                upperText.Contains("LT") || upperText.Contains("BLK") || 
                upperText.Contains("CITY") || upperText.Contains("REPARO"))
            {
                fieldCount++; // Count address indicators
            }
            
            if (fieldCount < 2)
            {
                _logger.LogWarning("⚠️ Document validation failed: Insufficient ID fields found (found {Count}, need at least 2)", fieldCount);
                _logger.LogWarning("Extracted text: {Text}", text.Substring(0, Math.Min(500, text.Length)));
                return false;
            }

            _logger.LogInformation("✅ Document validation passed: Philippine ID detected (markers: {Markers}, fields: {Fields})", 
                strongIdMarkers.Count(m => upperText.Contains(m)), fieldCount);
            return true;
        }

        /// <summary>
        /// Searches for Barangay 158, 159, 160, or 161 in the extracted text
        /// STRICT VALIDATION: Only matches exactly 158, 159, 160, or 161
        /// Requires actual Philippine ID document (not plain text or screenshots)
        /// </summary>
        private OcrResult ExtractBarangayNumber(string text)
        {
            // STEP 0: Validate that this is an actual Philippine ID document
            if (!IsValidPhilippineIdDocument(text))
            {
                _logger.LogError("❌ REJECTED: Document is not a valid Philippine ID");
                _logger.LogError("The uploaded file appears to be plain text, a screenshot, or not a valid Philippine ID document.");
                _logger.LogError("Please upload an actual Philippine ID document (Driver's License, National ID, PhilHealth ID, etc.)");
                
                return new OcrResult
                {
                    Success = false,
                    Message = "Invalid document type. Please upload an actual Philippine ID document (Driver's License, National ID, PhilHealth ID, Postal ID, etc.). Plain text or screenshots are not accepted.",
                    ExtractedText = text
                };
            }

            // Define valid barangays - ONLY these are accepted
            var validBarangays = new[] { "158", "159", "160", "161" };
            
            // STEP 1: First, try to find VALID barangays (158-161) - highest priority
            // Try multiple regex patterns to catch variations - STRICT patterns only
            // Handle OCR errors like "BARANG." instead of "BARANGAY"
            var validPatterns = new[]
            {
                @"\bBARANGAY\s+(158|159|160|161)\b",           // BARANGAY 158 (with word boundaries)
                @"\bBARANG\.?\s+(158|159|160|161)\b",          // BARANG. 158 or BARANG 158 (OCR error)
                @"\bBARANG\s+(158|159|160|161)\b",             // BARANG 158 (OCR error - missing AY)
                @"\bBRGY\.?\s+(158|159|160|161)\b",            // BRGY 158 or BRGY. 158 (with word boundaries)
                @"\bBARANGAY\s+NO\.?\s+(158|159|160|161)\b",   // BARANGAY NO. 158 (with word boundaries)
                @"\bBARANGAY\s+#\s+(158|159|160|161)\b",       // BARANGAY # 158 (with word boundaries)
                @"\b(158|159|160|161)\s+BARANGAY\b",           // 158 BARANGAY (with word boundaries)
                @"\b(158|159|160|161)\s+BARANG\.?\b",          // 158 BARANG. (OCR error)
                @"BARANG\.?\s*,\s*(158|159|160|161)\b",       // BARANG., 158 (from address line)
                @"BARANG\s*,\s*(158|159|160|161)\b",          // BARANG, 158 (from address line - OCR error)
                @"BA\s+(158|159|160|161)\b",                  // BA 158 (very garbled - BARANGAY cut off)
                @"(?:^|\s|,|\.)(158|159|160|161)(?:\s|$|,|\.)", // Just the numbers with context boundaries
                // Look for numbers near address keywords
                @"(?:LT|BLK|ADDRESS|BARANG|BRGY|CITY).*?(158|159|160|161)\b", // Number near address keywords
            };

            foreach (var pattern in validPatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success && match.Groups.Count > 1)
                {
                    var barangayNumber = match.Groups[1].Value.Trim();
                    
                    // CRITICAL: Double-check that the detected barangay is EXACTLY in the valid list
                    if (validBarangays.Contains(barangayNumber))
                    {
                        _logger.LogInformation("=== BARANGAY FOUND (VALIDATED) ===");
                        _logger.LogInformation("Pattern: {Pattern}", pattern);
                        _logger.LogInformation("Barangay: {Barangay}", barangayNumber);

                        return new OcrResult
                        {
                            Success = true,
                            BarangayNumber = barangayNumber,
                            Message = $"Residency verified in Barangay {barangayNumber}",
                            ExtractedText = text
                        };
                    }
                }
            }

            // STEP 2: If no valid barangay found, check if ANY other barangay number exists
            // This helps identify when an ineligible barangay is found (e.g., 168, 162, etc.)
            var otherBarangayPatterns = new[]
            {
                @"\bBARANGAY\s+(\d{2,4})\b",           // BARANGAY 168, BARANGAY 162, etc.
                @"\bBRGY\.?\s+(\d{2,4})\b",            // BRGY 168, BRGY. 162, etc.
                @"\bBARANGAY\s+NO\.?\s+(\d{2,4})\b",   // BARANGAY NO. 168, etc.
            };

            foreach (var pattern in otherBarangayPatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success && match.Groups.Count > 1)
                {
                    var detectedNumber = match.Groups[1].Value.Trim();
                    
                    // If it's NOT in the valid list, reject it explicitly
                    if (!validBarangays.Contains(detectedNumber))
                    {
                        _logger.LogWarning("⚠️ Detected non-eligible barangay: {Barangay} (not in 158-161)", detectedNumber);
                        return new OcrResult
                        {
                            Success = false,
                            BarangayNumber = detectedNumber, // Store for error message
                            Message = $"The document shows Barangay {detectedNumber}, which is not eligible for automatic verification. Only Barangay 158, 159, 160, or 161 are eligible. Your account will require manual review by an administrator.",
                            ExtractedText = text
                        };
                    }
                }
            }

            // STEP 3: No barangay number found at all
            _logger.LogWarning("No valid barangay number (158-161) found in text");
            _logger.LogInformation("Text searched (preview): {Text}", text.Length > 500 ? text.Substring(0, 500) + "..." : text);

            return new OcrResult
            {
                Success = false,
                Message = "Unable to verify residency. No valid Barangay number (158, 159, 160, or 161) found in the document. Please ensure your document clearly shows your barangay number.",
                ExtractedText = text
            };
        }
    }
}

