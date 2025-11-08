# Complete Fix for Azure OCR "Unauthorized" Error

## 🔍 Root Cause Analysis

The code is looking for:
- `AzureOCR:Endpoint` (with colon `:`)
- `AzureOCR:Key` (with colon `:`)

In ASP.NET Core, environment variables use **double underscore `__`** to represent colon `:`:
- `AzureOCR__Endpoint` → maps to `AzureOCR:Endpoint` ✅
- `AzureOCR__Key` → maps to `AzureOCR:Key` ✅

---

## 🚨 Critical Issues to Check

### Issue 1: Key is Truncated/Incomplete

**Problem:** Your key value might be incomplete or truncated.

**From your screenshot, I see:**
- Key value: `YOUR_AZURE_COMPUTER_VISION_KEY`

**But the original key from Computer Vision resource is:**
- `YOUR_AZURE_COMPUTER_VISION_KEY`

**The key is missing the end part!** (`COGaA2z` is missing)

**Solution:**
1. Go to **Computer Vision resource** → **Keys and Endpoint**
2. Copy the **complete KEY 1** (make sure you get the entire key)
3. Go to **App Service** → **Environment variables**
4. Edit `AzureOCR__Key`
5. Paste the **complete key** (should be about 100+ characters)
6. Click **OK**
7. Click **Apply** or **Save**
8. **Restart** the App Service

---

### Issue 2: Verify Settings Are Correct

**Check these in Azure Portal:**

1. **Go to App Service** → **Environment variables** (or **Configuration**)
2. **Verify both settings exist:**
   - `AzureOCR__Endpoint` (double underscore `__`)
   - `AzureOCR__Key` (double underscore `__`)

3. **Verify Endpoint value:**
   - Should be: `https://bhcare-ocr.cognitiveservices.azure.com/`
   - Must end with `/` (forward slash)
   - No extra spaces

4. **Verify Key value:**
   - Should be the **complete** key from Computer Vision resource
   - No spaces
   - Should be about 100+ characters long
   - Should match exactly what's in Computer Vision → Keys and Endpoint

---

### Issue 3: App Service Not Restarted

**After adding/updating settings, you MUST restart:**

1. Go to **App Service** → **Overview**
2. Click **"Restart"** button (top toolbar)
3. Wait **3-5 minutes** for restart to complete
4. Try the OCR again

---

### Issue 4: Check App Service Logs

**To see what's actually happening:**

1. Go to **App Service** → **Log stream** (left menu)
2. Try uploading an ID image
3. Watch the logs for:
   - `Azure OCR configuration is missing` ← Settings not found
   - `Azure Read API error: 401` ← Unauthorized (key wrong/incomplete)
   - `Azure Read API error: 403` ← Forbidden (key wrong/incomplete)
   - `Calling Azure Read API: https://...` ← Should show the endpoint being called

---

## ✅ Step-by-Step Complete Fix

### Step 1: Get the Complete Key

1. Go to **Azure Portal** → **Computer Vision** resource (`bhcare-ocr`)
2. Click **"Keys and Endpoint"** (left menu)
3. Click the **copy icon** next to **KEY 1**
4. **Save it somewhere** (you'll need the complete key)

### Step 2: Update App Service Settings

1. Go to **App Service** (`barangaybhcare`) → **Environment variables**
2. **Find `AzureOCR__Key`** in the list
3. **Click on it** to edit
4. **Delete the current value** (it's truncated)
5. **Paste the complete KEY 1** you copied
6. Click **"OK"** or **"Save"**

### Step 3: Verify Endpoint

1. **Find `AzureOCR__Endpoint`** in the list
2. **Click on it** to edit
3. **Verify the value is:** `https://bhcare-ocr.cognitiveservices.azure.com/`
4. Make sure it ends with `/`
5. Click **"OK"**

### Step 4: Save and Restart

1. Click **"Apply"** button at the bottom (or **"Save"** at the top)
2. Wait for save to complete
3. Go to **Overview** → Click **"Restart"** button
4. **Wait 3-5 minutes** for restart to complete

### Step 5: Test

1. Go to your website: `bhcare.software/Account/SignUp`
2. Upload an ID image
3. It should work now! ✅

---

## 🔧 Alternative: Use Azure CLI to Set Complete Key

If you prefer using command line:

```bash
# Set the complete key (replace with your actual complete key)
az webapp config appsettings set \
  --name barangaybhcare \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings \
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" \
    "AzureOCR__Key=YOUR_AZURE_COMPUTER_VISION_KEY"

# Restart the app
az webapp restart \
  --name barangaybhcare \
  --resource-group YOUR_RESOURCE_GROUP
```

**Replace:**
- `YOUR_RESOURCE_GROUP` with your actual resource group name
- The key value with your **complete** KEY 1 from Computer Vision resource

---

## 🎯 Most Likely Issue: Truncated Key

Based on your screenshot, **the key value is incomplete/truncated**. The Azure Portal might have cut off the end of the key when displaying it, or it wasn't copied completely.

**Fix:**
1. Get the **complete** KEY 1 from Computer Vision resource
2. Update `AzureOCR__Key` with the **complete** key
3. Save and restart

---

## 📋 Final Checklist

Before testing, verify ALL of these:

- [ ] `AzureOCR__Endpoint` exists (double underscore `__`)
- [ ] `AzureOCR__Key` exists (double underscore `__`)
- [ ] Endpoint value is: `https://bhcare-ocr.cognitiveservices.azure.com/` (ends with `/`)
- [ ] Key value is the **COMPLETE** key from Computer Vision resource (100+ characters)
- [ ] Key value matches exactly what's in Computer Vision → Keys and Endpoint
- [ ] No extra spaces in either value
- [ ] Settings are **Saved** (green notification)
- [ ] App Service is **Restarted** after updating settings
- [ ] Waited **3-5 minutes** after restart before testing

---

## 🚨 If Still Not Working

1. **Try using KEY 2 instead of KEY 1:**
   - Get KEY 2 from Computer Vision resource
   - Update `AzureOCR__Key` with KEY 2
   - Save and restart

2. **Regenerate the key:**
   - Go to Computer Vision → Keys and Endpoint
   - Click **"Regenerate Key 1"**
   - Copy the new key
   - Update `AzureOCR__Key` with the new key
   - Save and restart

3. **Check App Service Logs:**
   - Look for specific error messages
   - Check if the endpoint URL is correct
   - Check if the key is being read correctly

4. **Verify Computer Vision Resource:**
   - Make sure the resource is **Active** (not deleted/suspended)
   - Check if there are any usage limits or restrictions

---

## 💡 Pro Tip: Test the Key Directly

You can test if the key works using curl:

```bash
curl -X POST "https://bhcare-ocr.cognitiveservices.azure.com/vision/v3.2/read/analyze" \
  -H "Ocp-Apim-Subscription-Key: YOUR_COMPLETE_KEY_HERE" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @your-image.jpg
```

If this returns `202 Accepted`, the key is correct. If it returns `401 Unauthorized`, the key is wrong or incomplete.

---

## ✅ Summary

**Most Likely Issue:** The key value is **truncated/incomplete** in your App Service Configuration.

**Solution:**
1. Get the **complete** KEY 1 from Computer Vision resource
2. Update `AzureOCR__Key` with the **complete** key
3. Save and restart App Service
4. Wait 3-5 minutes
5. Test again

This should fix the "Unauthorized" error! 🎉

