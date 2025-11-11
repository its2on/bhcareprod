using System;
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
            _apiToken = _configuration["SmsSettings:ApiToken"] ?? "";
            _apiUrl = _configuration["SmsSettings:ApiUrl"] ?? "https://sms.iprogtech.com/api/send";
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

                // Prepare request payload
                var payload = new
                {
                    token = _apiToken,
                    to = phoneNumber,
                    message = message
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending SMS to {PhoneNumber}", phoneNumber);

                // Send SMS
                var response = await _httpClient.PostAsync(_apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}. Response: {Response}", phoneNumber, responseContent);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to send SMS to {PhoneNumber}. Status: {Status}, Response: {Response}", 
                        phoneNumber, response.StatusCode, responseContent);
                    return false;
                }
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

