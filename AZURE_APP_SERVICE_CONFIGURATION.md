# Azure App Service Configuration Guide

## 📋 Required Application Settings for Azure App Service

Go to **Azure Portal** → Your **App Service** → **Configuration** → **Application settings** → Click **+ New application setting**

---

## 🔐 **1. Database Connection**

**Name:** `ConnectionStrings__DefaultConnection`  
**Value:** `Server=tcp:bhcareserverprod.database.windows.net,1433;Initial Catalog=bhcareDB;Persist Security Info=False;User ID=bhcareprod;Password=YOUR_DB_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`

---

## 📧 **2. Email Settings (Gmail SMTP)**

**Name:** `EmailSettings__SmtpHost`  
**Value:** `smtp.gmail.com`

**Name:** `EmailSettings__SmtpPort`  
**Value:** `587`

**Name:** `EmailSettings__SmtpUsername`  
**Value:** `barangayexample549@gmail.com` (or your Gmail)

**Name:** `EmailSettings__SmtpPassword`  
**Value:** `YOUR_GMAIL_APP_PASSWORD` (your Gmail App Password)

**Name:** `EmailSettings__FromEmail`  
**Value:** `barangayexample549@gmail.com` (or your Gmail)

**Name:** `EmailSettings__EnableSsl`  
**Value:** `true`

---

## 📱 **3. SMS Settings (iprogtech)**

**Name:** `SmsSettings__ApiToken`  
**Value:** `YOUR_IPROGTECH_API_TOKEN` (your iprogtech API token)

**Name:** `SmsSettings__ApiUrl`  
**Value:** `https://sms.iprogtech.com/api/v1/sms_messages`

---

## 🔍 **4. Azure OCR (Computer Vision)**

**Name:** `AzureOCR__Endpoint`  
**Value:** `https://bhcare-ocr.cognitiveservices.azure.com/`

**Name:** `AzureOCR__Key`  
**Value:** `YOUR_AZURE_COMPUTER_VISION_KEY` (your Azure Computer Vision key)

---

## 🤖 **5. Gemini AI Vision API (Optional - for enhanced OCR)**

**Name:** `GeminiAPI__Key`  
**Value:** `YOUR_GEMINI_API_KEY` (your Google Gemini API key)

---

## 🔒 **6. Encryption Keys**

**Name:** `EncryptionKey`  
**Value:** `YourStrongEncryptionKeyHere1234567890123456` (32+ characters)

**Name:** `DataEncryption__Key`  
**Value:** `BHCARE_DataEncryption_Key_2024_Secure_32Chars` (32 characters)

---

## 👤 **7. Admin User (Initial Setup)**

**Name:** `AdminUser__Email`  
**Value:** `healthcenterbaesa@gmail.com`

**Name:** `AdminUser__Password`  
**Value:** `Admin123!` (change this!)

**Name:** `AdminUser__FullName`  
**Value:** `System Administrator`

**Name:** `AdminUser__IsSuperAdmin`  
**Value:** `true`

---

## ⏰ **8. Time Zone Settings**

**Name:** `ReminderSettings__TimeZoneId`  
**Value:** `Singapore Standard Time`

**Name:** `TimeZoneSettings__DefaultTimeZone`  
**Value:** `Singapore Standard Time`

**Name:** `TimeZoneSettings__Culture`  
**Value:** `en-PH`

**Name:** `TimeZoneSettings__DateFormat`  
**Value:** `MMM dd, yyyy`

**Name:** `TimeZoneSettings__TimeFormat`  
**Value:** `h:mm tt`

---

## 📝 **Quick Copy-Paste Format (Azure Portal)**

When adding settings in Azure Portal, use this format:

```
ConnectionStrings__DefaultConnection = Server=tcp:bhcareserverprod.database.windows.net,1433;Initial Catalog=bhcareDB;Persist Security Info=False;User ID=bhcareprod;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;

EmailSettings__SmtpHost = smtp.gmail.com
EmailSettings__SmtpPort = 587
EmailSettings__SmtpUsername = barangayexample549@gmail.com
EmailSettings__SmtpPassword = YOUR_GMAIL_APP_PASSWORD
EmailSettings__FromEmail = barangayexample549@gmail.com
EmailSettings__EnableSsl = true

SmsSettings__ApiToken = YOUR_IPROGTECH_API_TOKEN
SmsSettings__ApiUrl = https://sms.iprogtech.com/api/v1/sms_messages

AzureOCR__Endpoint = https://bhcare-ocr.cognitiveservices.azure.com/
AzureOCR__Key = YOUR_AZURE_COMPUTER_VISION_KEY

GeminiAPI__Key = YOUR_GEMINI_API_KEY

EncryptionKey = YourStrongEncryptionKeyHere1234567890123456
DataEncryption__Key = BHCARE_DataEncryption_Key_2024_Secure_32Chars

AdminUser__Email = healthcenterbaesa@gmail.com
AdminUser__Password = Admin123!
AdminUser__FullName = System Administrator
AdminUser__IsSuperAdmin = true

ReminderSettings__TimeZoneId = Singapore Standard Time
TimeZoneSettings__DefaultTimeZone = Singapore Standard Time
TimeZoneSettings__Culture = en-PH
TimeZoneSettings__DateFormat = MMM dd, yyyy
TimeZoneSettings__TimeFormat = h:mm tt
```

---

## ⚠️ **Important Notes:**

1. **Use double underscore (`__`)** for nested configuration (ASP.NET Core convention)
2. **Restart your App Service** after adding/updating settings
3. **Keep sensitive values secure** - never commit to GitHub
4. **Change default passwords** before production deployment
5. **Test each service** after configuration (Email, SMS, OCR)

---

## 🔄 **After Configuration:**

1. Click **Save** at the top
2. Click **Continue** to restart the app
3. Wait for restart to complete
4. Test the application

---

## ✅ **Verification Checklist:**

- [ ] Database connection working
- [ ] Email sending working (test OTP)
- [ ] SMS sending working (test OTP)
- [ ] ID scanning working (test OCR)
- [ ] Admin login working
- [ ] Time zone displaying correctly

