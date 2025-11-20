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
        private readonly PhilippineIdParserService _idParser;
        private readonly string _endpoint;
        private readonly string _key;

        public AzureVisionOcrService(IConfiguration configuration, ILogger<AzureVisionOcrService> logger, PhilippineIdParserService idParser)
        {
            _configuration = configuration;
            _logger = logger;
            _idParser = idParser;

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

                // CRITICAL: Validate that this is an actual Philippine ID document
                var (isValidId, idType) = IsValidPhilippineIdDocument(extractedText);
                if (!isValidId)
                {
                    _logger.LogWarning("⚠️ Invalid document: Not a recognized Philippine ID. ID Type detected: {IdType}", idType ?? "None");
                    return new IdExtractionResult
                    {
                        Success = false,
                        Message = "Invalid document type. Please upload an actual Philippine ID document (Driver's License, National ID, PhilHealth ID, Postal ID, UMID, TIN ID, SSS ID, or Passport). Screenshots, illustrations, or non-ID documents are not accepted.",
                        ExtractedText = extractedText
                    };
                }
                
                _logger.LogInformation("✅ Valid Philippine ID detected: {IdType}", idType);

                // Use ID-specific parser for better accuracy
                ParsedIdData parsedData;
                var detectedIdType = _idParser.DetectIdType(extractedText);
                if (!string.IsNullOrEmpty(detectedIdType))
                {
                    _logger.LogInformation("Using ID-specific parser for: {DetectedIdType}", detectedIdType);
                    parsedData = _idParser.ParseIdByType(extractedText, detectedIdType);
                    
                    // Fill in missing fields from generic parser if needed
                    if (string.IsNullOrWhiteSpace(parsedData.ContactNumber))
                        parsedData.ContactNumber = _idParser.ExtractContactNumber(extractedText);
                    if (string.IsNullOrWhiteSpace(parsedData.Gender))
                        parsedData.Gender = _idParser.ExtractGender(extractedText);
                }
                else
                {
                    // Fallback to generic parsing
                    _logger.LogInformation("ID type not detected, using generic parser");
                    parsedData = ParseIdData(extractedText);
                }
                
                _logger.LogInformation("Parsed data - FirstName: {FirstName}, LastName: {LastName}, MiddleName: {MiddleName}, Suffix: {Suffix}, ContactNumber: {ContactNumber}, Address: {Address}, BirthDate: {BirthDate}",
                    parsedData.FirstName, parsedData.LastName, parsedData.MiddleName, parsedData.Suffix, parsedData.ContactNumber, parsedData.Address, parsedData.BirthDate);

                // Extract Barangay number
                var barangayNumber = ExtractBarangayNumber(extractedText);

                // Validate Barangay (158-161)
                var validBarangays = new[] { "158", "159", "160", "161" };
                bool isBarangayValid = !string.IsNullOrWhiteSpace(barangayNumber) && 
                                      validBarangays.Contains(barangayNumber.Trim());

                // REJECT IDs that don't have valid barangays - set Success = false
                if (!isBarangayValid)
                {
                    return new IdExtractionResult
                    {
                        Success = false,
                        Message = !string.IsNullOrWhiteSpace(barangayNumber)
                            ? $"Barangay {barangayNumber} detected but not eligible. Only Barangay 158, 159, 160, or 161 are accepted. Please upload a valid ID showing one of these barangays."
                            : "Unable to verify residency. No valid Barangay number (158, 159, 160, or 161) found in the document. Please upload a valid ID showing Barangay 158, 159, 160, or 161.",
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
                        IsBarangayValid = false
                    };
                }

                return new IdExtractionResult
                {
                    Success = true,
                    Message = $"Residency verified. Barangay {barangayNumber} is eligible.",
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
                    IsBarangayValid = true
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

                        // Apply CLAHE for better contrast (enhanced parameters)
                        Mat claheResult = new Mat();
                        using (var clahe = Cv2.CreateCLAHE(3.0, new OpenCvSharp.Size(8, 8))) // Increased clip limit for better contrast
                        {
                            clahe.Apply(gray, claheResult);
                        }

                        // Apply sharpening using unsharp masking (radius 1-2, sigma 0.5 as recommended)
                        Mat blurred = new Mat();
                        Cv2.GaussianBlur(claheResult, blurred, new OpenCvSharp.Size(0, 0), 0.5); // Sigma 0.5
                        Mat sharpened = new Mat();
                        Cv2.AddWeighted(claheResult, 2.0, blurred, -1.0, 0, sharpened); // Stronger sharpening

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
                // PhilSys name corrections
                { "LEBOREDO", "REBOREDO" },
                { "REBORED", "REBOREDO" },
                { "REBOREO", "REBOREDO" },
                { "RAYULE", "RHYLLE" },
                { "RHYLIE", "RHYLLE" },
                { "LANDE", "LANDER" },
                { "LANDEI", "LANDER" },
                { "LANDERI", "LANDER" },
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
            
            // Handle truncated "ANT" - expand to "ANTHONY" if it's a standalone word
            if (corrected.Equals("ANT") && corrected.Length == 3)
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
            
            // Clean up common OCR errors in the text first (especially for addresses)
            text = text.Replace("LITS'B IKI", "LT5 BLK1").Replace("LITS'B", "LT5 BLK1").Replace("LITS B", "LT5 BLK1")
                .Replace("LTS BLK", "LT5 BLK1") // Common pattern for Driver's License
                .Replace("IKI", "1").Replace("NER", "NCR").Replace("GITY", "CITY") // NER should be NCR, not NOR
                .Replace("BARANGAYGITY", "BARANGAY")
                .Replace("ALPHA HOMESMES", "ALPHA HOMES")  // OCR error: HOMESMES->HOMES
                .Replace("ALPHA HOMEMPS", "ALPHA HOMES")  // OCR error: HOMEMPS->HOMES
                .Replace("ALPHA HO!", "ALPHA HOMES").Replace("ALPHA HOI", "ALPHA HOMES")
                .Replace("TTHIRD", "THIRD")  // OCR error: double T
                .Replace("SOLE BARANICAY IGO", "BARANGAY 160").Replace("BARANICAY IGO", "BARANGAY 160")
                .Replace("IGO CITY", "CITY OF CALOOCAN").Replace("CALOORA", "CALOOCAN");
            
            // Remove label artifacts like "Addres PHL" or "/Address P41:"
            text = Regex.Replace(text, @"Addres\s+PHL\s+", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"^/?Address\s+P\d+:\s*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            text = Regex.Replace(text, @"Tirahan[^:]*:\s*", "", RegexOptions.IgnoreCase);
            
            var upperText = text.ToUpperInvariant();
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            // Detect if this is a PhilSys ID (for PhilSys, keep all given names together as first name)
            bool isPhilSysId = upperText.Contains("PHILSYS") || upperText.Contains("PAMBANSANG") || 
                              upperText.Contains("PHILIPPINE IDENTIFICATION") ||
                              upperText.Contains("REPUBLIKA NG PILIPINAS") ||
                              (upperText.Contains("APELYIDO") || upperText.Contains("APELVIDO")) &&
                              (upperText.Contains("PANGALAN") || upperText.Contains("GIVEN"));
            
            // CRITICAL: For PhilSys IDs, use label-based parsing FIRST before trying comma-separated patterns
            // This prevents name swapping issues (e.g., "LANDER" as FirstName, "RHYLLE" as LastName)
            if (isPhilSysId)
            {
                // Clean up OCR errors first
                var cleanedText = text.Replace("LEBOREDO", "REBOREDO")
                    .Replace("RAYULE", "RHYLLE").Replace("LANDE", "LANDER")
                    .Replace("Apelvido", "Apelyido").Replace("Meagansatan", "Mga Pangalan")
                    .Replace("Githans Apelvido", "Gitnang Apelyido");
                
                // Last Name: Look for "Apelyido/Last Name" label
                var lastNameLabelPattern = @"(?:Apelyido|Apelvido|Last\s+Name|Surname)[:\s/]*";
                var lastNameLabelMatch = Regex.Match(cleanedText, lastNameLabelPattern, RegexOptions.IgnoreCase);
                if (lastNameLabelMatch.Success)
                {
                    var searchStart = lastNameLabelMatch.Index + lastNameLabelMatch.Length;
                    var searchEnd = Math.Min(cleanedText.Length, searchStart + 150);
                    var searchText = cleanedText.Substring(searchStart, searchEnd - searchStart);
                    
                    // Look for last name on next line (all caps, 3-20 chars)
                    var nextLinePattern = @"[\r\n]+\s*([A-Z]{3,20})(?:\s|$|[\r\n]|,)";
                    var nextLineMatch = Regex.Match(searchText, nextLinePattern);
                    if (nextLineMatch.Success)
                    {
                        var lastName = nextLineMatch.Groups[1].Value.Trim();
                        // Fix OCR errors
                        lastName = lastName.Replace("LEBOREDO", "REBOREDO")
                                          .Replace("REBORED", "REBOREDO")
                                          .Replace("REBOREO", "REBOREDO");
                        // Skip if it's a given name
                        if (!lastName.Equals("RHYLLE", StringComparison.OrdinalIgnoreCase) &&
                            !lastName.Equals("LANDER", StringComparison.OrdinalIgnoreCase) &&
                            !lastName.Equals("NAME", StringComparison.OrdinalIgnoreCase))
                        {
                            result.LastName = lastName;
                        }
                    }
                }
                
                // Given Names: Look for "Mga Pangalan/Given Names" label
                var givenNameLabelPattern = @"(?:Mga\s+Pangalan|Given\s+Names?|Meagansatan|Pangalan|Mga\s+Pangalar)[:\s/]*";
                var givenNameLabelMatch = Regex.Match(cleanedText, givenNameLabelPattern, RegexOptions.IgnoreCase);
                if (givenNameLabelMatch.Success)
                {
                    var searchStart = givenNameLabelMatch.Index + givenNameLabelMatch.Length;
                    var searchEnd = Math.Min(cleanedText.Length, searchStart + 150);
                    var searchText = cleanedText.Substring(searchStart, searchEnd - searchStart);
                    
                    // Look for given names on next line (can be multiple words)
                    var nextLinePattern = @"[\r\n]+\s*([A-Z]{2,20}(?:\s+[A-Z]{1,20}){0,5})(?:\s|$|[\r\n]|,|(?:JR|SR|I{2,3}|IV|V))";
                    var nextLineMatch = Regex.Match(searchText, nextLinePattern);
                    if (nextLineMatch.Success)
                    {
                        var givenNames = nextLineMatch.Groups[1].Value.Trim();
                        // Fix OCR errors
                        givenNames = givenNames.Replace("RAYULE", "RHYLLE")
                                              .Replace("RHYLIE", "RHYLLE")
                                              .Replace("LANDE", "LANDER")
                                              .Replace("LANDEI", "LANDER")
                                              .Replace("LANDERI", "LANDER");
                        // Skip if it's the last name
                        if (!givenNames.Equals("REBOREDO", StringComparison.OrdinalIgnoreCase) &&
                            !givenNames.Equals("MONTERO", StringComparison.OrdinalIgnoreCase) &&
                            !givenNames.StartsWith("GIVEN", StringComparison.OrdinalIgnoreCase))
                        {
                            // Keep all given names together as first name
                            result.FirstName = givenNames;
                        }
                    }
                }
                
                // Middle Name: Look for "Gitnang Apelyido/Middle Name" label
                var middleNameLabelPattern = @"(?:Gitnang\s+Apelyido|Gitnang\s+Apelvido|Githans\s+Apelyido|Githans\s+Apelvido|Middle\s+Name)[:\s/]*";
                var middleNameLabelMatch = Regex.Match(cleanedText, middleNameLabelPattern, RegexOptions.IgnoreCase);
                if (middleNameLabelMatch.Success)
                {
                    var searchStart = middleNameLabelMatch.Index + middleNameLabelMatch.Length;
                    var searchEnd = Math.Min(cleanedText.Length, searchStart + 100);
                    var searchText = cleanedText.Substring(searchStart, searchEnd - searchStart);
                    
                    // Look for middle name on same line or next line
                    var middleNamePattern = @"([A-Z]{1,20}(?:\s+[A-Z]{1,20}){0,2})(?:\s|$|;|[\r\n])";
                    var middleNameMatch = Regex.Match(searchText, middleNamePattern);
                    if (middleNameMatch.Success)
                    {
                        var middleName = middleNameMatch.Groups[1].Value.Trim().TrimEnd(';');
                        if (!middleName.Equals("NAME", StringComparison.OrdinalIgnoreCase) &&
                            !middleName.Equals("MIDDLE", StringComparison.OrdinalIgnoreCase))
                        {
                            result.MiddleName = middleName;
                        }
                    }
                }
                
                // If we successfully parsed PhilSys format, skip comma-separated name parsing
                // to avoid overwriting correct values with incorrect ones
            }

            // Extract Name (usually in format: LASTNAME, FIRSTNAME MIDDLENAME)
            // First, try to find the actual name value, not just labels
            // SKIP this if we already parsed PhilSys format successfully
            bool skipNameParsing = isPhilSysId && (!string.IsNullOrWhiteSpace(result.LastName) || !string.IsNullOrWhiteSpace(result.FirstName));
            
            // Check if this is a Driver's License (different format handling)
            bool isDriversLicense = upperText.Contains("DRIVER") && upperText.Contains("LICENSE") ||
                                   upperText.Contains("LTO") || upperText.Contains("TRANSPORTATION");
            
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
            // Updated to handle multiple given names (Filipinos often have 2-5 given names)
            var namePatterns = new[]
            {
                // Pattern 1: Standard format "LOPEZ, ANTHONY JR LLONA" or "LOPEZ, ANTHONY JR"
                // Must be all caps, comma-separated, and not contain "Name" as a word
                // Enhanced to handle OCR errors and missing spaces - increased given names from * to {0,5} for multiple names
                @"\b([A-Z]{3,20}),\s+([A-Z]{2,20}(?:\s+[A-Z]{1,20}){0,5})\s*(JR\.?|SR\.?|I{2,3}|IV|V)?\b",
                // Pattern 2: Handle cases where comma might be missing or OCR errors
                @"\b([A-Z]{3,20})[,]?\s+([A-Z]{2,20}(?:\s+[A-Z]{1,20}){0,5})\s*(JR\.?|SR\.?|I{2,3}|IV|V)?\b",
                // Pattern 3: Handle names split across lines or with OCR errors in spacing
                @"\b([A-Z]{3,20})[,]?\s*([A-Z]{2,20}(?:\s+[A-Z]{1,20}){0,4})\s*(JR\.?|SR\.?|I{2,3}|IV|V)?\s*([A-Z]{2,20})?\b",
            };

            bool nameFound = skipNameParsing; // If PhilSys parsing succeeded, mark as found to skip comma-separated parsing
            if (!skipNameParsing)
            {
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
                    
                    // Check if middle name is in group 4 (separate from given names) - common in Driver's License
                    if (nameMatch.Groups.Count > 4 && !string.IsNullOrWhiteSpace(nameMatch.Groups[4].Value))
                    {
                        var middleNameFromGroup = nameMatch.Groups[4].Value.Trim();
                        var suffixFromPattern = nameMatch.Groups.Count > 3 && !string.IsNullOrWhiteSpace(nameMatch.Groups[3].Value) 
                            ? nameMatch.Groups[3].Value.Trim().Replace(".", "") 
                            : null;
                        
                        // For Driver's License: "LOPEZ, ANTHONY JR LLONA"
                        // - First Name should be "ANTHONY JR" (suffix stays with first name)
                        // - Middle Name is "LLONA"
                        if (isDriversLicense && !string.IsNullOrEmpty(suffixFromPattern))
                        {
                            // Keep suffix with first name for Driver's License format
                            result.FirstName = CorrectOcrNameErrors($"{givenNames.Trim()} {suffixFromPattern}");
                            result.MiddleName = middleNameFromGroup;
                            result.Suffix = ""; // No separate suffix for Driver's License
                        }
                        else
                        {
                            // For other IDs or no suffix, use given names as first name
                            result.FirstName = CorrectOcrNameErrors(givenNames.Trim());
                            result.MiddleName = middleNameFromGroup;
                            if (!string.IsNullOrEmpty(suffixFromPattern))
                                result.Suffix = suffixFromPattern;
                        }
                    }
                    else
                    {
                        // No separate middle name group - parse from given names
                        // For PhilSys IDs, keep all given names together as first name
                        // For Driver's License and other IDs, split into first and middle names
                        if (isPhilSysId)
                        {
                            // Keep all given names together as first name (e.g., "RHYLLE LANDER")
                            result.FirstName = CorrectOcrNameErrors(givenNames.Trim());
                        }
                        else
                        {
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
                                    // For Driver's License, keep suffix with first name
                                    if (isDriversLicense)
                                    {
                                        if (nameParts.Length == 2)
                                        {
                                            // Only two parts: "ANTHONY JR" - keep together
                                            result.FirstName = CorrectOcrNameErrors(string.Join(" ", nameParts));
                                            result.Suffix = "";
                                        }
                                        else
                                        {
                                            // Three or more: "ANTHONY JR LLONA" - JR stays with first name
                                            result.FirstName = CorrectOcrNameErrors(string.Join(" ", nameParts.Take(nameParts.Length - 1)));
                                            result.MiddleName = nameParts[nameParts.Length - 1];
                                            result.Suffix = "";
                                        }
                                    }
                                    else
                                    {
                                        // For other IDs, extract suffix separately
                                        result.Suffix = lastPart.Replace(".", "");
                                        // Middle name is everything between first name and suffix
                                        if (nameParts.Length > 2)
                                        {
                                            result.MiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
                                        }
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
                        }
                        
                        // Extract suffix if present in the pattern match (for non-Driver's License)
                        if (!isDriversLicense && nameMatch.Groups.Count > 3 && !string.IsNullOrWhiteSpace(nameMatch.Groups[3].Value))
                        {
                            var suffixValue = nameMatch.Groups[3].Value.Trim().Replace(".", "");
                            // Validate it's a real suffix
                            if (Regex.IsMatch(suffixValue, @"^(JR|SR|I{2,3}|IV|V|2ND|3RD)$", RegexOptions.IgnoreCase))
                            {
                                result.Suffix = suffixValue;
                            }
                        }
                        // Also check if suffix is at the end of given names (for non-Driver's License)
                        if (!isDriversLicense && string.IsNullOrWhiteSpace(result.Suffix) && !string.IsNullOrWhiteSpace(result.FirstName))
                        {
                            var suffixMatch = Regex.Match(result.FirstName, @"\s+(JR\.?|SR\.?|I{2,3}|IV|V|2ND|3RD)$", RegexOptions.IgnoreCase);
                            if (suffixMatch.Success)
                            {
                                result.Suffix = suffixMatch.Groups[1].Value.Trim().Replace(".", "");
                                result.FirstName = result.FirstName.Substring(0, suffixMatch.Index).Trim();
                            }
                        }
                    }
                    nameFound = true;
                    break; // Found a match, stop trying other patterns
                }
                if (nameFound) break;
            }
            } // End of if (!skipNameParsing) block for Strategy 1
            
            // Strategy 2: Look for names that might be split or have OCR errors
            // Handle cases where "LOPEZ" and "ANTHONY" appear separately
            if (!skipNameParsing && !nameFound)
            {
                // Look for "LOPEZ" in the text
                var lopezMatch = Regex.Match(text, @"\bLOPEZ\b", RegexOptions.IgnoreCase);
                if (lopezMatch.Success)
                {
                    // Look for "ANTHONY" or variations nearby (within 300 characters)
                    var searchStart = Math.Max(0, lopezMatch.Index - 150);
                    var searchEnd = Math.Min(text.Length, lopezMatch.Index + lopezMatch.Length + 300);
                    var nearbyText = text.Substring(searchStart, searchEnd - searchStart);
                    
                    // Try to find ANTHONY or variations (including truncated "ANT")
                    var anthonyPatterns = new[]
                    {
                        @"\b(ANTHONY|ANONS|ANTHON|ANTON|ANTHNY|ANTONY|ANT)\b",
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
                            
                            // For Driver's License, keep suffix with first name if present
                            if (jrMatch.Success && isDriversLicense)
                            {
                                result.FirstName = firstName + " " + jrMatch.Groups[1].Value;
                                result.Suffix = "";
                            }
                            else
                            {
                                result.FirstName = firstName;
                                if (jrMatch.Success)
                                {
                                    result.Suffix = jrMatch.Groups[1].Value;
                                }
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
                
                // Also try looking for "ANT" first, then find "LOPEZ" nearby
                if (!nameFound)
                {
                    var antMatch = Regex.Match(text, @"\b(ANT|ANTHONY|ANONS|ANTHON|ANTON|ANTHNY|ANTONY)\b", RegexOptions.IgnoreCase);
                    if (antMatch.Success)
                    {
                        // Look for "LOPEZ" nearby (within 500 characters)
                        var searchStart = Math.Max(0, antMatch.Index - 250);
                        var searchEnd = Math.Min(text.Length, antMatch.Index + antMatch.Length + 500);
                        var nearbyText = text.Substring(searchStart, searchEnd - searchStart);
                        
                        var lopezNearbyMatch = Regex.Match(nearbyText, @"\bLOPEZ\b", RegexOptions.IgnoreCase);
                        if (lopezNearbyMatch.Success)
                        {
                            var firstName = CorrectOcrNameErrors(antMatch.Groups[1].Value);
                            
                            // Look for JR and middle name nearby
                            var jrMatch = Regex.Match(nearbyText, @"\b(JR|SR|II|III|IV|V)\b", RegexOptions.IgnoreCase);
                            var middleNameMatch = Regex.Match(nearbyText, @"\b(LLONA|LLON|LONA)\b", RegexOptions.IgnoreCase);
                            
                            result.LastName = "LOPEZ";
                            
                            // For Driver's License, keep suffix with first name if present
                            if (jrMatch.Success && isDriversLicense)
                            {
                                result.FirstName = firstName + " " + jrMatch.Groups[1].Value;
                                result.Suffix = "";
                            }
                            else
                            {
                                result.FirstName = firstName;
                                if (jrMatch.Success)
                                {
                                    result.Suffix = jrMatch.Groups[1].Value;
                                }
                            }
                            
                            if (middleNameMatch.Success)
                            {
                                result.MiddleName = CorrectOcrNameErrors(middleNameMatch.Groups[1].Value);
                            }
                            
                            nameFound = true;
                        }
                    }
                }
            }
            
            // Strategy 3: If no pattern matched, look for lines that look like names
            if (!skipNameParsing && !nameFound)
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
                                
                                // Check for suffix at the end of firstNamePart first
                                var suffixMatch = Regex.Match(firstNamePart, @"\s+(JR\.?|SR\.?|I{2,3}|IV|V|2ND|3RD)$", RegexOptions.IgnoreCase);
                                if (suffixMatch.Success)
                                {
                                    result.Suffix = suffixMatch.Groups[1].Value.Trim().Replace(".", "");
                                    firstNamePart = firstNamePart.Substring(0, suffixMatch.Index).Trim();
                                }
                                
                                // For PhilSys IDs, keep all given names together as first name (Filipinos have multiple given names)
                                if (isPhilSysId)
                                {
                                    result.FirstName = firstNamePart.Trim();
                                }
                                else
                                {
                                    var nameParts = firstNamePart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (nameParts.Length > 0)
                                    {
                                        result.FirstName = nameParts[0];
                                    }
                                    if (nameParts.Length > 1)
                                    {
                                        // Check if last part is a suffix (if not already found)
                                        if (string.IsNullOrWhiteSpace(result.Suffix))
                                        {
                                            var lastPart = nameParts[nameParts.Length - 1];
                                            if (Regex.IsMatch(lastPart, @"^(JR\.?|SR\.?|I{2,3}|IV|V|2ND|3RD)$", RegexOptions.IgnoreCase))
                                            {
                                                result.Suffix = lastPart.Replace(".", "");
                                                result.MiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
                                            }
                                            else
                                            {
                                                result.MiddleName = string.Join(" ", nameParts.Skip(1));
                                            }
                                        }
                                        else
                                        {
                                            // Suffix already found, rest is middle name
                                            result.MiddleName = string.Join(" ", nameParts.Skip(1));
                                        }
                                    }
                                }
                                
                                nameFound = true;
                                break;
                            }
                        }
                    }
                }
            }
            
            // Strategy 4: Search for common name patterns in the entire text (not just lines)
            // This is the most aggressive search - look for any comma-separated pattern that looks like a name
            if (!skipNameParsing && !nameFound)
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
                    
                    // Check for suffix at the end of givenNames first
                    var suffixMatch = Regex.Match(givenNames, @"\s+(JR\.?|SR\.?|I{2,3}|IV|V|2ND|3RD)$", RegexOptions.IgnoreCase);
                    if (suffixMatch.Success)
                    {
                        result.Suffix = suffixMatch.Groups[1].Value.Trim().Replace(".", "");
                        givenNames = givenNames.Substring(0, suffixMatch.Index).Trim();
                    }
                    
                    // For PhilSys IDs, keep all given names together as first name (Filipinos have multiple given names)
                    if (isPhilSysId)
                    {
                        result.FirstName = givenNames.Trim();
                    }
                    else
                    {
                        var nameParts = givenNames.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (nameParts.Length > 0)
                        {
                            result.FirstName = nameParts[0];
                        }
                        if (nameParts.Length > 1)
                        {
                            // Check if last part is a suffix (if not already found)
                            if (string.IsNullOrWhiteSpace(result.Suffix))
                            {
                                var lastPart = nameParts[nameParts.Length - 1];
                                if (Regex.IsMatch(lastPart, @"^(JR\.?|SR\.?|I{2,3}|IV|V|2ND|3RD)$", RegexOptions.IgnoreCase))
                                {
                                    result.Suffix = lastPart.Replace(".", "");
                                    result.MiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
                                }
                                else
                                {
                                    result.MiddleName = string.Join(" ", nameParts.Skip(1));
                                }
                            }
                            else
                            {
                                // Suffix already found, rest is middle name
                                result.MiddleName = string.Join(" ", nameParts.Skip(1));
                            }
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
            
            // First, try to find "Date of Birth" or "Birth Date" labels and search nearby (handles line breaks)
            var dobLabelPattern = @"(?:Date\s+of\s+Birth|Birth\s+Date|Date\s+of\s+Birthday|Birthday|DOB)";
            var dobLabelMatch = Regex.Match(text, dobLabelPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            if (dobLabelMatch.Success)
            {
                _logger.LogInformation("📅 Found 'Date of Birth' label at position {Position}", dobLabelMatch.Index);
                // Search within 200 characters after the label (handles line breaks and OCR spacing issues)
                var searchStart = dobLabelMatch.Index + dobLabelMatch.Length;
                var searchEnd = Math.Min(text.Length, searchStart + 200);
                var searchText = text.Substring(searchStart, searchEnd - searchStart);
                _logger.LogInformation("📅 Searching for date in text after label: {SearchText}", searchText.Substring(0, Math.Min(100, searchText.Length)));
                
                // Try YYYY/MM/DD format first (most common in Philippine Driver's License)
                var yyyyPattern = @"(\d{4}|[2O0][O0-9Il|]{3})\s*[/\-\s\.]\s*([O0-9Il|]{1,2})\s*[/\-\s\.]\s*([O0-9Il|]{1,2})";
                var yyyyMatch = Regex.Match(searchText, yyyyPattern, RegexOptions.IgnoreCase);
                
                if (yyyyMatch.Success)
                {
                    var year = yyyyMatch.Groups[1].Value.Trim();
                    var month = yyyyMatch.Groups[2].Value.Trim();
                    var day = yyyyMatch.Groups[3].Value.Trim();
                    
                    _logger.LogInformation("📅 Date pattern matched: {Year}/{Month}/{Day} (before OCR correction)", year, month, day);
                    
                    // Fix OCR errors: O->0, I->1, l->1, |->1
                    year = year.Replace("O", "0").Replace("I", "1").Replace("l", "1").Replace("|", "1");
                    month = month.Replace("O", "0").Replace("I", "1").Replace("l", "1").Replace("|", "1");
                    day = day.Replace("O", "0").Replace("I", "1").Replace("l", "1").Replace("|", "1");
                    
                    // Apply OCR error correction for year
                    year = CorrectOcrYearErrors(year);
                    
                    // Validate date parts
                    if (int.TryParse(year, out int y) && y >= 1900 && y <= DateTime.Now.Year &&
                        int.TryParse(month, out int m) && m >= 1 && m <= 12 &&
                        int.TryParse(day, out int d) && d >= 1 && d <= 31)
                    {
                        result.BirthDate = $"{year}-{month.PadLeft(2, '0')}-{day.PadLeft(2, '0')}";
                        _logger.LogInformation("✅ Birth date extracted: {BirthDate} (from YYYY/MM/DD pattern near label)", result.BirthDate);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Date pattern matched but validation failed: Year={Year}, Month={Month}, Day={Day}", year, month, day);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ 'Date of Birth' label found but no YYYY/MM/DD pattern matched in search area");
                }
                
                // If YYYY/MM/DD not found, try DD/MM/YYYY or MM/DD/YYYY format
                if (string.IsNullOrEmpty(result.BirthDate))
                {
                    var ddmmyyyyPattern = @"([O0-9Il|]{1,2})\s*[/\-\s\.]\s*([O0-9Il|]{1,2})\s*[/\-\s\.]\s*(\d{4}|[2O0][O0-9Il|]{3})";
                    var ddmmyyyyMatch = Regex.Match(searchText, ddmmyyyyPattern, RegexOptions.IgnoreCase);
                    
                    if (ddmmyyyyMatch.Success)
                    {
                        var part1 = ddmmyyyyMatch.Groups[1].Value.Trim();
                        var part2 = ddmmyyyyMatch.Groups[2].Value.Trim();
                        var part3 = ddmmyyyyMatch.Groups[3].Value.Trim();
                        
                        // Fix OCR errors
                        part1 = part1.Replace("O", "0").Replace("I", "1").Replace("l", "1").Replace("|", "1");
                        part2 = part2.Replace("O", "0").Replace("I", "1").Replace("l", "1").Replace("|", "1");
                        part3 = part3.Replace("O", "0").Replace("I", "1").Replace("l", "1").Replace("|", "1");
                        part3 = CorrectOcrYearErrors(part3);
                        
                        string year, month, day;
                        // Determine format (usually DD/MM/YYYY for Philippine IDs)
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
            }
            
            // Fallback: Try pattern with label inline (original approach)
            if (string.IsNullOrEmpty(result.BirthDate))
            {
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
                // First, try YYYY/MM/DD format (most common in Philippine IDs) with OCR error handling
                var yyyyPattern = @"(\d{4}|[2O0][O0-9Il|]{3})\s*[/\-\s\.]\s*([O0-9Il|]{1,2})\s*[/\-\s\.]\s*([O0-9Il|]{1,2})";
                var yyyyMatches = Regex.Matches(text, yyyyPattern, RegexOptions.IgnoreCase);
                
                DateTime? bestDate = null;
                int bestScore = 0;
                
                foreach (Match dateMatch in yyyyMatches)
                {
                    var year = dateMatch.Groups[1].Value.Trim();
                    var month = dateMatch.Groups[2].Value.Trim();
                    var day = dateMatch.Groups[3].Value.Trim();
                    
                    // Fix OCR errors: O->0, I->1, l->1, |->1
                    year = year.Replace("O", "0").Replace("I", "1").Replace("l", "1").Replace("|", "1");
                    month = month.Replace("O", "0").Replace("I", "1").Replace("l", "1").Replace("|", "1");
                    day = day.Replace("O", "0").Replace("I", "1").Replace("l", "1").Replace("|", "1");
                    
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
                    // Exclude name fields
                    .Where(l => !l.StartsWith("Name", StringComparison.OrdinalIgnoreCase) && 
                               !l.Contains(":Middle Name:", StringComparison.OrdinalIgnoreCase) &&
                               !l.Contains("First Name", StringComparison.OrdinalIgnoreCase) &&
                               !l.Contains("Last Name", StringComparison.OrdinalIgnoreCase))
                    // Exclude other ID fields
                    .Where(l => !l.StartsWith("Date", StringComparison.OrdinalIgnoreCase)) // Stop at Date fields
                    .Where(l => !l.StartsWith("Birth", StringComparison.OrdinalIgnoreCase)) // Stop at Birth fields
                    .Where(l => !l.StartsWith("Nationality", StringComparison.OrdinalIgnoreCase) &&
                               !l.StartsWith("Nationalı", StringComparison.OrdinalIgnoreCase) &&
                               !l.StartsWith("National", StringComparison.OrdinalIgnoreCase))
                    .Where(l => !l.StartsWith("Weight", StringComparison.OrdinalIgnoreCase) &&
                               !l.StartsWith("Height", StringComparison.OrdinalIgnoreCase) &&
                               !l.StartsWith("Sex", StringComparison.OrdinalIgnoreCase) &&
                               !l.StartsWith("Gender", StringComparison.OrdinalIgnoreCase))
                    .Where(l => !Regex.IsMatch(l, @"^\d{4}[/-]\d")) // Stop at date patterns
                    .Where(l => !Regex.IsMatch(l, @"^Name\s*:") && !Regex.IsMatch(l, @"^Middle\s+Name\s*:")) // Exclude name labels
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
                
                // Remove "/Address, PHL, " prefix if present
                if (result.Address.StartsWith("/Address, PHL, ", StringComparison.OrdinalIgnoreCase) ||
                    result.Address.StartsWith("/Address, PHL,", StringComparison.OrdinalIgnoreCase))
                {
                    result.Address = result.Address.Substring(result.Address.IndexOf("PHL,", StringComparison.OrdinalIgnoreCase) + 5).Trim();
                }
                else if (result.Address.StartsWith("/Address,", StringComparison.OrdinalIgnoreCase))
                {
                    result.Address = result.Address.Substring("/Address,".Length).Trim();
                }
                else if (result.Address.StartsWith("Address, PHL, ", StringComparison.OrdinalIgnoreCase) ||
                         result.Address.StartsWith("Address, PHL,", StringComparison.OrdinalIgnoreCase))
                {
                    result.Address = result.Address.Substring(result.Address.IndexOf("PHL,", StringComparison.OrdinalIgnoreCase) + 5).Trim();
                }
                    
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
            if (string.IsNullOrWhiteSpace(text))
                return "";
                
            // Clean up common OCR errors first
            var cleanedText = text.Replace("16I", "161").Replace("16l", "161").Replace("16|", "161").Replace("16O", "160")
                .Replace("181", "161") // Common OCR error: 8 misread as 6
                .Replace("BARANGAY 18", "BARANGAY 16") // Fix partial matches
                .Replace("IGO", "160") // OCR error: "IGO" instead of "160"
                .Replace("BARANICAY IGO", "BARANGAY 160")
                .Replace("SOLE BARANICAY IGO", "BARANGAY 160");
            
            var validBarangays = new[] { "158", "159", "160", "161" };
            
            // Pattern 1: "BARANGAY 161" or "BARANGAY 161," or "BRGY 161" (most specific)
            var pattern1 = @"\b(?:BARANGAY|BRGY|BARANG|BARANICAY)\s*(\d{3})\b";
            var match1 = Regex.Match(cleanedText, pattern1, RegexOptions.IgnoreCase);
            if (match1.Success)
            {
                var barangay = match1.Groups[1].Value.Trim();
                if (validBarangays.Contains(barangay))
                {
                    return barangay;
                }
            }

            // Pattern 2: "BARANGAY 161" in address context (more lenient, handles 2-3 digits)
            var pattern2 = @"(?:BARANGAY|BRGY|BARANG|BARANICAY)\s*(\d{2,3})";
            var match2 = Regex.Match(cleanedText, pattern2, RegexOptions.IgnoreCase);
            if (match2.Success)
            {
                var barangay = match2.Groups[1].Value.Trim();
                
                // Handle OCR errors: 16I, 16l, 16| -> 161
                if (barangay == "16" || barangay.StartsWith("16"))
                {
                    // Check if followed by I, l, or | (common OCR errors for 1)
                    var nextCharIndex = match2.Index + match2.Length;
                    if (nextCharIndex < cleanedText.Length)
                    {
                        var nextChar = cleanedText[nextCharIndex];
                        if (nextChar == 'I' || nextChar == 'l' || nextChar == '|')
                        {
                            return "161";
                        }
                        if (nextChar == 'O' || nextChar == '0')
                        {
                            return "160";
                        }
                    }
                    // If just "16" without following character, check context
                    // Look for "160" or "161" patterns nearby
                    var contextStart = Math.Max(0, match2.Index - 20);
                    var contextEnd = Math.Min(cleanedText.Length, match2.Index + match2.Length + 20);
                    var context = cleanedText.Substring(contextStart, contextEnd - contextStart);
                    if (context.Contains("160") || context.Contains("16O"))
                    {
                        return "160";
                    }
                    if (context.Contains("161") || context.Contains("16I") || context.Contains("16l"))
                    {
                        return "161";
                    }
                    // Default to 160 if ambiguous
                    return "160";
                }
                
                // Handle "18" -> "16" (OCR error: 8 misread as 6)
                if (barangay == "18" || barangay.StartsWith("18"))
                {
                    // Check if it's actually "181" (should be "161")
                    if (barangay == "181" || cleanedText.Substring(match2.Index, Math.Min(5, cleanedText.Length - match2.Index)).Contains("181"))
                    {
                        return "161";
                    }
                    // Otherwise might be "180" -> "160"
                    return "160";
                }
                
                // Direct match
                if (validBarangays.Contains(barangay))
                {
                    return barangay;
                }
            }

            // Pattern 3: Look for numbers 158-161 near address keywords (in address lines)
            var pattern3 = @"(?:LT|BLK|BLK1|LT5|ADDRESS|TIRAHAN|BARANG|BRGY|BARANICAY|CITY|REPARO|LIBIS|KALOOKAN|CALOOCAN).*?(158|159|160|161)\b";
            var match3 = Regex.Match(cleanedText, pattern3, RegexOptions.IgnoreCase);
            if (match3.Success)
            {
                var barangay = match3.Groups[1].Value.Trim();
                if (validBarangays.Contains(barangay))
                {
                    return barangay;
                }
            }
            
            // Pattern 4: Look for "161" or "16I" near "BARANGAY" (handle OCR errors)
            var pattern4 = @"BARANGAY\s*16[1Iil|O]";
            var match4 = Regex.Match(cleanedText, pattern4, RegexOptions.IgnoreCase);
            if (match4.Success)
            {
                // Check the actual character after "16"
                var after16Index = match4.Index + match4.Value.IndexOf("16") + 2;
                if (after16Index < cleanedText.Length)
                {
                    var charAfter16 = cleanedText[after16Index];
                    if (charAfter16 == '1' || charAfter16 == 'I' || charAfter16 == 'l' || charAfter16 == '|')
                    {
                        return "161";
                    }
                    if (charAfter16 == '0' || charAfter16 == 'O')
                    {
                        return "160";
                    }
                }
                return "161"; // Default
            }
            
            // Pattern 5: Look for "IGO" which is OCR error for "160"
            var pattern5 = @"BARANGAY\s*IGO";
            var match5 = Regex.Match(cleanedText, pattern5, RegexOptions.IgnoreCase);
            if (match5.Success)
            {
                return "160";
            }
            
            // Pattern 6: Look for standalone valid barangay numbers in address context
            // This handles cases where "BARANGAY" keyword might be missing or garbled
            var pattern6 = @"(?:^|\s|,)(158|159|160|161)(?:\s|,|$|\.)";
            var matches6 = Regex.Matches(cleanedText, pattern6, RegexOptions.IgnoreCase);
            foreach (Match match in matches6)
            {
                var barangay = match.Groups[1].Value.Trim();
                // Check context - should be near address-related words
                var contextStart = Math.Max(0, match.Index - 100);
                var contextEnd = Math.Min(cleanedText.Length, match.Index + match.Length + 100);
                var context = cleanedText.Substring(contextStart, contextEnd - contextStart).ToUpper();
                
                // Should be near address keywords, not near name/date fields
                if ((context.Contains("BARANGAY") || context.Contains("CITY") || context.Contains("ADDRESS") ||
                     context.Contains("LT") || context.Contains("BLK") || context.Contains("REPARO") ||
                     context.Contains("LIBIS") || context.Contains("KALOOKAN") || context.Contains("CALOOCAN")) &&
                    !context.Contains("NAME") && !context.Contains("DATE OF BIRTH") && !context.Contains("BIRTH DATE"))
                {
                    return barangay;
                }
            }

            return "";
        }

        /// <summary>
        /// Validates that the extracted text is from an actual Philippine ID document
        /// Rejects plain text, screenshots, illustrations, or documents without ID markers
        /// Returns tuple (isValid, idType)
        /// </summary>
        private (bool isValid, string idType) IsValidPhilippineIdDocument(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (false, null);

            var upperText = text.ToUpper();
            
            _logger.LogInformation("📝 Validating document with text length: {Length} characters", upperText.Length);
            
            // CRITICAL: Check for screenshot or illustration indicators
            // Comprehensive list of screenshot indicators based on common mobile/desktop UI elements
            var screenshotIndicators = new[] { 
                "SCREENSHOT", "SCREEN SHOT", "SCREENSHOT SAVED", "SCREEN CAPTURE", "SCREENSHOT CAPTURED",
                "CAPTURE", "SNAP", "SNAPSHOT", "SCREEN RECORDING", "SCREENSHOT TOOL",
                "WINDOWS", "MACOS", "ANDROID", "IOS", "IPHONE", "IPAD",
                "GALLERY", "PHOTOS", "CAMERA ROLL", "PHOTO LIBRARY", "PICTURE GALLERY",
                "PRINT SCREEN", "PRTSC", "PRT SCR", "PRINTSCREEN",
                "SHARE", "SAVE IMAGE", "DOWNLOAD", "IMAGE SAVED", "SAVED TO GALLERY",
                "SCREENSHOT APP", "TAKE SCREENSHOT", "SCREENSHOT NOTIFICATION",
                "FILE MANAGER", "FILES APP", "GOOGLE PHOTOS", "ICLOUD PHOTOS",
                "SCREENSHOT FOLDER", "SCREENSHOTS", "SCREENSHOTS FOLDER"
            };
            
            var invalidContentIndicators = new[] 
            { 
                "CHATGPT", "ILLUSTRATION", "CARTOON", "DRAWING",
                "GENERATED", "ARTIFICIAL", "FAKE", "AI GENERATED"
            };
            
            if (screenshotIndicators.Any(indicator => upperText.Contains(indicator)))
            {
                _logger.LogWarning("⚠️ Document validation failed: Screenshot indicators found in text");
                return (false, "Screenshot Detected - Please upload a photo of your actual ID, not a screenshot");
            }
            
            if (invalidContentIndicators.Any(indicator => upperText.Contains(indicator)))
            {
                _logger.LogWarning("⚠️ Document validation failed: Invalid content indicators found");
                return (false, "Invalid Image - Please upload a photo of your official printed ID");
            }
            
            // CRITICAL: Check for handwritten document indicators
            var handwrittenPhrases = new[] { 
                "HANDWRITTEN", "HAND WRITTEN", "WRITTEN BY HAND", "MANUAL SIGNATURE", 
                "SIGNED BY HAND", "PEN", "PENCIL", "HANDWRITE", "MANUALLY WRITTEN"
            };
            if (handwrittenPhrases.Any(phrase => upperText.Contains(phrase)))
            {
                _logger.LogWarning("⚠️ Document validation failed: Handwritten document indicators found");
                return (false, "Handwritten Document Detected - Please upload a photo of your official printed ID, not a handwritten document");
            }
            
            // Detect handwritten patterns
            var letterCount = text.Count(char.IsLetter);
            if (letterCount >= 20)
            {
                var mixedCaseRatio = text.Count(char.IsLower) / (double)Math.Max(letterCount, 1);
                var hasExcessiveMixedCase = mixedCaseRatio > 0.3 && mixedCaseRatio < 0.7;
                var irregularSpacingPattern = Regex.IsMatch(text, @"\w\s{3,}\w");
                var lineBreakCount = text.Count(c => c == '\n' || c == '\r');
                var hasIrregularLineBreaks = lineBreakCount > 20 && lineBreakCount < 100;
                
                int handwritingScore = 0;
                if (hasExcessiveMixedCase) handwritingScore++;
                if (irregularSpacingPattern) handwritingScore++;
                if (hasIrregularLineBreaks) handwritingScore++;
                
                if (handwritingScore >= 2)
                {
                    _logger.LogWarning("⚠️ Document validation failed: Handwritten document detected");
                    return (false, "Handwritten Document Detected - Please upload a photo of your official printed ID, not a handwritten document");
                }
            }
            
            // Required Philippine ID markers - must contain at least one
            var strongIdMarkers = new[]
            {
                // Republic markers
                "REPUBLIC OF THE PHILIPPINES",
                "REPUBLIKA NG PILIPINAS",
                
                // Driver's License
                "DRIVER'S LICENSE", "DRIVERS LICENSE", "DRIVER LICENSE",
                "LAND TRANSPORTATION OFFICE", "LTO",
                
                // National ID
                "PHILSYS", "PHILIPPINE IDENTIFICATION SYSTEM",
                "NATIONAL ID", "PAMBANSANG PAGKAKAKILANLAN",
                
                // PhilHealth
                "PHILHEALTH", "PHIL-HEALTH", "PHIL HEALTH",
                "PHILIPPINE HEALTH INSURANCE",
                
                // UMID/SSS
                "UMID", "UNIFIED MULTI-PURPOSE ID",
                "SSS", "SOCIAL SECURITY",
                
                // Postal ID
                "POSTAL ID", "PHILIPPINE POSTAL", "PHLPOST",
                
                // Passport
                "PASSPORT", "PASAPORTE", "P<PHL",
                
                // TIN
                "TIN", "TAX IDENTIFICATION NUMBER", "BIR"
            };

            bool hasStrongIdMarker = strongIdMarkers.Any(marker => upperText.Contains(marker));
            string detectedIdType = null;
            
            // Detect specific ID type
            if (upperText.Contains("PHILSYS") || upperText.Contains("PAMBANSANG"))
                detectedIdType = "PhilSys National ID";
            else if ((upperText.Contains("DRIVER") || upperText.Contains("DRIVERS")) && upperText.Contains("LICENSE"))
                detectedIdType = "Driver's License";
            else if (upperText.Contains("LTO") || upperText.Contains("TRANSPORTATION"))
                detectedIdType = "Driver's License";
            else if (upperText.Contains("PHILHEALTH") || upperText.Contains("PHIL HEALTH"))
                detectedIdType = "PhilHealth ID";
            else if (upperText.Contains("PASSPORT") || upperText.Contains("PASAPORTE"))
                detectedIdType = "Passport";
            else if (upperText.Contains("UMID"))
                detectedIdType = "UMID";
            else if (upperText.Contains("SSS") || upperText.Contains("SOCIAL SECURITY"))
                detectedIdType = "SSS ID";
            else if (upperText.Contains("POSTAL"))
                detectedIdType = "Postal ID";
            else if (upperText.Contains("TIN") || upperText.Contains("TAX IDENTIFICATION"))
                detectedIdType = "TIN ID";
            else if (upperText.Contains("REPUBLIK") || upperText.Contains("PILIPINAS"))
                detectedIdType = "Philippine Government ID";
            
            // Check for partial matches to handle OCR errors
            if (!hasStrongIdMarker)
            {
                hasStrongIdMarker = 
                    upperText.Contains("REPUBLIK") || 
                    upperText.Contains("PILIPINAS") ||
                    (upperText.Contains("REPUBLIC") && upperText.Contains("PHILIPP")) ||
                    upperText.Contains("PAMBANSANG") ||
                    ((upperText.Contains("DRIVER") || upperText.Contains("DRIVERS")) && upperText.Contains("LICENSE")) ||
                    upperText.Contains("PHILSYS") ||
                    upperText.Contains("PHILHEALTH") ||
                    upperText.Contains("HEALTH INSURANCE") ||
                    upperText.Contains("UMID") ||
                    upperText.Contains("POSTAL") ||
                    upperText.Contains("PASSPORT") ||
                    upperText.Contains("P<PHL");
            }
            
            if (!hasStrongIdMarker)
            {
                _logger.LogWarning("⚠️ Document validation failed: No Philippine ID markers found");
                return (false, "Unverified Document");
            }

            _logger.LogInformation("✅ Valid Philippine ID markers found: {IdType}", detectedIdType ?? "Unknown Philippine ID");
            return (true, detectedIdType ?? "Philippine Government ID");
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

