# OCR Retry and Fallback Logic

## Overview
Comprehensive error handling, retry mechanisms, and fallback strategies for robust OCR processing.

---

## Strategy 1: Smart Retry Logic

### Configuration

```json
{
  "OcrSettings": {
    "Provider": "Tesseract",
    "FallbackToAzure": true,
    "RetryPolicy": {
      "MaxRetries": 3,
      "InitialDelayMs": 500,
      "MaxDelayMs": 5000,
      "BackoffMultiplier": 2,
      "JitterMs": 200
    },
    "ErrorsThatTriggerFallback": [
      "empty result",
      "no text",
      "traineddata",
      "timeout"
    ]
  }
}
```

### Implementation

Create `Services/OcrRetryPolicy.cs`:

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BHCARE.Services
{
    public interface IOcrRetryPolicy
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName);
    }
    
    public class OcrRetryPolicy : IOcrRetryPolicy
    {
        private readonly ILogger<OcrRetryPolicy> _logger;
        private readonly int _maxRetries;
        private readonly int _initialDelayMs;
        private readonly int _maxDelayMs;
        private readonly double _backoffMultiplier;
        private readonly int _jitterMs;
        private readonly Random _random;
        
        public OcrRetryPolicy(ILogger<OcrRetryPolicy> logger, IConfiguration configuration)
        {
            _logger = logger;
            _maxRetries = int.Parse(configuration["OcrSettings:RetryPolicy:MaxRetries"] ?? "3");
            _initialDelayMs = int.Parse(configuration["OcrSettings:RetryPolicy:InitialDelayMs"] ?? "500");
            _maxDelayMs = int.Parse(configuration["OcrSettings:RetryPolicy:MaxDelayMs"] ?? "5000");
            _backoffMultiplier = double.Parse(configuration["OcrSettings:RetryPolicy:BackoffMultiplier"] ?? "2");
            _jitterMs = int.Parse(configuration["OcrSettings:RetryPolicy:JitterMs"] ?? "200");
            _random = new Random();
        }
        
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
        {
            int attempt = 0;
            Exception lastException = null;
            
            while (attempt < _maxRetries)
            {
                attempt++;
                
                try
                {
                    _logger.LogInformation($"{operationName}: Attempt {attempt}/{_maxRetries}");
                    
                    var result = await operation();
                    
                    if (attempt > 1)
                    {
                        _logger.LogInformation($"{operationName}: Succeeded on retry attempt {attempt}");
                    }
                    
                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt >= _maxRetries)
                    {
                        _logger.LogError(ex, $"{operationName}: All {_maxRetries} attempts failed");
                        break;
                    }
                    
                    // Calculate delay with exponential backoff and jitter
                    var delay = CalculateDelay(attempt);
                    _logger.LogWarning(ex, $"{operationName}: Attempt {attempt} failed. Retrying in {delay}ms");
                    
                    await Task.Delay(delay);
                }
            }
            
            throw new Exception($"{operationName} failed after {_maxRetries} attempts", lastException);
        }
        
        private int CalculateDelay(int attempt)
        {
            // Exponential backoff: initialDelay * (backoffMultiplier ^ attempt)
            var baseDelay = _initialDelayMs * Math.Pow(_backoffMultiplier, attempt - 1);
            
            // Cap at max delay
            baseDelay = Math.Min(baseDelay, _maxDelayMs);
            
            // Add jitter to prevent thundering herd
            var jitter = _random.Next(-_jitterMs, _jitterMs);
            var finalDelay = (int)baseDelay + jitter;
            
            return Math.Max(finalDelay, 0);
        }
    }
}
```

---

## Strategy 2: Manual Fallback Option

### Add Manual Entry Button

Update `Pages/Account/SignUp.cshtml` scanner section:

```html
<div class="card-body">
    <p class="text-muted mb-3">Upload your ID to automatically fill your information</p>
    
    <div class="row align-items-center">
        <div class="col-md-6">
            <input type="file" id="idScannerInput" accept="image/*" class="form-control" capture="environment" />
            <small class="text-muted mt-1 d-block">Upload your ID image (JPG, PNG)</small>
        </div>
        <div class="col-md-3">
            <button type="button" id="scanIdButton" class="btn btn-primary w-100">
                <i class="fas fa-camera me-2"></i> Scan ID
            </button>
        </div>
        <div class="col-md-3">
            <button type="button" id="skipScanButton" class="btn btn-outline-secondary w-100" 
                    onclick="document.getElementById('section1').scrollIntoView({behavior: 'smooth', block: 'start'})">
                <i class="fas fa-edit me-2"></i> Manual Entry
            </button>
        </div>
    </div>
    
    <!-- Add retry button that appears on error -->
    <div id="retrySection" class="mt-3 d-none">
        <div class="alert alert-info">
            <strong><i class="fas fa-info-circle me-2"></i> Scanning didn't work?</strong>
            <div class="mt-2">
                <button type="button" class="btn btn-sm btn-primary me-2" id="retryWithEnhancedButton">
                    <i class="fas fa-redo me-1"></i> Retry with Enhanced Mode
                </button>
                <button type="button" class="btn btn-sm btn-outline-primary me-2" 
                        onclick="document.getElementById('idScannerInput').click()">
                    <i class="fas fa-upload me-1"></i> Try Different Image
                </button>
                <button type="button" class="btn btn-sm btn-outline-secondary" id="giveUpScanButton">
                    <i class="fas fa-edit me-1"></i> Fill Manually Instead
                </button>
            </div>
        </div>
    </div>
</div>
```

### Add Retry JavaScript:

```javascript
// Retry with enhanced mode
document.getElementById('retryWithEnhancedButton')?.addEventListener('click', function() {
    const enhancedCheckbox = document.getElementById('enhancedModeCheckbox');
    if (enhancedCheckbox) {
        enhancedCheckbox.checked = true;
    }
    
    const scanButton = document.getElementById('scanIdButton');
    if (scanButton) {
        scanButton.click();
    }
});

// Give up and manually fill
document.getElementById('giveUpScanButton')?.addEventListener('click', function() {
    // Hide scanner section
    document.getElementById('retrySection').classList.add('d-none');
    document.getElementById('scannerResult').classList.add('d-none');
    
    // Show success message for manual entry
    document.getElementById('scannerResult').innerHTML = `
        <div class="alert alert-info">
            <i class="fas fa-info-circle me-2"></i> 
            <strong>Manual entry selected.</strong> Please fill all fields below.
        </div>`;
    document.getElementById('scannerResult').classList.remove('d-none');
    
    // Focus first input field
    setTimeout(() => {
        document.querySelector('input[name="Input.FirstName"]')?.focus();
    }, 300);
});
```

---

## Strategy 3: Image Quality Auto-Enhancement

### Auto-Retry with Enhancement

Add to scan button handler (after first failure):

```javascript
catch (error) {
    console.error('OCR failed:', error);
    
    // Check if this was first attempt without enhancement
    const enhancedCheckbox = document.getElementById('enhancedModeCheckbox');
    const wasEnhanced = enhancedCheckbox?.checked;
    
    if (!wasEnhanced && !error.message?.includes('validation')) {
        // Automatically retry with enhanced mode
        scannerResult.innerHTML = `
            <div class="alert alert-warning">
                <strong><i class="fas fa-exclamation-triangle me-2"></i> First attempt failed.</strong>
                <p class="mb-2">Automatically retrying with enhanced processing mode...</p>
                <div class="progress" style="height: 5px;">
                    <div class="progress-bar progress-bar-striped progress-bar-animated" 
                         style="width: 100%"></div>
                </div>
            </div>`;
        scannerResult.classList.remove('d-none');
        
        // Wait 1 second, then retry with enhanced mode
        await new Promise(resolve => setTimeout(resolve, 1000));
        
        enhancedCheckbox.checked = true;
        scanIdButton.click();
        return;
    }
    
    // Show retry options
    document.getElementById('retrySection')?.classList.remove('d-none');
    
    // ... rest of error handling ...
}
```

---

## Strategy 4: Progressive Degradation

### Fallback Hierarchy:

1. **Try Tesseract with standard settings**
2. **Retry Tesseract with enhanced mode**
3. **Try Azure Computer Vision (if configured)**
4. **Show manual entry option with pre-filled data if any**

### Implementation:

```csharp
private async Task<(bool success, string text, string provider)> TryOcrWithFallbacks(
    string imagePath, 
    bool initialEnhancedMode)
{
    var attempts = new List<(string provider, bool enhanced, Func<Task<string>> operation)>
    {
        ("Tesseract Standard", false, () => Task.FromResult(PerformOcrOnImage(imagePath, false))),
        ("Tesseract Enhanced", true, () => Task.FromResult(PerformOcrOnImage(imagePath, true))),
    };
    
    // Add Azure if configured
    if (_azureOcrService != null)
    {
        attempts.Add(("Azure Computer Vision", false, () => PerformAzureOcr(imagePath)));
    }
    
    // Try each method
    foreach (var (provider, enhanced, operation) in attempts)
    {
        try
        {
            _logger.LogInformation($"Attempting OCR with {provider}");
            
            var result = await operation();
            
            if (!string.IsNullOrWhiteSpace(result) && result.Length > 10)
            {
                _logger.LogInformation($"Success with {provider}");
                return (true, result, provider);
            }
            
            _logger.LogWarning($"{provider} returned insufficient text");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"{provider} failed: {ex.Message}");
        }
    }
    
    return (false, null, null);
}
```

---

## Strategy 5: Partial Success Handling

### Save Partial Results:

```csharp
private IdScannerResponse HandlePartialOcrSuccess(IdData data, float confidence)
{
    // Count how many critical fields were extracted
    int extractedFields = 0;
    int criticalFields = 0;
    
    // Critical fields
    if (!string.IsNullOrWhiteSpace(data.FirstName)) extractedFields++;
    criticalFields++;
    
    if (!string.IsNullOrWhiteSpace(data.LastName)) extractedFields++;
    criticalFields++;
    
    if (!string.IsNullOrWhiteSpace(data.BirthDate)) extractedFields++;
    criticalFields++;
    
    if (!string.IsNullOrWhiteSpace(data.Address)) extractedFields++;
    criticalFields++;
    
    var successRate = (float)extractedFields / criticalFields;
    
    if (successRate >= 0.5f) // At least 50% of critical fields
    {
        return new IdScannerResponse
        {
            Success = true,
            Message = $"Partial scan successful ({extractedFields}/{criticalFields} critical fields extracted)",
            Data = data,
            Confidence = confidence,
            ErrorDetails = "Some fields may need manual entry. Please review and complete missing information."
        };
    }
    
    return new IdScannerResponse
    {
        Success = false,
        Message = "Insufficient data extracted from ID",
        Data = data, // Still return partial data
        Confidence = confidence,
        ErrorDetails = $"Only {extractedFields}/{criticalFields} critical fields were extracted. Please fill the form manually or try a clearer image."
    };
}
```

---

## Strategy 6: User Guidance System

### Provide Specific Tips Based on Error:

```javascript
function getErrorGuidance(error) {
    const guidance = {
        title: 'Error Processing ID',
        message: error.message,
        tips: [],
        actions: []
    };
    
    if (error.message?.includes('blur')) {
        guidance.title = 'Image Too Blurry';
        guidance.tips = [
            'Hold your phone steady when taking the photo',
            'Use a tripod or rest phone on stable surface',
            'Ensure ID is flat (not bent or folded)',
            'Clean your camera lens'
        ];
        guidance.actions = ['retake', 'manual'];
    }
    else if (error.message?.includes('dark') || error.message?.includes('bright')) {
        guidance.title = 'Lighting Issue';
        guidance.tips = [
            'Use natural daylight if possible',
            'Avoid shadows falling on the ID',
            'Don\'t use flash (causes glare)',
            'Position light source behind you'
        ];
        guidance.actions = ['retake', 'enhance', 'manual'];
    }
    else if (error.message?.includes('resolution') || error.message?.includes('quality')) {
        guidance.title = 'Image Quality Too Low';
        guidance.tips = [
            'Move closer to the ID',
            'Use your phone\'s main camera (not selfie camera)',
            'Ensure camera is in focus before capturing',
            'Use highest quality setting on your camera app'
        ];
        guidance.actions = ['retake', 'manual'];
    }
    else if (error.message?.includes('no text') || error.message?.includes('empty')) {
        guidance.title = 'No Text Detected';
        guidance.tips = [
            'Ensure the entire ID is visible in frame',
            'ID should fill most of the image',
            'Avoid placing ID on patterned backgrounds',
            'Make sure it\'s a valid government-issued ID'
        ];
        guidance.actions = ['retake', 'enhance', 'manual'];
    }
    else if (error.message?.includes('timeout') || error.message?.includes('network')) {
        guidance.title = 'Connection Issue';
        guidance.tips = [
            'Check your internet connection',
            'Try again in a few moments',
            'Switch to WiFi if on mobile data',
            'Contact support if problem persists'
        ];
        guidance.actions = ['retry', 'manual'];
    }
    else {
        guidance.tips = [
            'Try taking a new photo with better lighting',
            'Ensure ID is flat and fully visible',
            'Make sure text is clear and readable',
            'You can also fill the form manually'
        ];
        guidance.actions = ['retake', 'enhance', 'manual'];
    }
    
    return guidance;
}

// Display guidance
function showErrorGuidance(error) {
    const guidance = getErrorGuidance(error);
    
    let actionsHtml = '<div class="mt-3 d-flex gap-2 flex-wrap">';
    
    if (guidance.actions.includes('retry')) {
        actionsHtml += `
            <button type="button" class="btn btn-sm btn-primary" onclick="document.getElementById('scanIdButton').click()">
                <i class="fas fa-redo me-1"></i> Try Again
            </button>`;
    }
    
    if (guidance.actions.includes('retake')) {
        actionsHtml += `
            <button type="button" class="btn btn-sm btn-primary" onclick="document.getElementById('idScannerInput').click()">
                <i class="fas fa-camera me-1"></i> Retake Photo
            </button>`;
    }
    
    if (guidance.actions.includes('enhance')) {
        actionsHtml += `
            <button type="button" class="btn btn-sm btn-outline-primary" id="retryEnhanced">
                <i class="fas fa-magic me-1"></i> Try Enhanced Mode
            </button>`;
    }
    
    if (guidance.actions.includes('manual')) {
        actionsHtml += `
            <button type="button" class="btn btn-sm btn-outline-secondary" onclick="document.querySelector('[name=\\'Input.FirstName\\']').focus()">
                <i class="fas fa-edit me-1"></i> Fill Manually
            </button>`;
    }
    
    actionsHtml += '</div>';
    
    const tipsHtml = guidance.tips.map(tip => `<li>${tip}</li>`).join('');
    
    scannerResult.innerHTML = `
        <div class="alert alert-danger">
            <strong><i class="fas fa-exclamation-circle me-2"></i> ${guidance.title}</strong>
            <p class="mt-2 mb-2">${guidance.message}</p>
            <div class="border-top pt-2 mt-2">
                <strong>Tips for better results:</strong>
                <ul class="mb-0 mt-2 small">${tipsHtml}</ul>
            </div>
            ${actionsHtml}
        </div>`;
    
    scannerResult.classList.remove('d-none');
}
```

---

## Strategy 7: Analytics and Monitoring

### Track OCR Success Rates:

```csharp
// Add to controller
private void LogOcrMetrics(string provider, bool success, long durationMs, string errorType = null)
{
    var metrics = new Dictionary<string, object>
    {
        { "Provider", provider },
        { "Success", success },
        { "DurationMs", durationMs },
        { "ErrorType", errorType ?? "None" },
        { "Timestamp", DateTime.UtcNow }
    };
    
    _logger.LogInformation("OCR_METRICS: {Metrics}", JsonConvert.SerializeObject(metrics));
    
    // Send to Application Insights or monitoring service
    // telemetryClient.TrackEvent("OcrProcessing", metrics);
}
```

### Dashboard Queries (Application Insights):

```kusto
// OCR success rate by provider
customEvents
| where name == "OcrProcessing"
| extend Provider = tostring(customDimensions.Provider)
| extend Success = tobool(customDimensions.Success)
| summarize SuccessRate = avg(todouble(Success)) * 100 by Provider
| render barchart

// Average processing time
customEvents
| where name == "OcrProcessing"
| extend Provider = tostring(customDimensions.Provider)
| extend DurationMs = todouble(customDimensions.DurationMs)
| summarize AvgDuration = avg(DurationMs) by Provider
| render timechart

// Common error types
customEvents
| where name == "OcrProcessing"
| where customDimensions.Success == "false"
| extend ErrorType = tostring(customDimensions.ErrorType)
| summarize Count = count() by ErrorType
| render piechart
```

---

## Complete Workflow

```
User uploads image
    ↓
Client-side validation
    ↓ (pass)
Upload to server
    ↓
Server-side validation
    ↓ (pass)
Try Tesseract (standard)
    ↓ (fail - empty result)
Try Tesseract (enhanced)
    ↓ (fail - still blurry)
Try Azure Computer Vision
    ↓ (success - partial data)
Return partial results to user
    ↓
User reviews and completes missing fields
    ↓
Submit form
```

---

## Summary

This comprehensive approach ensures:

✅ **Multiple fallback options**
✅ **User-friendly error messages**
✅ **Automatic retries with different strategies**
✅ **Manual entry always available**
✅ **Partial success handling**
✅ **Detailed guidance for users**
✅ **Monitoring and analytics**

---

## Deployment Priority

1. **Phase 1** (Immediate): Client-side validation + better error messages
2. **Phase 2** (Week 1): Retry logic + manual fallback UI
3. **Phase 3** (Week 2): Azure integration + progressive enhancement
4. **Phase 4** (Week 3): Analytics + monitoring dashboard

---

All documentation complete! See the companion files for implementation details.
