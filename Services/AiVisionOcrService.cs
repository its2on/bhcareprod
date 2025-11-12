using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Barangay.Services
{
    /// <summary>
    /// AI Vision OCR Service using Google Gemini Vision API
    /// Provides intelligent document reading with context understanding
    /// </summary>
    public class AiVisionOcrService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiVisionOcrService> _logger;
        private readonly PhilippineIdParserService _idParser;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AiVisionOcrService(
            IConfiguration configuration, 
            ILogger<AiVisionOcrService> logger, 
            PhilippineIdParserService idParser,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _idParser = idParser;
            _httpClient = httpClientFactory.CreateClient("GeminiVision");
            _httpClient.Timeout = TimeSpan.FromSeconds(60);

            // Get API key from configuration
            // Priority: Environment variables > appsettings.json
            var envKey = Environment.GetEnvironmentVariable("GeminiAPI__Key");
            var configKey = _configuration["GeminiAPI:Key"] ?? _configuration["GeminiAPI__Key"];

            _apiKey = envKey?.Trim() ?? configKey?.Trim() ?? "";

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogInformation("✓ Gemini Vision API configured - Key length: {KeyLength}, Source: {Source}",
                    _apiKey.Length, !string.IsNullOrEmpty(envKey) ? "Environment Variable" : "IConfiguration");
            }
            else
            {
                _logger.LogWarning("✗ Gemini Vision API key not configured. AI Vision features will not work.");
                _logger.LogWarning("To enable: Set GeminiAPI:Key in appsettings.json or GeminiAPI__Key as environment variable");
            }
        }

        /// <summary>
        /// Analyzes ID image using Gemini Vision API with intelligent extraction
        /// </summary>
        public async Task<IdExtractionResult> AnalyzeIdImageAsync(Stream imageStream, string fileName)
        {
            try
            {
                _logger.LogInformation("=== GEMINI AI VISION ANALYSIS START ===");
                _logger.LogInformation("File: {FileName}", fileName);

                if (string.IsNullOrEmpty(_apiKey))
                {
                    return new IdExtractionResult
                    {
                        Success = false,
                        Message = "Gemini Vision API is not configured. Please check appsettings.json or environment variables."
                    };
                }

                // Read image bytes
                byte[] imageBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await imageStream.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }

                // Convert to base64
                var base64Image = Convert.ToBase64String(imageBytes);

                // Create Gemini API request
                var requestBody = new
                {
                    contents = new object[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new
                                {
                                    text = @"You are an expert at reading Philippine ID documents. Extract the following information from this ID image:

1. ID Type (Driver's License, PhilSys National ID, PhilHealth, Postal ID, UMID, TIN ID, SSS ID, or Passport)
2. Last Name (Surname/Apelyido)
3. First Name (Given Names/Mga Pangalan) - include all given names as one field
4. Middle Name (Gitnang Apelyido) - if present
5. Suffix (Jr, Sr, II, III, etc.) - if present
6. Date of Birth (format: YYYY-MM-DD)
7. Complete Address (including house number, street, barangay, city)
8. Barangay number (158, 159, 160, or 161) - if present
9. Gender (Male/Female) - if visible
10. Contact Number - if visible

IMPORTANT:
- For PhilSys National ID: Extract 'Given Names' as First Name (keep all names together like 'RHYLLE LANDER')
- For Driver's License: Name format is usually 'SURNAME, FIRST NAME MIDDLE INITIAL'
- Date format: Convert to YYYY-MM-DD (e.g., 'JUNE 12, 2003' becomes '2003-06-12')
- Address: Include full address with barangay
- Only extract information that is clearly visible on the ID

Return the information in JSON format with these exact keys:
{
  ""idType"": ""string"",
  ""lastName"": ""string"",
  ""firstName"": ""string"",
  ""middleName"": ""string"",
  ""suffix"": ""string"",
  ""birthDate"": ""YYYY-MM-DD"",
  ""address"": ""string"",
  ""barangay"": ""string"",
  ""gender"": ""string"",
  ""contactNumber"": ""string"",
  ""extractedText"": ""full OCR text from the ID""
}"
                                },
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = "image/jpeg",
                                        data = base64Image
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        topK = 32,
                        topP = 1,
                        maxOutputTokens = 2048
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Try different model names and API versions
                // Common Gemini models: gemini-1.5-flash, gemini-1.5-pro, gemini-pro, gemini-pro-vision
                var modelNames = new[] { "gemini-1.5-flash", "gemini-1.5-pro", "gemini-pro" };
                var apiVersions = new[] { "v1beta", "v1" };
                HttpResponseMessage response = null;
                string errorContent = null;
                string successfulModel = null;
                string successfulVersion = null;
                
                foreach (var apiVersion in apiVersions)
                {
                    foreach (var modelName in modelNames)
                    {
                        var url = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{modelName}:generateContent?key={_apiKey}";
                        _logger.LogInformation("Trying Gemini API: {Version} / {Model}", apiVersion, modelName);
                        
                        response = await _httpClient.PostAsync(url, content);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            successfulModel = modelName;
                            successfulVersion = apiVersion;
                            _logger.LogInformation("✓ Gemini API call successful: {Version} / {Model}", apiVersion, modelName);
                            break;
                        }
                        else
                        {
                            errorContent = await response.Content.ReadAsStringAsync();
                            _logger.LogWarning("Gemini API {Version}/{Model} failed: {StatusCode}", apiVersion, modelName, response.StatusCode);
                            
                            // If 404, try next model/version
                            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                continue;
                            }
                        }
                    }
                    
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        break; // Success, exit both loops
                    }
                }

                if (response == null || !response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini API unavailable after trying all models/versions. This is not critical - system will use standard OCR.");
                    _logger.LogInformation("Falling back to standard OCR services (Local OCR / Azure OCR)");
                    // Return gracefully - the system will fall back to other OCR services automatically
                    return new IdExtractionResult
                    {
                        Success = false,
                        Message = "AI Vision service temporarily unavailable. Using standard OCR instead."
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                
                string textResponse;
                using (var responseJson = JsonDocument.Parse(responseContent))
                {
                    // Extract the text response
                    textResponse = responseJson.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "";
                }

                _logger.LogInformation("Gemini AI response received: {Length} characters", textResponse?.Length ?? 0);

                // Try to parse JSON from the response
                var result = new IdExtractionResult
                {
                    Success = true,
                    ExtractedText = textResponse ?? ""
                };

                // Try to extract JSON from the response (it might be wrapped in markdown code blocks)
                var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                    textResponse ?? "", 
                    @"```json\s*(\{.*?\})\s*```|(\{.*\})", 
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                if (jsonMatch.Success)
                {
                    var jsonText = jsonMatch.Groups[1].Success ? jsonMatch.Groups[1].Value : jsonMatch.Groups[2].Value;
                    try
                    {
                        using (var extractedData = JsonDocument.Parse(jsonText))
                        {
                            var root = extractedData.RootElement;

                            result.FirstName = root.TryGetProperty("firstName", out var fn) ? fn.GetString()?.Trim() : "";
                            result.LastName = root.TryGetProperty("lastName", out var ln) ? ln.GetString()?.Trim() : "";
                            result.MiddleName = root.TryGetProperty("middleName", out var mn) ? mn.GetString()?.Trim() : "";
                            result.Suffix = root.TryGetProperty("suffix", out var sf) ? sf.GetString()?.Trim() : "";
                            result.BirthDate = root.TryGetProperty("birthDate", out var bd) ? bd.GetString()?.Trim() : "";
                            result.Address = root.TryGetProperty("address", out var addr) ? addr.GetString()?.Trim() : "";
                            result.BarangayNumber = root.TryGetProperty("barangay", out var brgy) ? brgy.GetString()?.Trim() : "";
                            result.Gender = root.TryGetProperty("gender", out var gen) ? gen.GetString()?.Trim() : "";
                            result.ContactNumber = root.TryGetProperty("contactNumber", out var contact) ? contact.GetString()?.Trim() : "";

                            // Use extracted text if provided
                            if (root.TryGetProperty("extractedText", out var extText))
                            {
                                result.ExtractedText = extText.GetString() ?? result.ExtractedText;
                            }
                        }

                        // Validate barangay
                        result.IsBarangayValid = !string.IsNullOrEmpty(result.BarangayNumber) &&
                                               new[] { "158", "159", "160", "161" }.Contains(result.BarangayNumber.Trim());

                        _logger.LogInformation("✓ Gemini AI extracted - FirstName: {FirstName}, LastName: {LastName}, BirthDate: {BirthDate}, Barangay: {Barangay}",
                            result.FirstName, result.LastName, result.BirthDate, result.BarangayNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse JSON from Gemini response, using text extraction instead");
                        // Fallback to text parsing
                        ParseFromText(textResponse ?? "", result);
                    }
                }
                else
                {
                    // No JSON found, try to parse from text
                    ParseFromText(textResponse ?? "", result);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Gemini AI Vision analysis");
                return new IdExtractionResult
                {
                    Success = false,
                    Message = $"AI Vision error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Fallback: Parse data from text response if JSON parsing fails
        /// </summary>
        private void ParseFromText(string text, IdExtractionResult result)
        {
            // Try to detect ID type
            var idType = _idParser.DetectIdType(text);
            if (!string.IsNullOrEmpty(idType))
            {
                // Use ID-specific parser
                var parsed = _idParser.ParseIdByType(text, idType);
                result.FirstName = parsed.FirstName;
                result.LastName = parsed.LastName;
                result.MiddleName = parsed.MiddleName;
                result.Suffix = parsed.Suffix;
                result.BirthDate = parsed.BirthDate;
                result.Address = parsed.Address;
                result.BarangayNumber = ExtractBarangay(text);
                result.Gender = parsed.Gender;
                result.ContactNumber = parsed.ContactNumber;
            }
            else
            {
                // Use generic extraction from text
                result.BirthDate = ExtractDateFromText(text);
                result.Address = ExtractAddressFromText(text);
                result.BarangayNumber = ExtractBarangay(text);
                result.ContactNumber = _idParser.ExtractContactNumber(text);
                result.Gender = _idParser.ExtractGender(text);
            }

            result.IsBarangayValid = !string.IsNullOrEmpty(result.BarangayNumber) &&
                                   new[] { "158", "159", "160", "161" }.Contains(result.BarangayNumber.Trim());
        }

        private string ExtractBarangay(string text)
        {
            var pattern = @"\bBARANGAY\s+(158|159|160|161)\b";
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : "";
        }

        private string ExtractDateFromText(string text)
        {
            // Try various date patterns
            var patterns = new[]
            {
                @"(\d{4})-(\d{2})-(\d{2})",
                @"(\d{2})/(\d{2})/(\d{4})",
                @"([A-Z]+)\s+(\d{1,2}),\s+(\d{4})"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    // Convert to YYYY-MM-DD format
                    // Implementation depends on pattern matched
                    return match.Value; // Simplified
                }
            }
            return "";
        }

        private string ExtractAddressFromText(string text)
        {
            // Simple address extraction
            var addressKeywords = new[] { "ADDRESS", "TIRAHAN", "BARANGAY" };
            foreach (var keyword in addressKeywords)
            {
                var index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var addressText = text.Substring(index + keyword.Length).Trim();
                    // Take first few lines or until next major field
                    var lines = addressText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Take(4)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                    return string.Join(", ", lines);
                }
            }
            return "";
        }
    }
}

