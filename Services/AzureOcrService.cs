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
        private readonly string _region;

        public AzureOcrService(IConfiguration configuration, ILogger<AzureOcrService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;

            // Prioritize environment variables from Azure App Service (they take precedence)
            // In ASP.NET Core, IConfiguration automatically includes environment variables
            // So _configuration["AzureOCR__Key"] will read from environment variables if they exist
            // We check both direct environment variables and IConfiguration to be thorough
            
            // Check direct environment variables first (most reliable)
            var envEndpoint = Environment.GetEnvironmentVariable("AzureOCR__Endpoint");
            var envKey = Environment.GetEnvironmentVariable("AzureOCR__Key");
            
            // DIAGNOSTIC: Log all AzureOCR-related environment variables to help debug
            _logger.LogWarning("=== AZURE OCR ENVIRONMENT VARIABLE DIAGNOSTICS ===");
            var allEnvVars = Environment.GetEnvironmentVariables();
            var azureOcrVars = new System.Collections.Hashtable();
            foreach (System.Collections.DictionaryEntry entry in allEnvVars)
            {
                var key = entry.Key?.ToString() ?? "";
                if (key.Contains("AzureOCR", StringComparison.OrdinalIgnoreCase) || 
                    key.Contains("OCR", StringComparison.OrdinalIgnoreCase))
                {
                    azureOcrVars[key] = entry.Value;
                    _logger.LogWarning("Found environment variable: {Key} = {Value} (Length: {Length})", 
                        key, 
                        entry.Value?.ToString()?.Substring(0, Math.Min(20, entry.Value?.ToString()?.Length ?? 0)) + "...", 
                        entry.Value?.ToString()?.Length ?? 0);
                }
            }
            if (azureOcrVars.Count == 0)
            {
                _logger.LogWarning("No AzureOCR-related environment variables found!");
            }
            _logger.LogWarning("Direct env var check - AzureOCR__Endpoint: {Value}", envEndpoint ?? "NULL");
            _logger.LogWarning("Direct env var check - AzureOCR__Key: {Value} (Length: {Length})", 
                envKey != null ? envKey.Substring(0, Math.Min(20, envKey.Length)) + "..." : "NULL", 
                envKey?.Length ?? 0);
            
            // Check IConfiguration (which includes environment variables automatically)
            var configEndpointUnderscore = _configuration["AzureOCR__Endpoint"];
            var configKeyUnderscore = _configuration["AzureOCR__Key"];
            var configEndpointColon = _configuration["AzureOCR:Endpoint"];
            var configKeyColon = _configuration["AzureOCR:Key"];
            
            _logger.LogWarning("IConfiguration check - AzureOCR__Endpoint: {Value}", configEndpointUnderscore ?? "NULL");
            _logger.LogWarning("IConfiguration check - AzureOCR__Key: {Value} (Length: {Length})", 
                configKeyUnderscore != null ? configKeyUnderscore.Substring(0, Math.Min(20, configKeyUnderscore.Length)) + "..." : "NULL",
                configKeyUnderscore?.Length ?? 0);
            _logger.LogWarning("IConfiguration check - AzureOCR:Endpoint: {Value}", configEndpointColon ?? "NULL");
            _logger.LogWarning("IConfiguration check - AzureOCR:Key: {Value} (Length: {Length})", 
                configKeyColon != null ? configKeyColon.Substring(0, Math.Min(20, configKeyColon.Length)) + "..." : "NULL",
                configKeyColon?.Length ?? 0);
            _logger.LogWarning("================================================");
            
            // Determine which source we're using and log it
            // Priority: Direct env var > IConfiguration with double underscore > IConfiguration with colon
            if (!string.IsNullOrEmpty(envKey))
            {
                _subscriptionKey = envKey.Trim();
                _logger.LogInformation("Azure OCR Key loaded from DIRECT ENVIRONMENT VARIABLE (AzureOCR__Key) - Length: {Length}", _subscriptionKey.Length);
            }
            else if (!string.IsNullOrEmpty(configKeyUnderscore))
            {
                _subscriptionKey = configKeyUnderscore.Trim();
                _logger.LogInformation("Azure OCR Key loaded from IConfiguration (AzureOCR__Key) - Length: {Length} - This may include environment variables", _subscriptionKey.Length);
            }
            else if (!string.IsNullOrEmpty(configKeyColon))
            {
                _subscriptionKey = configKeyColon.Trim();
                _logger.LogInformation("Azure OCR Key loaded from IConfiguration (AzureOCR:Key) - Length: {Length} - This may include environment variables", _subscriptionKey.Length);
            }
            else
            {
                _subscriptionKey = null;
                _logger.LogWarning("Azure OCR Key not found in any configuration source!");
            }
            
            // Validate that the key is not a placeholder
            if (!string.IsNullOrEmpty(_subscriptionKey))
            {
                var trimmedKey = _subscriptionKey.Trim();
                if (trimmedKey.Equals("YOUR_AZURE_OCR_KEY_HERE", StringComparison.OrdinalIgnoreCase) ||
                    trimmedKey.Equals("YOUR_KEY_HERE", StringComparison.OrdinalIgnoreCase) ||
                    trimmedKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ||
                    trimmedKey.Length == 0)
                {
                    _subscriptionKey = null;
                    _logger.LogError("Azure OCR Key is a placeholder or empty. Please set AzureOCR__Key in Azure App Service Configuration with the complete 100-character key from Computer Vision resource.");
                }
            }
            
            if (!string.IsNullOrEmpty(envEndpoint))
            {
                _endpoint = envEndpoint.Trim();
            }
            else if (!string.IsNullOrEmpty(configEndpointUnderscore))
            {
                _endpoint = configEndpointUnderscore.Trim();
            }
            else if (!string.IsNullOrEmpty(configEndpointColon))
            {
                _endpoint = configEndpointColon.Trim();
            }
            else
            {
                _endpoint = null;
            }
            
            // Remove any hidden characters or encoding issues
            // IMPORTANT: Only remove control characters and leading/trailing whitespace
            // Do NOT remove any alphanumeric characters as they are part of the key
            if (!string.IsNullOrEmpty(_subscriptionKey))
            {
                var originalLength = _subscriptionKey.Length;
                // Remove only control characters (non-printable), keep all alphanumeric and special chars
                _subscriptionKey = new string(_subscriptionKey.Where(c => !char.IsControl(c)).ToArray()).Trim();
                if (_subscriptionKey.Length != originalLength)
                {
                    _logger.LogWarning("Azure OCR Key was modified during sanitization - Original length: {Original}, New length: {New}", originalLength, _subscriptionKey.Length);
                }
            }

            // Get region from configuration or environment variable
            // For multi-service resources, region header is required
            var envRegion = Environment.GetEnvironmentVariable("AzureOCR__Region");
            var configRegionUnderscore = _configuration["AzureOCR__Region"];
            var configRegionColon = _configuration["AzureOCR:Region"];
            
            _region = !string.IsNullOrEmpty(envRegion) 
                ? envRegion.Trim() 
                : !string.IsNullOrEmpty(configRegionUnderscore) 
                    ? configRegionUnderscore.Trim() 
                    : !string.IsNullOrEmpty(configRegionColon) 
                        ? configRegionColon.Trim() 
                        : "southeastasia"; // Default to southeastasia based on resource location
            
            // Enhanced validation and logging
            if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_subscriptionKey))
            {
                _logger.LogWarning("Azure Computer Vision credentials not configured");
            }
            else
            {
                // Validate key length (Azure Computer Vision keys are typically 100 characters, but can vary)
                var trimmedKey = _subscriptionKey.Trim();
                
                // Log the exact key being used for debugging (first and last 10 chars only for security)
                _logger.LogInformation("Azure OCR Key loaded - Length: {Length} characters, First 10: {First10}, Last 10: {Last10}", 
                    trimmedKey.Length,
                    trimmedKey.Substring(0, Math.Min(10, trimmedKey.Length)),
                    trimmedKey.Substring(Math.Max(0, trimmedKey.Length - 10)));
                
                // Critical validation: Keys should be 100 characters. 83 characters indicates truncation.
                if (trimmedKey.Length < 50)
                {
                    _logger.LogError("Azure OCR Key appears to be too short! Length: {Length} characters. First 10: {First10}, Last 10: {Last10}. Please verify the complete key from Computer Vision resource.", 
                        trimmedKey.Length, 
                        trimmedKey.Substring(0, Math.Min(10, trimmedKey.Length)),
                        trimmedKey.Substring(Math.Max(0, trimmedKey.Length - 10)));
                }
                else if (trimmedKey.Length < 95)
                {
                    _logger.LogError("Azure OCR Key length is {Length} characters (expected 100). The key appears to be INCOMPLETE/TRUNCATED. This will cause 401 Unauthorized errors. Please copy the COMPLETE 100-character key from Azure Portal → Computer Vision resource (bhcare-ocr) → Keys and Endpoint → KEY 1, and update AzureOCR__Key in Azure App Service Configuration. Current key appears to be truncated.", trimmedKey.Length);
                }
                else if (trimmedKey.Length == 100)
                {
                    _logger.LogInformation("Azure OCR configured - Endpoint: {Endpoint}, Key length: {Length} (correct), Region: {Region}", 
                        _endpoint, trimmedKey.Length, _region);
                }
                else
                {
                    _logger.LogInformation("Azure OCR configured - Endpoint: {Endpoint}, Key length: {Length}, Region: {Region}", 
                        _endpoint, trimmedKey.Length, _region);
                }
            }
        }

        /// <summary>
        /// Extracts region from endpoint URL if it's a regional endpoint
        /// Returns null for custom endpoints
        /// </summary>
        private string ExtractRegionFromEndpoint(string endpoint)
        {
            try
            {
                // Check if it's a regional endpoint: https://{region}.api.cognitive.microsoft.com
                if (endpoint.Contains(".api.cognitive.microsoft.com"))
                {
                    var uri = new Uri(endpoint);
                    var host = uri.Host;
                    var parts = host.Split('.');
                    if (parts.Length > 0 && parts[0] != "api")
                    {
                        return parts[0]; // Return the region (e.g., "eastus", "westus2")
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract region from endpoint: {Endpoint}", endpoint);
            }
            
            // Custom endpoints (like bhcare-ocr.cognitiveservices.azure.com) don't need region header
            return null;
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
                // Azure Computer Vision keys are typically 100 characters
                // Keys with length < 95 are likely truncated and will cause 401 errors
                var trimmedKey = _subscriptionKey.Trim();
                if (trimmedKey.Length < 50)
                {
                    _logger.LogError("Azure OCR Key appears to be too short! Length: {Length} characters. Please verify the complete key from Computer Vision resource.", trimmedKey.Length);
                    return new OcrResult
                    {
                        Success = false,
                        Message = $"OCR service configuration error. The API key appears to be incomplete (length: {trimmedKey.Length} characters, expected 100). Please verify AzureOCR__Key in Azure App Service contains the complete key from Computer Vision resource (bhcare-ocr) → Keys and Endpoint → KEY 1."
                    };
                }
                
                // Critical: Keys with length < 95 are likely truncated (83 characters is a common truncation)
                if (trimmedKey.Length < 95)
                {
                    _logger.LogError("Azure OCR Key length is {Length} characters (expected 100). The key appears to be INCOMPLETE/TRUNCATED. This will cause 401 Unauthorized errors. Please copy the COMPLETE 100-character key from Azure Portal → Computer Vision resource (bhcare-ocr) → Keys and Endpoint → KEY 1, and update AzureOCR__Key in Azure App Service Configuration.", trimmedKey.Length);
                    return new OcrResult
                    {
                        Success = false,
                        Message = $"OCR service configuration error. The API key appears to be INCOMPLETE/TRUNCATED (length: {trimmedKey.Length} characters, expected 100). " +
                            $"This typically happens when the key is not fully copied from Azure Portal. " +
                            $"Please: 1) Go to Azure Portal → Computer Vision resource (bhcare-ocr) → Keys and Endpoint, 2) Click 'Show' next to KEY 1, 3) Copy the COMPLETE 100-character key (ensure no characters are cut off), " +
                            $"4) Update AzureOCR__Key in Azure App Service → Configuration → Application settings with the complete key, 5) Restart the app service."
                    };
                }

                // Step 1: Submit document for OCR analysis
                // Ensure endpoint is properly formatted (remove trailing slash, ensure it's the base endpoint)
                var baseEndpoint = _endpoint.Trim().TrimEnd('/');
                if (!baseEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                    !baseEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    baseEndpoint = "https://" + baseEndpoint;
                }
                
                // Use v4.0 API endpoint for better performance and features
                var analyzeUrl = $"{baseEndpoint}/vision/v4.0/read/analyze";
                
                // Diagnostic logging
                _logger.LogInformation("Submitting to Azure OCR: {Url}", analyzeUrl);
                _logger.LogInformation("Endpoint: {Endpoint}", baseEndpoint);
                _logger.LogInformation("Using key length: {Length} characters", trimmedKey.Length);
                _logger.LogInformation("Key first 10 chars: {First10}", trimmedKey.Substring(0, Math.Min(10, trimmedKey.Length)));
                _logger.LogInformation("Key last 10 chars: {Last10}", trimmedKey.Substring(Math.Max(0, trimmedKey.Length - 10)));
                
                // Log the full key for debugging (only in development)
                #if DEBUG
                _logger.LogInformation("FULL KEY FOR DEBUGGING: {FullKey}", trimmedKey);
                #endif
                
                // Check if key contains only valid characters (alphanumeric and some special chars)
                if (trimmedKey.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
                {
                    _logger.LogWarning("Key contains control characters or whitespace - this may cause authentication issues");
                }
                
                // Verify key format - Azure keys should be alphanumeric
                var invalidChars = trimmedKey.Where(c => !char.IsLetterOrDigit(c)).ToList();
                if (invalidChars.Any())
                {
                    _logger.LogWarning("Key contains non-alphanumeric characters: {Chars}. This may cause authentication issues.", string.Join(", ", invalidChars.Distinct()));
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, analyzeUrl);
                
                // Use TryAddWithoutValidation to avoid header validation issues
                if (!request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", trimmedKey))
                {
                    _logger.LogError("Failed to add Ocp-Apim-Subscription-Key header");
                    return new OcrResult
                    {
                        Success = false,
                        Message = "Failed to configure OCR request. Please contact administrator."
                    };
                }
                
                // Add region header for multi-service resources (required for Computer Vision)
                // For custom endpoints like bhcare-ocr.cognitiveservices.azure.com, region header is required
                // Region is set to southeastasia based on resource location
                if (!string.IsNullOrEmpty(_region))
                {
                    if (!request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Region", _region))
                    {
                        _logger.LogWarning("Failed to add Ocp-Apim-Subscription-Region header, but continuing anyway");
                    }
                    else
                    {
                        _logger.LogInformation("Added region header: {Region} (required for multi-service Computer Vision resource)", _region);
                    }
                }
                else
                {
                    // Try to extract region from endpoint if it's a regional endpoint
                    // Regional endpoints look like: https://{region}.api.cognitive.microsoft.com
                    var extractedRegion = ExtractRegionFromEndpoint(baseEndpoint);
                    if (!string.IsNullOrEmpty(extractedRegion))
                    {
                        if (!request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Region", extractedRegion))
                        {
                            _logger.LogWarning("Failed to add Ocp-Apim-Subscription-Region header, but continuing anyway");
                        }
                        else
                        {
                            _logger.LogInformation("Added region header from endpoint: {Region}", extractedRegion);
                        }
                    }
                    else
                    {
                        // Default to southeastasia if no region found
                        if (!request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Region", "southeastasia"))
                        {
                            _logger.LogWarning("Failed to add default Ocp-Apim-Subscription-Region header (southeastasia)");
                        }
                        else
                        {
                            _logger.LogInformation("Added default region header: southeastasia");
                        }
                    }
                }
                
                // Verify the key header was added correctly
                if (!request.Headers.Contains("Ocp-Apim-Subscription-Key"))
                {
                    _logger.LogError("Ocp-Apim-Subscription-Key header not found in request");
                    return new OcrResult
                    {
                        Success = false,
                        Message = "Failed to configure OCR request headers. Please contact administrator."
                    };
                }

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
                        string errorMessage;
                        if (trimmedKey.Length < 95)
                        {
                            // Key is truncated (83 characters indicates incomplete copy)
                            errorMessage = $"OCR service authentication failed (401 Unauthorized). The API key appears to be INCOMPLETE/TRUNCATED (length: {trimmedKey.Length} characters, expected 100 characters). " +
                                $"This typically happens when the key is not fully copied from Azure Portal. " +
                                $"Please: 1) Go to Azure Portal → Computer Vision resource (bhcare-ocr) → Keys and Endpoint, 2) Click 'Show' next to KEY 1, 3) Copy the COMPLETE 100-character key (ensure no characters are cut off), " +
                                $"4) Update AzureOCR__Key in Azure App Service → Configuration → Application settings with the complete key, 5) Restart the app service.";
                        }
                        else if (trimmedKey.Length == 100)
                        {
                            // Key length is correct but still getting 401 - might be wrong key or missing region header
                            errorMessage = "OCR service authentication failed (401 Unauthorized). The API key length is correct (100 characters), but authentication still failed. " +
                                "Possible causes: 1) The key may be incorrect or expired - verify KEY 1 in Azure Portal → Computer Vision resource (bhcare-ocr) → Keys and Endpoint, " +
                                "2) The key may not match the Computer Vision resource, 3) Region header may be missing. " +
                                "Please verify AzureOCR__Key in Azure App Service Configuration matches exactly with KEY 1 from the Computer Vision resource, and ensure AzureOCR__Region is set to 'southeastasia'.";
                        }
                        else
                        {
                            errorMessage = "OCR service authentication failed (401 Unauthorized). The API key may be invalid, expired, or doesn't match the Computer Vision resource. " +
                                "Please verify the key in Azure Portal → Computer Vision resource (bhcare-ocr) → Keys and Endpoint, and ensure AzureOCR__Key in Azure App Service Configuration matches exactly.";
                        }
                        
                        _logger.LogError("401 Unauthorized - Endpoint: {Endpoint}, Key length: {Length}, Region: {Region}. Azure error: {Error}", 
                            baseEndpoint, trimmedKey.Length, _region ?? "not set", errorContent);
                        return new OcrResult
                        {
                            Success = false,
                            Message = errorMessage
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
                
                // Add region header for polling requests as well
                if (!string.IsNullOrEmpty(_region))
                {
                    request.Headers.Add("Ocp-Apim-Subscription-Region", _region);
                }

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
        /// STRICT VALIDATION: Requires actual ID document markers, not just address fields
        /// Returns tuple (isValid, idType)
        /// </summary>
        private (bool isValid, string idType) IsValidPhilippineIdDocument(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (false, null);

            var upperText = text.ToUpper();
            
            // DEBUG: Log extracted text to diagnose issues
            _logger.LogInformation("📝 Extracted text length: {Length} characters", upperText.Length);
            _logger.LogInformation("📝 Text preview (first 500 chars): {Preview}", upperText.Length > 500 ? upperText.Substring(0, 500) : upperText);
            
            // CRITICAL: Check for screenshot indicators in text (screenshots often have UI elements)
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
            if (screenshotIndicators.Any(indicator => upperText.Contains(indicator)))
            {
                _logger.LogWarning("⚠️ Document validation failed: Screenshot indicators found in text");
                return (false, "Screenshot Detected - Please upload a photo of your actual ID, not a screenshot");
            }
            
            // CRITICAL: Check for handwritten indicators (handwritten IDs are not accepted)
            // Handwritten text often has inconsistent character spacing, mixed case, and lacks structure
            // Note: We detect handwritten patterns through analysis rather than keyword matching
            
            // Detect handwritten patterns: excessive mixed case, inconsistent spacing, irregular patterns
            // Real IDs have consistent formatting - handwritten ones don't
            var mixedCaseRatio = text.Count(char.IsLower) / (double)Math.Max(text.Count(char.IsLetter), 1);
            var hasExcessiveMixedCase = mixedCaseRatio > 0.3 && mixedCaseRatio < 0.7; // Handwritten often mixes case inconsistently
            
            // Check for irregular spacing patterns (handwritten often has inconsistent spacing)
            var irregularSpacingPattern = Regex.IsMatch(text, @"\w\s{3,}\w"); // Multiple spaces between words
            var hasIrregularSpacing = irregularSpacingPattern;
            
            // Check for handwriting-like patterns: inconsistent line breaks, irregular formatting
            var lineBreakCount = text.Count(c => c == '\n' || c == '\r');
            var hasIrregularLineBreaks = lineBreakCount > 20 && lineBreakCount < 100; // Too many line breaks suggests handwritten
            
            // If multiple handwriting indicators are present, reject
            int handwritingScore = 0;
            if (hasExcessiveMixedCase) handwritingScore++;
            if (hasIrregularSpacing) handwritingScore++;
            if (hasIrregularLineBreaks) handwritingScore++;
            
            // Also check for common handwritten phrases
            var handwrittenPhrases = new[] { 
                "HANDWRITTEN", "HAND WRITTEN", "WRITTEN BY HAND", "MANUAL SIGNATURE", 
                "SIGNED BY HAND", "PEN", "PENCIL", "HANDWRITE", "MANUALLY WRITTEN"
            };
            if (handwrittenPhrases.Any(phrase => upperText.Contains(phrase)))
            {
                handwritingScore += 2; // Strong indicator
            }
            
            if (handwritingScore >= 2)
            {
                _logger.LogWarning("⚠️ Document validation failed: Handwritten document detected");
                _logger.LogWarning("Handwriting indicators: Mixed case={MixedCase}, Irregular spacing={Spacing}, Irregular breaks={Breaks}", 
                    hasExcessiveMixedCase, hasIrregularSpacing, hasIrregularLineBreaks);
                return (false, "Handwritten Document Detected - Please upload a photo of your official printed ID, not a handwritten document");
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
                "PHIL HEALTH",
                "PHILIPPINE HEALTH INSURANCE",
                "HEALTH INSURANCE CORPORATION",
                "MEMBER ID",
                "MDR ID", // PhilHealth Member Data Record
                
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
                "PASAPORTE",
                "REPUBLIC OF THE PHILIPPINES PASSPORT",
                "REPUBLIKA NG PILIPINAS PASSPORT",
                "P<PHL", // Machine readable zone marker for PH passport
                
                // TIN ID markers (REQUIRED)
                "TIN",
                "TAX IDENTIFICATION NUMBER",
                "BIR",
                "BUREAU OF INTERNAL REVENUE"
            };

            // Check for STRONG ID markers first and detect ID type
            bool hasStrongIdMarker = strongIdMarkers.Any(marker => upperText.Contains(marker));
            string detectedIdType = null;
            
            // AGGRESSIVE: Detect specific ID type with partial matches for OCR errors
            if (upperText.Contains("PHILSYS") || upperText.Contains("PAMBANSANG") || upperText.Contains("PAGKAKAKILANLAN") || upperText.Contains("PHILIPPINE IDENTIFICATION"))
                detectedIdType = "PhilSys";
            else if ((upperText.Contains("DRIVER") || upperText.Contains("DRIVERS")) && upperText.Contains("LICENSE"))
                detectedIdType = "Driver's License";
            else if (upperText.Contains("TRANSPORTATION") || upperText.Contains("LTO"))
                detectedIdType = "Driver's License";
            else if (upperText.Contains("PHILHEALTH") || upperText.Contains("PHIL-HEALTH") || upperText.Contains("PHIL HEALTH") || 
                     upperText.Contains("HEALTH INSURANCE CORPORATION") || upperText.Contains("MDR ID"))
                detectedIdType = "PhilHealth ID";
            else if (upperText.Contains("PASSPORT") || upperText.Contains("PASAPORTE") || upperText.Contains("P<PHL"))
                detectedIdType = "Passport";
            else if (upperText.Contains("UMID") || upperText.Contains("UNIFIED MULTI-PURPOSE") || upperText.Contains("MULTI-PURPOSE ID"))
                detectedIdType = "UMID";
            else if (upperText.Contains("SSS") || upperText.Contains("SOCIAL SECURITY"))
                detectedIdType = "SSS ID";
            else if (upperText.Contains("POSTAL") || upperText.Contains("PHLPOST") || upperText.Contains("POST OFFICE"))
                detectedIdType = "Postal ID";
            else if (upperText.Contains("TIN") || upperText.Contains("TAX IDENTIFICATION") || upperText.Contains("BIR"))
                detectedIdType = "TIN ID";
            else if (upperText.Contains("GSIS"))
                detectedIdType = "GSIS ID";
            else if (upperText.Contains("REPUBLIK") || upperText.Contains("PILIPINAS") || (upperText.Contains("REPUBLIC") && upperText.Contains("PHILIPP")))
                detectedIdType = "Philippine Government ID";
            
            _logger.LogInformation("🔍 Strong marker found: {HasMarker}, Detected type: {IdType}", hasStrongIdMarker, detectedIdType ?? "None");
            
            // Also check for partial matches of strong markers (handle OCR errors)
            // VERY AGGRESSIVE: Even partial words should trigger acceptance
            if (!hasStrongIdMarker)
            {
                hasStrongIdMarker = 
                    upperText.Contains("REPUBLIK") || // OCR error for "REPUBLIKA"
                    upperText.Contains("PILIPINAS") || // Tagalog spelling
                    (upperText.Contains("REPUBLIC") && upperText.Contains("PHILIPP")) || // Partial match
                    upperText.Contains("PAMBANSANG") || // PhilSys marker
                    upperText.Contains("PAGKAKAKILANLAN") || // PhilSys marker
                    upperText.Contains("TRANSPORTATION") || // LTO
                    ((upperText.Contains("DRIVER") || upperText.Contains("DRIVERS")) && upperText.Contains("LICENSE")) ||
                    upperText.Contains("PHILSYS") ||
                    upperText.Contains("PHILHEALTH") || upperText.Contains("PHIL HEALTH") || // PhilHealth
                    upperText.Contains("HEALTH INSURANCE") || // PhilHealth Corporation
                    upperText.Contains("UMID") ||
                    upperText.Contains("POSTAL") ||
                    upperText.Contains("PASSPORT") || upperText.Contains("PASAPORTE") || // Passport EN/TL
                    upperText.Contains("P<PHL") || // Passport machine readable zone
                    upperText.Contains("LTO") || // Land Transportation Office
                    ((upperText.Contains("TAX") || upperText.Contains("TIN")) && upperText.Contains("IDENTIFICATION"));
                    
                if (hasStrongIdMarker)
                {
                    _logger.LogInformation("✅ Found marker through partial matching!");
                }
            }
            
            // CRITICAL: Must have a STRONG ID marker - screenshots won't have these
            if (!hasStrongIdMarker)
            {
                _logger.LogWarning("⚠️ Document validation failed: No strong Philippine ID markers found");
                _logger.LogWarning("Text preview: {Preview}", text.Substring(0, Math.Min(500, text.Length)));
                _logger.LogWarning("Screenshots and non-ID documents are rejected. Please upload an actual Philippine ID document.");
                return (false, "Unverified / Invalid ID Image");
            }

            // Additional validation: Check for ID-specific fields (more lenient to handle OCR errors)
            var idFields = new[]
            {
                "LAST NAME", "SURNAME", "APELYIDO", "APELLIDO", "LAST", "NAME",
                "FIRST NAME", "GIVEN NAME", "MGA PANGALAN", "FIRST", "GIVEN",
                "DATE OF BIRTH", "BIRTH DATE", "KAPANGANAKAN", "BIRTH", "DATE",
                "ADDRESS", "TIRAHAN", "BARANGAY", "BARANG", "BRGY",
                "SEX", "GENDER", "KASARIAN", "MALE", "FEMALE"
            };

            // Document should have at least 1 ID field if strong marker exists
            // (Reduced from 2 to be more lenient with OCR variations)
            int fieldCount = idFields.Count(field => upperText.Contains(field));
            
            // Also check for common patterns even if exact words not found
            if (Regex.IsMatch(upperText, @"\b[A-Z]{2,},\s*[A-Z]{2,}\b")) // Name pattern: LOPEZ, ANTHONY
                fieldCount++;
            if (Regex.IsMatch(upperText, @"\d{2}/\d{2}/\d{4}")) // Date pattern: 10/14/2003
                fieldCount++;
            if (Regex.IsMatch(upperText, @"BARANG(AY)?\s*\d{3}")) // Barangay pattern
                fieldCount++;
            
            // VERY LENIENT: If we have strong ID markers, ALWAYS pass validation
            // Strong markers like "REPUBLIKA NG PILIPINAS", "DRIVER'S LICENSE", "PHILSYS" are enough
            if (hasStrongIdMarker)
            {
                if (fieldCount < 1)
                {
                    _logger.LogInformation("✅ Passing validation based on strong government markers (ID Type: {IdType})", detectedIdType);
                }
                // Auto-pass with strong markers - don't require field counts
            }
            else if (fieldCount < 1)
            {
                _logger.LogWarning("⚠️ Document validation failed: No strong markers and no ID fields found");
                return (false, "Unverified / Invalid ID Image");
            }

            _logger.LogInformation("✅ Document validation passed: Philippine ID detected");
            _logger.LogInformation("   ID Type: {IdType}", detectedIdType ?? "Unknown Philippine ID");
            _logger.LogInformation("   Strong Markers: {Markers}, ID Fields: {Fields}", strongIdMarkers.Count(m => upperText.Contains(m)), fieldCount);
            return (true, detectedIdType ?? "Philippine Government ID");
        }

        /// <summary>
        /// Searches for Barangay 158, 159, 160, or 161 in the extracted text
        /// STRICT VALIDATION: Only matches exactly 158, 159, 160, or 161
        /// Requires actual Philippine ID document (not plain text or screenshots)
        /// </summary>
        private OcrResult ExtractBarangayNumber(string text)
        {
            // STEP 0: Validate that this is an actual Philippine ID document
            var (isValid, idType) = IsValidPhilippineIdDocument(text);
            
            if (!isValid)
            {
                _logger.LogError("❌ REJECTED: Document is not a valid Philippine ID");
                _logger.LogError("The uploaded file appears to be plain text, a screenshot, or not a valid Philippine ID document.");
                _logger.LogError("Please upload an actual Philippine ID document (Driver's License, National ID, PhilHealth ID, etc.)");
                
                bool isScreenshot = idType?.Contains("Screenshot") == true;
                
                // Provide specific error message based on rejection reason
                string errorMessage = "Invalid document type. ";
                if (isScreenshot)
                {
                    errorMessage = "Screenshot detected. Please upload a photo of your actual physical ID document, not a screenshot from your device. ";
                }
                else if (idType?.Contains("Handwritten") == true)
                {
                    errorMessage = "Handwritten document detected. Please upload a photo of your official printed ID document, not a handwritten document. ";
                }
                else
                {
                    errorMessage = "The uploaded document is not a recognized Philippine government-issued ID. ";
                }
                
                errorMessage += "Accepted IDs include: Driver's License, National ID (PhilSys), PhilHealth ID, Postal ID, UMID, TIN ID, SSS ID, or Passport. " +
                               "Only printed official documents are accepted. Screenshots, handwritten documents, or non-printed documents will be rejected.";
                
                return new OcrResult
                {
                    Success = false,
                    Status = "unverified",
                    IdType = idType,
                    BarangayMatch = false,
                    Message = errorMessage,
                    ExtractedText = text,
                    IsScreenshot = isScreenshot
                };
            }
            
            _logger.LogInformation("📄 Valid Philippine ID detected: {IdType}", idType);

            // Define valid barangays - ONLY these are accepted
            var validBarangays = new[] { "158", "159", "160", "161" };
            
            // STEP 1: First, try to find VALID barangays (158-161) - highest priority
            // Try multiple regex patterns to catch variations - STRICT patterns only
            var validPatterns = new[]
            {
                // HIGHEST PRIORITY: "BARANGAY 161," or "BARANGAY 161" (handles commas, periods, etc.)
                @"\bBARANGAY\s+(158|159|160|161)(?:[,\s\.]|$|\b)",           // BARANGAY 158 (with punctuation support)
                @"BARANGAY\s+(158|159|160|161)(?:[,\s\.]|$|\b)",            // BARANGAY 158 (without word boundary)
                @"\bBRGY\.?\s+(158|159|160|161)(?:[,\s\.]|$|\b)",            // BRGY 158 or BRGY. 158 (with punctuation support)
                @"\bBARANGAY\s+NO\.?\s+(158|159|160|161)(?:[,\s\.]|$|\b)",   // BARANGAY NO. 158 (with punctuation support)
                @"\bBARANGAY\s+#\s+(158|159|160|161)(?:[,\s\.]|$|\b)",       // BARANGAY # 158 (with punctuation support)
                @"\b(158|159|160|161)\s+BARANGAY\b",           // 158 BARANGAY (with word boundaries)
                @"(?:^|\s|,|\.)(158|159|160|161)(?:\s|$|,|\.)", // Just the numbers with context boundaries
                // More lenient patterns for OCR errors - updated to handle punctuation
                @"BARANGAY\s*[:\-]?\s*(158|159|160|161)(?:[,\s\.]|$|\b)",     // BARANGAY: 161 or BARANGAY-161
                @"BARANGAY\s+(?:NO\.?\s+)?(158|159|160|161)(?:[,\s\.]|$|\b)", // BARANGAY NO 161 (with optional NO)
                @"(158|159|160|161)\s*(?:BARANGAY|BRGY)",      // 161 BARANGAY or 161 BRGY
                // Direct matches - updated to handle punctuation
                @"BARANGAY\s*161(?:[,\s\.]|$|\b)|BARANGAY\s*160(?:[,\s\.]|$|\b)|BARANGAY\s*159(?:[,\s\.]|$|\b)|BARANGAY\s*158(?:[,\s\.]|$|\b)",
            };

            // Log the text being searched for debugging
            _logger.LogInformation("=== SEARCHING FOR BARANGAY 158-161 ===");
            _logger.LogInformation("Text length: {Length} characters", text.Length);
            _logger.LogInformation("Text preview (first 1000 chars): {Preview}", text.Length > 1000 ? text.Substring(0, 1000) + "..." : text);
            
            foreach (var pattern in validPatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success && match.Groups.Count > 1)
                {
                    var barangayNumber = match.Groups[1].Value.Trim();
                    _logger.LogInformation("🔍 Pattern matched: {Pattern} → Found: {Barangay}", pattern, barangayNumber);
                    
                    // CRITICAL: Double-check that the detected barangay is EXACTLY in the valid list
                    if (validBarangays.Contains(barangayNumber))
                    {
                        _logger.LogInformation("=== ✅ BARANGAY FOUND (VALIDATED) ===");
                        _logger.LogInformation("Pattern: {Pattern}", pattern);
                        _logger.LogInformation("Barangay: {Barangay}", barangayNumber);
                        _logger.LogInformation("ID Type: {IdType}", idType);

                        return new OcrResult
                        {
                            Success = true,
                            Status = "verified",
                            IdType = idType,
                            BarangayMatch = true,
                            BarangayNumber = barangayNumber,
                            Message = $"Valid {idType} and Barangay {barangayNumber} verified",
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
                            Status = "unverified",
                            IdType = idType,
                            BarangayMatch = false,
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
                Status = "unverified",
                IdType = idType,
                BarangayMatch = false,
                Message = "Unable to verify residency. No valid Barangay number (158, 159, 160, or 161) found in the document. " +
                         "Please ensure your ID clearly displays your address with Barangay 158, 159, 160, or 161. " +
                         "The address must be clearly visible and readable in the uploaded document.",
                ExtractedText = text
            };
        }
    }

    public class OcrResult
    {
        public bool Success { get; set; }
        public string Status { get; set; } // "verified" or "unverified"
        public string IdType { get; set; } // "PhilSys", "Driver's License", etc.
        public bool BarangayMatch { get; set; }
        public string BarangayNumber { get; set; }
        public string Message { get; set; }
        public string ExtractedText { get; set; }
        public double? ConfidenceScore { get; set; } // OCR confidence level
        public bool IsScreenshot { get; set; }
    }
}
