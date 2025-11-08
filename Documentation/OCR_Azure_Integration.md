# Azure Computer Vision OCR Integration

## Overview
This document provides step-by-step instructions to integrate Azure Computer Vision as an OCR provider, either as the primary service or as a fallback for Tesseract.

---

## Step 1: Create Azure Computer Vision Resource

### Using Azure Portal:

1. **Sign in to Azure Portal**: https://portal.azure.com

2. **Create Resource**:
   - Click "Create a resource"
   - Search for "Computer Vision"
   - Click "Create"

3. **Configure Resource**:
   ```
   Resource Group: bhcare-resources (or create new)
   Name: bhcare-ocr-service
   Region: Southeast Asia (or nearest region)
   Pricing Tier: F0 (Free) or S1 (Standard)
   ```

4. **Review + Create**: Click "Create" and wait for deployment

5. **Get Credentials**:
   - Go to resource → "Keys and Endpoint"
   - Copy **Key 1** (or Key 2)
   - Copy **Endpoint** URL

### Using Azure CLI:

```bash
# Login to Azure
az login

# Create resource group
az group create --name bhcare-resources --location southeastasia

# Create Computer Vision resource
az cognitiveservices account create \
  --name bhcare-ocr-service \
  --resource-group bhcare-resources \
  --kind ComputerVision \
  --sku F0 \
  --location southeastasia \
  --yes

# Get endpoint
az cognitiveservices account show \
  --name bhcare-ocr-service \
  --resource-group bhcare-resources \
  --query "properties.endpoint" -o tsv

# Get key
az cognitiveservices account keys list \
  --name bhcare-ocr-service \
  --resource-group bhcare-resources \
  --query "key1" -o tsv
```

---

## Step 2: Configure Application Settings

### Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "AzureComputerVision": {
    "Endpoint": "https://bhcare-ocr-service.cognitiveservices.azure.com/",
    "SubscriptionKey": "your-subscription-key-here",
    "ApiVersion": "v3.2",
    "ReadOperationTimeoutSeconds": 30,
    "MaxRetries": 3
  },
  "OcrSettings": {
    "Provider": "Tesseract",
    "FallbackToAzure": true,
    "FallbackOnErrors": ["blur", "no text", "empty result"],
    "MaxRetries": 2,
    "RetryDelayMilliseconds": 1000
  }
}
```

### Update `appsettings.Production.json`:

```json
{
  "AzureComputerVision": {
    "Endpoint": "https://your-production-resource.cognitiveservices.azure.com/",
    "SubscriptionKey": "${AZURE_CV_KEY}"
  }
}
```

**Important**: Store the subscription key in environment variables or Azure Key Vault for production.

---

## Step 3: Add Azure OCR Service Class

Create new file: `Services/AzureOcrService.cs`

```csharp
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BHCARE.Services
{
    public interface IAzureOcrService
    {
        Task<string> ExtractTextFromImageAsync(string imagePath);
    }
    
    public class AzureOcrService : IAzureOcrService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureOcrService> _logger;
        
        private readonly string _endpoint;
        private readonly string _subscriptionKey;
        private readonly int _timeout;
        private readonly int _maxRetries;
        
        public AzureOcrService(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            ILogger<AzureOcrService> logger)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _logger = logger;
            
            _endpoint = _configuration["AzureComputerVision:Endpoint"];
            _subscriptionKey = _configuration["AzureComputerVision:SubscriptionKey"];
            _timeout = int.Parse(_configuration["AzureComputerVision:ReadOperationTimeoutSeconds"] ?? "30");
            _maxRetries = int.Parse(_configuration["AzureComputerVision:MaxRetries"] ?? "3");
            
            if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_subscriptionKey))
            {
                throw new InvalidOperationException("Azure Computer Vision credentials not configured");
            }
        }
        
        public async Task<string> ExtractTextFromImageAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image file not found", imagePath);
            }
            
            try
            {
                _logger.LogInformation("Starting Azure Computer Vision OCR");
                
                // Read image bytes
                byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
                _logger.LogInformation($"Image size: {imageBytes.Length} bytes");
                
                // Create HTTP client
                var client = _clientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(_timeout);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
                
                // Call Read API to start analysis
                var readUrl = $"{_endpoint.TrimEnd('/')}/vision/v3.2/read/analyze?language=en&readingOrder=natural";
                
                var content = new ByteArrayContent(imageBytes);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                
                _logger.LogInformation($"Calling Azure Read API: {readUrl}");
                var response = await client.PostAsync(readUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Azure API error: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"Azure Read API failed: {response.StatusCode}");
                }
                
                // Get operation location from response headers
                if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
                {
                    throw new InvalidOperationException("No Operation-Location header in response");
                }
                
                var operationLocation = operationLocations.FirstOrDefault();
                _logger.LogInformation($"Operation location: {operationLocation}");
                
                // Poll for results
                string extractedText = await PollForResultAsync(client, operationLocation);
                
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    throw new InvalidOperationException("Azure OCR returned no text");
                }
                
                _logger.LogInformation($"Azure OCR extracted {extractedText.Length} characters");
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure OCR failed");
                throw;
            }
        }
        
        private async Task<string> PollForResultAsync(HttpClient client, string operationLocation)
        {
            int attempts = 0;
            
            while (attempts < _maxRetries * 10) // Poll up to 10 times per retry
            {
                await Task.Delay(1000); // Wait 1 second between polls
                attempts++;
                
                _logger.LogDebug($"Polling for results (attempt {attempts})");
                
                var resultResponse = await client.GetAsync(operationLocation);
                resultResponse.EnsureSuccessStatusCode();
                
                var resultJson = await resultResponse.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(resultJson);
                
                string status = result.status;
                _logger.LogDebug($"Operation status: {status}");
                
                if (status == "succeeded")
                {
                    // Extract text from all lines
                    var lines = new System.Collections.Generic.List<string>();
                    
                    if (result.analyzeResult?.readResults != null)
                    {
                        foreach (var readResult in result.analyzeResult.readResults)
                        {
                            if (readResult.lines != null)
                            {
                                foreach (var line in readResult.lines)
                                {
                                    string lineText = line.text;
                                    if (!string.IsNullOrWhiteSpace(lineText))
                                    {
                                        lines.Add(lineText);
                                    }
                                }
                            }
                        }
                    }
                    
                    var extractedText = string.Join("\n", lines);
                    _logger.LogInformation($"Successfully extracted {lines.Count} lines of text");
                    return extractedText;
                }
                else if (status == "failed")
                {
                    var errorMessage = result.analyzeResult?.errors?[0]?.message ?? "Unknown error";
                    throw new InvalidOperationException($"Azure OCR processing failed: {errorMessage}");
                }
                // Continue polling if status is "running" or "notStarted"
            }
            
            throw new TimeoutException($"Azure OCR timed out after {attempts} attempts");
        }
    }
}
```

---

## Step 4: Register Service in Program.cs

Add after line 490 in `Program.cs`:

```csharp
// Register Azure OCR Service
builder.Services.AddScoped<IAzureOcrService, AzureOcrService>();
```

---

## Step 5: Update IdScannerController

### Add Dependency Injection:

Update constructor (line 30):

```csharp
private readonly ILogger<IdScannerController> _logger;
private readonly IWebHostEnvironment _environment;
private readonly IHttpClientFactory _clientFactory;
private readonly IConfiguration _configuration;
private readonly IAzureOcrService _azureOcrService;

public IdScannerController(
    ILogger<IdScannerController> logger,
    IWebHostEnvironment environment,
    IHttpClientFactory clientFactory,
    IConfiguration configuration,
    IAzureOcrService azureOcrService = null) // Optional - may not be configured
{
    _logger = logger;
    _environment = environment;
    _clientFactory = clientFactory;
    _configuration = configuration;
    _azureOcrService = azureOcrService;
}
```

### Add Azure OCR Method:

Add this method to the controller:

```csharp
private async Task<string> PerformAzureOcr(string imagePath)
{
    if (_azureOcrService == null)
    {
        throw new InvalidOperationException("Azure OCR service not configured");
    }
    
    try
    {
        _logger.LogInformation("Using Azure Computer Vision OCR");
        return await _azureOcrService.ExtractTextFromImageAsync(imagePath);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Azure OCR failed");
        throw new Exception($"Azure OCR error: {ex.Message}", ex);
    }
}
```

### Update PerformOcr with Fallback Logic:

Replace the existing `PerformOcr` method (line 247):

```csharp
private async Task<string> PerformOcr(string imagePath, bool enhancedMode)
{
    var ocrProvider = _configuration["OcrSettings:Provider"] ?? "Tesseract";
    var fallbackToAzure = bool.Parse(_configuration["OcrSettings:FallbackToAzure"] ?? "false");
    var maxRetries = int.Parse(_configuration["OcrSettings:MaxRetries"] ?? "2");
    var retryDelay = int.Parse(_configuration["OcrSettings:RetryDelayMilliseconds"] ?? "1000");
    
    Exception lastException = null;
    
    // Try primary provider with retries
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            _logger.LogInformation($"OCR attempt {attempt + 1}/{maxRetries} using {ocrProvider}");
            
            string result;
            if (ocrProvider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                result = await PerformAzureOcr(imagePath);
            }
            else
            {
                result = PerformOcrOnImage(imagePath, enhancedMode);
            }
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                _logger.LogInformation($"OCR succeeded on attempt {attempt + 1}");
                return result;
            }
            
            _logger.LogWarning($"OCR attempt {attempt + 1} returned empty result");
        }
        catch (Exception ex)
        {
            lastException = ex;
            _logger.LogWarning(ex, $"OCR attempt {attempt + 1} failed: {ex.Message}");
            
            // Wait before retry
            if (attempt < maxRetries - 1)
            {
                await Task.Delay(retryDelay);
            }
        }
    }
    
    // Try fallback to Azure if enabled and primary wasn't Azure
    if (fallbackToAzure && !ocrProvider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            _logger.LogInformation("Primary OCR failed, attempting Azure fallback");
            var result = await PerformAzureOcr(imagePath);
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                _logger.LogInformation("Azure fallback succeeded");
                return result;
            }
        }
        catch (Exception azureEx)
        {
            _logger.LogError(azureEx, "Azure fallback also failed");
            lastException = azureEx;
        }
    }
    
    // All attempts failed
    var errorMessage = lastException?.Message ?? "OCR processing failed";
    throw new Exception($"OCR failed after {maxRetries} attempts: {errorMessage}", lastException);
}
```

---

## Step 6: Endpoint Troubleshooting

### Common Endpoint Issues:

❌ **Wrong**: `southeastasia-1.in.azure.com`
✅ **Correct**: `https://your-resource-name.cognitiveservices.azure.com/`

❌ **Wrong**: `https://southeastasia.api.cognitive.microsoft.com/`
✅ **Correct**: Use the specific resource endpoint from portal

### Verify Endpoint:

```bash
# Test endpoint with curl
curl -X POST "YOUR_ENDPOINT/vision/v3.2/read/analyze" \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  -H "Content-Type: application/octet-stream" \
  --data-binary "@test-image.jpg"

# Should return 202 Accepted with Operation-Location header
```

---

## Step 7: Monitor and Test

### Add Logging:

```csharp
_logger.LogInformation($"Azure CV Endpoint: {_configuration["AzureComputerVision:Endpoint"]}");
_logger.LogInformation($"Azure CV configured: {_azureOcrService != null}");
```

### Test Azure OCR:

Create test endpoint in controller:

```csharp
[HttpPost("test-azure-ocr")]
public async Task<IActionResult> TestAzureOcr(IFormFile file)
{
    if (file == null || file.Length == 0)
        return BadRequest("No file uploaded");
    
    var tempPath = Path.Combine(_environment.WebRootPath, "temp");
    Directory.CreateDirectory(tempPath);
    
    var filePath = Path.Combine(tempPath, $"test_{Guid.NewGuid()}.png");
    
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    
    try
    {
        var result = await PerformAzureOcr(filePath);
        return Ok(new { success = true, text = result });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, error = ex.Message });
    }
    finally
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
```

---

## Step 8: Cost Management

### Free Tier (F0):
- **20 transactions per minute**
- **5,000 transactions per month**
- Sufficient for development/testing

### Standard Tier (S1):
- **10 transactions per second**
- **$1.00 per 1,000 transactions (0-1M)**
- Better for production

### Cost Optimization:
1. Use Tesseract as primary, Azure as fallback
2. Cache OCR results for duplicate images
3. Implement rate limiting
4. Monitor usage in Azure Portal

---

## Step 9: Production Checklist

- [ ] Store subscription key in Azure Key Vault
- [ ] Configure managed identity for key access
- [ ] Set up Application Insights for monitoring
- [ ] Configure retry policies
- [ ] Test failover between Tesseract and Azure
- [ ] Set up alerts for API failures
- [ ] Document region selection rationale
- [ ] Load test with expected volume

---

## Comparison: Tesseract vs Azure

| Feature | Tesseract | Azure Computer Vision |
|---------|-----------|----------------------|
| **Cost** | Free | Free tier available, paid after |
| **Speed** | 5-15 seconds | 1-3 seconds |
| **Accuracy** | Good for clear text | Excellent, handles blur better |
| **Languages** | 100+ with data files | Auto-detect, 164+ languages |
| **Offline** | Yes | No (requires internet) |
| **Setup** | Complex (language files) | Simple (API key) |
| **Dependencies** | Large (OpenCV, Tesseract) | None (REST API) |
| **Philippine IDs** | Good with tuning | Excellent out-of-box |

---

## Recommended Strategy

**Development**: Use Tesseract only (free, no external dependencies)

**Production**: Tesseract primary, Azure fallback
```json
{
  "OcrSettings": {
    "Provider": "Tesseract",
    "FallbackToAzure": true
  }
}
```

This gives you:
- ✅ Cost efficiency (most requests use free Tesseract)
- ✅ Reliability (Azure handles difficult images)
- ✅ Scalability (Azure handles load spikes)

---

## Next Steps

See `OCR_Retry_And_Fallback_Logic.md` for advanced error handling strategies.
