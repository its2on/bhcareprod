# Quick Fix Script for Azure OCR "Unauthorized" Error
# This script sets the correct Azure OCR settings in Azure App Service

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Azure OCR Settings Fix" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Azure CLI is installed
if (!(Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: Azure CLI is not installed." -ForegroundColor Red
    Write-Host "Please install Azure CLI first:" -ForegroundColor Yellow
    Write-Host "https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Or use the Azure Portal method in FIX_OCR_UNAUTHORIZED_QUICK.md" -ForegroundColor Yellow
    exit 1
}

# Check if logged in
Write-Host "Checking Azure login status..." -ForegroundColor Yellow
$account = az account show 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Please login to Azure..." -ForegroundColor Yellow
    az login
}

# Configuration
$resourceGroupName = "BHcare"
$appServiceName = "bhcare-webapp"
$ocrEndpoint = "https://bhcare-ocr.cognitiveservices.azure.com/"

Write-Host ""
Write-Host "Configuration:" -ForegroundColor Cyan
Write-Host "  Resource Group: $resourceGroupName" -ForegroundColor White
Write-Host "  App Service: $appServiceName" -ForegroundColor White
Write-Host "  OCR Endpoint: $ocrEndpoint" -ForegroundColor White
Write-Host ""

# Verify App Service exists
Write-Host "Verifying App Service exists..." -ForegroundColor Yellow
$appService = az webapp show --name $appServiceName --resource-group $resourceGroupName 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: App Service '$appServiceName' not found!" -ForegroundColor Red
    Write-Host "Please check the App Service name and resource group." -ForegroundColor Yellow
    exit 1
}
Write-Host "App Service found!" -ForegroundColor Green
Write-Host ""

# Get Azure OCR Key
Write-Host "Please enter your Azure Computer Vision Key:" -ForegroundColor Yellow
Write-Host "(Get it from: Azure Portal → Computer Vision → Keys and Endpoint → KEY 1)" -ForegroundColor Gray
$ocrKey = Read-Host "Enter Azure OCR Key"

if ([string]::IsNullOrWhiteSpace($ocrKey)) {
    Write-Host "ERROR: OCR Key cannot be empty!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Updating Azure App Service settings..." -ForegroundColor Yellow

# Set the settings (using double underscore __ for nested config)
az webapp config appsettings set `
    --name $appServiceName `
    --resource-group $resourceGroupName `
    --settings `
        "AzureOCR__Endpoint=$ocrEndpoint" `
        "AzureOCR__Key=$ocrKey"

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Settings updated successfully!" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Failed to update settings" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Restarting App Service..." -ForegroundColor Yellow
az webapp restart --name $appServiceName --resource-group $resourceGroupName

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] App Service restarted!" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Failed to restart App Service" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Fix Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Settings configured:" -ForegroundColor Cyan
Write-Host "  AzureOCR__Endpoint = $ocrEndpoint" -ForegroundColor White
Write-Host "  AzureOCR__Key = $($ocrKey.Substring(0, [Math]::Min(20, $ocrKey.Length)))..." -ForegroundColor White
Write-Host ""
Write-Host "Please wait 3-5 minutes for the restart to complete," -ForegroundColor Yellow
Write-Host "then test OCR on your website." -ForegroundColor Yellow
Write-Host ""
Write-Host "App Service URL: https://$appServiceName.azurewebsites.net" -ForegroundColor Cyan
Write-Host ""

