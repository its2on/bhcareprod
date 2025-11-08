# Quick Fix: OCR "Unauthorized" Error on Azure

## Problem
OCR works on localhost but returns "Unauthorized" on Azure deployment.

## Root Cause
Azure App Service needs environment variables with **double underscores (`__`)** instead of colons (`:`) for nested configuration.

## Quick Fix (5 minutes)

### Step 1: Get Your Azure OCR Key
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Computer Vision** resource → **bhcare-ocr**
3. Click **"Keys and Endpoint"** (left menu)
4. Click the **copy icon** next to **KEY 1**
5. **Save the complete key** (should be ~100+ characters)

### Step 2: Configure Azure App Service
1. Go to **App Services** → **bhcare-webapp** (or your app service name)
2. Click **"Configuration"** (left menu)
3. Click **"Application settings"** tab
4. Look for these settings:
   - `AzureOCR__Endpoint` (double underscore `__`)
   - `AzureOCR__Key` (double underscore `__`)

### Step 3: Add/Update Settings

**If settings DON'T exist:**
1. Click **"+ New application setting"**
2. Add these two settings:

**Setting 1:**
- **Name**: `AzureOCR__Endpoint` (double underscore)
- **Value**: `https://bhcare-ocr.cognitiveservices.azure.com/`
- Click **OK**

**Setting 2:**
- **Name**: `AzureOCR__Key` (double underscore)
- **Value**: Paste the **complete KEY 1** you copied from Step 1
- Click **OK**

**If settings EXIST:**
1. Click on `AzureOCR__Endpoint` to edit
   - Verify value is: `https://bhcare-ocr.cognitiveservices.azure.com/`
   - Must end with `/`
   - Click **OK**

2. Click on `AzureOCR__Key` to edit
   - **Delete the current value completely**
   - Paste the **complete KEY 1** from Step 1
   - Make sure there are no spaces
   - Click **OK**

### Step 4: Save and Restart
1. Click **"Save"** button at the top
2. Click **"Continue"** when prompted to restart
3. **Wait 3-5 minutes** for restart to complete

### Step 5: Test
1. Go to your website: `bhcare.software/Account/SignUp`
2. Upload an ID image
3. OCR should work now! ✅

---

## Important Notes

⚠️ **CRITICAL:**
- Setting names MUST use **double underscore `__`** (not single `_`)
- `AzureOCR__Endpoint` (correct) ✅
- `AzureOCR_Endpoint` (wrong) ❌

⚠️ **Key must be COMPLETE:**
- Should be ~100+ characters long
- Must match exactly what's in Computer Vision → Keys and Endpoint
- No spaces before or after

⚠️ **Endpoint must end with `/`:**
- `https://bhcare-ocr.cognitiveservices.azure.com/` ✅
- `https://bhcare-ocr.cognitiveservices.azure.com` ❌

---

## Verify Settings Are Correct

After saving, verify:
1. Go to **Configuration** → **Application settings**
2. Find `AzureOCR__Endpoint`:
   - Value should be: `https://bhcare-ocr.cognitiveservices.azure.com/`
3. Find `AzureOCR__Key`:
   - Value should be the complete key (100+ characters)
   - Should match what's in Computer Vision resource

---

## If Still Not Working

1. **Check App Service Logs:**
   - Go to **Log stream** (left menu)
   - Try uploading an ID
   - Look for error messages

2. **Try KEY 2 instead:**
   - Get KEY 2 from Computer Vision resource
   - Update `AzureOCR__Key` with KEY 2
   - Save and restart

3. **Regenerate the key:**
   - Go to Computer Vision → Keys and Endpoint
   - Click **"Regenerate Key 1"**
   - Copy the new key
   - Update `AzureOCR__Key` with the new key
   - Save and restart

---

## Quick Reference

**App Service Name**: `bhcare-webapp`  
**Resource Group**: `BHcare`  
**Computer Vision Resource**: `bhcare-ocr`  
**Required Settings**:
- `AzureOCR__Endpoint` = `https://bhcare-ocr.cognitiveservices.azure.com/`
- `AzureOCR__Key` = (complete KEY 1 from Computer Vision)

