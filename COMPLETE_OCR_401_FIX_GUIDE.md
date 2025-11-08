# Complete Guide: Fix Azure OCR 401 Unauthorized Error

## 🔍 Problem Summary

**Error:** `401 Unauthorized - Access denied due to invalid subscription key or wrong API endpoint`

**Location:** BHCARE Sign-Up page ID scanning feature

**Root Cause:** The Azure Computer Vision key in Azure App Service is **truncated/incomplete** (83-84 characters instead of 100+ characters)

---

## 📊 Diagnostic Evidence

From the App Service logs:
```
AzureOCR:Key value: Length=83, First10=3g63cprznc, Last10=AFACOGaA2z
Environment variable check - AzureOCR__Key: Length=84
```

**Expected:** Azure Computer Vision keys are **100 characters long** (alphanumeric)

**Actual:** Key in App Service is only **83-84 characters** - **TRUNCATED**

---

## ✅ Solution: Update Azure App Service with Complete Key

### Step 1: Get the Complete Key from Computer Vision

1. Go to **Azure Portal** → **Computer Vision** → **bhcare-ocr**
2. Click **"Keys and Endpoint"** (left menu)
3. Find **KEY 1** (should be 100 characters long)
4. Click the **copy icon** (📋) next to KEY 1
5. **Save the complete key** somewhere temporarily

**Expected Key Format:**
- Length: **100 characters**
- Format: Alphanumeric (letters and numbers)
- Example: `YOUR_COMPLETE_100_CHARACTER_KEY_FROM_COMPUTER_VISION_RESOURCE`

### Step 2: Update Azure App Service Configuration

1. Go to **Azure Portal** → **App Services** → **barangaybhcare**
2. Click **"Environment variables"** (left menu under Settings)
3. Click **"App settings"** tab
4. Find **`AzureOCR__Key`** in the list
5. **Click on `AzureOCR__Key`** to edit
6. In the **Value** field:
   - **Select all** (Ctrl+A)
   - **Delete** the current value
   - **Paste** the complete KEY 1 you copied (should be 100 characters)
   - **Verify:** No spaces before or after
7. Click **"Apply"** (in the dialog)
8. Click **"Apply"** (at the bottom of the Environment variables page)
9. **Restart** the App Service:
   - Go to **Overview** → Click **"Restart"** button
   - Wait **3-5 minutes** for restart to complete

### Step 3: Verify the Fix

1. Go to **App Service** → **Log stream**
2. Try uploading an ID image on your website
3. Check the logs - you should see:
   ```
   AzureOCR:Key value: Length=100, First10=3g63cprczn, Last10=AFACOGaA2z
   Environment variable check - AzureOCR__Key: Length=100
   ```
4. OCR should now work! ✅

---

## 🔧 Alternative: Use Azure CLI

If you prefer command line:

```bash
# Set the complete key (replace with your actual 100-character key from Computer Vision)
az webapp config appsettings set \
  --name barangaybhcare \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings \
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" \
    "AzureOCR__Key=YOUR_COMPLETE_100_CHARACTER_KEY_FROM_COMPUTER_VISION"

# Restart the app
az webapp restart \
  --name barangaybhcare \
  --resource-group YOUR_RESOURCE_GROUP
```

---

## 📋 Configuration Checklist

Before testing, verify ALL of these:

- [ ] `AzureOCR__Endpoint` exists (double underscore `__`)
- [ ] `AzureOCR__Key` exists (double underscore `__`)
- [ ] Endpoint value is: `https://bhcare-ocr.cognitiveservices.azure.com/` (ends with `/`)
- [ ] Key value is **100 characters long** (not 83-84)
- [ ] Key value matches exactly what's in Computer Vision → Keys and Endpoint → KEY 1
- [ ] No leading or trailing spaces in the key
- [ ] Settings are **Saved** (green notification)
- [ ] App Service is **Restarted** after updating settings
- [ ] Waited **3-5 minutes** after restart before testing

---

## 🚨 Common Mistakes to Avoid

### ❌ Wrong Setting Name Format
- `AzureOCR_Key` (single underscore) - **WRONG**
- `AzureOCR__Key` (double underscore) - **CORRECT** ✅

### ❌ Truncated Key
- Key length: 83-84 characters - **WRONG** ❌
- Key length: 100 characters - **CORRECT** ✅

### ❌ Wrong Endpoint Format
- `https://bhcare-ocr.cognitiveservices.azure.com` (no trailing slash) - **WRONG** ❌
- `https://bhcare-ocr.cognitiveservices.azure.com/` (with trailing slash) - **CORRECT** ✅

### ❌ Not Restarting After Update
- Updating settings but not restarting - **WRONG** ❌
- Updating settings AND restarting - **CORRECT** ✅

---

## 🔍 Code Verification

The code in `Pages/Account/SignUp.cshtml.cs` is correct:

```csharp
var azureEndpoint = _configuration["AzureOCR:Endpoint"];
var azureKey = _configuration["AzureOCR:Key"];

// Automatic whitespace trimming
azureKey = azureKey.Trim();
azureEndpoint = azureEndpoint.Trim();

// HTTP client setup
httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", azureKey);
```

**The code is working correctly** - the issue is the **configuration value** in Azure App Service.

---

## 📝 Key Facts About Azure Computer Vision Keys

1. **Length:** 100 characters (alphanumeric)
2. **Format:** Not hex - it's alphanumeric (letters and numbers)
3. **Location:** Computer Vision resource → Keys and Endpoint → KEY 1 or KEY 2
4. **Usage:** Both KEY 1 and KEY 2 work - use either one
5. **Security:** Never commit keys to Git - use Azure App Service environment variables

---

## 🎯 Quick Fix Summary

1. **Get complete KEY 1** from Computer Vision (100 characters)
2. **Update `AzureOCR__Key`** in App Service with complete key
3. **Save** and **Restart** App Service
4. **Wait 3-5 minutes**
5. **Test** - OCR should work! ✅

---

## 💡 Why This Happened

The key was likely:
- Truncated when copying from Azure Portal
- Incomplete when first set up
- Modified/truncated accidentally

**Solution:** Always use the **copy icon** (📋) in Azure Portal to copy keys completely, never copy manually.

---

## ✅ After Fix

Once the complete key is set:
- Logs will show: `Length=100` (instead of 83-84)
- OCR will work correctly
- No more 401 Unauthorized errors

The diagnostic logging we added will confirm the fix is working!

