# How to Deploy Code to GitHub and Azure

## Quick Steps to Deploy

### Step 1: Commit Your Changes

Open your terminal/PowerShell in your project folder and run:

```bash
# Check what files have changed
git status

# Add all changed files
git add .

# Or add specific files
git add Pages/Account/SignUp.cshtml.cs
git add appsettings.Production.json

# Commit with a message
git commit -m "Add Azure OCR logging and fix configuration"
```

### Step 2: Push to GitHub

```bash
# Push to main branch (this will trigger GitHub Actions)
git push origin main
```

### Step 3: Monitor GitHub Actions

1. Go to your **GitHub repository** in a web browser
2. Click on the **"Actions"** tab
3. You should see a workflow running: **"Build and deploy ASP.Net Core app to Azure Web App - barangaybhcare"**
4. Click on it to see the progress
5. Wait for it to complete (usually 5-10 minutes)

### Step 4: Verify Deployment

After the workflow completes:
1. Go to **Azure Portal** → Your **App Service** (`barangaybhcare`)
2. Go to **Log stream** (left menu)
3. Try uploading an ID image again
4. **Look for the new log messages:**
   - `Azure OCR Endpoint: https://...`
   - `Azure OCR Key length: XXX characters`
   - `Azure OCR Key (first 10 chars): ...`
   - `Azure OCR Key (last 10 chars): ...`

---

## Alternative: Manual Trigger (If Needed)

If you want to trigger deployment manually without pushing:

1. Go to **GitHub repository** → **Actions** tab
2. Click on **"Build and deploy ASP.Net Core app to Azure Web App - barangaybhcare"**
3. Click **"Run workflow"** button (top right)
4. Select **"main"** branch
5. Click **"Run workflow"**

---

## What Gets Deployed

Your GitHub Actions workflow (`.github/workflows/main_barangaybhcare.yml`) will:
1. ✅ Checkout your code
2. ✅ Build the .NET application
3. ✅ Publish the application
4. ✅ Deploy to Azure App Service (`barangaybhcare`)

---

## Important Notes

- **The code changes will be deployed**, but **the Azure App Service settings still need to be fixed separately**
- After deployment, make sure you've fixed the App Service settings:
  - `AzureOCR__Endpoint` (double underscore)
  - `AzureOCR__Key` (double underscore)
- **Restart the App Service** after updating settings

---

## Troubleshooting

### If GitHub Actions Fails:

1. Check the **Actions** tab for error messages
2. Common issues:
   - Build errors → Check your code
   - Authentication errors → Check Azure secrets in GitHub
   - Deployment errors → Check App Service name and resource group

### If Deployment Succeeds But Still Getting 401:

1. **Verify App Service settings are correct:**
   - Go to Azure Portal → App Service → Configuration
   - Check `AzureOCR__Endpoint` and `AzureOCR__Key` (double underscore)
2. **Restart App Service** after fixing settings
3. **Check logs** to see what key is being read

---

## Quick Command Summary

```bash
# 1. Check status
git status

# 2. Add changes
git add .

# 3. Commit
git commit -m "Add Azure OCR logging"

# 4. Push (triggers deployment)
git push origin main

# 5. Monitor at: https://github.com/YOUR_USERNAME/YOUR_REPO/actions
```

