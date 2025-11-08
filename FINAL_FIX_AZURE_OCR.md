# Final Fix for Azure OCR 401 Unauthorized Error

## 🔍 Root Cause

The logs show the request is being made, but Azure is rejecting the key. This means:
1. The configuration might not be read correctly (wrong setting name)
2. The key value might be wrong/incomplete
3. The key might not match the endpoint

## ✅ Complete Fix Steps

### Step 1: Verify Setting Names in App Service

**CRITICAL: Both settings MUST use DOUBLE underscore `__`**

1. Go to **Azure Portal** → **App Service** (`barangaybhcare`) → **Environment variables**
2. **Check if you have BOTH of these settings:**
   - `AzureOCR__Endpoint` (double underscore `__`) ✅
   - `AzureOCR__Key` (double underscore `__`) ✅

3. **If you see these (WRONG - delete them):**
   - `AzureOCR_Endpoint` (single underscore) ❌
   - `AzureOCR_Key` (single underscore) ❌
   
   **Delete them immediately!**

### Step 2: Get the Complete Key from Computer Vision

1. Go to **Azure Portal** → **Computer Vision** resource (`bhcare-ocr`)
2. Click **"Keys and Endpoint"** (left menu)
3. **Copy KEY 1 completely** (click the copy icon)
4. **Verify the key:**
   - Should be about 100+ characters long
   - Should NOT end with `!` or any special character
   - Should be alphanumeric (letters and numbers only)

### Step 3: Update App Service Settings

1. Go to **App Service** → **Environment variables**
2. **Setting 1: `AzureOCR__Endpoint`**
   - Click to edit
   - Value should be: `https://bhcare-ocr.cognitiveservices.azure.com/`
   - Must end with `/`
   - Click **OK**

3. **Setting 2: `AzureOCR__Key`**
   - Click to edit
   - **Delete the entire current value**
   - **Paste the complete KEY 1** you copied from Step 2
   - **Verify:**
     - No spaces before or after
     - Complete key (100+ characters)
     - Matches exactly what's in Computer Vision resource
   - Click **OK**

### Step 4: Save and Restart

1. Click **"Apply"** at the bottom (or **"Save"** at the top)
2. Wait for save to complete
3. Go to **Overview** → Click **"Restart"** button
4. **Wait 5 minutes** for restart to complete

### Step 5: Deploy Updated Code (If Not Already Deployed)

The logging code needs to be deployed to see what key is being read:

1. **Commit and push** your code changes (if not already done)
2. **Deploy** to Azure App Service
3. Wait for deployment to complete

### Step 6: Test and Check Logs

1. Go to your website and try uploading an ID image
2. Go to **App Service** → **Log stream**
3. **Look for these log messages:**
   - `Azure OCR Endpoint: https://...`
   - `Azure OCR Key length: XXX characters`
   - `Azure OCR Key (first 10 chars): ...`
   - `Azure OCR Key (last 10 chars): ...`

4. **Compare:**
   - Does the key length match your Computer Vision key?
   - Do the first/last 10 characters match?

---

## 🚨 If Still Not Working

### Option 1: Try KEY 2 Instead

1. Go to **Computer Vision** → **Keys and Endpoint**
2. Copy **KEY 2** (instead of KEY 1)
3. Update `AzureOCR__Key` with KEY 2
4. Save and restart
5. Test again

### Option 2: Regenerate the Key

1. Go to **Computer Vision** → **Keys and Endpoint**
2. Click **"Regenerate Key 1"**
3. Copy the **new KEY 1**
4. Update `AzureOCR__Key` with the new key
5. Save and restart
6. Test again

### Option 3: Test Key Directly

**Test if the key works outside your app:**

1. Go to **Azure Cloud Shell** (top right in Azure Portal)
2. Run this command (replace with your actual key):

```bash
curl -X POST "https://bhcare-ocr.cognitiveservices.azure.com/vision/v3.2/read/analyze" \
  -H "Ocp-Apim-Subscription-Key: YOUR_COMPLETE_KEY_HERE" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @/path/to/test/image.jpg \
  -v
```

**Expected results:**
- `HTTP/2 202` → Key works ✅
- `HTTP/2 401` → Key is wrong ❌

---

## 📋 Checklist

Before testing, verify ALL of these:

- [ ] `AzureOCR__Endpoint` exists (double underscore `__`)
- [ ] `AzureOCR__Key` exists (double underscore `__`)
- [ ] NO `AzureOCR_Endpoint` or `AzureOCR_Key` (single underscore) exist
- [ ] Endpoint value is: `https://bhcare-ocr.cognitiveservices.azure.com/` (ends with `/`)
- [ ] Key value is the **complete** key from Computer Vision resource
- [ ] Key value has no spaces before or after
- [ ] Key value matches exactly what's in Computer Vision → Keys and Endpoint
- [ ] Settings are **Saved** (green notification)
- [ ] App Service is **Restarted** after updating settings
- [ ] Waited **5 minutes** after restart before testing
- [ ] Code with logging is **deployed** to App Service

---

## 🎯 Most Likely Issues

1. **Setting name wrong:** Using `AzureOCR_Key` (single underscore) instead of `AzureOCR__Key` (double underscore)
2. **Key incomplete:** Key is truncated or has wrong characters
3. **Key doesn't match endpoint:** Key is from a different Computer Vision resource

---

## ✅ After Fixing

Once you've fixed the settings and restarted, the logs should show:
- `Azure OCR Key length: 100+ characters` (or whatever the actual length is)
- `Azure OCR Key (first 10 chars): 3g63cprczn...`
- `Azure OCR Key (last 10 chars): ...OGaA2z`

And the OCR should work without 401 errors!

