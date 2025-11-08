# Step-by-Step Guide: Fix Azure OCR "Unauthorized" Error

This guide will walk you through fixing the "OCR service error: Unauthorized" error step by step.

---

## 📋 Prerequisites

- Access to Azure Portal (https://portal.azure.com)
- Your Azure Computer Vision resource name (or create one)
- Access to your Azure App Service

---

## Step 1: Get Your Azure Computer Vision Key

### 1.1 Login to Azure Portal

1. Go to **https://portal.azure.com**
2. Sign in with your Azure account

### 1.2 Find Your Computer Vision Resource

**Option A: If you already have a Computer Vision resource:**

1. In the search bar at the top, type: **"Computer Vision"** or **"bhcare-ocr"**
2. Click on your Computer Vision resource from the results
3. Skip to **Step 1.3**

**Option B: If you don't have a Computer Vision resource yet:**

1. Click **"Create a resource"** (top left, green button)
2. In the search box, type: **"Computer Vision"**
3. Click on **"Computer Vision"** from the results
4. Click **"Create"** button
5. Fill in the form:
   - **Subscription:** Select your subscription
   - **Resource Group:** Select existing or create new (e.g., "BHcare")
   - **Region:** Select **Southeast Asia** (or nearest to you)
   - **Name:** Enter **"bhcare-ocr"** (or any name you prefer)
   - **Pricing Tier:** Select **F0 (Free)** or **S1 (Standard)**
6. Click **"Review + Create"**
7. Click **"Create"**
8. Wait for deployment to complete (about 1-2 minutes)
9. Click **"Go to resource"**

### 1.3 Get Your Key

1. In your Computer Vision resource page, look at the **left menu**
2. Under **"Resource Management"** section, click **"Keys and Endpoint"**
3. You will see:
   - **Key 1:** `[Your key here]` ← **Copy this one**
   - **Key 2:** `[Backup key]`
   - **Endpoint:** `https://bhcare-ocr.cognitiveservices.azure.com/`
4. Click the **copy icon** (📋) next to **Key 1**
5. **Save this key somewhere safe** (you'll need it in the next step)

---

## Step 2: Configure Azure App Service

### 2.1 Navigate to Your App Service

1. In Azure Portal, go back to the home page (click **"Home"** in top left)
2. In the search bar, type your **App Service name** (e.g., "bhcare-webapp")
3. Click on your App Service from the results

### 2.2 Open Configuration

1. In your App Service page, look at the **left menu**
2. Under **"Settings"** section, click **"Configuration"**
3. You should see the **"Application settings"** tab (should be selected by default)

### 2.3 Add Azure OCR Endpoint

1. Click **"+ New application setting"** button (top of the settings list)
2. Fill in:
   - **Name:** `AzureOCR__Endpoint`
   - **Value:** `https://bhcare-ocr.cognitiveservices.azure.com/`
   - ⚠️ **Important:** Use double underscore `__` (not single underscore)
3. Click **"OK"**

### 2.4 Add Azure OCR Key

1. Click **"+ New application setting"** button again
2. Fill in:
   - **Name:** `AzureOCR__Key`
   - **Value:** Paste the **Key 1** you copied from Step 1.3
   - ⚠️ **Important:** Use double underscore `__` (not single underscore)
3. Click **"OK"**

### 2.5 Save and Restart

1. Click **"Save"** button at the top of the page
2. A popup will appear asking: **"Save changes to application settings?"**
3. Click **"Continue"** (this will restart your app)
4. Wait for the save to complete (about 30 seconds)
5. You should see a green notification: **"Successfully saved application settings"**

---

## Step 3: Verify Configuration

### 3.1 Check Settings Are Saved

1. Still in **Configuration** → **Application settings**
2. Scroll down and verify you can see:
   - `AzureOCR__Endpoint` = `https://bhcare-ocr.cognitiveservices.azure.com/`
   - `AzureOCR__Key` = `[Your key - should be visible]`
3. If both are there, ✅ **Configuration is correct!**

### 3.2 Test the OCR Functionality

1. Go to your deployed website (e.g., `https://your-app-name.azurewebsites.net`)
2. Navigate to the **Sign Up** page
3. Scroll down to **"Quick Fill with ID Scanner"** section
4. Click **"Choose File"** and select an ID image (JPG or PNG)
5. Click **"Process Selected Image"** button
6. Wait for processing...

**Expected Result:**
- ✅ If it works: You should see the form fields automatically filled with data from the ID
- ❌ If it still shows "Unauthorized": Check Step 4 (Troubleshooting)

---

## Step 4: Troubleshooting (If Still Not Working)

### Issue 1: Settings Not Showing

**Problem:** Can't see `AzureOCR__Endpoint` or `AzureOCR__Key` in the list

**Solution:**
1. Make sure you clicked **"Save"** after adding the settings
2. Refresh the page (F5)
3. Check if the settings are there (they should be at the bottom of the list)

### Issue 2: Still Getting "Unauthorized" Error

**Problem:** Settings are there but still getting error

**Checklist:**
- [ ] Is `AzureOCR__Endpoint` spelled correctly? (Must be `AzureOCR__Endpoint` with double underscore)
- [ ] Is `AzureOCR__Key` spelled correctly? (Must be `AzureOCR__Key` with double underscore)
- [ ] Did you copy the **entire key**? (Should be about 32 characters)
- [ ] Did you restart the app? (Click "Save" should have restarted it)
- [ ] Is the endpoint URL correct? (Should end with `/`)

**Solution:**
1. Delete both settings (`AzureOCR__Endpoint` and `AzureOCR__Key`)
2. Click **"Save"**
3. Add them again following **Step 2.3** and **Step 2.4**
4. Click **"Save"** again
5. Wait 1-2 minutes for the app to restart
6. Try again

### Issue 3: Wrong Endpoint URL

**Problem:** Your Computer Vision resource has a different endpoint

**Solution:**
1. Go back to your Computer Vision resource
2. Go to **"Keys and Endpoint"**
3. Copy the **Endpoint** URL (should be like `https://your-resource-name.cognitiveservices.azure.com/`)
4. Go back to App Service → Configuration
5. Edit `AzureOCR__Endpoint` setting
6. Replace the value with your actual endpoint URL
7. Click **"Save"**

### Issue 4: Key is Wrong or Expired

**Problem:** The key might be incorrect or expired

**Solution:**
1. Go back to Computer Vision resource → **"Keys and Endpoint"**
2. Try using **Key 2** instead of Key 1
3. Update `AzureOCR__Key` in App Service Configuration
4. Click **"Save"**

---

## Step 5: Alternative Method (Using Azure CLI)

If you prefer using command line, you can set the values using Azure CLI:

### 5.1 Install Azure CLI (if not installed)

- Download from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli

### 5.2 Login to Azure

```bash
az login
```

### 5.3 Set the Configuration

```bash
az webapp config appsettings set \
  --name YOUR_APP_SERVICE_NAME \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings \
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" \
    "AzureOCR__Key=YOUR_ACTUAL_KEY_HERE"
```

**Replace:**
- `YOUR_APP_SERVICE_NAME` with your actual App Service name
- `YOUR_RESOURCE_GROUP` with your actual resource group name
- `YOUR_ACTUAL_KEY_HERE` with the Key 1 you copied

### 5.4 Restart the App

```bash
az webapp restart \
  --name YOUR_APP_SERVICE_NAME \
  --resource-group YOUR_RESOURCE_GROUP
```

---

## ✅ Success Checklist

After completing all steps, verify:

- [ ] Azure Computer Vision resource exists
- [ ] Key 1 is copied and saved
- [ ] `AzureOCR__Endpoint` is added in App Service Configuration
- [ ] `AzureOCR__Key` is added in App Service Configuration
- [ ] Settings are saved (green notification)
- [ ] App is restarted
- [ ] OCR functionality works without "Unauthorized" error

---

## 📞 Need Help?

If you're still having issues:

1. **Check the App Service Logs:**
   - App Service → **Log stream** (left menu)
   - Look for any error messages

2. **Verify the Key:**
   - Make sure the key is correct (32 characters, no spaces)
   - Try using Key 2 instead of Key 1

3. **Check Resource Status:**
   - Make sure your Computer Vision resource is **Active** (not deleted or suspended)

---

## 🎉 Done!

Once you see the OCR working without errors, you're all set! The "Unauthorized" error should be completely resolved.

