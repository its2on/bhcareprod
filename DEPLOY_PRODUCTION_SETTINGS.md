# Deploy Production Settings to Azure

This guide shows how to deploy settings from `appsettings.Production.json` to Azure App Service.

## Option 1: Using Azure Portal (Recommended - No CLI Required)

### Step 1: Configure Connection String

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **App Services** → **bhcare-webapp**
3. Go to **Configuration** → **Connection strings**
4. Click **+ New connection string**
5. Set:
   - **Name**: `DefaultConnection`
   - **Value**: `Server=tcp:bhcareserverprod.database.windows.net,1433;Initial Catalog=bhcareDB;Persist Security Info=False;User ID=bhcareprod;Password=prodcarebh.123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`
   - **Type**: `SQLServer`
6. Click **OK** then **Save**

### Step 2: Configure App Settings

1. In the same App Service, go to **Configuration** → **Application settings**
2. Click **+ New application setting** for each setting below:

#### Required Settings:

```
ASPNETCORE_ENVIRONMENT = Production
EncryptionKey = BHCARE_Production_Encryption_Key_2024_Secure_32Chars
DataEncryption__Key = BHCARE_Production_DataEncryption_Key_2024_Secure_32Chars
EmailSettings__SmtpHost = smtp.gmail.com
EmailSettings__SmtpPort = 587
EmailSettings__SmtpUsername = barangayexample549@gmail.com
EmailSettings__SmtpPassword = [YOUR_EMAIL_PASSWORD]
EmailSettings__FromEmail = barangayexample549@gmail.com
EmailSettings__EnableSsl = true
AzureOCR__Endpoint = https://bhcare-ocr.cognitiveservices.azure.com/
AzureOCR__Key = [YOUR_AZURE_OCR_KEY]
```

**Important Notes:**
- Use **double underscore** (`__`) for nested settings (e.g., `DataEncryption__Key`, `EmailSettings__SmtpHost`)
- Replace `[YOUR_EMAIL_PASSWORD]` with the actual Gmail app password
- Replace `[YOUR_AZURE_OCR_KEY]` with your Azure Computer Vision key

3. Click **Save** at the top
4. Click **Continue** to restart the app

### Step 3: Verify Deployment

1. Go to **Overview** in your App Service
2. Click the **URL** to open your application
3. Verify the application loads correctly

---

## Option 2: Using Azure CLI (If Installed)

1. Install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
2. Run the deployment script:
   ```powershell
   .\deploy-production-settings.ps1
   ```
3. Follow the prompts to enter sensitive values

---

## Option 3: Using GitHub Actions (Automatic)

The GitHub Actions workflow (`.github/workflows/azure-deploy.yml`) automatically configures these settings when you push to the `main` branch.

**Required GitHub Secrets:**
- `AZURE_CREDENTIALS` - Azure service principal credentials
- `AZURE_SQL_CONNECTION_STRING` - Database connection string
- `EMAIL_PASSWORD` - Gmail app password
- `AZURE_OCR_KEY` - Azure Computer Vision key

To add secrets:
1. Go to your GitHub repository
2. **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each secret with the name and value

---

## Current Production Settings

From `appsettings.Production.json`:

- **Database**: `bhcareserverprod.database.windows.net`
- **Database Name**: `bhcareDB`
- **Email**: `barangayexample549@gmail.com`
- **Azure OCR Endpoint**: `https://bhcare-ocr.cognitiveservices.azure.com/`

---

## Security Best Practices

⚠️ **Important**: Never commit sensitive values to Git!

- Store passwords and keys in:
  - Azure Key Vault (recommended for production)
  - GitHub Secrets (for CI/CD)
  - Environment variables (for local development)

- The `appsettings.Production.json` file uses placeholders:
  - `{AZURE_EMAIL_PASSWORD}` - Should be set in Azure App Settings
  - `{AZURE_ADMIN_PASSWORD}` - Should be set in Azure App Settings
  - `YOUR_AZURE_COMPUTER_VISION_KEY` - Should be set in Azure App Settings

---

## Troubleshooting

### Settings Not Applied
- Ensure you clicked **Save** after adding settings
- Restart the App Service after configuration changes
- Check that setting names use double underscore (`__`) for nested settings

### Connection Issues
- Verify SQL Server firewall allows Azure services
- Check connection string format is correct
- Ensure database credentials are correct

### Email Not Working
- Verify Gmail app password is correct (not regular password)
- Check `EmailSettings__EnableSsl` is set to `true`
- Verify SMTP port is `587`

---

## Quick Reference

**App Service Name**: `bhcare-webapp`  
**Resource Group**: `BHcare`  
**App Service URL**: `https://bhcare-webapp.azurewebsites.net`

