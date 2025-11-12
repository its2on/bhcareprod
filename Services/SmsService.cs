using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Barangay.Services
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
    }

    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;
        private readonly string _apiToken;
        private readonly string _apiUrl;

        public SmsService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SmsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            
            // Get SMS API configuration from appsettings.json
            // Try SmsSettings first, then fallback to SMS_USERNAME/SMS_PASSWORD for Azure compatibility
            _apiToken = _configuration["SmsSettings:ApiToken"] 
                ?? _configuration["SMS_PASSWORD"] 
                ?? "";
            _apiUrl = _configuration["SmsSettings:ApiUrl"] 
                ?? _configuration["SMS_USERNAME"] 
                ?? "https://sms.iprogtech.com/api/v1/sms_messages";
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiToken))
                {
                    _logger.LogWarning("SMS API Token is not configured. SMS will not be sent.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    _logger.LogWarning("Phone number is empty. SMS will not be sent.");
                    return false;
                }

                // Format phone number (remove spaces, ensure it starts with country code)
                phoneNumber = FormatPhoneNumber(phoneNumber);

                // Prepare request payload - using iprogtech API format
                var payload = new
                {
                    api_token = _apiToken,
                    phone_number = phoneNumber,
                    message = message
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending SMS to {PhoneNumber}", phoneNumber);

                // Send SMS - try multiple endpoint variations if the main one fails
                _logger.LogInformation("Attempting to send SMS to {PhoneNumber} via {ApiUrl}", phoneNumber, _apiUrl);
                _logger.LogInformation("SMS Request Payload: {Payload}", jsonPayload);
                
                // List of possible endpoint variations to try (most likely first)
                // Based on iprogtech API documentation: https://sms.iprogtech.com/api/v1/sms_messages
                var endpointVariations = new List<string>
                {
                    _apiUrl, // Original endpoint from config (should be /api/v1/sms_messages)
                    // Try variations of the v1 endpoint
                    _apiUrl.Replace("/api/v1/sms_messages", "/api/v1/sms_messages"), // Exact match
                    _apiUrl.Replace("/v1/sms_messages", "/sms_messages"), // Without v1
                    _apiUrl.Replace("/api/v1/sms_messages", "/api/sms_messages"), // Without v1
                    "https://sms.iprogtech.com/api/v1/sms_messages", // Direct correct endpoint
                    // Legacy endpoint variations (for backward compatibility)
                    _apiUrl.Replace("/api/v1/sms_messages", "/api/send"),
                    _apiUrl.Replace("/api/v1/sms_messages", "/send"),
                };
                
                // Remove duplicates while preserving order
                endpointVariations = endpointVariations.Distinct().ToList();
                
                // Try each endpoint
                foreach (var endpoint in endpointVariations)
                {
                    try
                    {
                        _logger.LogInformation("Trying SMS endpoint: {Endpoint}", endpoint);
                        var response = await _httpClient.PostAsync(endpoint, content);
                        var responseContent = await response.Content.ReadAsStringAsync();
                        
                        _logger.LogInformation("SMS API Response Status: {Status}, Content Length: {Length} for endpoint {Endpoint}", 
                            response.StatusCode, responseContent?.Length ?? 0, endpoint);

                        if (response.IsSuccessStatusCode)
                        {
                            // Try to parse JSON response to verify success
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(responseContent))
                                {
                                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                                    if (jsonResponse.TryGetProperty("status", out var statusElement))
                                    {
                                        var status = statusElement.GetInt32();
                                        if (status == 200)
                                        {
                                            _logger.LogInformation("SMS sent successfully to {PhoneNumber} via {Endpoint}. Response: {Response}", 
                                                phoneNumber, endpoint, responseContent);
                                            return true;
                                        }
                                        else
                                        {
                                            _logger.LogWarning("SMS API returned status {Status} in response body. Response: {Response}", 
                                                status, responseContent);
                                        }
                                    }
                                    else
                                    {
                                        // No status field, but HTTP status is success, assume it worked
                                        _logger.LogInformation("SMS sent successfully to {PhoneNumber} via {Endpoint}. Response: {Response}", 
                                            phoneNumber, endpoint, responseContent);
                                        return true;
                                    }
                                }
                                else
                                {
                                    // Empty response but HTTP status is success, assume it worked
                                    _logger.LogInformation("SMS sent successfully to {PhoneNumber} via {Endpoint} (empty response)", phoneNumber, endpoint);
                                    return true;
                                }
                            }
                            catch (JsonException)
                            {
                                // Not JSON or parse error, but HTTP status is success, assume it worked
                                _logger.LogInformation("SMS sent successfully to {PhoneNumber} via {Endpoint}. Response: {Response}", 
                                    phoneNumber, endpoint, responseContent);
                                return true;
                            }
                        }
                        else if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                        {
                            // If it's not 404, this might be the right endpoint but with wrong credentials/format
                            _logger.LogWarning("Endpoint {Endpoint} returned {Status}. This might be the correct endpoint but with wrong credentials. Response: {Response}", 
                                endpoint, response.StatusCode, responseContent);
                            // Continue to try other endpoints, but log this as a potential match
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error trying endpoint {Endpoint}", endpoint);
                        // Continue to next endpoint
                    }
                }
                
                // If all endpoints failed, log the error
                _logger.LogError("All SMS endpoint variations failed. The SMS service endpoint may be incorrect. Please verify the correct endpoint URL with your SMS provider (iprogtech). Last tried: {ApiUrl}", _apiUrl);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            // Remove all non-digit characters
            phoneNumber = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^\d]", "");

            // If it starts with 0, replace with country code 63 (Philippines)
            if (phoneNumber.StartsWith("0"))
            {
                phoneNumber = "63" + phoneNumber.Substring(1);
            }
            // If it doesn't start with country code, add it
            else if (!phoneNumber.StartsWith("63"))
            {
                phoneNumber = "63" + phoneNumber;
            }

            return phoneNumber;
        }
    }
}

