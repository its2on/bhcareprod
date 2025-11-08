# Verify Azure OCR Setup - Troubleshooting Guide

If you're still getting "OCR service error: Unauthorized", follow this checklist:

---

## ✅ Step 1: Verify Settings in Azure App Service

### Check if Settings Exist:

1. Go to **Azure Portal** → Your **App Service** → **Configuration** → **Application settings**
2. **Search for** (use Ctrl+F or Cmd+F):
   - `AzureOCR__Endpoint` ← Must exist
   - `AzureOCR__Key` ← Must exist

### If Settings Are Missing:

**Add them now:**

1. Click **"+ New application setting"**
2. **Setting 1:**
   - Name: `AzureOCR__Endpoint` (double underscore `__`)
   - Value: `https://bhcare-ocr.cognitiveservices.azure.com/`
3. Click **"+ New application setting"** again
4. **Setting 2:**
   - Name: `AzureOCR__Key` (double underscore `__`)
   - Value: `YOUR_AZURE_COMPUTER_VISION_KEY`
5. Click **"Save"** at the top
6. Click **"Continue"** to restart

---

## ✅ Step 2: Verify Setting Names (Common Mistake!)

**⚠️ CRITICAL: The setting names MUST use double underscore `__`**

❌ **WRONG:**
- `AzureOCR_Endpoint` (single underscore)
- `AzureOCR_Key` (single underscore)
- `AzureOCR:Endpoint` (colon)
- `AzureOCR:Key` (colon)

✅ **CORRECT:**
- `AzureOCR__Endpoint` (double underscore)
- `AzureOCR__Key` (double underscore)

**If you used single underscore, delete and re-add with double underscore!**

---

## ✅ Step 3: Verify Setting Values

### Check Endpoint:
- Should be: `https://bhcare-ocr.cognitiveservices.azure.com/`
- Must end with `/` (forward slash)
- No extra spaces before or after

### Check Key:
- Should be: `YOUR_AZURE_COMPUTER_VISION_KEY`
- No spaces
- Complete key (should be long, about 100+ characters)
- Matches the key from Computer Vision resource

---

## ✅ Step 4: Restart App Service

**After adding/updating settings, you MUST restart:**

1. In Azure Portal → Your App Service
2. Click **"Restart"** button (top toolbar)
3. Wait 1-2 minutes for restart to complete
4. Try the OCR again

---

## ✅ Step 5: Check App Service Logs

**To see what's happening:**

1. Go to **App Service** → **Log stream** (left menu)
2. Try uploading an ID image again
3. Watch the logs for:
   - `Azure OCR configuration is missing` ← Means settings not found
   - `Azure Read API error: 401` ← Means unauthorized (key wrong)
   - `Azure Read API error: 403` ← Means forbidden (key wrong)

---

## 🔧 Quick Fix Commands (Azure CLI)

**If you have Azure CLI installed, use these commands:**

```bash
# 1. Check current settings
az webapp config appsettings list \
  --name YOUR_APP_SERVICE_NAME \
  --resource-group YOUR_RESOURCE_GROUP \
  --query "[?name=='AzureOCR__Endpoint' || name=='AzureOCR__Key']"

# 2. Set the settings (replace YOUR_APP_SERVICE_NAME and YOUR_RESOURCE_GROUP)
az webapp config appsettings set \
  --name YOUR_APP_SERVICE_NAME \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings \
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" \
    "AzureOCR__Key=YOUR_AZURE_COMPUTER_VISION_KEY"

# 3. Restart the app
az webapp restart \
  --name YOUR_APP_SERVICE_NAME \
  --resource-group YOUR_RESOURCE_GROUP
```

---

## 🚨 Common Issues & Solutions

### Issue 1: Settings Not Found

**Error in logs:** `Azure OCR configuration is missing`

**Solution:**
- Settings don't exist in App Service Configuration
- Add them following Step 1 above

---

### Issue 2: Wrong Setting Name Format

**Error:** Still getting "Unauthorized" even though settings exist

**Solution:**
- Check if you used single underscore `_` instead of double `__`
- Delete the wrong settings
- Add them again with double underscore `__`

---

### Issue 3: Key is Wrong

**Error:** `401 Unauthorized` or `403 Forbidden`

**Solution:**
1. Go back to Computer Vision resource → **Keys and Endpoint**
2. Try using **KEY 2** instead of KEY 1
3. Update `AzureOCR__Key` in App Service Configuration
4. Save and restart

---

### Issue 4: App Not Restarted

**Error:** Settings are correct but still not working

**Solution:**
- Manually restart the App Service
- Wait 2-3 minutes after restart
- Try again

---

### Issue 5: Endpoint URL Wrong

**Error:** Can't connect to endpoint

**Solution:**
1. Check your Computer Vision resource endpoint
2. Make sure it matches exactly (including the `/` at the end)
3. Update `AzureOCR__Endpoint` if different

---

## 📋 Complete Checklist

Before testing, verify ALL of these:

- [ ] `AzureOCR__Endpoint` exists in App Service Configuration
- [ ] `AzureOCR__Key` exists in App Service Configuration
- [ ] Both use **double underscore** `__` (not single `_`)
- [ ] Endpoint value is: `https://bhcare-ocr.cognitiveservices.azure.com/`
- [ ] Key value matches your Computer Vision KEY 1
- [ ] No extra spaces in values
- [ ] Settings are **Saved** (green notification)
- [ ] App Service is **Restarted** after adding settings
- [ ] Waited 2-3 minutes after restart before testing

---

## 🎯 Still Not Working?

If you've checked everything above and it still doesn't work:

1. **Delete both settings** from App Service Configuration
2. **Save** and **Restart**
3. **Add them again** following Step 1 exactly
4. **Save** and **Restart** again
5. **Wait 3 minutes**
6. **Try again**

If it still fails, check:
- Is your Computer Vision resource **Active**? (not deleted/suspended)
- Is the key still valid? (try regenerating it)
- Are there any network restrictions on your App Service?

---

## 💡 Pro Tip: Test with Azure CLI

You can test if the key works directly:

```bash
# Test the key (replace with your actual endpoint and key)
curl -X POST "https://bhcare-ocr.cognitiveservices.azure.com/vision/v3.2/read/analyze" \
  -H "Ocp-Apim-Subscription-Key: YOUR_AZURE_COMPUTER_VISION_KEY" \
  -H "Content-Type: application/json" \
  --data-binary @your-image.jpg
```

If this works, the key is correct and the issue is in the App Service configuration.

