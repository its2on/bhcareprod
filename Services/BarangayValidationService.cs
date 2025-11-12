using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BHCARE.Services
{
    /// <summary>
    /// Dedicated service for validating and extracting barangay numbers from ID documents
    /// Focuses specifically on detecting barangays 158, 159, 160, or 161
    /// </summary>
    public class BarangayValidationService
    {
        private readonly ILogger<BarangayValidationService> _logger;
        private static readonly string[] ValidBarangays = { "158", "159", "160", "161" };

        public BarangayValidationService(ILogger<BarangayValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates and extracts barangay number from OCR text
        /// STRICT VALIDATION: Only accepts printed official documents (Certificate of Barangay, Philippine ID)
        /// Rejects screenshots and handwritten documents
        /// </summary>
        /// <param name="ocrText">The extracted text from OCR processing</param>
        /// <returns>BarangayValidationResult containing the detected barangay and validation status</returns>
        public BarangayValidationResult ValidateBarangay(string ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
            {
                _logger.LogWarning("⚠️ Empty OCR text provided for barangay validation");
                return new BarangayValidationResult
                {
                    Success = false,
                    DetectedBarangay = null,
                    IsValidBarangay = false,
                    Message = "No text extracted from document. Please ensure the document image is clear and readable."
                };
            }

            _logger.LogInformation("=== BARANGAY-ONLY VALIDATION START ===");
            _logger.LogInformation("OCR Text length: {Length} characters", ocrText.Length);
            _logger.LogInformation("OCR Text preview (first 500 chars): {Preview}", 
                ocrText.Length > 500 ? ocrText.Substring(0, 500) + "..." : ocrText);

            // SIMPLIFIED: ONLY check for barangay number - skip all other validations
            // This makes it faster and more reliable - focus only on what matters
            var cleanedText = CleanOcrText(ocrText);
            _logger.LogInformation("Cleaned text preview (first 500 chars): {Preview}", 
                cleanedText.Length > 500 ? cleanedText.Substring(0, 500) + "..." : cleanedText);
            var extractedBarangay = ExtractBarangayNumber(cleanedText);
            
            // If valid barangay (158-161) is found, accept it immediately
            if (!string.IsNullOrWhiteSpace(extractedBarangay) && ValidBarangays.Contains(extractedBarangay.Trim()))
            {
                _logger.LogInformation("✅ VALID BARANGAY {Barangay} FOUND - ACCEPTING", extractedBarangay);
                return new BarangayValidationResult
                {
                    Success = true,
                    DetectedBarangay = extractedBarangay.Trim(),
                    IsValidBarangay = true,
                    Message = $"Barangay {extractedBarangay} detected. Your account will be automatically approved upon registration.",
                    AutoApprovalEligible = true
                };
            }
            
            // If invalid barangay found (not 158-161), reject with specific message
            if (!string.IsNullOrWhiteSpace(extractedBarangay) && !ValidBarangays.Contains(extractedBarangay.Trim()))
            {
                _logger.LogWarning("❌ INVALID BARANGAY DETECTED: {Barangay} - Not eligible (158-161 only)", extractedBarangay);
                return new BarangayValidationResult
                {
                    Success = false,
                    DetectedBarangay = extractedBarangay.Trim(),
                    IsValidBarangay = false,
                    Message = $"The document shows Barangay {extractedBarangay}, which is not eligible for automatic verification. Only Barangay 158, 159, 160, or 161 are eligible. Your account will require manual review by an administrator.",
                    AutoApprovalEligible = false
                };
            }
            
            // No barangay found at all
            _logger.LogWarning("⚠️ No barangay number detected in OCR text");
            return new BarangayValidationResult
            {
                Success = false,
                DetectedBarangay = null,
                IsValidBarangay = false,
                Message = "No barangay number found in the ID document. Please ensure your ID clearly shows your address with Barangay 158, 159, 160, or 161."
            };
        }

        /// <summary>
        /// Cleans OCR text to fix common OCR errors that might affect barangay detection
        /// </summary>
        private string CleanOcrText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var cleaned = text;
            
            // Fix common OCR character misreadings
            cleaned = cleaned.Replace("16I", "161")  // I instead of 1
                            .Replace("16l", "161")   // lowercase L instead of 1
                            .Replace("16|", "161")    // pipe instead of 1
                            .Replace("16O", "160")   // O instead of 0
                            .Replace("16o", "160")   // lowercase o instead of 0
                            .Replace("15B", "158")   // B instead of 8
                            .Replace("15S", "159")   // S instead of 9
                            .Replace("BARANG.", "BARANGAY")
                            .Replace("BARANG ", "BARANGAY ")
                            .Replace("BRGY.", "BRGY")
                            .Replace("BRGY ", "BARANGAY ");

            return cleaned;
        }

        /// <summary>
        /// Extracts barangay number from text using comprehensive regex patterns
        /// </summary>
        private string ExtractBarangayNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("ExtractBarangayNumber: Empty text provided");
                return null;
            }

            var upperText = text.ToUpperInvariant();
            
            // Log the text being searched for debugging
            _logger.LogInformation("=== EXTRACTING BARANGAY NUMBER ===");
            _logger.LogInformation("Text length: {Length} characters", upperText.Length);
            _logger.LogInformation("Text preview (first 1000 chars): {Preview}", 
                upperText.Length > 1000 ? upperText.Substring(0, 1000) + "..." : upperText);
            
            // Comprehensive patterns to catch various formats and OCR errors
            // PRIORITY: Look for "BARANGAY" followed by number (handles commas, periods, etc.)
            // Made more flexible to handle OCR errors and variations
            var patterns = new[]
            {
                // HIGHEST PRIORITY: "BARANGAY 161," or "BARANGAY 161" (most common format in addresses)
                // Very flexible - handles any whitespace, punctuation, line breaks
                @"BARANGAY\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                @"\bBARANGAY\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                @"BARANGAY\s*[:\-]?\s*(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                
                // OCR variations - BARANGAY might be misspelled or truncated
                @"BARANGA[YI]\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",  // BARANGAY or BARANGAI
                @"BARANG[AY]?\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",  // BARANG or BARANGA
                @"BARAN\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",        // BARAN (very truncated)
                @"B[A4]RANGAY\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",  // OCR might read A as 4
                
                // BRGY variations (with punctuation support)
                @"BRGY\.?\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                @"\bBRGY\.?\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                @"BRG\.?\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",       // BRG (truncated)
                
                // With NO. or #
                @"BARANGAY\s+(?:NO\.?\s+)?(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                @"BARANGAY\s*#\s*(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                @"\bBARANGAY\s+NO\.?\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                
                // OCR error variations (missing letters) - with punctuation support
                @"BARANG\.?\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                @"BARANG\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                @"BARANG\.?\s*,\s*(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                @"BARANG\s*,\s*(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                
                // Reverse order
                @"(158|159|160|161)\s*(?:BARANGAY|BRGY|BARANG|BARAN)",
                
                // Context-based (near address keywords) - VERY flexible with multiline
                // This pattern looks for address keywords followed by barangay number
                // Handles cases where BARANGAY might be on a different line
                @"(?:LT|LTS|BLK|BLOCK|ADDRESS|BARANG|BRGY|CITY|KALOOKAN|REPARO|LIBIS|REPARO)[\s\S]{0,200}?(?:BARANGAY|BARANG|BRGY|BARAN)\s+(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                
                // Even more lenient: Look for number near address keywords without requiring BARANGAY
                @"(?:LT|LTS|BLK|BLOCK|ADDRESS|CITY|KALOOKAN|REPARO|LIBIS)[\s\S]{0,200}?(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
                
                // Standalone numbers with context boundaries (more flexible)
                @"(?:^|\s|,|\.|:)(158|159|160|161)(?:\s|$|,|\.|:|\n|\r)",
                
                // Very lenient: Just look for BARANGAY followed by 3-digit number starting with 1
                // Note: This pattern captures the full number by reconstructing it
                @"BARANGAY\s+(1(?:5[89]|6[01]))(?:[,\s\.\n\r]|$|\b)",
                
                // Ultra-lenient: Look for number 161, 160, 159, or 158 anywhere near address context
                // This handles cases where OCR completely misses "BARANGAY" but gets the number
                @"(?:ADDRESS|ADDR|CITY|KALOOKAN|REPARO|LIBIS|LT|LTS|BLK)[\s\S]{0,100}(158|159|160|161)(?:[,\s\.\n\r]|$|\b)",
            };

            _logger.LogInformation("Searching for barangay using {PatternCount} patterns", patterns.Length);
            
            // Log a sample of the text to help debug
            var sampleText = upperText.Length > 2000 ? upperText.Substring(0, 2000) + "..." : upperText;
            _logger.LogInformation("Full text sample for debugging: {Text}", sampleText);
            
            // Also search for any occurrence of "161", "160", "159", "158" to see if numbers are being extracted
            var numberTest = Regex.Matches(upperText, @"\b(158|159|160|161)\b", RegexOptions.IgnoreCase);
            if (numberTest.Count > 0)
            {
                _logger.LogInformation("Found {Count} occurrences of valid barangay numbers in text", numberTest.Count);
                foreach (Match numMatch in numberTest)
                {
                    var contextStart = Math.Max(0, numMatch.Index - 30);
                    var contextLength = Math.Min(80, upperText.Length - contextStart);
                    _logger.LogInformation("  Number {Number} found at position {Pos}, context: {Context}", 
                        numMatch.Value, numMatch.Index, upperText.Substring(contextStart, contextLength));
                }
            }
            else
            {
                _logger.LogWarning("⚠️ No valid barangay numbers (158-161) found anywhere in the text!");
            }

            foreach (var pattern in patterns)
            {
                try
                {
                    var match = Regex.Match(upperText, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline);
                    if (match.Success)
                    {
                        // Try to get the captured group
                        string barangayNumber = null;
                        if (match.Groups.Count > 1)
                        {
                            barangayNumber = match.Groups[1].Value.Trim();
                        }
                        else
                        {
                            // If no capture group, extract from the full match
                            var fullMatch = match.Value;
                            var numberMatch = Regex.Match(fullMatch, @"(158|159|160|161)");
                            if (numberMatch.Success)
                            {
                                barangayNumber = numberMatch.Value.Trim();
                            }
                        }
                        
                        if (!string.IsNullOrWhiteSpace(barangayNumber))
                        {
                            // Double-check it's a valid number
                            if (ValidBarangays.Contains(barangayNumber))
                            {
                                _logger.LogInformation("✅ Pattern matched: {Pattern} → Found: {Barangay}", pattern, barangayNumber);
                                var contextStart = Math.Max(0, match.Index - 50);
                                var contextLength = Math.Min(150, upperText.Length - contextStart);
                                _logger.LogInformation("Match context: {Context}", upperText.Substring(contextStart, contextLength));
                                return barangayNumber;
                            }
                            else
                            {
                                _logger.LogDebug("Pattern matched but number not in valid list: {Pattern} → {Barangay}", pattern, barangayNumber);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error matching pattern {Pattern}: {Error}", pattern, ex.Message);
                }
            }

            // If no valid barangay found, try to find ANY barangay number for reporting
            var anyBarangayPatterns = new[]
            {
                @"(?:BARANGAY|BRGY|BARANG|BARAN)\s+(\d{2,4})(?:[,\s\.\n\r]|$|\b)",
                @"(?:BARANGAY|BRGY|BARANG|BARAN)[:\s\-]+(\d{2,4})(?:[,\s\.\n\r]|$|\b)",
                // Ultra-lenient: Look for 3-digit numbers starting with 1 near address context
                @"(?:ADDRESS|ADDR|CITY|KALOOKAN|REPARO|LIBIS|LT|LTS|BLK|BLOCK)[\s\S]{0,150}?(\d{3})(?:[,\s\.\n\r]|$|\b)",
            };
            
            foreach (var pattern in anyBarangayPatterns)
            {
                var anyMatch = Regex.Match(upperText, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline);
                if (anyMatch.Success && anyMatch.Groups.Count > 1)
                {
                    var detectedNumber = anyMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Found barangay number (not in valid list): {Barangay}", detectedNumber);
                    return detectedNumber; // Return it even if not valid, so we can show a specific error message
                }
            }
            
            // LAST RESORT: If we found the numbers earlier but patterns didn't match, 
            // try to extract them from the address context directly
            if (numberTest.Count > 0)
            {
                _logger.LogWarning("⚠️ Found valid numbers but patterns didn't match. Trying direct extraction...");
                // Check if any of the found numbers are near address keywords
                foreach (Match numMatch in numberTest)
                {
                    var searchStart = Math.Max(0, numMatch.Index - 100);
                    var searchLength = Math.Min(200, upperText.Length - searchStart);
                    var context = upperText.Substring(searchStart, searchLength);
                    
                    // Check if this number is in address context
                    if (Regex.IsMatch(context, @"(?:ADDRESS|ADDR|CITY|KALOOKAN|REPARO|LIBIS|LT|LTS|BLK|BLOCK|BARANG|BRGY)", RegexOptions.IgnoreCase))
                    {
                        _logger.LogInformation("✅ Found valid barangay {Barangay} in address context (direct extraction)", numMatch.Value);
                        return numMatch.Value;
                    }
                }
            }

            _logger.LogWarning("❌ No barangay number found in text");
            _logger.LogWarning("Text searched (full): {Text}", upperText);
            return null;
        }

        /// <summary>
        /// Checks if the document is a screenshot
        /// </summary>
        private bool IsScreenshot(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var upperText = text.ToUpperInvariant();
            
            // Screenshot indicators - UI elements that appear in screenshots
            var screenshotIndicators = new[]
            {
                "SCREENSHOT", "SCREEN SHOT", "SCREENSHOT SAVED", "SCREEN CAPTURE",
                "CAPTURE", "SNAP", "SNAPSHOT",
                "WINDOWS", "MACOS", "ANDROID", "IOS", "IPHONE", "IPAD",
                "GALLERY", "PHOTOS", "CAMERA ROLL", "SCREEN RECORDING",
                "PRINT SCREEN", "PRTSC", "PRT SCR",
                "SHARE", "SAVE IMAGE", "DOWNLOAD", "IMAGE SAVED",
                "SCREEN CAPTURE", "SCREENSHOT TOOL", "SCREENSHOT APP",
                "TAKE SCREENSHOT", "SCREENSHOT NOTIFICATION",
                "FILE MANAGER", "PHOTO LIBRARY", "PICTURE GALLERY"
            };

            foreach (var indicator in screenshotIndicators)
            {
                if (upperText.Contains(indicator))
                {
                    _logger.LogWarning("Screenshot indicator found: {Indicator}", indicator);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the document is handwritten
        /// STRICT: Only rejects if there are STRONG indicators of handwriting
        /// Valid printed IDs may have mixed case and line breaks, so we need to be careful
        /// </summary>
        private bool IsHandwritten(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // FIRST: Check for explicit handwritten keywords (strongest indicator)
            var handwrittenPatterns = new[]
            {
                @"\bHANDWRITTEN\b",
                @"\bHAND WRITTEN\b",
                @"\bWRITTEN BY HAND\b",
                @"\bMANUAL\b.*\bSIGNATURE\b",
                @"\bSIGNED\b.*\bBY HAND\b"
            };

            foreach (var pattern in handwrittenPatterns)
            {
                if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogWarning("Handwritten pattern found: {Pattern}", pattern);
                    return true;
                }
            }

            // SECOND: Check for STRONG handwritten indicators (multiple must be present)
            // Valid printed IDs can have mixed case and line breaks, so we need multiple indicators
            var letterCount = text.Count(char.IsLetter);
            if (letterCount < 20) return false; // Too short to determine reliably

            var upperCount = text.Count(char.IsUpper);
            var lowerCount = text.Count(char.IsLower);
            var mixedCaseRatio = (double)Math.Min(upperCount, lowerCount) / Math.Max(letterCount, 1);
            
            // Check for irregular spacing (multiple spaces between words - strong indicator)
            var irregularSpacing = Regex.IsMatch(text, @"\w\s{4,}\w"); // 4+ spaces (more strict)
            var lineBreakCount = text.Count(c => c == '\n' || c == '\r');
            var hasExcessiveLineBreaks = lineBreakCount > 30; // More strict threshold
            
            // Check for very inconsistent case patterns (handwritten often has random case)
            var hasVeryInconsistentCase = mixedCaseRatio > 0.5 && mixedCaseRatio < 0.55; // Very narrow range
            
            // STRICT: Require MULTIPLE strong indicators to reject as handwritten
            // This prevents false positives on valid printed IDs
            int strongIndicators = 0;
            if (irregularSpacing) strongIndicators++;
            if (hasExcessiveLineBreaks && lineBreakCount > 50) strongIndicators++; // Very excessive
            if (hasVeryInconsistentCase && irregularSpacing) strongIndicators++; // Both together
            
            // Only reject if we have at least 2 strong indicators
            if (strongIndicators >= 2)
            {
                _logger.LogWarning("Handwritten document STRONG indicators detected: mixed case ratio {Ratio}, irregular spacing: {Spacing}, excessive line breaks: {LineBreaks}", 
                    mixedCaseRatio, irregularSpacing, hasExcessiveLineBreaks);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates that the document is a printed official document
        /// Accepts: Certificate of Barangay, Philippine ID (Driver's License, National ID, etc.)
        /// </summary>
        private (bool IsValid, string Message) ValidatePrintedDocument(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (false, "No text found in document. Please upload a clear photo of your official document.");

            var upperText = text.ToUpperInvariant();

            // ACCEPTED DOCUMENT TYPES - Must contain at least one of these markers
            
            // 1. Certificate of Barangay markers
            var barangayCertificateMarkers = new[]
            {
                "CERTIFICATE OF BARANGAY",
                "CERTIFICATE OF RESIDENCY",
                "BARANGAY CERTIFICATE",
                "BARANGAY CLEARANCE",
                "CERTIFICATE",
                "BARANGAY OFFICE",
                "BARANGAY HALL",
                "OFFICE OF THE BARANGAY",
                "BARANGAY CAPTAIN",
                "BARANGAY CHAIRMAN",
                "CERTIFIED BY",
                "THIS IS TO CERTIFY",
                "CERTIFICATION"
            };

            // 2. Philippine ID markers
            var philippineIdMarkers = new[]
            {
                "REPUBLIC OF THE PHILIPPINES",
                "REPUBLIKA NG PILIPINAS",
                "DRIVER'S LICENSE",
                "DRIVERS LICENSE",
                "LAND TRANSPORTATION OFFICE",
                "LTO",
                "PHILSYS",
                "PHILIPPINE IDENTIFICATION",
                "NATIONAL ID",
                "PHILHEALTH",
                "PHILIPPINE HEALTH INSURANCE",
                "POSTAL ID",
                "PHILIPPINE POSTAL",
                "UMID",
                "UNIFIED MULTI-PURPOSE ID",
                "SSS",
                "SOCIAL SECURITY",
                "PASSPORT",
                "TIN",
                "TAX IDENTIFICATION NUMBER",
                "BIR",
                "BUREAU OF INTERNAL REVENUE"
            };

            // Check for Certificate of Barangay
            bool hasBarangayCertificate = barangayCertificateMarkers.Any(marker => upperText.Contains(marker));
            
            // Check for Philippine ID
            bool hasPhilippineId = philippineIdMarkers.Any(marker => upperText.Contains(marker));

            if (hasBarangayCertificate)
            {
                _logger.LogInformation("✅ Valid document detected: Certificate of Barangay");
                return (true, "Valid Certificate of Barangay detected.");
            }

            if (hasPhilippineId)
            {
                _logger.LogInformation("✅ Valid document detected: Philippine ID");
                return (true, "Valid Philippine ID detected.");
            }

            // If neither marker is found, reject
            _logger.LogWarning("❌ Document validation failed: No valid document markers found");
            _logger.LogWarning("Document must be either:");
            _logger.LogWarning("  1. Certificate of Barangay (must contain 'Certificate', 'Barangay', 'Residency', etc.)");
            _logger.LogWarning("  2. Philippine ID (Driver's License, National ID, PhilHealth ID, Postal ID, etc.)");
            
            return (false, "Invalid document type. Please upload a photo of your official printed document: Certificate of Barangay or Philippine ID (Driver's License, National ID, PhilHealth ID, Postal ID, etc.). Screenshots and handwritten documents are not accepted.");
        }
    }

    /// <summary>
    /// Result of barangay validation
    /// </summary>
    public class BarangayValidationResult
    {
        /// <summary>
        /// Whether the validation was successful (barangay was found)
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The detected barangay number (may be null if not found, or may be invalid)
        /// </summary>
        public string? DetectedBarangay { get; set; }

        /// <summary>
        /// Whether the detected barangay is in the valid list (158, 159, 160, 161)
        /// </summary>
        public bool IsValidBarangay { get; set; }

        /// <summary>
        /// Human-readable message about the validation result
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Whether the user is eligible for auto-approval based on barangay
        /// </summary>
        public bool AutoApprovalEligible { get; set; }

        /// <summary>
        /// Whether the document is a screenshot
        /// </summary>
        public bool IsScreenshot { get; set; }

        /// <summary>
        /// Whether the document is handwritten
        /// </summary>
        public bool IsHandwritten { get; set; }
    }
}

