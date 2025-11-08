# Environment Variables Configuration

## Required Environment Variables

### Database Configuration
- `DB_SERVER` - Database server name/IP
- `DB_NAME` - Database name
- `DB_USER` - Database username
- `DB_PASSWORD` - Database password

### Encryption Configuration
- `BHCARE_ENCRYPTION_KEY` - 32-character encryption key for data encryption

### Email Configuration
- `SMTP_HOST` - SMTP server hostname
- `SMTP_PORT` - SMTP server port (usually 587)
- `SMTP_USERNAME` - SMTP username
- `SMTP_PASSWORD` - SMTP password
- `FROM_EMAIL` - From email address

### Admin User Configuration
- `ADMIN_EMAIL` - Admin user email
- `ADMIN_PASSWORD` - Admin user password
- `ADMIN_FULLNAME` - Admin user full name

### Azure OCR Configuration
- `AzureOCR__Endpoint` - Azure Computer Vision endpoint URL
- `AzureOCR__Key` - Azure Computer Vision subscription key

## Development Setup

For development, use `appsettings.Development.json` which contains the development values.

## Production Setup

For production, set the environment variables and use `appsettings.Production.json`.

### Windows (PowerShell)
```powershell
$env:BHCARE_ENCRYPTION_KEY="YourSecure32CharacterEncryptionKey123"
$env:DB_SERVER="your-db-server"
$env:DB_NAME="Barangay"
$env:DB_USER="your-db-user"
$env:DB_PASSWORD="your-db-password"
$env:SMTP_HOST="smtp.gmail.com"
$env:SMTP_PORT="587"
$env:SMTP_USERNAME="your-email@gmail.com"
$env:SMTP_PASSWORD="your-app-password"
$env:FROM_EMAIL="your-email@gmail.com"
$env:ADMIN_EMAIL="admin@yourdomain.com"
$env:ADMIN_PASSWORD="SecureAdminPassword123!"
$env:ADMIN_FULLNAME="System Administrator"
$env:AzureOCR__Endpoint="https://bhcare-ocr.cognitiveservices.azure.com/"
$env:AzureOCR__Key="your-azure-computer-vision-key-here"
```

### Linux/macOS (Bash)
```bash
export BHCARE_ENCRYPTION_KEY="YourSecure32CharacterEncryptionKey123"
export DB_SERVER="your-db-server"
export DB_NAME="Barangay"
export DB_USER="your-db-user"
export DB_PASSWORD="your-db-password"
export SMTP_HOST="smtp.gmail.com"
export SMTP_PORT="587"
export SMTP_USERNAME="your-email@gmail.com"
export SMTP_PASSWORD="your-app-password"
export FROM_EMAIL="your-email@gmail.com"
export ADMIN_EMAIL="admin@yourdomain.com"
export ADMIN_PASSWORD="SecureAdminPassword123!"
export ADMIN_FULLNAME="System Administrator"
export AzureOCR__Endpoint="https://bhcare-ocr.cognitiveservices.azure.com/"
export AzureOCR__Key="your-azure-computer-vision-key-here"
```

## Azure App Service Configuration

For Azure App Service deployments, configure these as Application Settings in Azure Portal:

### Via Azure Portal
1. Go to **Azure Portal** → **App Service** → **Configuration** → **Application settings**
2. Add the following settings:

| Setting Name | Value | Description |
|-------------|-------|-------------|
| `AzureOCR__Endpoint` | `https://bhcare-ocr.cognitiveservices.azure.com/` | Azure Computer Vision endpoint |
| `AzureOCR__Key` | `your-actual-key-here` | Azure Computer Vision subscription key |

### Via Azure CLI
```bash
az webapp config appsettings set \
  --name YOUR_APP_SERVICE_NAME \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings \
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" \
    "AzureOCR__Key=your-actual-azure-ocr-key"
```

### Via PowerShell
```powershell
az webapp config appsettings set `
  --name "YOUR_APP_SERVICE_NAME" `
  --resource-group "YOUR_RESOURCE_GROUP" `
  --settings `
    "AzureOCR__Endpoint=https://bhcare-ocr.cognitiveservices.azure.com/" `
    "AzureOCR__Key=your-actual-azure-ocr-key"
```

## GitHub Actions / CI/CD

For GitHub Actions deployments, add these as repository secrets:

1. Go to **GitHub Repository** → **Settings** → **Secrets and variables** → **Actions**
2. Add secret: `AZURE_OCR_KEY` with your Azure Computer Vision key value

The deployment workflow will automatically use this secret.

## Security Notes

1. **Never commit sensitive values to version control**
2. **Use strong, unique encryption keys**
3. **Rotate encryption keys regularly**
4. **Use environment variables for all sensitive configuration**
5. **Keep production configuration separate from development**
6. **Store Azure OCR key in Azure Key Vault for enhanced security (recommended)**
