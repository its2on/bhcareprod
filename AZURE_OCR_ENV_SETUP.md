# Azure OCR Environment Variables Setup

## Quick Setup Guide

This guide shows you how to configure Azure OCR environment variables for different deployment scenarios.

---

## 🔧 For Azure App Service (Production)

### Option 1: Azure Portal (Recommended for Quick Setup)

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your **App Service** → **Configuration** → **Application settings**
3. Click **+ New application setting**
4. Add these two settings:

   **Setting 1:**
   - **Name:** `AzureOCR__Endpoint`
   - **Value:** `https://bhcare-ocr.cognitiveservices.azure.com/`
   - Click **OK**

   **Setting 2:**
   - **Name:** `AzureOCR__Key`
   - **Value:** `YOUR_ACTUAL_AZURE_COMPUTER_VISION_KEY`
   - Click **OK**

5. Click **Save** at the top
6. Click **Continue** to restart the app

### Option 2: Azure CLI

```bash
az webapp config appsettings set \
  --name YOUR_APP_SERVICE_NAME \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings \
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" \
    "AzureOCR__Key=YOUR_ACTUAL_AZURE_COMPUTER_VISION_KEY"

# Restart the app
az webapp restart --name YOUR_APP_SERVICE_NAME --resource-group YOUR_RESOURCE_GROUP
```

### Option 3: PowerShell

```powershell
az webapp config appsettings set `
  --name "YOUR_APP_SERVICE_NAME" `
  --resource-group "YOUR_RESOURCE_GROUP" `
  --settings `
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" `
    "AzureOCR__Key=YOUR_ACTUAL_AZURE_COMPUTER_VISION_KEY"

# Restart the app
az webapp restart --name "YOUR_APP_SERVICE_NAME" --resource-group "YOUR_RESOURCE_GROUP"
```

---

## 🔄 For GitHub Actions / CI/CD

1. Go to your **GitHub Repository**
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add secret:
   - **Name:** `AZURE_OCR_KEY`
   - **Value:** Your actual Azure Computer Vision subscription key
   - Click **Add secret**

The deployment workflow (`.github/workflows/azure-deploy.yml`) will automatically use this secret.

---

## 💻 For Local Development

### Windows (PowerShell)

```powershell
$env:AzureOCR__Endpoint="https://bhcare-ocr.cognitiveservices.azure.com/"
$env:AzureOCR__Key="your-azure-computer-vision-key-here"
```

### Windows (Command Prompt)

```cmd
set AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/
set AzureOCR__Key=your-azure-computer-vision-key-here
```

### Linux/macOS (Bash)

```bash
export AzureOCR__Endpoint="https://bhcare-ocr.cognitiveservices.azure.com/"
export AzureOCR__Key="your-azure-computer-vision-key-here"
```

### Using .env file (if supported)

Create a `.env` file in your project root:

```env
AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/
AzureOCR__Key=your-azure-computer-vision-key-here
```

---

## 🔑 How to Get Your Azure Computer Vision Key

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your **Computer Vision** resource (or create one if you don't have it)
3. Go to **Keys and Endpoint** section
4. Copy **Key 1** or **Key 2**
5. Copy the **Endpoint** URL (should be like `https://your-resource-name.cognitiveservices.azure.com/`)

---

## ✅ Verify Configuration

After setting up the environment variables, verify they're working:

1. **Check Azure App Service:**
   - Go to **Configuration** → **Application settings**
   - Verify both `AzureOCR__Endpoint` and `AzureOCR__Key` are present

2. **Test the OCR functionality:**
   - Go to the Sign Up page
   - Try uploading an ID image
   - The OCR should work without "Unauthorized" errors

---

## 🚨 Troubleshooting

### Error: "OCR service error: Unauthorized"

**Cause:** Azure OCR key is missing or incorrect.

**Solution:**
1. Verify the key is set correctly in Azure App Service Configuration
2. Make sure the key matches the one from Azure Portal
3. Restart the App Service after adding/updating settings

### Error: "OCR service is not configured"

**Cause:** Environment variables are not set.

**Solution:**
1. Add both `AzureOCR__Endpoint` and `AzureOCR__Key` to Azure App Service Configuration
2. Restart the App Service

### Key Format

- The key should be a 32-character alphanumeric string
- Example format: `a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6`
- No spaces or special characters (except hyphens in some cases)

---

## 📝 Notes

- **Double underscore (`__`)** is used in ASP.NET Core to represent nested configuration (e.g., `AzureOCR__Key` maps to `AzureOCR:Key` in JSON)
- Environment variables take precedence over `appsettings.json` values
- Never commit actual keys to version control
- Use Azure Key Vault for enhanced security in production (recommended)

---

## 🔐 Security Best Practices

1. ✅ Store keys in Azure Key Vault (recommended for production)
2. ✅ Use GitHub Secrets for CI/CD pipelines
3. ✅ Rotate keys regularly
4. ✅ Never commit keys to source control
5. ✅ Use different keys for development and production
6. ✅ Monitor key usage in Azure Portal

