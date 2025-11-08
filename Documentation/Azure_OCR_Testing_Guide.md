# Azure OCR Integration - Testing Guide

## 🧪 Quick Test Checklist

### Pre-Testing Setup

1. **Verify Configuration**
   ```bash
   # Check appsettings.json contains:
   "AzureOCR": {
     "Endpoint": "https://bhcare-ocr.cognitiveservices.azure.com/",
     "Key": "3g63c..."
   }
   ```

2. **Start Application**
   ```bash
   dotnet run
   ```

3. **Navigate to Sign-Up Page**
   ```
   https://localhost:5001/Account/SignUp
   ```

---

## ✅ Test Cases

### Test 1: Valid Philippine National ID ⭐ PRIORITY

**Steps**:
1. Prepare a clear photo of Philippine National ID (PhilSys)
2. Navigate to Sign-Up page
3. Scroll to "Quick Fill with ID Scanner" section
4. Click "Choose File" button
5. Select ID image (JPG or PNG)
6. Click "Scan ID" button
7. Wait for processing (3-6 seconds)

**Expected Results**:
- ✅ Progress bar shows: "Validating → Uploading → Processing → Analyzing"
- ✅ Green success alert appears
- ✅ Extracted text is displayed in gray box
- ✅ "Auto-filled fields: First Name, Last Name, Address" message
- ✅ Form fields are populated:
  - `Input.FirstName` = extracted first name
  - `Input.LastName` = extracted last name
  - `Input.Address` = extracted address

**Screenshots**: Take screenshots of success message and filled form

---

### Test 2: Invalid File Type ❌

**Steps**:
1. Try to upload PDF file
2. Click "Scan ID"

**Expected Results**:
- ✅ Red error alert: "Invalid file type"
- ✅ Message: "Please upload JPG or PNG images only"
- ✅ No API call made (check Network tab)

---

### Test 3: File Size Exceeded ❌

**Steps**:
1. Upload image larger than 5MB
2. Click "Scan ID"

**Expected Results**:
- ✅ Red error alert: "File too large (X.XMB)"
- ✅ Message: "Maximum file size is 5MB"
- ✅ No API call made

---

### Test 4: Low Resolution Image ❌

**Steps**:
1. Upload image with dimensions < 600x400 pixels
2. Click "Scan ID"

**Expected Results**:
- ✅ Red error alert: "Image Quality Issues"
- ✅ Lists: "Resolution too low (widthxheight). Minimum: 600x400 pixels"
- ✅ "Retake Photo" button shown

---

### Test 5: Blank/Non-ID Image ❌

**Steps**:
1. Upload blank white image or random photo
2. Click "Scan ID"

**Expected Results**:
- ✅ Processing completes
- ✅ Red/orange alert: "No text could be extracted" OR "Text extracted but could not auto-fill fields"
- ✅ Raw extracted text shown (may be empty or garbage)
- ✅ Manual entry instructions provided

---

### Test 6: Blurry Image ❌

**Steps**:
1. Upload out-of-focus ID photo
2. Click "Scan ID"

**Expected Results**:
- ✅ Either client-side validation fails (blur score too low)
- ✅ OR Azure extracts partial text but can't parse fields
- ✅ User guidance provided

---

### Test 7: Driver's License 🚗

**Steps**:
1. Upload Philippine Driver's License
2. Click "Scan ID"

**Expected Results**:
- ✅ OCR extracts text successfully
- ✅ Name fields may auto-fill (depends on format)
- ✅ Address may auto-fill

**Note**: Different ID formats may have different label names

---

### Test 8: Network Error 🌐

**Steps**:
1. Disconnect internet or block Azure endpoint
2. Upload valid ID image
3. Click "Scan ID"

**Expected Results**:
- ✅ Processing shows progress
- ✅ Eventually fails with error
- ✅ Error message: "Connection Error" or "Server error"
- ✅ Retry options provided

---

### Test 9: Concurrent Requests 🚀

**Steps**:
1. Open two browser tabs with Sign-Up page
2. Upload ID in both tabs simultaneously
3. Click "Scan ID" in both

**Expected Results**:
- ✅ Both requests process independently
- ✅ No interference between tabs
- ✅ Both complete successfully

**Note**: Watch for rate limiting if many requests

---

### Test 10: Form Validation After Auto-Fill ✍️

**Steps**:
1. Successfully scan ID and auto-fill fields
2. Try to submit form without filling other required fields (email, username, password)
3. Submit form

**Expected Results**:
- ✅ Form validation catches missing fields
- ✅ Auto-filled fields remain filled
- ✅ User prompted to complete missing fields

---

## 🔍 Manual Verification

### Check Network Tab (Browser DevTools)

1. Open DevTools (F12)
2. Go to Network tab
3. Click "Scan ID"

**Expected Requests**:

1. **POST /Account/SignUp?handler=ScanId**
   - Status: 200 OK
   - Request Payload: FormData with `idImage`
   - Response: JSON with `success`, `text`, `firstName`, `lastName`, `address`

2. **POST to Azure endpoint** (via server)
   - Not visible in browser (server-side call)

---

### Check Console Logs (Browser DevTools)

Look for:
```javascript
Processing ID with Azure OCR: filename.jpg image/jpeg 2048000
Azure OCR response: {success: true, text: "...", firstName: "...", ...}
```

No errors should appear in console during normal operation.

---

### Check Server Logs

Look for:
```
[Information] Processing ID image: filename.jpg, Size: 2048000 bytes
[Information] Calling Azure Read API: https://bhcare-ocr.cognitiveservices.azure.com/vision/v3.2/read/analyze?language=en
[Information] Operation location: https://...
[Debug] Polling for OCR results (attempt 1/30)
[Debug] OCR status: running
[Debug] OCR status: succeeded
[Information] Extracted 25 lines of text
[Information] Parsed data - FirstName: John, LastName: Doe, Address: 123 Main St
[Information] OCR completed successfully. Extracted text length: 856
```

---

## 📊 Performance Benchmarks

### Expected Processing Times

| Step | Expected Duration |
|------|------------------|
| Client validation | < 100ms |
| Image upload | < 1 second |
| Azure Read API call | < 1 second |
| Polling for results | 2-4 seconds |
| Parsing and response | < 500ms |
| **Total** | **3-6 seconds** |

If processing takes longer than 10 seconds, investigate:
- Network latency
- Azure service health
- Image file size

---

## 🐛 Common Issues & Solutions

### Issue: "Cannot read properties of null (reading 'value')"

**Cause**: Form field selector is wrong

**Check**:
```javascript
document.querySelector('input[name="Input.FirstName"]') // Correct
document.querySelector('input[name="FirstName"]')       // Wrong
```

---

### Issue: Handler returns 404 Not Found

**Cause**: Handler method name doesn't match URL

**Verify**:
- Method name: `OnPostScanIdAsync`
- URL: `/Account/SignUp?handler=ScanId`
- ✅ Match: "ScanId" → `OnPostScanIdAsync`

---

### Issue: No auto-fill despite successful OCR

**Cause**: Parsing logic couldn't find field labels

**Debug**:
1. Check extracted text in UI
2. Look for label keywords:
   - "SURNAME", "LAST NAME", "FAMILY NAME"
   - "GIVEN NAME", "FIRST NAME"
   - "ADDRESS", "RESIDENCE"
3. Update `ParseIdData` method if new format

---

### Issue: Azure returns 401 Unauthorized

**Cause**: Invalid API key

**Solutions**:
1. Verify key in `appsettings.json`
2. Check key in Azure Portal
3. Regenerate key if needed
4. Restart application after updating config

---

### Issue: Azure returns 429 Too Many Requests

**Cause**: Rate limit exceeded (Free tier: 20 req/min)

**Solutions**:
1. Wait 1 minute and retry
2. Upgrade to Standard tier
3. Implement client-side rate limiting

---

## 📝 Test Report Template

```markdown
# Azure OCR Test Report

**Date**: [Date]
**Tester**: [Your Name]
**Environment**: [Development/Staging/Production]

## Test Results

| Test Case | Status | Notes |
|-----------|--------|-------|
| Valid National ID | ✅ Pass | All fields filled correctly |
| Invalid File Type | ✅ Pass | Error shown as expected |
| File Too Large | ✅ Pass | Rejected with message |
| Low Resolution | ✅ Pass | Client validation worked |
| Blank Image | ✅ Pass | Graceful error handling |
| Blurry Image | ⚠️ Partial | OCR ran but couldn't parse |
| Driver's License | ✅ Pass | Name extracted, address not |
| Network Error | ✅ Pass | Error message appropriate |
| Concurrent Requests | ✅ Pass | Both processed successfully |
| Form Validation | ✅ Pass | Auto-filled data preserved |

## Performance

- Average processing time: 4.2 seconds
- Fastest: 2.8 seconds
- Slowest: 6.1 seconds

## Issues Found

1. [Issue description]
   - Severity: High/Medium/Low
   - Steps to reproduce: ...
   - Expected vs Actual: ...

## Recommendations

1. [Recommendation 1]
2. [Recommendation 2]

## Sign-off

✅ Approved for [Next Stage]
❌ Rejected - needs fixes

**Signature**: _______________
**Date**: _______________
```

---

## 🎯 Acceptance Criteria

The integration is considered successful if:

- ✅ **Happy Path Works**: Valid IDs are scanned and fields auto-filled
- ✅ **Validation Works**: Invalid files rejected before upload
- ✅ **Errors Handled**: All error scenarios show user-friendly messages
- ✅ **Performance Acceptable**: Processing completes in < 10 seconds
- ✅ **No Console Errors**: Clean console during normal operation
- ✅ **No Server Errors**: No 500 errors in server logs
- ✅ **Mobile Compatible**: Works on phone camera capture
- ✅ **Form Integration**: Auto-filled data is properly validated

---

## 📱 Mobile Testing

### Additional Tests for Mobile Devices

1. **Camera Capture**:
   - Click file input → opens camera
   - Take photo of ID directly
   - Photo quality sufficient for OCR

2. **Touch Interface**:
   - Buttons easily tappable
   - File input accessible
   - Progress visible on small screen

3. **Network Conditions**:
   - Test on 3G/4G/5G
   - Test on WiFi
   - Test with poor connection

---

## 🔐 Security Testing

### Additional Security Checks

1. **API Key Exposure**:
   - ✅ Key not visible in client-side code
   - ✅ Key not in Network requests
   - ✅ Key only in server configuration

2. **File Upload Security**:
   - ✅ File type validation enforced
   - ✅ File size validation enforced
   - ✅ No arbitrary file execution

3. **Data Privacy**:
   - ✅ Images not permanently stored (unless intended)
   - ✅ OCR results not logged with PII
   - ✅ HTTPS used for all requests

---

## ✅ Final Checklist Before Production

- [ ] All test cases pass
- [ ] Performance meets requirements
- [ ] Error handling verified
- [ ] Security review complete
- [ ] API key moved to Key Vault
- [ ] Logging and monitoring configured
- [ ] User documentation updated
- [ ] Stakeholder approval obtained

---

**Testing Date**: November 6, 2025
**Status**: Ready for Testing
**Next Steps**: Execute test cases and report results
