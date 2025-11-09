# PowerShell script to configure Azure Vision OCR in Azure App Service
# Run this script to automatically set the environment variables

Write-Host "Configuring Azure Vision OCR for App Service..." -ForegroundColor Green

# Check if Azure CLI is installed
if (!(Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "Azure CLI is not installed. Please install it first." -ForegroundColor Red
    Write-Host "Download from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Yellow
    exit 1
}

# Login to Azure (if not already logged in)
Write-Host "Checking Azure login status..." -ForegroundColor Yellow
$loginStatus = az account show 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Please login to Azure..." -ForegroundColor Yellow
    az login
}

# Configuration
$appServiceName = "barangaybhcare"
$resourceGroup = ""  # Will be auto-detected if empty
$endpoint = "https://bhcare-ocr.cognitiveservices.azure.com/"
# Get key from user input or environment variable for security
$key = $env:AZURE_OCR_KEY
if ([string]::IsNullOrEmpty($key)) {
    Write-Host "Azure OCR Key not found in environment variable AZURE_OCR_KEY." -ForegroundColor Yellow
    $key = Read-Host "Enter your Azure OCR Key (from Azure Portal → Computer Vision → Keys and Endpoint)" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($key)
    $key = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

# Auto-detect resource group if not provided
if ([string]::IsNullOrEmpty($resourceGroup)) {
    Write-Host "Auto-detecting resource group..." -ForegroundColor Yellow
    $resourceGroup = az webapp show --name $appServiceName --query resourceGroup -o tsv 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Could not auto-detect resource group. Please provide it manually." -ForegroundColor Red
        $resourceGroup = Read-Host "Enter resource group name"
    } else {
        Write-Host "Found resource group: $resourceGroup" -ForegroundColor Green
    }
}

Write-Host "`nConfiguration:" -ForegroundColor Cyan
Write-Host "  App Service: $appServiceName" -ForegroundColor White
Write-Host "  Resource Group: $resourceGroup" -ForegroundColor White
Write-Host "  Endpoint: $endpoint" -ForegroundColor White
Write-Host "  Key: $($key.Substring(0, 10))...$($key.Substring($key.Length - 10))" -ForegroundColor White

# Set environment variables
Write-Host "`nSetting Azure Vision OCR environment variables..." -ForegroundColor Yellow

az webapp config appsettings set `
    --name $appServiceName `
    --resource-group $resourceGroup `
    --settings `
        AzureOCR__Endpoint=$endpoint `
        AzureOCR__Key=$key

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Environment variables configured successfully!" -ForegroundColor Green
    Write-Host "`nRestarting App Service..." -ForegroundColor Yellow
    az webapp restart --name $appServiceName --resource-group $resourceGroup
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ App Service restarted!" -ForegroundColor Green
        Write-Host "`nAzure Vision OCR should now be configured." -ForegroundColor Green
        Write-Host "Check the logs to verify: 'Azure Vision OCR configured - Endpoint: ...'" -ForegroundColor Cyan
    } else {
        Write-Host "⚠ Configuration saved but restart failed. Please restart manually." -ForegroundColor Yellow
    }
} else {
    Write-Host "✗ Failed to configure environment variables." -ForegroundColor Red
    exit 1
}

