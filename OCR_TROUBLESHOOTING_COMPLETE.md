# Complete OCR Troubleshooting Guide for Azure App Service Linux

## Problem Summary

Your application has **two OCR issues**:

1. **Azure Vision OCR not configured** - Credentials not being read from configuration
2. **Local OCR (Tesseract) failing** - Missing native libraries (`libleptonica-1.82.0.so`, `OpenCvSharpExtern.so`)

---

## Solution 1: Fix Azure Vision OCR Configuration (IMMEDIATE FIX)

Azure Vision OCR is the **quickest solution** since it doesn't require native libraries.

### Step 1: Configure Azure App Service Environment Variables

1. Go to **Azure Portal** → Your App Service (`barangaybhcare`)
2. Navigate to **Configuration** → **Application settings**
3. Click **+ New application setting**
4. Add these **two settings**:

   **Setting 1:**
   - **Name:** `AzureOCR__Endpoint` (double underscore)
   - **Value:** `https://bhcare-ocr.cognitiveservices.azure.com/`
   - Click **OK**

   **Setting 2:**
   - **Name:** `AzureOCR__Key` (double underscore)
   - **Value:** `YOUR_AZURE_OCR_KEY_HERE` (Get this from Azure Portal → Computer Vision resource → Keys and Endpoint)
   - Click **OK**

5. Click **Save** at the top
6. **Restart** the App Service (Configuration → Overview → Restart)

### Step 2: Verify Configuration

After restart, check the logs. You should see:
- ✅ No more "Azure Vision OCR credentials not configured" warnings
- ✅ "Azure Vision OCR extracted text length: XXX" when processing images

### Why This Works

ASP.NET Core reads environment variables with double underscores (`__`) as nested configuration. So `AzureOCR__Key` maps to `AzureOCR:Key` in your code.

---

## Solution 2: Fix Local OCR (Tesseract) - Docker Deployment (LONG-TERM FIX)

Azure App Service Linux has a **read-only filesystem** (except `/home`), so you **cannot install packages at runtime**. You must use **Docker deployment**.

### Option A: Use Docker Deployment (Recommended)

Your `Dockerfile` already includes all necessary libraries. Follow these steps:

#### Step 1: Build and Push Docker Image

**Option 1: Using Azure Container Registry (ACR)**

```bash
# 1. Create ACR (if not exists)
az acr create --resource-group <your-resource-group> --name bhcareprod --sku Basic

# 2. Build and push image
az acr build --registry bhcareprod --image barangaybhcare:latest .
```

**Option 2: Using Docker Hub (Free)**

```bash
# 1. Login to Docker Hub
docker login

# 2. Build image
docker build -t <your-dockerhub-username>/bhcare-app:latest .

# 3. Push image
docker push <your-dockerhub-username>/bhcare-app:latest
```

#### Step 2: Configure Azure App Service for Docker

1. Go to **Azure Portal** → App Service (`barangaybhcare`)
2. Navigate to **Deployment Center** → **Settings**
3. Change **Source** to: **Container Registry** (or **Docker Hub** if using Docker Hub)
4. Configure:
   - **Registry:** Your ACR or Docker Hub
   - **Image and tag:** `barangaybhcare:latest` (or your Docker Hub image)
   - **Continuous Deployment:** Enable (optional)
5. Click **Save**
6. **Restart** the App Service

#### Step 3: Verify Docker Deployment

After deployment, check logs for:
- ✅ "✓ Tesseract native libraries are available"
- ✅ No more `DllNotFoundException` errors
- ✅ "✓ OpenCV library found"

### Option B: Use Azure Vision OCR Only (No Local OCR)

If Docker deployment is not feasible, you can disable local OCR and use only Azure Vision OCR:

1. Configure Azure Vision OCR (Solution 1 above)
2. The app will automatically fall back to Azure Vision OCR when local OCR fails

---

## Solution 3: Alternative - Use Azure Container Instances (ACI)

If Docker deployment to App Service is complex, use Azure Container Instances:

```bash
# Create container instance
az container create \
  --resource-group <your-resource-group> \
  --name bhcare-container \
  --image <your-registry>/bhcare-app:latest \
  --dns-name-label bhcare-app \
  --ports 80 \
  --environment-variables \
    AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/ \
    AzureOCR__Key=YOUR_AZURE_OCR_KEY_HERE
```

---

## Verification Checklist

After applying fixes, verify:

### Azure Vision OCR:
- [ ] No "Azure Vision OCR credentials not configured" warnings in logs
- [ ] OCR processing completes successfully
- [ ] Text is extracted from ID images
- [ ] Form fields are auto-filled

### Local OCR (if using Docker):
- [ ] "✓ Tesseract native libraries are available" in logs
- [ ] No `DllNotFoundException` errors
- [ ] "✓ OpenCV library found" in logs
- [ ] Local OCR processes images successfully

---

## Troubleshooting Common Issues

### Issue: "Azure Vision OCR credentials not configured" persists

**Solution:**
1. Verify environment variables use **double underscores** (`AzureOCR__Key`, not `AzureOCR_Key`)
2. Check variable names are **exactly** as shown (case-sensitive)
3. **Restart** App Service after adding variables
4. Check logs to see which configuration source is being used

### Issue: Docker image build fails

**Solution:**
1. Test Dockerfile locally: `docker build -t test-image .`
2. Check Dockerfile syntax
3. Ensure all dependencies are listed in Dockerfile

### Issue: Docker deployment fails

**Solution:**
1. Verify container registry credentials
2. Check App Service has permission to pull from registry
3. Verify image name and tag are correct
4. Check App Service logs for specific errors

### Issue: Native libraries still missing after Docker deployment

**Solution:**
1. Verify Dockerfile includes all `apt-get install` commands
2. Check Dockerfile creates symlinks for Leptonica
3. Rebuild Docker image
4. Verify image includes libraries: `docker run --rm <image> ls -la /usr/lib/x86_64-linux-gnu/ | grep lept`

---

## Quick Fix Summary

**For immediate OCR functionality:**
1. ✅ Configure Azure Vision OCR environment variables (5 minutes)
2. ✅ Restart App Service
3. ✅ Test OCR functionality

**For complete solution (Local + Azure OCR):**
1. ✅ Build Docker image with native libraries
2. ✅ Configure App Service for Docker deployment
3. ✅ Deploy and verify both OCR services work

---

## Next Steps

1. **Apply Solution 1** (Azure Vision OCR) - This will get OCR working immediately
2. **Plan Solution 2** (Docker) - For long-term stability and local OCR support
3. **Test thoroughly** - Upload various ID images to verify extraction accuracy

---

## Support

If issues persist:
1. Check Azure App Service logs (Log stream)
2. Verify all environment variables are set correctly
3. Ensure App Service is restarted after configuration changes
4. Review Docker build logs if using container deployment

