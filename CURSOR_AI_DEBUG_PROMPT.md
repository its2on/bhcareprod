# Cursor AI Debug Prompt: Fix Azure OCR 401 Unauthorized Error

## Problem Statement

The BHCARE system's Azure OCR integration for ID scanning during sign-up is returning a **401 Unauthorized** error when calling the Azure Vision Read API. The error message is: "Access denied due to invalid subscription key or wrong API endpoint."

## Current Configuration

- **App Service**: `barangay` Web App (Azure App Service)
- **Application Settings**:
  - `AzureOCR_Key` = `36c3prczncb3aep9eb4wbPg32Mo1k6ETLBZW7W3iD9uLqIQj99J9BBKaqCBlyX13w3AAFAOCaGaAz`
  - `AzureOCR_Endpoint` = `https://bhcare-ocr.cognitiveservices.azure.com/`
- **Code Location**: `Pages/Account/SignUp.cshtml.cs` (SignUpModel class)
- **Method**: `OnPostScanIdAsync` or similar handler
- **Framework**: .NET 8, ASP.NET Core

## Error Details

- **Error Code**: 401 Unauthorized
- **Error Message**: "Access denied due to invalid subscription key or wrong API endpoint. Make sure to provide a valid key for an active subscription and use a correct regional API endpoint for your resource."
- **API Endpoint Called**: `https://bhcare-ocr.cognitiveservices.azure.com/vision/v3.2/read/analyze?language=en`
- **HTTP Header Used**: `Ocp-Apim-Subscription-Key`

## Root Causes to Investigate

1. **CRITICAL: Setting Name Format Issue**
   - Current setting: `AzureOCR_Key` (single underscore)
   - **Required**: `AzureOCR__Key` (double underscore `__`)
   - ASP.NET Core uses double underscore `__` to represent nested configuration (`AzureOCR:Key`)

2. **Invalid or Truncated Key**
   - The key appears to be incomplete or incorrect
   - Verify the complete key from Azure Portal matches exactly

3. **Wrong Endpoint or Region Mismatch**
   - Verify the Computer Vision resource region matches the endpoint
   - Check if resource is "Computer Vision" or "Multi-service" Cognitive Services

4. **Missing or Expired Subscription**
   - Verify the Azure subscription is active
   - Check if the Computer Vision resource is active and not deleted

5. **Key Not Being Read Correctly**
   - Configuration might not be loading from App Service settings
   - Verify the configuration is being read correctly

## Step-by-Step Debugging Instructions

### Step 1: Add Comprehensive Logging

Add detailed logging to see what values are actually being read:

```csharp
// In SignUp.cshtml.cs, in the OCR handler method
var azureEndpoint = _configuration["AzureOCR:Endpoint"];
var azureKey = _configuration["AzureOCR:Key"];

// Add detailed logging
_logger.LogInformation("=== AZURE OCR CONFIGURATION DEBUG ===");
_logger.LogInformation($"AzureOCR:Endpoint = {azureEndpoint ?? "NULL"}");
_logger.LogInformation($"AzureOCR:Key = {(string.IsNullOrEmpty(azureKey) ? "NULL or EMPTY" : $"Length: {azureKey.Length}, First 10: {azureKey.Substring(0, Math.Min(10, azureKey.Length))}..., Last 10: ...{azureKey.Substring(Math.Max(0, azureKey.Length - 10))}")}");

if (string.IsNullOrEmpty(azureEndpoint) || string.IsNullOrEmpty(azureKey))
{
    _logger.LogError("Azure OCR configuration is missing or incomplete!");
    _logger.LogError($"Endpoint is null/empty: {string.IsNullOrEmpty(azureEndpoint)}");
    _logger.LogError($"Key is null/empty: {string.IsNullOrEmpty(azureKey)}");
    return new JsonResult(new { success = false, message = "OCR service is not configured" });
}
```

### Step 2: Verify Azure Portal Configuration

**Navigate to Azure Portal:**
1. Go to https://portal.azure.com
2. Search for "Computer Vision" or "bhcare-ocr"
3. Select your Computer Vision resource
4. Go to **"Keys and Endpoint"** (left menu, under Resource Management)

**Verify:**
- **Endpoint**: Should be `https://bhcare-ocr.cognitiveservices.azure.com/` (or similar)
- **Key 1**: Copy the complete key (should be 100+ characters)
- **Region**: Note the region (e.g., "Southeast Asia")

**Check App Service Settings:**
1. Go to **App Service** → **Configuration** → **Application settings**
2. Verify these settings exist with **EXACT names**:
   - `AzureOCR__Endpoint` (double underscore `__`)
   - `AzureOCR__Key` (double underscore `__`)
3. **CRITICAL**: If you see `AzureOCR_Key` (single underscore), **DELETE IT** and create `AzureOCR__Key` (double underscore)

### Step 3: Fix App Service Configuration

**Option A: Via Azure Portal**
1. Go to **App Service** → **Configuration** → **Application settings**
2. **Delete** `AzureOCR_Key` (single underscore) if it exists
3. Click **"+ New application setting"**
4. Add:
   - **Name**: `AzureOCR__Endpoint` (double underscore)
   - **Value**: `https://bhcare-ocr.cognitiveservices.azure.com/` (must end with `/`)
5. Click **"+ New application setting"** again
6. Add:
   - **Name**: `AzureOCR__Key` (double underscore)
   - **Value**: Paste the **complete KEY 1** from Computer Vision resource
7. Click **"Save"** at the top
8. Click **"Continue"** to restart

**Option B: Via Azure CLI**
```bash
# Get the complete key from Computer Vision resource first
az cognitiveservices account keys list \
  --name bhcare-ocr \
  --resource-group YOUR_RESOURCE_GROUP \
  --query "key1" -o tsv

# Set the app settings (replace YOUR_APP_SERVICE_NAME and YOUR_RESOURCE_GROUP)
az webapp config appsettings set \
  --name YOUR_APP_SERVICE_NAME \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings \
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" \
    "AzureOCR__Key=YOUR_COMPLETE_KEY_HERE"

# Restart the app
az webapp restart \
  --name YOUR_APP_SERVICE_NAME \
  --resource-group YOUR_RESOURCE_GROUP
```

### Step 4: Test API Directly

**Test the key using curl:**
```bash
curl -X POST "https://bhcare-ocr.cognitiveservices.azure.com/vision/v3.2/read/analyze" \
  -H "Ocp-Apim-Subscription-Key: YOUR_COMPLETE_KEY_HERE" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @path/to/test/image.jpg \
  -v
```

**Expected Results:**
- `HTTP/2 202 Accepted` → Key works ✅
- `HTTP/2 401 Unauthorized` → Key is wrong ❌
- `HTTP/2 403 Forbidden` → Key is wrong or resource has restrictions ❌

### Step 5: Update Code with Better Error Handling

```csharp
public async Task<IActionResult> OnPostScanIdAsync(IFormFile idImage)
{
    try
    {
        // Validate file
        if (idImage == null || idImage.Length == 0)
        {
            return new JsonResult(new { success = false, message = "No image file uploaded" });
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(idImage.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return new JsonResult(new { success = false, message = "Only JPG and PNG files are supported" });
        }

        _logger.LogInformation($"Processing ID image: {idImage.FileName}, Size: {idImage.Length} bytes");

        // Get Azure OCR configuration
        var azureEndpoint = _configuration["AzureOCR:Endpoint"];
        var azureKey = _configuration["AzureOCR:Key"];

        // Detailed logging
        _logger.LogInformation("=== AZURE OCR CONFIGURATION ===");
        _logger.LogInformation($"Endpoint: {azureEndpoint ?? "NULL"}");
        _logger.LogInformation($"Key Length: {azureKey?.Length ?? 0} characters");
        if (!string.IsNullOrEmpty(azureKey))
        {
            _logger.LogInformation($"Key First 10: {azureKey.Substring(0, Math.Min(10, azureKey.Length))}...");
            _logger.LogInformation($"Key Last 10: ...{azureKey.Substring(Math.Max(0, azureKey.Length - 10))}");
        }

        if (string.IsNullOrEmpty(azureEndpoint) || string.IsNullOrEmpty(azureKey))
        {
            _logger.LogError("Azure OCR configuration is missing!");
            return new JsonResult(new { success = false, message = "OCR service is not configured. Please contact support." });
        }

        // Convert image to byte array
        byte[] imageBytes;
        using (var memoryStream = new MemoryStream())
        {
            await idImage.CopyToAsync(memoryStream);
            imageBytes = memoryStream.ToArray();
        }

        // Create HTTP client
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", azureKey);
        httpClient.Timeout = TimeSpan.FromSeconds(600);

        // Step 1: Submit image to Azure Read API
        var readApiUrl = $"{azureEndpoint.TrimEnd('/')}/vision/v3.2/read/analyze?language=en";
        var content = new ByteArrayContent(imageBytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        _logger.LogInformation($"Calling Azure Read API: {readApiUrl}");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(600));
        using var request = new HttpRequestMessage(HttpMethod.Post, readApiUrl)
        {
            Content = content
        };
        
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Azure Read API error: {response.StatusCode} - {errorContent}");
            
            // Provide user-friendly error messages
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new JsonResult(new { 
                    success = false, 
                    message = "OCR service authentication failed. Please contact support or try again later.",
                    errorDetails = "Unauthorized - Invalid subscription key or endpoint"
                });
            }
            
            return new JsonResult(new { 
                success = false, 
                message = $"OCR service error: {response.StatusCode}",
                errorDetails = errorContent
            });
        }

        // Get operation location from response headers
        if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
        {
            _logger.LogError("No Operation-Location header in Azure response");
            return new JsonResult(new { success = false, message = "OCR operation failed to start" });
        }

        var operationLocation = operationLocations.FirstOrDefault();
        _logger.LogInformation($"Operation location: {operationLocation}");

        // Step 2: Poll for results
        string extractedText = await PollForOcrResultAsync(httpClient, operationLocation);

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return new JsonResult(new { success = false, message = "No text could be extracted from the image" });
        }

        _logger.LogInformation($"OCR completed successfully. Extracted text length: {extractedText.Length}");
        
        // Parse and return results
        // ... rest of your parsing logic ...
        
        return new JsonResult(new { 
            success = true, 
            text = extractedText,
            // ... other parsed fields ...
        });
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "HTTP request error during OCR processing");
        return new JsonResult(new { 
            success = false, 
            message = "Network error while processing ID. Please try again.",
            errorDetails = ex.Message
        });
    }
    catch (TaskCanceledException ex)
    {
        _logger.LogError(ex, "OCR request timed out");
        return new JsonResult(new { 
            success = false, 
            message = "OCR processing timed out. Please try again with a smaller image.",
            errorDetails = "Request timeout"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during OCR processing");
        return new JsonResult(new { 
            success = false, 
            message = "An error occurred while processing the ID. Please try again or fill manually.",
            errorDetails = ex.Message
        });
    }
}
```

### Step 6: Add Configuration Validation

Add validation at application startup:

```csharp
// In Program.cs or Startup.cs
var azureOcrEndpoint = builder.Configuration["AzureOCR:Endpoint"];
var azureOcrKey = builder.Configuration["AzureOCR:Key"];

if (string.IsNullOrEmpty(azureOcrEndpoint) || string.IsNullOrEmpty(azureOcrKey))
{
    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
    logger.LogWarning("Azure OCR configuration is missing. ID scanning will not work.");
}
else
{
    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"Azure OCR configured. Endpoint: {azureOcrEndpoint}, Key length: {azureOcrKey.Length}");
}
```

### Step 7: Add Fallback to Manual Entry

Update the frontend to gracefully handle OCR failures:

```javascript
// In SignUp.cshtml or your JavaScript file
async function processIdImage(file) {
    try {
        const formData = new FormData();
        formData.append('idImage', file);
        
        const response = await fetch('/Account/SignUp?handler=ScanId', {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': getAntiForgeryToken()
            }
        });
        
        const result = await response.json();
        
        if (result.success) {
            // Auto-fill form fields
            fillFormFields(result);
        } else {
            // Show error but allow manual entry
            showError(result.message || 'OCR processing failed. Please fill the form manually.');
            enableManualEntry();
        }
    } catch (error) {
        console.error('Error processing ID:', error);
        showError('An error occurred. Please fill the form manually.');
        enableManualEntry();
    }
}
```

## Verification Checklist

After implementing fixes, verify:

- [ ] `AzureOCR__Endpoint` exists in App Service settings (double underscore)
- [ ] `AzureOCR__Key` exists in App Service settings (double underscore)
- [ ] No `AzureOCR_Key` or `AzureOCR_Endpoint` (single underscore) exist
- [ ] Endpoint value is complete: `https://bhcare-ocr.cognitiveservices.azure.com/` (ends with `/`)
- [ ] Key value is complete and matches Computer Vision resource KEY 1 exactly
- [ ] App Service has been restarted after updating settings
- [ ] Logs show the configuration is being read correctly
- [ ] Direct API test (curl) returns 202 Accepted
- [ ] Code includes proper error handling
- [ ] Frontend gracefully handles OCR failures

## Expected Log Output After Fix

```
info: Barangay.Pages.Account.SignUpModel[0]
      === AZURE OCR CONFIGURATION ===
info: Barangay.Pages.Account.SignUpModel[0]
      Endpoint: https://bhcare-ocr.cognitiveservices.azure.com/
info: Barangay.Pages.Account.SignUpModel[0]
      Key Length: 100 characters
info: Barangay.Pages.Account.SignUpModel[0]
      Key First 10: 3g63cprczn...
info: Barangay.Pages.Account.SignUpModel[0]
      Key Last 10: ...OGaA2z
info: Barangay.Pages.Account.SignUpModel[0]
      Calling Azure Read API: https://bhcare-ocr.cognitiveservices.azure.com/vision/v3.2/read/analyze?language=en
info: System.Net.Http.HttpClient.Default.ClientHandler[101]
      Received HTTP response headers after XXXms - 202
```

## Additional Notes

- **API Version**: Currently using v3.2. If issues persist, consider upgrading to v4.0 (requires endpoint change)
- **Region**: Ensure the Computer Vision resource region matches your App Service region for best performance
- **Key Rotation**: If key is regenerated, update App Service settings immediately
- **Security**: Consider using Azure Key Vault for storing the OCR key in production

## Quick Fix Summary

1. **CRITICAL**: Change setting name from `AzureOCR_Key` to `AzureOCR__Key` (double underscore)
2. Verify the complete key from Computer Vision resource
3. Update App Service settings with correct names and values
4. Restart App Service
5. Add logging to verify configuration is read correctly
6. Test with curl to verify key works
7. Deploy updated code with better error handling

