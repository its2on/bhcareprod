# Debug Azure OCR "Unauthorized" Error - Complete Guide

Since you've updated the key and it's still not working, let's debug systematically.

---

## 🔍 Step 1: Check App Service Logs (Most Important!)

**This will show us exactly what's happening:**

1. Go to **Azure Portal** → Your **App Service** (`barangaybhcare`)
2. Click **"Log stream"** in the left menu (under Monitoring)
3. **Keep this open** in a separate tab/window
4. Go to your website and try uploading an ID image
5. **Watch the logs** - you should see messages like:

**Look for these messages:**
- `Calling Azure Read API: https://...` ← Shows the endpoint being called
- `Azure Read API error: 401 - ...` ← Shows the exact error
- `Azure OCR configuration is missing` ← Means settings not found
- `Azure Read API error: 403 - ...` ← Forbidden error

**What to check:**
- Is the endpoint URL correct?
- What's the exact error message?
- Is the key being read? (Check if there are any warnings about missing config)

---

## 🔍 Step 2: Verify Configuration is Being Read

**Check if the app is actually reading the settings:**

1. Go to **App Service** → **Log stream**
2. Look for these log messages when the app starts:
   - `Azure Computer Vision credentials not configured` ← Means settings not found
   - If you don't see this, the settings might be found but the key is wrong

**Alternative: Add temporary logging**

We can add a log to see what values are being read. But first, let's check the logs.

---

## 🔍 Step 3: Verify Key Value in App Service

**Make sure the key is complete and correct:**

1. Go to **App Service** → **Environment variables**
2. Find `AzureOCR__Key`
3. Click **"Show values"** button (eye icon at the top)
4. **Verify:**
   - The key is **complete** (should be 100+ characters)
   - No extra spaces at the beginning or end
   - Matches exactly what's in Computer Vision → Keys and Endpoint

**Common issues:**
- Key is truncated (missing end characters)
- Extra spaces before/after the key
- Wrong key (using KEY 2 when KEY 1 is configured, or vice versa)

---

## 🔍 Step 4: Test the Key Directly

**Test if the key works outside of your app:**

### Option A: Using curl (if you have it installed)

```bash
curl -X POST "https://bhcare-ocr.cognitiveservices.azure.com/vision/v3.2/read/analyze" \
  -H "Ocp-Apim-Subscription-Key: YOUR_COMPLETE_KEY_HERE" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @path/to/your/image.jpg
```

**Expected result:**
- `202 Accepted` → Key is correct ✅
- `401 Unauthorized` → Key is wrong ❌
- `403 Forbidden` → Key is wrong or resource has restrictions ❌

### Option B: Using Postman or similar tool

1. Create a POST request to:
   - URL: `https://bhcare-ocr.cognitiveservices.azure.com/vision/v3.2/read/analyze`
2. Add headers:
   - `Ocp-Apim-Subscription-Key`: Your complete key
   - `Content-Type`: `application/octet-stream`
3. Add body:
   - Select "binary"
   - Upload a test image
4. Send request

**Check the response:**
- Status `202` → Key works ✅
- Status `401` → Key is wrong ❌

---

## 🔍 Step 5: Check Computer Vision Resource Status

**Verify the resource is active and accessible:**

1. Go to **Azure Portal** → **Computer Vision** resource (`bhcare-ocr`)
2. Check **"Overview"** page:
   - Status should be **"Running"** or **"Succeeded"**
   - No errors or warnings
3. Check **"Keys and Endpoint"**:
   - Both KEY 1 and KEY 2 should be visible
   - Endpoint should be: `https://bhcare-ocr.cognitiveservices.azure.com/`
4. Check **"Networking"** (if available):
   - Make sure there are no network restrictions blocking your App Service
   - If "Public access" is enabled, that's good

---

## 🔍 Step 6: Verify Endpoint URL

**Make sure the endpoint matches exactly:**

1. Go to **Computer Vision** → **Keys and Endpoint**
2. Copy the **Endpoint** URL exactly
3. Go to **App Service** → **Environment variables**
4. Check `AzureOCR__Endpoint`:
   - Should match exactly (including the `/` at the end)
   - Should be: `https://bhcare-ocr.cognitiveservices.azure.com/`

**Common issues:**
- Missing `/` at the end
- Extra spaces
- Wrong endpoint (different resource)

---

## 🔍 Step 7: Try Regenerating the Key

**Sometimes keys get corrupted or have issues:**

1. Go to **Computer Vision** → **Keys and Endpoint**
2. Click **"Regenerate Key 1"** (or use KEY 2)
3. Copy the **new key**
4. Go to **App Service** → **Environment variables**
5. Update `AzureOCR__Key` with the **new key**
6. Click **"Apply"** or **"Save"**
7. **Restart** the App Service
8. Wait 3-5 minutes
9. Test again

---

## 🔍 Step 8: Check for Hidden Characters or Encoding Issues

**Sometimes keys have hidden characters:**

1. In **App Service** → **Environment variables**
2. Edit `AzureOCR__Key`
3. **Delete the entire value**
4. **Type or paste the key fresh** (don't copy from a previous field)
5. Make sure there are **no spaces** before or after
6. Click **"OK"**
7. Click **"Apply"** or **"Save"**
8. **Restart** the App Service

---

## 🔍 Step 9: Verify App Service Restart

**Make sure the app actually restarted:**

1. Go to **App Service** → **Overview**
2. Check **"Status"** - should be **"Running"**
3. Check **"Last restart time"** - should be recent (after you updated settings)
4. If it's old, manually restart:
   - Click **"Restart"** button
   - Wait 3-5 minutes
   - Try again

---

## 🔍 Step 10: Check Network/Firewall Restrictions

**Check if there are any network restrictions:**

1. Go to **App Service** → **Networking**
2. Check if there are any:
   - VNet restrictions
   - Access restrictions
   - Firewall rules
3. Go to **Computer Vision** → **Networking** (if available)
4. Check if there are any:
   - Network restrictions
   - Private endpoint settings

**If there are restrictions:**
- Make sure your App Service is allowed to access the Computer Vision resource
- Or remove the restrictions temporarily to test

---

## 🎯 Most Likely Issues (Based on Your Situation)

### Issue 1: Key Still Truncated or Has Spaces

**Solution:**
- Delete the key value completely
- Get fresh key from Computer Vision resource
- Paste it carefully (no spaces)
- Save and restart

### Issue 2: App Service Not Restarted

**Solution:**
- Manually restart the App Service
- Wait 3-5 minutes
- Try again

### Issue 3: Configuration Not Loading

**Solution:**
- Check App Service logs for "Azure OCR configuration is missing"
- If you see this, the settings aren't being read
- Verify the setting names use double underscore `__`

### Issue 4: Wrong Endpoint or Key

**Solution:**
- Verify endpoint matches Computer Vision resource exactly
- Verify key matches KEY 1 or KEY 2 exactly
- Try regenerating the key

---

## 📋 Action Items - Do These Now

1. **Check App Service Logs** (Step 1) - This is the most important!
   - What error message do you see?
   - What endpoint is being called?
   - Is the configuration being read?

2. **Verify Key is Complete** (Step 3)
   - Show values in App Service
   - Compare with Computer Vision resource
   - Make sure it's the complete key

3. **Test Key Directly** (Step 4)
   - Use curl or Postman
   - Does the key work outside the app?

4. **Restart App Service** (Step 9)
   - Make sure it restarted after updating settings

---

## 💡 Quick Test Script

**Run this in Azure Cloud Shell to test your key:**

```bash
# Replace with your actual key
KEY="YOUR_AZURE_COMPUTER_VISION_KEY"

# Test the key
curl -X POST "https://bhcare-ocr.cognitiveservices.azure.com/vision/v3.2/read/analyze" \
  -H "Ocp-Apim-Subscription-Key: $KEY" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @/path/to/test/image.jpg \
  -v
```

**Check the response:**
- `HTTP/2 202` → Key works ✅
- `HTTP/2 401` → Key is wrong ❌

---

## 🚨 Next Steps

**After checking the logs (Step 1), share:**
1. What error message appears in the logs?
2. What endpoint URL is being called?
3. Is there a message about "configuration is missing"?

**This will help us identify the exact issue!**

