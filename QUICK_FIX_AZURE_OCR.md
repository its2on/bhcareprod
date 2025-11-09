# Quick Fix: Configure Azure Vision OCR (5 Minutes)

## The Problem
Your logs show: `Azure Vision OCR credentials not configured. Checked: AzureOCR__Endpoint, AzureOCR:Endpoint, AzureOCR__Key, AzureOCR:Key`

This means the environment variables are **not set** in Azure App Service.

## Solution: Set Environment Variables

### Option 1: Use PowerShell Script (Easiest)

1. **Run the script:**
   ```powershell
   .\configure-azure-ocr.ps1
   ```

2. **Follow the prompts** - it will automatically:
   - Detect your resource group
   - Set the environment variables
   - Restart the App Service

### Option 2: Manual Configuration via Azure Portal

1. Go to **Azure Portal** → App Service (`barangaybhcare`)
2. Navigate to **Configuration** → **Application settings**
3. Click **+ New application setting**
4. Add **Setting 1:**
   - **Name:** `AzureOCR__Endpoint` (double underscore `__`)
   - **Value:** `https://bhcare-ocr.cognitiveservices.azure.com/`
   - Click **OK**
5. Add **Setting 2:**
   - **Name:** `AzureOCR__Key` (double underscore `__`)
   - **Value:** `YOUR_AZURE_OCR_KEY_HERE` (Get this from Azure Portal → Computer Vision resource → Keys and Endpoint → KEY 1)
   - Click **OK**
6. Click **Save** at the top
7. **Restart** the App Service

### Option 3: Use Azure CLI (Command Line)

```bash
az webapp config appsettings set \
  --name barangaybhcare \
  --resource-group <your-resource-group> \
  --settings \
    AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/ \
    AzureOCR__Key=YOUR_AZURE_OCR_KEY_HERE

az webapp restart --name barangaybhcare --resource-group <your-resource-group>
```

## Verification

After restart, check the logs. You should see:
- ✅ `Azure Vision OCR configured - Endpoint: https://..., Key length: 100`
- ✅ No more "Azure Vision OCR credentials not configured" warnings
- ✅ OCR processing should work when uploading ID images

## Why This Works

ASP.NET Core reads environment variables with double underscores (`__`) as nested configuration. So:
- `AzureOCR__Endpoint` → `AzureOCR:Endpoint` in code
- `AzureOCR__Key` → `AzureOCR:Key` in code

The code now checks both formats, so it will find the configuration once you set these variables.

## Next Steps

Once Azure Vision OCR is working:
1. Test OCR by uploading an ID image
2. For local OCR (Tesseract), you'll need Docker deployment (see `OCR_TROUBLESHOOTING_COMPLETE.md`)

