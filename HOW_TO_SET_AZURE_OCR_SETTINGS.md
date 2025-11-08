# How to Set Azure OCR Settings - Step by Step Guide

## Method 1: Using Azure CLI (Command Line)

### Step 1: Install Azure CLI (if not installed)

**Windows:**
- Download from: https://aka.ms/installazurecliwindows
- Or use: `winget install -e --id Microsoft.AzureCLI`

**Mac:**
```bash
brew install azure-cli
```

**Linux:**
```bash
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

### Step 2: Login to Azure

Open PowerShell (Windows) or Terminal (Mac/Linux) and run:

```bash
az login
```

This will open a browser window for you to sign in to Azure.

### Step 3: Get Your Values

**Get your App Service name:**
- Go to Azure Portal → Your App Service
- The name is shown at the top (e.g., `barangaybhcare` or `barangay`)

**Get your Resource Group name:**
- Go to Azure Portal → Your App Service → Overview
- Look for "Resource group" (e.g., `BHcare` or `BHCARE-RG`)

**Get your complete Azure OCR Key:**
- Go to Azure Portal → Computer Vision resource (`bhcare-ocr`)
- Go to "Keys and Endpoint" (left menu)
- Click the copy icon next to "Key 1"
- Save it somewhere (you'll need it)

### Step 4: Run the Command

**Replace these values in the command:**
- `YOUR_APP_SERVICE_NAME` → Your actual App Service name (e.g., `barangaybhcare`)
- `YOUR_RESOURCE_GROUP` → Your actual resource group name (e.g., `BHcare`)
- `YOUR_COMPLETE_KEY_HERE` → Your complete KEY 1 from Computer Vision resource

**Windows (PowerShell):**
```powershell
az webapp config appsettings set `
  --name "barangaybhcare" `
  --resource-group "BHcare" `
  --settings `
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" `
    "AzureOCR__Key=YOUR_AZURE_COMPUTER_VISION_KEY"
```

**Mac/Linux (Bash):**
```bash
az webapp config appsettings set \
  --name "barangaybhcare" \
  --resource-group "BHcare" \
  --settings \
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" \
    "AzureOCR__Key=YOUR_AZURE_COMPUTER_VISION_KEY"
```

**Important Notes:**
- Replace `barangaybhcare` with your actual App Service name
- Replace `BHcare` with your actual resource group name
- Replace the key value with your complete KEY 1 from Computer Vision resource
- Use **double underscore** `__` in the setting names (not single `_`)

### Step 5: Restart the App Service

After setting the values, restart your App Service:

```bash
az webapp restart \
  --name "barangaybhcare" \
  --resource-group "BHcare"
```

**Or via PowerShell:**
```powershell
az webapp restart `
  --name "barangaybhcare" `
  --resource-group "BHcare"
```

---

## Method 2: Using Azure Portal (Easier - No CLI Needed)

### Step 1: Go to App Service Configuration

1. Go to **Azure Portal**: https://portal.azure.com
2. Search for your **App Service** name (e.g., `barangaybhcare`)
3. Click on your App Service
4. In the left menu, under **"Settings"**, click **"Configuration"**
5. Click the **"Application settings"** tab

### Step 2: Delete Wrong Settings (if they exist)

1. Look for `AzureOCR_Key` (single underscore) or `AzureOCR_Endpoint` (single underscore)
2. If they exist, click the **trash icon** (🗑️) to delete them
3. Click **"Save"** if you deleted anything

### Step 3: Add Correct Settings

**Add Setting 1: Endpoint**
1. Click **"+ New application setting"** button
2. Fill in:
   - **Name**: `AzureOCR__Endpoint` (double underscore `__`)
   - **Value**: `https://bhcare-ocr.cognitiveservices.azure.com/`
   - Make sure it ends with `/`
3. Click **"OK"**

**Add Setting 2: Key**
1. Click **"+ New application setting"** button again
2. Fill in:
   - **Name**: `AzureOCR__Key` (double underscore `__`)
   - **Value**: Paste your complete KEY 1 from Computer Vision resource
     - Go to Computer Vision resource → Keys and Endpoint
     - Copy KEY 1 completely
     - Paste it here (no spaces before/after)
3. Click **"OK"**

### Step 4: Save and Restart

1. Click **"Save"** button at the top
2. A popup will appear: "Save changes to application settings?"
3. Click **"Continue"** (this will restart your app)
4. Wait 3-5 minutes for restart to complete

---

## Method 3: Using Azure Cloud Shell (In Browser)

### Step 1: Open Cloud Shell

1. Go to **Azure Portal**: https://portal.azure.com
2. Click the **Cloud Shell icon** (top right, looks like `>_`)
3. Choose **"Bash"** or **"PowerShell"**

### Step 2: Run the Command

**Bash:**
```bash
az webapp config appsettings set \
  --name "YOUR_APP_SERVICE_NAME" \
  --resource-group "YOUR_RESOURCE_GROUP" \
  --settings \
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" \
    "AzureOCR__Key=YOUR_COMPLETE_KEY_HERE"
```

**PowerShell:**
```powershell
az webapp config appsettings set `
  --name "YOUR_APP_SERVICE_NAME" `
  --resource-group "YOUR_RESOURCE_GROUP" `
  --settings `
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" `
    "AzureOCR__Key=YOUR_COMPLETE_KEY_HERE"
```

### Step 3: Restart

```bash
az webapp restart --name "YOUR_APP_SERVICE_NAME" --resource-group "YOUR_RESOURCE_GROUP"
```

---

## Verification Steps

After setting the values, verify they're correct:

### Check via Azure Portal:
1. Go to **App Service** → **Configuration** → **Application settings**
2. Verify you see:
   - `AzureOCR__Endpoint` = `https://bhcare-ocr.cognitiveservices.azure.com/`
   - `AzureOCR__Key` = `[Your complete key]`
3. Make sure both use **double underscore** `__`

### Check via Azure CLI:
```bash
az webapp config appsettings list \
  --name "YOUR_APP_SERVICE_NAME" \
  --resource-group "YOUR_RESOURCE_GROUP" \
  --query "[?name=='AzureOCR__Endpoint' || name=='AzureOCR__Key']" \
  --output table
```

---

## Common Issues and Solutions

### Issue 1: "Command not found: az"
**Solution:** Azure CLI is not installed. Install it using Step 1 above.

### Issue 2: "Please run 'az login'"
**Solution:** Run `az login` first to authenticate.

### Issue 3: "Resource group not found"
**Solution:** Check your resource group name in Azure Portal → App Service → Overview.

### Issue 4: "App Service not found"
**Solution:** Check your App Service name in Azure Portal.

### Issue 5: Key still not working after setting
**Solution:**
1. Verify the key is complete (100+ characters)
2. Verify setting names use double underscore `__`
3. Restart the App Service
4. Wait 5 minutes after restart
5. Check App Service logs to see what key is being read

---

## Quick Reference

**Setting Names (MUST use double underscore):**
- ✅ `AzureOCR__Endpoint` (correct)
- ✅ `AzureOCR__Key` (correct)
- ❌ `AzureOCR_Endpoint` (wrong - single underscore)
- ❌ `AzureOCR_Key` (wrong - single underscore)

**Endpoint Value:**
- ✅ `https://bhcare-ocr.cognitiveservices.azure.com/` (ends with `/`)
- ❌ `https://bhcare-ocr.cognitiveservices.azure.com` (missing `/`)

**Key Value:**
- ✅ Complete key from Computer Vision resource (100+ characters)
- ❌ Truncated or incomplete key
- ❌ Key with extra spaces

---

## Recommended Method

**For most users, I recommend Method 2 (Azure Portal)** because:
- ✅ No command line needed
- ✅ Visual interface
- ✅ Easy to verify
- ✅ Less chance of typos

Use Method 1 (Azure CLI) if you:
- Prefer command line
- Need to automate
- Want to script the setup

