# Diagnose OCR "Unauthorized" Error - Step by Step

Since your environment variables are correct but Azure is still blocking OCR, let's diagnose systematically.

## 🔍 Step 1: Check App Service Logs (CRITICAL!)

**This will show us exactly what's happening:**

1. Go to **Azure Portal** → Your **App Service** (`barangaybhcare` or `bhcare-webapp`)
2. Click **"Log stream"** in the left menu (under Monitoring)
3. **Keep this open** in a separate tab/window
4. Go to your website (`bhcare.software/Account/SignUp`) and try uploading an ID image
5. **Watch the logs immediately** - you should see messages like:

**Look for these log messages:**
- `Azure OCR Endpoint: https://...` ← Shows the endpoint being read
- `Azure OCR Key length: XXX characters` ← Shows the key length
- `Azure OCR Key (first 10 chars): ...` ← First 10 characters of key
- `Azure OCR Key (last 10 chars): ...` ← Last 10 characters of key
- `Calling Azure Read API: https://...` ← Shows the API URL being called
- `Azure Read API error: 401 - ...` ← Shows the exact error

**What to check:**
- Does the key length match your Computer Vision key? (Should be ~100+ characters)
- Do the first/last 10 characters match your Computer Vision key?
- Is the endpoint URL correct?
- What's the exact error message from Azure?

---

## 🔍 Step 2: Verify Key Value in App Service

**Check for hidden issues:**

1. Go to **App Service** → **Environment variables** → **Application settings**
2. Find `AzureOCR__Key`
3. Click **"Show values"** button (eye icon at the top) to reveal the actual value
4. **Click on `AzureOCR__Key`** to edit it
5. **Select ALL the text** (Ctrl+A) and check:
   - Are there any **spaces at the beginning or end**?
   - Is the key **complete**? (Should be ~100+ characters)
   - Does it match **exactly** what's in Computer Vision → Keys and Endpoint?

**Common issues:**
- **Leading/trailing spaces** - These will cause 401 Unauthorized
- **Truncated key** - Missing end characters
- **Wrong key** - Key from different Computer Vision resource

---

## 🔍 Step 3: Verify Key Matches Endpoint

**The key and endpoint must belong to the same Computer Vision resource:**

1. Go to **Computer Vision** resource → **bhcare-ocr** → **Keys and Endpoint**
2. **Copy KEY 1** completely
3. **Copy the Endpoint** (should be: `https://bhcare-ocr.cognitiveservices.azure.com/`)
4. Go to **App Service** → **Environment variables**
5. **Compare:**
   - Does `AzureOCR__Endpoint` match the endpoint from Computer Vision?
   - Does `AzureOCR__Key` match KEY 1 from Computer Vision?

**If they don't match:**
- The key and endpoint are from different resources
- Update both to match the same Computer Vision resource

---

## 🔍 Step 4: Check for Whitespace Issues

**Whitespace can cause 401 Unauthorized:**

1. Go to **App Service** → **Environment variables** → **Application settings**
2. Find `AzureOCR__Key`
3. Click to edit
4. **Delete ALL the text** (select all and delete)
5. Go to **Computer Vision** → **Keys and Endpoint** → **KEY 1**
6. **Copy the key** (click the copy icon)
7. **Paste it directly** into the App Service setting
8. **Don't add any spaces** - paste it exactly as copied
9. Click **OK**
10. Click **Save**
11. **Restart** the App Service
12. Wait 3-5 minutes
13. Test again

---

## 🔍 Step 5: Try KEY 2 Instead

**Sometimes KEY 1 has issues, try KEY 2:**

1. Go to **Computer Vision** → **Keys and Endpoint**
2. **Copy KEY 2** (instead of KEY 1)
3. Go to **App Service** → **Environment variables**
4. Edit `AzureOCR__Key`
5. **Delete the current value**
6. **Paste KEY 2**
7. Click **OK**
8. Click **Save**
9. **Restart** the App Service
10. Wait 3-5 minutes
11. Test again

---

## 🔍 Step 6: Verify App Service Restarted

**After updating settings, you MUST restart:**

1. Go to **App Service** → **Overview**
2. Check the **"Status"** - should be "Running"
3. If you just updated settings, click **"Restart"** button
4. **Wait 3-5 minutes** for restart to complete
5. The status should show "Running" again
6. Test OCR again

**Note:** Sometimes Azure takes a few minutes to apply configuration changes. Wait at least 3-5 minutes after restart before testing.

---

## 🔍 Step 7: Test Key Directly (Advanced)

**Test if the key works outside your app:**

1. Go to **Azure Portal** → Click **Cloud Shell** icon (top right)
2. Choose **Bash** or **PowerShell**
3. Run this command (replace with your actual key):

```bash
curl -X POST "https://bhcare-ocr.cognitiveservices.azure.com/vision/v3.2/read/analyze" \
  -H "Ocp-Apim-Subscription-Key: YOUR_COMPLETE_KEY_HERE" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @/path/to/test/image.jpg \
  -v
```

**Expected results:**
- `HTTP/2 202 Accepted` → Key works ✅
- `HTTP/2 401 Unauthorized` → Key is wrong/invalid ❌
- `HTTP/2 403 Forbidden` → Key doesn't have permission ❌

---

## 🚨 Most Common Issues

### Issue 1: Leading/Trailing Spaces
**Symptom:** Key looks correct but still getting 401
**Fix:** Delete and re-paste the key, making sure no spaces

### Issue 2: Key Regenerated But Not Updated
**Symptom:** Key was regenerated in Computer Vision but App Service still has old key
**Fix:** Get the new key from Computer Vision and update App Service

### Issue 3: Key and Endpoint Mismatch
**Symptom:** Key and endpoint are from different Computer Vision resources
**Fix:** Make sure both come from the same Computer Vision resource

### Issue 4: App Service Not Restarted
**Symptom:** Settings updated but still not working
**Fix:** Restart the App Service after updating settings

### Issue 5: Configuration Not Applied
**Symptom:** Settings look correct but app still uses old values
**Fix:** Wait 5 minutes after restart, or try stopping and starting the app

---

## 📋 Diagnostic Checklist

Before testing, verify ALL of these:

- [ ] Checked App Service Logs and saw the actual key being read
- [ ] Key length matches Computer Vision key (~100+ characters)
- [ ] First/last 10 characters of key match Computer Vision key
- [ ] No leading or trailing spaces in the key
- [ ] Key and endpoint are from the same Computer Vision resource
- [ ] App Service was restarted after updating settings
- [ ] Waited 3-5 minutes after restart before testing
- [ ] Tried KEY 2 instead of KEY 1 (if KEY 1 doesn't work)

---

## 💡 Next Steps

After checking the logs, you'll know:
1. **If the key is being read correctly** - Check the log messages
2. **If the key is correct** - Compare first/last 10 chars with Computer Vision
3. **If there's a whitespace issue** - The key will have extra characters
4. **If the endpoint is correct** - Check the log message

**Share the log messages** and we can pinpoint the exact issue!

