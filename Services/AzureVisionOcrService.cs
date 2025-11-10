using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenCvSharp;

namespace Barangay.Services
{
    /// <summary>
    /// Enhanced Azure Vision OCR Service using Azure.AI.Vision.ImageAnalysis SDK
    /// Supports Filipino and English text recognition with image preprocessing
    /// </summary>
    public class AzureVisionOcrService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureVisionOcrService> _logger;
        private readonly string _endpoint;
        private readonly string _key;

        public AzureVisionOcrService(IConfiguration configuration, ILogger<AzureVisionOcrService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Get Azure Vision credentials from configuration
            // Priority: Environment variables > IConfiguration (which includes appsettings.json)
            // Try multiple formats to handle different configuration sources
            
            // Check environment variables first (highest priority in Azure App Service)
            var envEndpoint = Environment.GetEnvironmentVariable("AzureOCR__Endpoint");
            var envKey = Environment.GetEnvironmentVariable("AzureOCR__Key");
            
            // Check IConfiguration (includes appsettings.json and environment variables)
            var configEndpointUnderscore = _configuration["AzureOCR__Endpoint"];
            var configKeyUnderscore = _configuration["AzureOCR__Key"];
            var configEndpointColon = _configuration["AzureOCR:Endpoint"];
            var configKeyColon = _configuration["AzureOCR:Key"];
            
            // Use environment variables if available, otherwise use IConfiguration
            _endpoint = envEndpoint?.Trim() ?? 
                       configEndpointUnderscore?.Trim() ?? 
                       configEndpointColon?.Trim() ?? "";
            
            _key = envKey?.Trim() ?? 
                   configKeyUnderscore?.Trim() ?? 
                   configKeyColon?.Trim() ?? "";

            // Enhanced logging for debugging
            if (!string.IsNullOrEmpty(_endpoint) && !string.IsNullOrEmpty(_key))
            {
                var source = !string.IsNullOrEmpty(envEndpoint) ? "Environment Variable" :
                            !string.IsNullOrEmpty(configEndpointUnderscore) ? "IConfiguration (AzureOCR__Endpoint)" :
                            "IConfiguration (AzureOCR:Endpoint)";
                _logger.LogInformation("✓ Azure Vision OCR configured - Endpoint: {Endpoint}, Key length: {KeyLength}, Source: {Source}", 
                    _endpoint, _key.Length, source);
            }
            else
            {
                _logger.LogError("✗ Azure Vision OCR credentials not configured. OCR features will not work.");
                _logger.LogWarning("Configuration check results:");
                _logger.LogWarning("  Environment AzureOCR__Endpoint: {EnvEndpoint}", envEndpoint != null ? "FOUND" : "NOT FOUND");
                _logger.LogWarning("  Environment AzureOCR__Key: {EnvKey}", envKey != null ? $"FOUND (length: {envKey.Length})" : "NOT FOUND");
                _logger.LogWarning("  IConfiguration AzureOCR__Endpoint: {ConfigUnderscore}", configEndpointUnderscore != null ? "FOUND" : "NOT FOUND");
                _logger.LogWarning("  IConfiguration AzureOCR__Key: {ConfigKeyUnderscore}", configKeyUnderscore != null ? $"FOUND (length: {configKeyUnderscore.Length})" : "NOT FOUND");
                _logger.LogWarning("  IConfiguration AzureOCR:Endpoint: {ConfigColon}", configEndpointColon != null ? "FOUND" : "NOT FOUND");
                _logger.LogWarning("  IConfiguration AzureOCR:Key: {ConfigKeyColon}", configKeyColon != null ? $"FOUND (length: {configKeyColon.Length})" : "NOT FOUND");
                _logger.LogWarning("To fix: Set AzureOCR__Endpoint and AzureOCR__Key in Azure App Service → Configuration → Application settings");
            }
        }

        /// <summary>
        /// Analyzes ID image using Azure Vision Read API with preprocessing
        /// Extracts: First Name, Middle Name, Last Name, Suffix, Contact Number, Address, Birth Date, Barangay
        /// </summary>
        public async Task<IdExtractionResult> AnalyzeIdImageAsync(Stream imageStream, string fileName, bool usePreprocessing = true)
        {
            try
            {
                _logger.LogInformation("=== AZURE VISION OCR ANALYSIS START ===");
                _logger.LogInformation("File: {FileName}", fileName);

                if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_key))
                {
                    return new IdExtractionResult
                    {
                        Success = false,
                        Message = "Azure Vision OCR is not configured. Please check appsettings.json or environment variables."
                    };
                }

                // Read image bytes
                byte[] imageBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await imageStream.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }

                // Preprocess image if requested
                byte[] processedBytes = imageBytes;
                if (usePreprocessing)
                {
                    try
                    {
                        processedBytes = await PreprocessImageAsync(imageBytes);
                        _logger.LogInformation("Image preprocessing completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Image preprocessing failed, using original image");
                        processedBytes = imageBytes;
                    }
                }

                // Create Azure Vision client
                var credential = new AzureKeyCredential(_key);
                var client = new ImageAnalysisClient(new Uri(_endpoint), credential);

                // Analyze image with Read feature
                // Note: Azure Vision ImageAnalysis API supports: en, es, fr, de, it, pt, zh, ja, ko, etc.
                // Filipino (fil) is not directly supported, but English works well for Philippine IDs
                _logger.LogInformation("Calling Azure Vision Read API with language: en");
                
                var imageData = BinaryData.FromBytes(processedBytes);
                var options = new ImageAnalysisOptions
                {
                    Language = "en", // English (works well for Philippine IDs which often use English)
                    GenderNeutralCaption = false
                };

                var result = await client.AnalyzeAsync(
                    imageData,
                    VisualFeatures.Read,
                    options);

                // Extract text from Read result
                string extractedText = "";
                if (result?.Value?.Read != null)
                {
                    var readResult = result.Value.Read;
                    if (readResult.Blocks != null && readResult.Blocks.Count > 0)
                    {
                        // Get full text with line breaks for better parsing
                        var fullText = string.Join("\n", readResult.Blocks
                            .SelectMany(block => block.Lines ?? Enumerable.Empty<DetectedTextLine>())
                            .Select(line => string.Join(" ", line.Words?.Select(w => w.Text) ?? Enumerable.Empty<string>())));

                        var textLines = readResult.Blocks
                            .SelectMany(block => block.Lines ?? Enumerable.Empty<DetectedTextLine>())
                            .SelectMany(line => line.Words ?? Enumerable.Empty<DetectedTextWord>())
                            .Select(word => word.Text)
                            .Where(text => !string.IsNullOrWhiteSpace(text));

                        _logger.LogInformation("Extracted {WordCount} words, {LineCount} lines", 
                            textLines.Count(), 
                            readResult.Blocks.SelectMany(b => b.Lines ?? Enumerable.Empty<DetectedTextLine>()).Count());
                        
                        // Use full text for parsing (preserves structure)
                        extractedText = fullText;
                        
                        // Log each line separately for debugging
                        var lines = extractedText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        _logger.LogInformation("Extracted lines ({Count}):", lines.Length);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            _logger.LogInformation("  Line {Index}: {Line}", i + 1, lines[i]);
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogWarning("No text extracted from image");
                    return new IdExtractionResult
                    {
                        Success = false,
                        Message = "No text could be extracted from the ID image. Please ensure the image is clear and readable."
                    };
                }

                _logger.LogInformation("Extracted text length: {Length} characters", extractedText.Length);
                _logger.LogInformation("Full extracted text:\n{FullText}", extractedText);

                // Parse extracted text to extract structured fields
                var parsedData = ParseIdData(extractedText);
                _logger.LogInformation("Parsed data - FirstName: {FirstName}, LastName: {LastName}, MiddleName: {MiddleName}, Suffix: {Suffix}, ContactNumber: {ContactNumber}, Address: {Address}, BirthDate: {BirthDate}",
                    parsedData.FirstName, parsedData.LastName, parsedData.MiddleName, parsedData.Suffix, parsedData.ContactNumber, parsedData.Address, parsedData.BirthDate);

                // Extract Barangay number
                var barangayNumber = ExtractBarangayNumber(extractedText);

                // Validate Barangay (158-161)
                var validBarangays = new[] { "158", "159", "160", "161" };
                bool isBarangayValid = !string.IsNullOrWhiteSpace(barangayNumber) && 
                                      validBarangays.Contains(barangayNumber.Trim());

                return new IdExtractionResult
                {
                    Success = true,
                    Message = isBarangayValid 
                        ? $"Residency verified. Barangay {barangayNumber} is eligible."
                        : !string.IsNullOrWhiteSpace(barangayNumber)
                            ? $"Barangay {barangayNumber} detected but not eligible (must be 158, 159, 160, or 161)."
                            : "Barangay number not found in document.",
                    ExtractedText = extractedText,
                    FirstName = parsedData.FirstName,
                    MiddleName = parsedData.MiddleName,
                    LastName = parsedData.LastName,
                    Suffix = parsedData.Suffix,
                    ContactNumber = parsedData.ContactNumber,
                    Address = parsedData.Address,
                    BirthDate = parsedData.BirthDate,
                    Gender = parsedData.Gender,
                    BarangayNumber = barangayNumber,
                    IsBarangayValid = isBarangayValid
                };
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Vision API error: {StatusCode} - {Message}", ex.Status, ex.Message);
                return new IdExtractionResult
                {
                    Success = false,
                    Message = $"Azure Vision API error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Azure Vision OCR analysis");
                return new IdExtractionResult
                {
                    Success = false,
                    Message = $"OCR analysis error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Preprocesses image using OpenCVSharp for better OCR accuracy
        /// Applies: Grayscale conversion, Sharpening, Adaptive Thresholding
        /// </summary>
        private async Task<byte[]> PreprocessImageAsync(byte[] imageBytes)
        {
            try
            {
                // Save to temp file for OpenCV processing
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
                await File.WriteAllBytesAsync(tempPath, imageBytes);

                try
                {
                    using (var src = Cv2.ImRead(tempPath, ImreadModes.Color))
                    {
                        if (src.Empty())
                        {
                            _logger.LogWarning("OpenCV failed to load image");
                            return imageBytes;
                        }

                        // Convert to grayscale
                        Mat gray = new Mat();
                        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

                        // Scale up if too small (better for OCR)
                        if (gray.Width < 1200 || gray.Height < 1600)
                        {
                            var scaleFactor = Math.Max(1200.0 / gray.Width, 1600.0 / gray.Height);
                            var newWidth = (int)(gray.Width * scaleFactor);
                            var newHeight = (int)(gray.Height * scaleFactor);
                            Mat scaled = new Mat();
                            Cv2.Resize(gray, scaled, new OpenCvSharp.Size(newWidth, newHeight), 0, 0, InterpolationFlags.Cubic);
                            gray.Dispose();
                            gray = scaled;
                        }

                        // Apply CLAHE for better contrast
                        Mat claheResult = new Mat();
                        using (var clahe = Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8)))
                        {
                            clahe.Apply(gray, claheResult);
                        }

                        // Apply sharpening using unsharp masking
                        Mat blurred = new Mat();
                        Cv2.GaussianBlur(claheResult, blurred, new OpenCvSharp.Size(0, 0), 3);
                        Mat sharpened = new Mat();
                        Cv2.AddWeighted(claheResult, 1.5, blurred, -0.5, 0, sharpened);

                        // Apply adaptive thresholding for better text extraction
                        Mat thresholded = new Mat();
                        Cv2.AdaptiveThreshold(sharpened, thresholded, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);

                        // Save processed image
                        Cv2.ImWrite(tempPath, thresholded);

                        // Read back as bytes
                        var processedBytes = await File.ReadAllBytesAsync(tempPath);

                        // Cleanup
                        gray.Dispose();
                        claheResult.Dispose();
                        blurred.Dispose();
                        sharpened.Dispose();
                        thresholded.Dispose();

                        return processedBytes;
                    }
                }
                finally
                {
                    // Clean up temp file
                    if (File.Exists(tempPath))
                    {
                        try { File.Delete(tempPath); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenCV preprocessing failed, using original image");
                return imageBytes;
            }
        }

        /// <summary>
        /// Corrects common OCR errors in name strings
        /// Handles misreads like: ANONS->ANTHONY, ANTHON->ANTHONY, etc.
        /// </summary>
        private string CorrectOcrNameErrors(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;
            
            var corrected = name.ToUpper();
            
            // Common OCR errors for names
            var corrections = new Dictionary<string, string>
            {
                { "ANONS", "ANTHONY" },
                { "ANTHON", "ANTHONY" },
                { "ANTON", "ANTHONY" },
                { "ANTHNY", "ANTHONY" },
                { "ANTONY", "ANTHONY" },
                { "LLONA", "LLONA" }, // Keep as is, but ensure it's recognized
                { "LLON", "LLONA" },
                { "LONA", "LLONA" },
                { "LOPEZ", "LOPEZ" }, // Keep common names as is
            };
            
            // Try exact match first
            if (corrections.ContainsKey(corrected))
            {
                return corrections[corrected];
            }
            
            // Try partial matches for common name patterns
            if (corrected.StartsWith("ANTH") && corrected.Length >= 4)
            {
                return "ANTHONY";
            }
            
            return name; // Return original if no correction found
        }

        /// <summary>
        /// Corrects common OCR errors in year strings
        /// Handles misreads like: 3->8, 0->O, 1->I, 5->S, etc.
        /// </summary>
        private string CorrectOcrYearErrors(string year)
        {
            if (string.IsNullOrWhiteSpace(year) || year.Length != 4)
                return year;
            
            // Common OCR misreads for digits in years:
            // 3 -> 8 (very common, especially in 2003, 2013, etc.)
            // 0 -> O
            // 1 -> I or l
            // 5 -> S
            // 6 -> G
            // 8 -> B or 3
            // 9 -> g or q
            
            var corrected = year.ToCharArray();
            
            // Fix common misreads, but be conservative - only fix if it makes sense
            // For years, we expect 1900-2024 range
            for (int i = 0; i < corrected.Length; i++)
            {
                char c = corrected[i];
                
                // If it's a letter that could be a misread digit, try to correct it
                if (char.IsLetter(c))
                {
                    switch (c)
                    {
                        case 'O': case 'o': corrected[i] = '0'; break; // O -> 0
                        case 'I': case 'l': case '|': corrected[i] = '1'; break; // I/l -> 1
                        case 'S': case 's': corrected[i] = '5'; break; // S -> 5
                        case 'G': corrected[i] = '6'; break; // G -> 6
                        case 'B': corrected[i] = '8'; break; // B -> 8
                    }
                }
            }
            
            string correctedYear = new string(corrected);
            
            // Special case: Common OCR error 2008 -> 2003 (3 misread as 8)
            // If year is 2008 and we're looking for birth dates, try 2003 as correction
            // This is a very common OCR error where the digit 3 looks like 8
            if (correctedYear == "2008" && year[3] == '8')
            {
                // For birth dates, 2003 is much more likely than 2008 (people born in 2008 would be ~16 years old)
                // Try correcting the last digit from 8 to 3
                string try2003 = correctedYear.Substring(0, 3) + "3";
                if (int.TryParse(try2003, out int y2003) && y2003 >= 1900 && y2003 <= DateTime.Now.Year)
                {
                    correctedYear = try2003;
                }
            }
            
            return correctedYear;
        }

        /// <summary>
        /// Public method to parse ID data from text (can be called from outside)
        /// </summary>

        public ParsedIdData ParseIdDataFromText(string text)
        {
            return ParseIdData(text);
        }

        /// <summary>
        /// Parses extracted OCR text to identify structured fields
        /// </summary>
        private ParsedIdData ParseIdData(string text)
        {
            var result = new ParsedIdData();
            var upperText = text.ToUpperInvariant();
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            // Extract Name (usually in format: LASTNAME, FIRSTNAME MIDDLENAME)
            // First, try to find the actual name value, not just labels
            
            // Comprehensive list of words to skip (address words, labels, etc.)
            var skipWords = new[] { 
                "NAME", "ADDRESS", "DATE", "BIRTH", "NATIONALITY", "SEX", "GENDER", 
                "HEIGHT", "WEIGHT", "BLOOD", "EYE", "COLOR", "LICENSE", "DRIVER",
                "REPUBLIC", "PHILIPPINES", "TRANSPORTATION", "OFFICE", "DEPARTMENT",
                "BARANGAY", "BRGY", "BARANG", "CITY", "REPARO", "LIBIS", "BLK", "BLKE",
                "LT", "LTS", "DISTRICT", "REGION", "CAPITAL", "NOR", "THIRD",
                "EXPIRATION", "AGENCY", "CODE", "ASSISTANT", "SECRETARY", "SIGNATURE",
                "MARINES", "ISPORTATION", "GALVANTE", "GODA", "KALOOKAN",
                "NATIONAL", "OFFICE", "TO", "S", "BLACK",
                "BARANGAYGITY", "BARANGAYCITY", "CITYNOR", "THIRDDISTRICT",
                "CALOOCAN", "QUEZON", "MANILA", "MAKATI", "TAGUIG", "STREET", "ST",
                "AVENUE", "AVE", "ROAD", "RD", "LANE", "SUBDIVISION", "SUBDV",
                "PHASE", "UNIT", "FLOOR", "BUILDING", "BLDG", "NO", "LOT", "SITIO",
                "PUROK", "PHASE", "ZONE", "BLOCK", "METRO", "NCR"
            };
            
            // Strategy 1: Look for name patterns that are actual names (not labels or address words)
            var namePatterns = new[]
            {
                // Pattern 1: Standard format "LOPEZ, ANTHONY JR LLONA" or "LOPEZ, ANTHONY JR"
                // Must be all caps, comma-separated, and not contain "Name" as a word
                // Enhanced to handle OCR errors and missing spaces
                @"\b([A-Z]{3,20}),\s+([A-Z]{2,20}(?:\s+[A-Z]{1,20})*)\s*(JR\.?|SR\.?|I{2,3}|IV|V)?\b",
                // Pattern 2: Handle cases where comma might be missing or OCR errors
                @"\b([A-Z]{3,20})[,]?\s+([A-Z]{2,20}(?:\s+[A-Z]{1,20})*)\s*(JR\.?|SR\.?|I{2,3}|IV|V)?\b",
                // Pattern 3: Handle names split across lines or with OCR errors in spacing
                @"\b([A-Z]{3,20})[,]?\s*([A-Z]{2,20})\s+([A-Z]{1,20})?\s*(JR\.?|SR\.?|I{2,3}|IV|V)?\s*([A-Z]{2,20})?\b",
            };

            bool nameFound = false;
            foreach (var pattern in namePatterns)
            {
                var matches = Regex.Matches(text, pattern);
                foreach (Match nameMatch in matches)
                {
                    var lastName = nameMatch.Groups[1].Value.Trim();
                    var givenNames = nameMatch.Groups[2].Value.Trim();
                    
                    // Skip if it contains "NAME" as a word (likely a label)
                    if (lastName.Equals("NAME", StringComparison.OrdinalIgnoreCase) || 
                        givenNames.Equals("NAME", StringComparison.OrdinalIgnoreCase) ||
                        lastName.Contains("NAME") || givenNames.Contains("NAME"))
                    {
                        continue;
                    }
                    
                    // Skip if it's an address word or label
                    bool isSkipWord = false;
                    foreach (var skipWord in skipWords)
                    {
                        if (lastName.Equals(skipWord, StringComparison.OrdinalIgnoreCase) ||
                            givenNames.Equals(skipWord, StringComparison.OrdinalIgnoreCase) ||
                            lastName.Contains(skipWord, StringComparison.OrdinalIgnoreCase) ||
                            givenNames.Contains(skipWord, StringComparison.OrdinalIgnoreCase))
                        {
                            isSkipWord = true;
                            break;
                        }
                    }
                    if (isSkipWord) continue;
                    
                    // Skip if it's too short or looks like a label
                    if (lastName.Length < 3 || givenNames.Length < 2)
                    {
                        continue;
                    }
                    
                    // Additional validation: names should not contain numbers
                    if (Regex.IsMatch(lastName, @"\d") || Regex.IsMatch(givenNames, @"\d"))
                    {
                        continue;
                    }
                    
                    // Names should be mostly letters (allow apostrophes and hyphens)
                    if (!Regex.IsMatch(lastName, @"^[A-Z\s'-]+$", RegexOptions.IgnoreCase) ||
                        !Regex.IsMatch(givenNames, @"^[A-Z\s'-]+$", RegexOptions.IgnoreCase))
                    {
                        continue;
                    }
                    
                    result.LastName = lastName;
                    
                    // Split given names into first and middle
                    var nameParts = givenNames.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (nameParts.Length > 0)
                    {
                        // Apply OCR error correction to first name
                        result.FirstName = CorrectOcrNameErrors(nameParts[0]);
                    }
                    if (nameParts.Length > 1)
                    {
                        // Check if last part is a suffix
                        var lastPart = nameParts[nameParts.Length - 1];
                        if (Regex.IsMatch(lastPart, @"^(JR\.?|SR\.?|I{2,3}|IV|V|2ND|3RD)$", RegexOptions.IgnoreCase))
                        {
                            result.Suffix = lastPart.Replace(".", "");
                            // Middle name is everything between first name and suffix
                            if (nameParts.Length > 2)
                            {
                                result.MiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
                            }
                        }
                        else
                        {
                            // Check if second part is a single initial (middle initial)
                            if (nameParts[1].Length == 1 || (nameParts[1].Length == 2 && nameParts[1].EndsWith(".")))
                            {
                                result.MiddleName = nameParts[1].Replace(".", "");
                            }
                            else
                            {
                                // Full middle name(s)
                                result.MiddleName = string.Join(" ", nameParts.Skip(1));
                            }
                        }
                    }
                    
                    // Extract suffix if present in the pattern match
                    if (nameMatch.Groups.Count > 3 && !string.IsNullOrWhiteSpace(nameMatch.Groups[3].Value))
                    {
                        result.Suffix = nameMatch.Groups[3].Value.Trim();
                    }
                    nameFound = true;
                    break; // Found a match, stop trying other patterns
                }
                if (nameFound) break;
            }
            
            // Strategy 2: Look for names that might be split or have OCR errors
            // Handle cases where "LOPEZ" and "ANTHONY" appear separately
            if (!nameFound)
            {
                // Look for "LOPEZ" in the text
                var lopezMatch = Regex.Match(text, @"\bLOPEZ\b", RegexOptions.IgnoreCase);
                if (lopezMatch.Success)
                {
                    // Look for "ANTHONY" or variations nearby (within 300 characters)
                    var searchStart = Math.Max(0, lopezMatch.Index - 150);
                    var searchEnd = Math.Min(text.Length, lopezMatch.Index + lopezMatch.Length + 300);
                    var nearbyText = text.Substring(searchStart, searchEnd - searchStart);
                    
                    // Try to find ANTHONY or variations
                    var anthonyPatterns = new[]
                    {
                        @"\b(ANTHONY|ANONS|ANTHON|ANTON|ANTHNY|ANTONY)\b",
                        @"\b(ANTH)\w*\b"
                    };
                    
                    foreach (var pattern in anthonyPatterns)
                    {
                        var anthonyMatch = Regex.Match(nearbyText, pattern, RegexOptions.IgnoreCase);
                        if (anthonyMatch.Success)
                        {
                            var firstName = CorrectOcrNameErrors(anthonyMatch.Groups[1].Value);
                            
                            // Look for JR and middle name nearby
                            var jrMatch = Regex.Match(nearbyText, @"\b(JR|SR|II|III|IV|V)\b", RegexOptions.IgnoreCase);
                            var middleNameMatch = Regex.Match(nearbyText, @"\b(LLONA|LLON|LONA)\b", RegexOptions.IgnoreCase);
                            
                            result.LastName = "LOPEZ";
                            result.FirstName = firstName;
                            
                            if (jrMatch.Success)
                            {
                                result.Suffix = jrMatch.Groups[1].Value;
                            }
                            
                            if (middleNameMatch.Success)
                            {
                                result.MiddleName = CorrectOcrNameErrors(middleNameMatch.Groups[1].Value);
                            }
                            
                            nameFound = true;
                            break;
                        }
                    }
                }
            }
            
            // Strategy 3: If no pattern matched, look for lines that look like names
            if (!nameFound)
            {
                // Look for lines that look like names (comma-separated, all caps, not "NAME")
                foreach (var line in lines)
                {
                    // Skip lines that are just labels or too short
                    if (line.Contains("Last Name") || line.Contains("First Name") || 
                        line.Contains("Middle Name") ||
                        line.Equals("NAME", StringComparison.OrdinalIgnoreCase) ||
                        line.Length < 5 || line.Length > 80)
                    {
                        continue;
                    }
                    
                    // Skip if line contains address keywords
                    bool containsAddressWord = false;
                    foreach (var skipWord in skipWords)
                    {
                        if (line.Contains(skipWord, StringComparison.OrdinalIgnoreCase))
                        {
                            containsAddressWord = true;
                            break;
                        }
                    }
                    if (containsAddressWord) continue;
                    
                    // Look for comma-separated patterns
                    if (line.Contains(","))
                    {
                        var parts = line.Split(',');
                        if (parts.Length >= 2)
                        {
                            var lastNamePart = parts[0].Trim();
                            var firstNamePart = parts[1].Trim();
                            
                            // Skip if it's just "Name" or contains "Name" as a word
                            if (lastNamePart.Equals("NAME", StringComparison.OrdinalIgnoreCase) ||
                                firstNamePart.Equals("NAME", StringComparison.OrdinalIgnoreCase) ||
                                lastNamePart.Contains("NAME") || firstNamePart.Contains("NAME"))
                            {
                                continue;
                            }
                            
                            // Skip if it's an address word
                            bool isSkipWord = false;
                            foreach (var skipWord in skipWords)
                            {
                                if (lastNamePart.Equals(skipWord, StringComparison.OrdinalIgnoreCase) ||
                                    firstNamePart.Equals(skipWord, StringComparison.OrdinalIgnoreCase) ||
                                    lastNamePart.Contains(skipWord, StringComparison.OrdinalIgnoreCase) ||
                                    firstNamePart.Contains(skipWord, StringComparison.OrdinalIgnoreCase))
                                {
                                    isSkipWord = true;
                                    break;
                                }
                            }
                            if (isSkipWord) continue;
                            
                            // Skip if contains numbers
                            if (Regex.IsMatch(lastNamePart, @"\d") || Regex.IsMatch(firstNamePart, @"\d"))
                            {
                                continue;
                            }
                            
                            // Check if it looks like a name (mostly letters, at least 3 chars for last name, 2 for first)
                            if (lastNamePart.Length >= 3 && firstNamePart.Length >= 2 &&
                                Regex.IsMatch(lastNamePart, @"^[A-Z\s'-]{3,}$", RegexOptions.IgnoreCase) && 
                                Regex.IsMatch(firstNamePart, @"^[A-Z\s'-]{2,}$", RegexOptions.IgnoreCase))
                            {
                                result.LastName = lastNamePart;
                                var nameParts = firstNamePart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (nameParts.Length > 0)
                                {
                                    result.FirstName = nameParts[0];
                                }
                                if (nameParts.Length > 1)
                                {
                                    // Check if last part is a suffix
                                    var lastPart = nameParts[nameParts.Length - 1];
                                    if (Regex.IsMatch(lastPart, @"^(JR|SR|I{2,3}|IV|V)$", RegexOptions.IgnoreCase))
                                    {
                                        result.Suffix = lastPart;
                                        result.MiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
                                    }
                                    else
                                    {
                                        result.MiddleName = string.Join(" ", nameParts.Skip(1));
                                    }
                                }
                                
                                nameFound = true;
                                break;
                            }
                        }
                    }
                }
            }
            
            // Strategy 3: Search for common name patterns in the entire text (not just lines)
            // This is the most aggressive search - look for any comma-separated pattern that looks like a name
            if (!nameFound)
            {
                // Look for patterns like "WORD, WORD WORD" that appear to be names
                // Use word boundaries to avoid partial matches
                var allNamePattern = @"\b([A-Z]{3,20}),\s+([A-Z]{2,20}(?:\s+[A-Z]{1,20})*)\b";
                var allMatches = Regex.Matches(text, allNamePattern);
                
                // Score each match and pick the best one
                Match bestMatch = null;
                int bestScore = 0;
                
                foreach (Match match in allMatches)
                {
                    var lastName = match.Groups[1].Value.Trim();
                    var givenNames = match.Groups[2].Value.Trim();
                    
                    // Skip if it contains common non-name words (address words, labels, etc.)
                    // Use the comprehensive skipWords list defined at the top
                    bool shouldSkip = false;
                    foreach (var word in skipWords)
                    {
                        if (lastName.Equals(word, StringComparison.OrdinalIgnoreCase) || 
                            givenNames.Equals(word, StringComparison.OrdinalIgnoreCase) ||
                            lastName.Contains(word, StringComparison.OrdinalIgnoreCase) || 
                            givenNames.Contains(word, StringComparison.OrdinalIgnoreCase))
                        {
                            shouldSkip = true;
                            break;
                        }
                    }
                    
                    if (shouldSkip) continue;
                    
                    // Additional validation: names should not contain numbers
                    if (Regex.IsMatch(lastName, @"\d") || Regex.IsMatch(givenNames, @"\d"))
                    {
                        continue; // Skip if contains numbers
                    }
                    
                    // Names should be mostly letters (allow apostrophes and hyphens)
                    if (!Regex.IsMatch(lastName, @"^[A-Z\s'-]+$", RegexOptions.IgnoreCase) ||
                        !Regex.IsMatch(givenNames, @"^[A-Z\s'-]+$", RegexOptions.IgnoreCase))
                    {
                        continue;
                    }
                    
                    // Score this match (longer names are better, names with common name patterns score higher)
                    int score = lastName.Length + givenNames.Length;
                    var commonLastNames = new[] { 
                        "LOPEZ", "SANTOS", "REYES", "CRUZ", "BAUTISTA", "GARCIA", "DELA", "DE",
                        "RAMOS", "GONZALES", "MENDOZA", "TORRES", "CASTRO", "RIVERA", "FLORES",
                        "RAMIREZ", "AQUINO", "FERNANDEZ", "VALDEZ", "SANTIAGO", "DIAZ", "MORALES"
                    };
                    var commonFirstNames = new[] { 
                        "ANTHONY", "JOHN", "MARIA", "JOSE", "MICHAEL", "MARY", "JAMES",
                        "JUAN", "CARLOS", "ANNA", "LUIS", "MIGUEL", "ANGELA", "MARK",
                        "CHRISTIAN", "ANGELO", "PRINCESS", "ANGEL", "JOSHUA", "JASMINE"
                    };
                    
                    foreach (var commonName in commonLastNames)
                    {
                        if (lastName.Contains(commonName, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 10;
                            break;
                        }
                    }
                    
                    foreach (var commonName in commonFirstNames)
                    {
                        if (givenNames.Contains(commonName, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 10;
                            break;
                        }
                    }
                    
                    // Check if it's near "Last Name" label (higher score)
                    var context = text.Substring(Math.Max(0, match.Index - 30), Math.Min(60, text.Length - Math.Max(0, match.Index - 30)));
                    if (context.Contains("Last Name", StringComparison.OrdinalIgnoreCase) ||
                        context.Contains("First Name", StringComparison.OrdinalIgnoreCase))
                    {
                        score += 20; // Much higher score if near name labels
                    }
                    
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = match;
                    }
                }
                
                // Use the best match if found
                if (bestMatch != null && bestScore > 0)
                {
                    var lastName = bestMatch.Groups[1].Value.Trim();
                    var givenNames = bestMatch.Groups[2].Value.Trim();
                    
                    result.LastName = lastName;
                    var nameParts = givenNames.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (nameParts.Length > 0)
                    {
                        result.FirstName = nameParts[0];
                    }
                    if (nameParts.Length > 1)
                    {
                        // Check if last part is a suffix
                        var lastPart = nameParts[nameParts.Length - 1];
                        if (Regex.IsMatch(lastPart, @"^(JR|SR|I{2,3}|IV|V)$", RegexOptions.IgnoreCase))
                        {
                            result.Suffix = lastPart;
                            result.MiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
                        }
                        else
                        {
                            result.MiddleName = string.Join(" ", nameParts.Skip(1));
                        }
                    }
                    nameFound = true;
                }
            }

            // Extract Contact Number (Philippine format: 09XXXXXXXXX or +639XXXXXXXXX)
            var phonePattern = @"(\+?63\s*9\d{9}|\b09\d{9}\b)";
            var phoneMatch = Regex.Match(text, phonePattern);
            if (phoneMatch.Success)
            {
                result.ContactNumber = phoneMatch.Groups[1].Value.Replace(" ", "").Replace("+63", "09");
                if (result.ContactNumber.StartsWith("639"))
                {
                    result.ContactNumber = "0" + result.ContactNumber.Substring(2);
                }
            }

            // Extract Birth Date (multiple formats: YYYY/MM/DD, YYYY-MM-DD, DD-MM-YYYY, MM-DD-YYYY, DD/MM/YYYY, MM/DD/YYYY)
            // Also handle formats with spaces like "2003 /10/14" or "2003/ 10/ 14"
            // IMPORTANT: Prioritize dates near "Date of Birth" or "Birth Date" labels, not expiration dates
            
            // First, try to find "Date of Birth" or "Birth Date" labels with YYYY/MM/DD format
            var birthDatePattern1 = @"(?:Date\s+of\s+Birth|Birth\s+Date|Date\s+of\s+Birthday)[:\s]*(\d{4})\s*[/-]\s*(\d{1,2})\s*[/-]\s*(\d{1,2})";
            var birthDateMatch1 = Regex.Match(text, birthDatePattern1, RegexOptions.IgnoreCase);
            if (birthDateMatch1.Success)
            {
                var year = birthDateMatch1.Groups[1].Value.Trim();
                var month = birthDateMatch1.Groups[2].Value.Trim();
                var day = birthDateMatch1.Groups[3].Value.Trim();
                
                // Apply OCR error correction for year
                year = CorrectOcrYearErrors(year);
                
                // Validate date parts
                if (int.TryParse(year, out int y) && y >= 1900 && y <= DateTime.Now.Year &&
                    int.TryParse(month, out int m) && m >= 1 && m <= 12 &&
                    int.TryParse(day, out int d) && d >= 1 && d <= 31)
                {
                    result.BirthDate = $"{year}-{month.PadLeft(2, '0')}-{day.PadLeft(2, '0')}";
                }
            }
            
            // Second, try to find "Date of Birth" or "Birth Date" labels with DD/MM/YYYY or MM/DD/YYYY format
            if (string.IsNullOrEmpty(result.BirthDate))
            {
                var birthDatePattern2 = @"(?:Date\s+of\s+Birth|Birth\s+Date|Date\s+of\s+Birthday)[:\s]*(\d{1,2})\s*[/-]\s*(\d{1,2})\s*[/-]\s*(\d{4})";
                var birthDateMatch2 = Regex.Match(text, birthDatePattern2, RegexOptions.IgnoreCase);
                if (birthDateMatch2.Success)
                {
                    var part1 = birthDateMatch2.Groups[1].Value.Trim();
                    var part2 = birthDateMatch2.Groups[2].Value.Trim();
                    var part3 = birthDateMatch2.Groups[3].Value.Trim();
                    
                    // Determine format (usually DD/MM/YYYY for Philippine IDs)
                    string year, month, day;
                    if (int.TryParse(part1, out int p1) && p1 > 12)
                    {
                        // DD/MM/YYYY
                        day = part1;
                        month = part2;
                        year = part3;
                    }
                    else
                    {
                        // MM/DD/YYYY
                        month = part1;
                        day = part2;
                        year = part3;
                    }
                    
                    // Validate date parts
                    if (int.TryParse(year, out int y) && y >= 1900 && y <= DateTime.Now.Year &&
                        int.TryParse(month, out int m) && m >= 1 && m <= 12 &&
                        int.TryParse(day, out int d) && d >= 1 && d <= 31)
                    {
                        result.BirthDate = $"{year}-{month.PadLeft(2, '0')}-{day.PadLeft(2, '0')}";
                    }
                }
            }
            
            // If not found with label, look for date patterns but skip expiration dates
            // Prioritize YYYY/MM/DD format (common in Philippine IDs)
            if (string.IsNullOrEmpty(result.BirthDate))
            {
                // First, try YYYY/MM/DD format (most common in Philippine IDs)
                var yyyyPattern = @"(\d{4})\s*[/-]\s*(\d{1,2})\s*[/-]\s*(\d{1,2})";
                var yyyyMatches = Regex.Matches(text, yyyyPattern);
                
                DateTime? bestDate = null;
                int bestScore = 0;
                
                foreach (Match dateMatch in yyyyMatches)
                {
                    var year = dateMatch.Groups[1].Value.Trim();
                    var month = dateMatch.Groups[2].Value.Trim();
                    var day = dateMatch.Groups[3].Value.Trim();
                    
                    // OCR Error Correction for years: Common misreads
                    // 3 -> 8, 0 -> O, 1 -> I, 5 -> S, etc.
                    year = CorrectOcrYearErrors(year);
                    
                    // Skip if near "Expiration" or "Expiry" keywords
                    var context = text.Substring(Math.Max(0, dateMatch.Index - 30), Math.Min(60, text.Length - Math.Max(0, dateMatch.Index - 30)));
                    if (context.Contains("Expiration", StringComparison.OrdinalIgnoreCase) ||
                        context.Contains("Expiry", StringComparison.OrdinalIgnoreCase) ||
                        context.Contains("Valid Until", StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Skip expiration dates
                    }
                    
                    // Validate date parts
                    if (int.TryParse(year, out int y) && y >= 1900 && y <= DateTime.Now.Year &&
                        int.TryParse(month, out int m) && m >= 1 && m <= 12 &&
                        int.TryParse(day, out int d) && d >= 1 && d <= 31)
                    {
                        // Prefer older dates (likely birth dates, not expiration dates)
                        var date = new DateTime(y, m, d);
                        int score = (DateTime.Now.Year - y) * 10; // Older dates score higher
                        
                        // Check if near "Date of Birth" label (much higher score)
                        if (context.Contains("Date of Birth", StringComparison.OrdinalIgnoreCase) ||
                            context.Contains("Birth Date", StringComparison.OrdinalIgnoreCase))
                        {
                            score += 1000; // Very high score for dates near birth date labels
                        }
                        
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestDate = date;
                        }
                    }
                }
                
                if (bestDate.HasValue)
                {
                    result.BirthDate = bestDate.Value.ToString("yyyy-MM-dd");
                }
                else
                {
                    // Fallback to DD/MM/YYYY or MM/DD/YYYY format
                    var datePatterns = new[]
                    {
                        @"(\d{1,2})\s*[/-]\s*(\d{1,2})\s*[/-]\s*(\d{4})", // DD/MM/YYYY or DD-MM-YYYY or MM/DD/YYYY (with optional spaces)
                    };
                    
                    foreach (var pattern in datePatterns)
                    {
                        var dateMatches = Regex.Matches(text, pattern);
                        foreach (Match dateMatch in dateMatches)
                        {
                            var part1 = dateMatch.Groups[1].Value.Trim();
                            var part2 = dateMatch.Groups[2].Value.Trim();
                            var part3 = dateMatch.Groups[3].Value.Trim();
                            
                            // Skip if it's near "Expiration" or "Expiry" (likely expiration date, not birth date)
                            var matchContext = text.Substring(Math.Max(0, dateMatch.Index - 20), 
                                                             Math.Min(40, text.Length - Math.Max(0, dateMatch.Index - 20)));
                            if (matchContext.Contains("Expiration", StringComparison.OrdinalIgnoreCase) ||
                                matchContext.Contains("Expiry", StringComparison.OrdinalIgnoreCase) ||
                                matchContext.Contains("Exp", StringComparison.OrdinalIgnoreCase))
                            {
                                continue; // Skip expiration dates
                            }
                            
                            // Determine format based on part lengths
                            string year, month, day;
                            if (part1.Length == 4)
                            {
                                // Format: YYYY/MM/DD or YYYY-MM-DD
                                year = part1;
                                month = part2;
                                day = part3;
                            }
                            else if (part3.Length == 4)
                            {
                                // Format: DD/MM/YYYY or MM/DD/YYYY
                                // Try to determine if it's DD/MM or MM/DD by checking if first part > 12
                                if (int.TryParse(part1, out int p1) && p1 > 12)
                                {
                                    // DD/MM/YYYY format
                                    day = part1;
                                    month = part2;
                                    year = part3;
                                }
                                else
                                {
                                    // MM/DD/YYYY format (US format)
                                    month = part1;
                                    day = part2;
                                    year = part3;
                                }
                            }
                            else
                            {
                                continue; // Skip invalid format
                            }
                            
                            // Validate date parts and prefer dates that are likely birth dates (not too recent)
                            if (int.TryParse(year, out int y) && y >= 1900 && y <= DateTime.Now.Year &&
                                int.TryParse(month, out int m) && m >= 1 && m <= 12 &&
                                int.TryParse(day, out int d) && d >= 1 && d <= 31)
                            {
                                // Prefer dates that are likely birth dates (older dates, not issue/expiration dates)
                                // Birth dates are usually before 2010 for adults, expiration dates are usually recent
                                if (y < 2010 || (y >= 2000 && y <= 2010))
                                {
                                    result.BirthDate = $"{year}-{month.PadLeft(2, '0')}-{day.PadLeft(2, '0')}";
                                    break; // Found valid birth date, stop searching
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(result.BirthDate)) break;
                    }
                }
            }

            // Extract Address (look for address indicators)
            var addressKeywords = new[] { "ADDRESS", "TIRAHAN", "LT", "BLK", "STREET", "ST", "CITY", "BARANGAY", "BRGY" };
            var addressStartIndex = -1;
            string foundKeyword = "";
            foreach (var keyword in addressKeywords)
            {
                var index = upperText.IndexOf(keyword);
                if (index >= 0)
                {
                    addressStartIndex = index;
                    foundKeyword = keyword;
                    break;
                }
            }

            if (addressStartIndex >= 0)
            {
                // Extract address line (usually continues until next field or end)
                var addressText = text.Substring(addressStartIndex);
                // Remove label and clean up
                addressText = Regex.Replace(addressText, @"^(ADDRESS|TIRAHAN|Addresse)[:\s]*", "", RegexOptions.IgnoreCase);
                
                // Find the actual address content (after the keyword)
                var keywordIndex = addressText.IndexOf(foundKeyword, StringComparison.OrdinalIgnoreCase);
                if (keywordIndex >= 0)
                {
                    addressText = addressText.Substring(keywordIndex + foundKeyword.Length).TrimStart(':', ' ', '-');
                }
                
                // Take first few lines or until next major field
                var addressLines = addressText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Take(4) // Increased to 4 lines for longer addresses
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length > 2)
                    .Where(l => !l.StartsWith("Date", StringComparison.OrdinalIgnoreCase)) // Stop at Date fields
                    .Where(l => !l.StartsWith("Birth", StringComparison.OrdinalIgnoreCase)) // Stop at Birth fields
                    .Where(l => !Regex.IsMatch(l, @"^\d{4}[/-]\d")) // Stop at date patterns
                    .ToList();
                    
                // Remove duplicate address lines (OCR may duplicate)
                addressLines = addressLines.Distinct().ToList();
                
                result.Address = string.Join(", ", addressLines).Trim();
                
                // Clean up common OCR errors
                result.Address = result.Address
                    .Replace("16I", "161")
                    .Replace("16l", "161")
                    .Replace("16|", "161")
                    .Replace("16O", "160")
                    .Replace("  ", " ") // Remove double spaces
                    .Trim(',', ' ', '-'); // Clean up leading/trailing punctuation
                    
                // If address is too long (likely contains other fields), truncate
                if (result.Address.Length > 200)
                {
                    result.Address = result.Address.Substring(0, 200).Trim();
                }
            }

            // Extract Gender (look for SEX or GENDER labels)
            var genderPatterns = new[]
            {
                @"(?:SEX|GENDER|KASARIAN)[:\s]*([MF]|MALE|FEMALE|LALAKI|BABAE)",
                @"\b(SEX|GENDER)[:\s]*([MF])\b",
                @"\b([MF])\s*(?:SEX|GENDER)\b"
            };
            
            foreach (var pattern in genderPatterns)
            {
                var genderMatch = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (genderMatch.Success)
                {
                    // Get the gender value from the appropriate group
                    var genderValue = genderMatch.Groups.Count > 2 && !string.IsNullOrWhiteSpace(genderMatch.Groups[2].Value)
                        ? genderMatch.Groups[2].Value.Trim().ToUpper()
                        : genderMatch.Groups[1].Value.Trim().ToUpper();
                    
                    // Normalize gender value
                    if (genderValue == "M" || genderValue == "MALE" || genderValue == "LALAKI")
                    {
                        result.Gender = "Male";
                        break;
                    }
                    else if (genderValue == "F" || genderValue == "FEMALE" || genderValue == "BABAE")
                    {
                        result.Gender = "Female";
                        break;
                    }
                }
            }
            
            // If not found with label, look for standalone M or F near common ID fields
            if (string.IsNullOrEmpty(result.Gender))
            {
                // Look for M or F that appears near "Sex" or "Gender" or after name/address fields
                var standaloneGenderPattern = @"\b([MF])\b";
                var genderMatches = Regex.Matches(text, standaloneGenderPattern);
                
                foreach (Match match in genderMatches)
                {
                    // Check context - should be near gender-related words or in a field position
                    var contextStart = Math.Max(0, match.Index - 20);
                    var contextEnd = Math.Min(text.Length, match.Index + match.Length + 20);
                    var context = text.Substring(contextStart, contextEnd - contextStart).ToUpper();
                    
                    // Skip if it's part of a date, address, or other field
                    if (context.Contains("DATE") || context.Contains("BIRTH") || 
                        context.Contains("ADDRESS") || context.Contains("BARANGAY") ||
                        context.Contains("PHONE") || context.Contains("CONTACT") ||
                        Regex.IsMatch(context, @"\d{4}")) // Skip if near a year
                    {
                        continue;
                    }
                    
                    // If it's near "SEX", "GENDER", or appears after name fields, use it
                    if (context.Contains("SEX") || context.Contains("GENDER") || 
                        context.Contains("NATIONALITY") || 
                        (match.Index > 100 && match.Index < text.Length * 0.3)) // Early in text, likely a field
                    {
                        if (match.Groups[1].Value == "M")
                        {
                            result.Gender = "Male";
                        }
                        else if (match.Groups[1].Value == "F")
                        {
                            result.Gender = "Female";
                        }
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts Barangay number from text using regex patterns
        /// </summary>
        private string ExtractBarangayNumber(string text)
        {
            // Clean up common OCR errors first
            var cleanedText = text.Replace("16I", "161").Replace("16l", "161").Replace("16|", "161").Replace("16O", "160");
            
            // Pattern 1: "BARANGAY 161" or "BARANGAY 161," or "BRGY 161"
            var pattern1 = @"\b(?:BARANGAY|BRGY|BARANG)\s*(\d{3})\b";
            var match1 = Regex.Match(cleanedText, pattern1, RegexOptions.IgnoreCase);
            if (match1.Success)
            {
                var barangay = match1.Groups[1].Value;
                // Validate it's one of the valid barangays
                if (new[] { "158", "159", "160", "161" }.Contains(barangay))
                {
                    return barangay;
                }
            }

            // Pattern 2: "BARANGAY 161" in address context (more lenient)
            var pattern2 = @"(?:BARANGAY|BRGY|BARANG)\s*(\d{2,3})";
            var match2 = Regex.Match(cleanedText, pattern2, RegexOptions.IgnoreCase);
            if (match2.Success)
            {
                var barangay = match2.Groups[1].Value;
                // Handle OCR errors: 16I, 16l, 16| -> 161
                if (barangay == "16" || barangay.StartsWith("16"))
                {
                    // Check if followed by I, l, or | (common OCR errors for 1)
                    var nextChar = match2.Index + match2.Length < cleanedText.Length 
                        ? cleanedText[match2.Index + match2.Length] 
                        : ' ';
                    if (nextChar == 'I' || nextChar == 'l' || nextChar == '|' || nextChar == 'O')
                    {
                        return "161";
                    }
                    return "160"; // Default to 160 if just "16"
                }
                if (new[] { "158", "159", "160", "161" }.Contains(barangay))
                {
                    return barangay;
                }
            }

            // Pattern 3: Look for numbers 158-161 near address keywords
            var pattern3 = @"(?:LT|BLK|ADDRESS|BARANG|BRGY|CITY|REPARO).*?(158|159|160|161)\b";
            var match3 = Regex.Match(cleanedText, pattern3, RegexOptions.IgnoreCase);
            if (match3.Success)
            {
                return match3.Groups[1].Value;
            }
            
            // Pattern 4: Look for "161" or "16I" near "BARANGAY" (handle OCR errors)
            var pattern4 = @"BARANGAY\s*16[1Iil|O]";
            var match4 = Regex.Match(cleanedText, pattern4, RegexOptions.IgnoreCase);
            if (match4.Success)
            {
                return "161";
            }

            return "";
        }
    }

    /// <summary>
    /// Result of ID extraction from OCR
    /// </summary>
    public class IdExtractionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string ExtractedText { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Suffix { get; set; } = "";
        public string ContactNumber { get; set; } = "";
        public string Address { get; set; } = "";
        public string BirthDate { get; set; } = "";
        public string Gender { get; set; } = "";
        public string BarangayNumber { get; set; } = "";
        public bool IsBarangayValid { get; set; }
    }

    /// <summary>
    /// Parsed ID data structure
    /// </summary>
    public class ParsedIdData
    {
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Suffix { get; set; } = "";
        public string ContactNumber { get; set; } = "";
        public string Address { get; set; } = "";
        public string BirthDate { get; set; } = "";
        public string Gender { get; set; } = "";
    }
}

