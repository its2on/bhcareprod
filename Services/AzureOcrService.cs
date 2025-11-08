using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Barangay.Services
{
    public class AzureOcrService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureOcrService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _subscriptionKey;

        public AzureOcrService(IConfiguration configuration, ILogger<AzureOcrService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;

            _endpoint = _configuration["AzureOCR:Endpoint"]?.Trim();
            _subscriptionKey = _configuration["AzureOCR:Key"]?.Trim();

            // Enhanced validation and logging
            if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_subscriptionKey))
            {
                _logger.LogWarning("Azure Computer Vision credentials not configured");
            }
            else
            {
                // Validate key length (Azure Computer Vision keys are typically 100 characters)
                if (_subscriptionKey.Length < 80)
                {
                    _logger.LogError("Azure OCR Key appears to be truncated! Expected ~100 characters, got {Length} characters. First 10: {First10}, Last 10: {Last10}", 
                        _subscriptionKey.Length, 
                        _subscriptionKey.Substring(0, Math.Min(10, _subscriptionKey.Length)),
                        _subscriptionKey.Substring(Math.Max(0, _subscriptionKey.Length - 10)));
                }
                else
                {
                    _logger.LogInformation("Azure OCR configured - Endpoint: {Endpoint}, Key length: {Length}", 
                        _endpoint, _subscriptionKey.Length);
                }
            }
        }

        /// <summary>
        /// Analyzes a document and extracts barangay number (158, 159, 160, or 161)
        /// </summary>
        public async Task<OcrResult> AnalyzeResidencyDocumentAsync(Stream documentStream, string fileName)
        {
            try
            {
                _logger.LogInformation("=== OCR ANALYSIS START ===");
                _logger.LogInformation("File: {FileName}", fileName);

                if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_subscriptionKey))
                {
                    _logger.LogError("Azure Computer Vision not configured");
                    return new OcrResult
                    {
                        Success = false,
                        Message = "OCR service not configured. Contact administrator."
                    };
                }

                // Validate key length before making request
                var trimmedKey = _subscriptionKey.Trim();
                if (trimmedKey.Length < 80)
                {
                    _logger.LogError("Azure OCR Key is invalid or truncated! Length: {Length} (expected ~100). Please update AzureOCR__Key in App Service with complete key.", trimmedKey.Length);
                    return new OcrResult
                    {
                        Success = false,
                        Message = "OCR service configuration error. The API key appears to be incomplete. Please contact administrator."
                    };
                }

                // Step 1: Submit document for OCR analysis
                var trimmedEndpoint = _endpoint.Trim().TrimEnd('/');
                var analyzeUrl = $"{trimmedEndpoint}/vision/v3.2/read/analyze";
                
                _logger.LogInformation("Submitting to Azure OCR: {Url}", analyzeUrl);
                _logger.LogInformation("Using key length: {Length} characters", trimmedKey.Length);

                using var request = new HttpRequestMessage(HttpMethod.Post, analyzeUrl);
                request.Headers.Add("Ocp-Apim-Subscription-Key", trimmedKey);

                // Convert stream to byte array
                using var memoryStream = new MemoryStream();
                await documentStream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                request.Content = new ByteArrayContent(imageBytes);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Azure OCR submission failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    
                    // Provide specific error messages for common issues
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogError("401 Unauthorized - Key length: {Length}. This usually means the key is invalid or truncated. Expected ~100 characters.", trimmedKey.Length);
                        return new OcrResult
                        {
                            Success = false,
                            Message = "OCR service authentication failed. The API key may be invalid or incomplete. Please contact administrator."
                        };
                    }
                    
                    return new OcrResult
                    {
                        Success = false,
                        Message = $"OCR submission failed: {response.StatusCode}"
                    };
                }

                // Get the operation location (URL to poll for results)
                if (!response.Headers.TryGetValues("Operation-Location", out var operationLocationValues))
                {
                    _logger.LogError("Operation-Location header not found in response");
                    return new OcrResult
                    {
                        Success = false,
                        Message = "OCR operation location not found"
                    };
                }

                var operationLocation = operationLocationValues.FirstOrDefault();
                _logger.LogInformation("Operation Location: {OperationLocation}", operationLocation);

                // Step 2: Poll for results
                var extractedText = await PollForResultsAsync(operationLocation);

                if (string.IsNullOrEmpty(extractedText))
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
        /// Polls the Azure OCR endpoint until results are ready
        /// </summary>
        private async Task<string> PollForResultsAsync(string operationLocation)
        {
            var maxAttempts = 10;
            var delayMs = 1000; // 1 second

            for (int i = 0; i < maxAttempts; i++)
            {
                _logger.LogInformation("Polling attempt {Attempt}/{MaxAttempts}", i + 1, maxAttempts);

                using var request = new HttpRequestMessage(HttpMethod.Get, operationLocation);
                request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey?.Trim() ?? string.Empty);

                var response = await _httpClient.SendAsync(request);
                var resultJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Poll response status: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Poll request failed: {StatusCode} - {Content}", response.StatusCode, resultJson);
                    return null;
                }

                using var doc = JsonDocument.Parse(resultJson);
                var status = doc.RootElement.GetProperty("status").GetString();

                _logger.LogInformation("OCR Status: {Status}", status);

                if (status == "succeeded")
                {
                    // Extract all text from OCR result
                    var sb = new StringBuilder();

                    if (doc.RootElement.TryGetProperty("analyzeResult", out var analyzeResult))
                    {
                        if (analyzeResult.TryGetProperty("readResults", out var readResults))
                        {
                            foreach (var page in readResults.EnumerateArray())
                            {
                                if (page.TryGetProperty("lines", out var lines))
                                {
                                    foreach (var line in lines.EnumerateArray())
                                    {
                                        if (line.TryGetProperty("text", out var text))
                                        {
                                            sb.AppendLine(text.GetString());
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return sb.ToString();
                }
                else if (status == "failed")
                {
                    _logger.LogError("OCR processing failed");
                    return null;
                }

                // Still running, wait and retry
                await Task.Delay(delayMs);
            }

            _logger.LogWarning("OCR polling timed out after {MaxAttempts} attempts", maxAttempts);
            return null;
        }

        /// <summary>
        /// Validates that the extracted text is from an actual Philippine ID document
        /// Rejects plain text, screenshots, or documents without ID markers
        /// </summary>
        private bool IsValidPhilippineIdDocument(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var upperText = text.ToUpper();
            
            // Required Philippine ID markers - document must contain at least one
            var idMarkers = new[]
            {
                // Republic of the Philippines markers
                "REPUBLIC OF THE PHILIPPINES",
                "REPUBLIKA NG PILIPINAS",
                "REPUBLIC OF THE PHILIPPINE",
                
                // Driver's License markers
                "DRIVER'S LICENSE",
                "DRIVERS LICENSE",
                "DRIVER LICENSE",
                "LICENSE TO DRIVE",
                "LAND TRANSPORTATION OFFICE",
                "LTO",
                "DEPARTMENT OF TRANSPORTATION",
                "PROFESSIONAL DRIVER",
                "NON-PROFESSIONAL DRIVER",
                "NONPROFESSIONAL DRIVER",
                
                // National ID markers
                "PHILSYS",
                "PHILIPPINE IDENTIFICATION SYSTEM",
                "PHILIPPINE NATIONAL ID",
                "NATIONAL ID",
                "PAMBANSANG PAGKAKAKILANLAN",
                "PHILIPPINE IDENTIFICATION CARD",
                
                // PhilHealth markers
                "PHILHEALTH",
                "PHIL-HEALTH",
                "PHILIPPINE HEALTH INSURANCE",
                "MEMBER ID",
                
                // UMID/SSS markers
                "UMID",
                "UNIFIED MULTI-PURPOSE ID",
                "GSIS",
                "SSS",
                "SOCIAL SECURITY",
                
                // Postal ID markers
                "POSTAL ID",
                "PHILIPPINE POSTAL",
                "PHLPOST",
                "POST OFFICE",
                
                // Passport markers
                "PASSPORT",
                "REPUBLIC OF THE PHILIPPINES PASSPORT",
                
                // TIN ID markers
                "TIN",
                "TAX IDENTIFICATION NUMBER",
                "BIR",
                "BUREAU OF INTERNAL REVENUE"
            };

            // Check if text contains at least one ID marker
            bool hasIdMarker = idMarkers.Any(marker => upperText.Contains(marker));
            
            if (!hasIdMarker)
            {
                _logger.LogWarning("⚠️ Document validation failed: No Philippine ID markers found");
                _logger.LogWarning("Text preview: {Preview}", text.Substring(0, Math.Min(500, text.Length)));
                return false;
            }

            // Additional validation: Check for ID-specific fields
            var idFields = new[]
            {
                "LAST NAME", "SURNAME", "APELYIDO", "APELLIDO",
                "FIRST NAME", "GIVEN NAME", "MGA PANGALAN",
                "DATE OF BIRTH", "BIRTH DATE", "KAPANGANAKAN",
                "ADDRESS", "TIRAHAN",
                "SEX", "GENDER", "KASARIAN"
            };

            // Document should have at least 2 ID fields (name + address or birth date)
            int fieldCount = idFields.Count(field => upperText.Contains(field));
            
            if (fieldCount < 2)
            {
                _logger.LogWarning("⚠️ Document validation failed: Insufficient ID fields found (found {Count}, need at least 2)", fieldCount);
                return false;
            }

            _logger.LogInformation("✅ Document validation passed: Philippine ID detected (markers: {Markers}, fields: {Fields})", 
                idMarkers.Count(m => upperText.Contains(m)), fieldCount);
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
            var validPatterns = new[]
            {
                @"\bBARANGAY\s+(158|159|160|161)\b",           // BARANGAY 158 (with word boundaries)
                @"\bBRGY\.?\s+(158|159|160|161)\b",            // BRGY 158 or BRGY. 158 (with word boundaries)
                @"\bBARANGAY\s+NO\.?\s+(158|159|160|161)\b",   // BARANGAY NO. 158 (with word boundaries)
                @"\bBARANGAY\s+#\s+(158|159|160|161)\b",       // BARANGAY # 158 (with word boundaries)
                @"\b(158|159|160|161)\s+BARANGAY\b",           // 158 BARANGAY (with word boundaries)
                @"(?:^|\s|,|\.)(158|159|160|161)(?:\s|$|,|\.)", // Just the numbers with context boundaries
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

    public class OcrResult
    {
        public bool Success { get; set; }
        public string BarangayNumber { get; set; }
        public string Message { get; set; }
        public string ExtractedText { get; set; }
    }
}
