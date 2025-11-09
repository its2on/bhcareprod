# Azure OCR Key Setup

## 🔐 Security Note
The Azure OCR key has been removed from `appsettings.json` for security reasons.

## 📋 Configuration Files

### Development (Local)
- **File:** `appsettings.json`
- **Status:** Key is empty `""`
- **Action:** Add your key locally (DO NOT commit)

### Production (Azure/Server)
You have **two options** to set the Azure OCR key in production:

---

## Option 1: Use appsettings.Production.json (Recommended)

1. **Edit `appsettings.Production.json`** on your server:
   ```json
   "AzureOCR": {
     "Endpoint": "https://bhcare-ocr.cognitiveservices.azure.com/",
     "Key": "YOUR_AZURE_OCR_KEY_HERE"
   }
   ```

2. **This file is in `.gitignore`** so it won't be pushed to GitHub

---

## Option 2: Use Environment Variables (Most Secure)

Set environment variable on your Azure App Service or server:

```bash
AzureOCR__Key=YOUR_AZURE_OCR_KEY_HERE
```

**Note:** Use double underscore `__` for nested config (ASP.NET Core convention)

### Azure App Service:
1. Go to your App Service in Azure Portal
2. Navigate to **Configuration** → **Application settings**
3. Click **+ New application setting**
4. Name: `AzureOCR__Key`
5. Value: `YOUR_AZURE_OCR_KEY_HERE`
6. Click **OK** → **Save**

---

## 🧪 Testing Locally

If you want to test OCR locally:

1. **Edit `appsettings.json`** (your local copy only):
   ```json
   "AzureOCR": {
     "Endpoint": "https://bhcare-ocr.cognitiveservices.azure.com/",
     "Key": "YOUR_AZURE_OCR_KEY_HERE"
   }
   ```

2. **DO NOT commit this change!**
   ```bash
   git checkout appsettings.json  # Revert changes before committing
   ```

---

## 🔑 Your Azure OCR Key

**Endpoint:** `https://bhcare-ocr.cognitiveservices.azure.com/`  
**Key:** `YOUR_AZURE_OCR_KEY_HERE`

**Keep this key secure!** Do not share publicly or commit to GitHub.
