@echo off
echo Starting BHCARE Azure Deployment...
echo.

REM Check if Azure CLI is installed
az --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Azure CLI is not installed. Please install it first.
    echo Download from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
    pause
    exit /b 1
)

REM Login to Azure
echo Checking Azure login status...
az account show >nul 2>&1
if %errorlevel% neq 0 (
    echo Please login to Azure...
    az login
)

REM Set variables
set RESOURCE_GROUP=BHCARE-RG
set APP_SERVICE_NAME=bhcare-app-%RANDOM%
set APP_SERVICE_PLAN=bhcare-plan
set LOCATION="East US"
set SQL_SERVER_NAME=bhcare-sql-%RANDOM%
set SQL_DATABASE_NAME=bhcareDB
set SQL_ADMIN_USERNAME=bhcareadmin
set SQL_ADMIN_PASSWORD=BHCARE@2024!Secure

echo Deployment Configuration:
echo Resource Group: %RESOURCE_GROUP%
echo App Service: %APP_SERVICE_NAME%
echo SQL Server: %SQL_SERVER_NAME%
echo Location: %LOCATION%
echo.

REM Create resource group
echo Creating resource group...
az group create --name %RESOURCE_GROUP% --location %LOCATION%

REM Create App Service Plan
echo Creating App Service Plan...
az appservice plan create --name %APP_SERVICE_PLAN% --resource-group %RESOURCE_GROUP% --location %LOCATION% --sku B1 --is-linux

REM Create App Service
echo Creating App Service...
az webapp create --name %APP_SERVICE_NAME% --resource-group %RESOURCE_GROUP% --plan %APP_SERVICE_PLAN% --runtime "DOTNET|8.0"

REM Create SQL Server
echo Creating SQL Server...
az sql server create --name %SQL_SERVER_NAME% --resource-group %RESOURCE_GROUP% --location %LOCATION% --admin-user %SQL_ADMIN_USERNAME% --admin-password %SQL_ADMIN_PASSWORD%

REM Create SQL Database
echo Creating SQL Database...
az sql db create --name %SQL_DATABASE_NAME% --resource-group %RESOURCE_GROUP% --server %SQL_SERVER_NAME% --service-objective Basic

REM Configure firewall rule
echo Configuring SQL Server firewall...
az sql server firewall-rule create --resource-group %RESOURCE_GROUP% --server %SQL_SERVER_NAME% --name "AllowAzureServices" --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

REM Configure connection string
echo Configuring connection string...
set CONNECTION_STRING=Server=tcp:%SQL_SERVER_NAME%.database.windows.net,1433;Initial Catalog=%SQL_DATABASE_NAME%;Persist Security Info=False;User ID=%SQL_ADMIN_USERNAME%;Password=%SQL_ADMIN_PASSWORD%;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;

az webapp config connection-string set --name %APP_SERVICE_NAME% --resource-group %RESOURCE_GROUP% --connection-string-type SQLServer --settings "DefaultConnection=%CONNECTION_STRING%"

REM Configure app settings
echo Configuring app settings...
REM NOTE: Replace YOUR_AZURE_OCR_KEY with your actual Azure Computer Vision key
az webapp config appsettings set --name %APP_SERVICE_NAME% --resource-group %RESOURCE_GROUP% --settings "ASPNETCORE_ENVIRONMENT=Production" "EncryptionKey=BHCARE_Production_Encryption_Key_2024_Secure_32Chars" "DataEncryption__Key=BHCARE_Production_DataEncryption_Key_2024_Secure_32Chars" "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" "AzureOCR__Key=YOUR_AZURE_OCR_KEY"

echo.
echo Deployment completed successfully!
echo App Service URL: https://%APP_SERVICE_NAME%.azurewebsites.net
echo SQL Server: %SQL_SERVER_NAME%.database.windows.net
echo Database: %SQL_DATABASE_NAME%
echo.
echo Next steps:
echo 1. Deploy your code using Azure Portal or Visual Studio
echo 2. Run database migrations
echo 3. Configure custom domain (optional)
echo.
pause
