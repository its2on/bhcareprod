# Fix for Native Libraries on Azure App Service Linux

## Problem
Azure App Service on Linux uses a read-only filesystem (except `/home`), so you cannot install system packages like Tesseract and OpenCV at runtime using `apt-get`.

## Solutions

### Option 1: Use Docker Deployment (Recommended)

The Dockerfile already includes all necessary native libraries. You need to:

1. **Create Azure Container Registry (ACR)**:
   ```bash
   az acr create --resource-group <your-resource-group> --name <registry-name> --sku Basic
   ```

2. **Build and push Docker image**:
   ```bash
   az acr build --registry <registry-name> --image barangaybhcare:latest .
   ```

3. **Configure App Service to use Docker**:
   - Azure Portal → App Service → Deployment Center
   - Source: Container Registry
   - Registry: Your ACR
   - Image: `barangaybhcare:latest`
   - Tag: `latest`

### Option 2: Manual Configuration via Azure Portal

Since the startup script won't work, you need to configure Azure App Service to use a custom Docker image or use build extensions.

**Quick Fix (Temporary)**:
1. Go to Azure Portal → Your App Service (`barangaybhcare`)
2. Configuration → General settings
3. Set **Startup Command** to:
   ```
   export LD_LIBRARY_PATH=/usr/lib/x86_64-linux-gnu:/usr/lib:$LD_LIBRARY_PATH && dotnet Barangay.dll
   ```
   This won't install libraries but will help if they're already present.

### Option 3: Use Azure App Service Build Extensions

Azure App Service supports build extensions that can install packages during deployment:

1. Create `.deployment` file in project root:
   ```ini
   [config]
   SCM_DO_BUILD_DURING_DEPLOYMENT=true
   ```

2. Create `deploy.sh` in project root:
   ```bash
   #!/bin/bash
   apt-get update
   apt-get install -y tesseract-ocr tesseract-ocr-eng libleptonica-dev libtesseract-dev libleptonica5 libtesseract4 libopencv-dev libopencv4
   ```

   **Note**: This may not work on standard Azure App Service Linux.

### Option 4: Switch to Azure Container Instances or VM

If Docker deployment is not feasible, consider:
- Azure Container Instances (ACI)
- Azure Virtual Machine with full control
- Azure Kubernetes Service (AKS)

## Recommended: Docker Deployment

The GitHub Actions workflow has been updated to support Docker deployment. You need to:

1. **Create Azure Container Registry** (if not exists):
   ```bash
   az acr create --resource-group <resource-group> --name bhcareprod --sku Basic
   ```

2. **Update the workflow** with your ACR name (already done in `.github/workflows/main_barangaybhcare.yml`)

3. **Push the changes** - the workflow will automatically build and deploy the Docker image

## Verification

After deployment, check logs for:
- ✓ Tesseract native libraries are available
- ✓ OpenCV library found
- No `DllNotFoundException` errors

