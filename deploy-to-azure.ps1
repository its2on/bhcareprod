# Azure Deployment Script for BHCARE
# Run this script to deploy your application to Azure

Write-Host "Starting Azure deployment for BHCARE..." -ForegroundColor Green

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

# Set variables
$resourceGroupName = "BHCARE-RG"
$appServiceName = "bhcare-app-$(Get-Random)"
$appServicePlanName = "bhcare-plan"
$location = "East US"
$sqlServerName = "bhcare-sql-$(Get-Random)"
$sqlDatabaseName = "bhcareDB"
$sqlAdminUsername = "bhcareadmin"
$sqlAdminPassword = "BHCARE@2024!Secure"

Write-Host "Deployment Configuration:" -ForegroundColor Cyan
Write-Host "Resource Group: $resourceGroupName" -ForegroundColor White
Write-Host "App Service: $appServiceName" -ForegroundColor White
Write-Host "SQL Server: $sqlServerName" -ForegroundColor White
Write-Host "Location: $location" -ForegroundColor White

# Create resource group
Write-Host "Creating resource group..." -ForegroundColor Yellow
az group create --name $resourceGroupName --location "$location"

# Create App Service Plan
Write-Host "Creating App Service Plan..." -ForegroundColor Yellow
az appservice plan create --name $appServicePlanName --resource-group $resourceGroupName --location "$location" --sku B1 --is-linux

# Create App Service
Write-Host "Creating App Service..." -ForegroundColor Yellow
az webapp create --name $appServiceName --resource-group $resourceGroupName --plan $appServicePlanName --runtime "DOTNET|8.0"

# Create SQL Server
Write-Host "Creating SQL Server..." -ForegroundColor Yellow
az sql server create --name $sqlServerName --resource-group $resourceGroupName --location "$location" --admin-user $sqlAdminUsername --admin-password $sqlAdminPassword

# Create SQL Database
Write-Host "Creating SQL Database..." -ForegroundColor Yellow
az sql db create --name $sqlDatabaseName --resource-group $resourceGroupName --server $sqlServerName --service-objective Basic

# Configure firewall rule for Azure services
Write-Host "Configuring SQL Server firewall..." -ForegroundColor Yellow
az sql server firewall-rule create --resource-group $resourceGroupName --server $sqlServerName --name "AllowAzureServices" --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

# Configure App Service settings
Write-Host "Configuring App Service settings..." -ForegroundColor Yellow
$connectionString = "Server=tcp:$sqlServerName.database.windows.net,1433;Initial Catalog=$sqlDatabaseName;Persist Security Info=False;User ID=$sqlAdminUsername;Password=$sqlAdminPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config connection-string set --name $appServiceName --resource-group $resourceGroupName --connection-string-type SQLServer --settings "DefaultConnection=$connectionString"

# Configure App Settings
# NOTE: Replace YOUR_AZURE_OCR_KEY with your actual Azure Computer Vision key
az webapp config appsettings set --name $appServiceName --resource-group $resourceGroupName --settings `
    "ASPNETCORE_ENVIRONMENT=Production" `
    "EncryptionKey=BHCARE_Production_Encryption_Key_2024_Secure_32Chars" `
    "DataEncryption__Key=BHCARE_Production_DataEncryption_Key_2024_Secure_32Chars" `
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" `
    "AzureOCR__Key=YOUR_AZURE_OCR_KEY"

Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "App Service URL: https://$appServiceName.azurewebsites.net" -ForegroundColor Cyan
Write-Host "SQL Server: $sqlServerName.database.windows.net" -ForegroundColor Cyan
Write-Host "Database: $sqlDatabaseName" -ForegroundColor Cyan

Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update your connection string in appsettings.Production.json" -ForegroundColor White
Write-Host "2. Deploy your code using: az webapp deployment source config-zip --name $appServiceName --resource-group $resourceGroupName --src <your-zip-file>" -ForegroundColor White
Write-Host "3. Run database migrations on the deployed app" -ForegroundColor White
