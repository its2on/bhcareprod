# Deploy Production Settings to Azure App Service
# This script configures Azure App Service with settings from appsettings.Production.json

Write-Host "Deploying Production Settings to Azure..." -ForegroundColor Green

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

# Set variables from appsettings.Production.json
$resourceGroupName = "BHcare"
$appServiceName = "bhcare-webapp"

Write-Host "Configuration:" -ForegroundColor Cyan
Write-Host "Resource Group: $resourceGroupName" -ForegroundColor White
Write-Host "App Service: $appServiceName" -ForegroundColor White
Write-Host ""

# Verify App Service exists
Write-Host "Verifying App Service exists..." -ForegroundColor Yellow
$appService = az webapp show --name $appServiceName --resource-group $resourceGroupName 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "App Service '$appServiceName' not found in resource group '$resourceGroupName'." -ForegroundColor Red
    Write-Host "Please create the App Service first or update the script with the correct names." -ForegroundColor Yellow
    exit 1
}

Write-Host "App Service found!" -ForegroundColor Green

# Configure Connection String
Write-Host "Configuring database connection string..." -ForegroundColor Yellow
$connectionString = "Server=tcp:bhcareserverprod.database.windows.net,1433;Initial Catalog=bhcareDB;Persist Security Info=False;User ID=bhcareprod;Password=prodcarebh.123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config connection-string set `
    --name $appServiceName `
    --resource-group $resourceGroupName `
    --connection-string-type SQLServer `
    --settings "DefaultConnection=$connectionString"

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Connection string configured successfully!" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Failed to configure connection string" -ForegroundColor Red
}

# Configure App Settings
Write-Host "Configuring app settings..." -ForegroundColor Yellow

# Read Azure OCR Key from environment or prompt
$azureOcrKey = $env:AZURE_OCR_KEY
if ([string]::IsNullOrEmpty($azureOcrKey)) {
    Write-Host "Azure OCR Key not found in environment variables." -ForegroundColor Yellow
    Write-Host "Please set AZURE_OCR_KEY environment variable or enter it manually." -ForegroundColor Yellow
    $azureOcrKey = Read-Host "Enter Azure Computer Vision Key"
}

# Read Email Password from environment or prompt
$emailPassword = $env:AZURE_EMAIL_PASSWORD
if ([string]::IsNullOrEmpty($emailPassword)) {
    Write-Host "Email password not found in environment variables." -ForegroundColor Yellow
    $emailPassword = Read-Host "Enter Email Password (or press Enter to skip)"
}

# Read Admin Password from environment or prompt
$adminPassword = $env:AZURE_ADMIN_PASSWORD
if ([string]::IsNullOrEmpty($adminPassword)) {
    Write-Host "Admin password not found in environment variables." -ForegroundColor Yellow
    $adminPassword = Read-Host "Enter Admin Password (or press Enter to skip)"
}

# Build settings array
$settings = @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "EncryptionKey=BHCARE_Production_Encryption_Key_2024_Secure_32Chars",
    "DataEncryption__Key=BHCARE_Production_DataEncryption_Key_2024_Secure_32Chars",
    "EmailSettings__SmtpHost=smtp.gmail.com",
    "EmailSettings__SmtpPort=587",
    "EmailSettings__SmtpUsername=barangayexample549@gmail.com",
    "EmailSettings__FromEmail=barangayexample549@gmail.com",
    "EmailSettings__EnableSsl=true",
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/"
)

# Add optional settings if provided
if (![string]::IsNullOrEmpty($emailPassword)) {
    $settings += "EmailSettings__SmtpPassword=$emailPassword"
}

if (![string]::IsNullOrEmpty($azureOcrKey)) {
    $settings += "AzureOCR__Key=$azureOcrKey"
}

if (![string]::IsNullOrEmpty($adminPassword)) {
    $settings += "AdminUser__Password=$adminPassword"
}

# Configure app settings
$settingsString = $settings -join " "
az webapp config appsettings set `
    --name $appServiceName `
    --resource-group $resourceGroupName `
    --settings $settingsString

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] App settings configured successfully!" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Failed to configure app settings" -ForegroundColor Red
}

# Restart App Service
Write-Host "Restarting App Service..." -ForegroundColor Yellow
az webapp restart --name $appServiceName --resource-group $resourceGroupName

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] App Service restarted successfully!" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Failed to restart App Service" -ForegroundColor Red
}

Write-Host ""
Write-Host "Deployment completed!" -ForegroundColor Green
Write-Host "App Service URL: https://$appServiceName.azurewebsites.net" -ForegroundColor Cyan
Write-Host ""
Write-Host "Note: Store sensitive values in Azure Key Vault or GitHub Secrets." -ForegroundColor Yellow

