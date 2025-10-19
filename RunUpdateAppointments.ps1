# PowerShell script to update Draft appointments to Pending for non-assessment consultation types
# This fixes existing appointments that were created before the code changes

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Update Draft Appointments to Pending" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Get connection string from appsettings.json
$appsettingsPath = "appsettings.json"
if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
    $connectionString = $appsettings.ConnectionStrings.DefaultConnection
    Write-Host "Connection string found in appsettings.json" -ForegroundColor Green
} else {
    Write-Host "Error: appsettings.json not found!" -ForegroundColor Red
    exit 1
}

# SQL script path
$sqlScriptPath = "UpdateDraftAppointmentsToPending.sql"

if (-not (Test-Path $sqlScriptPath)) {
    Write-Host "Error: SQL script not found at $sqlScriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "Reading SQL script..." -ForegroundColor Yellow
$sqlScript = Get-Content $sqlScriptPath -Raw

Write-Host "Executing SQL update..." -ForegroundColor Yellow

try {
    # Execute using sqlcmd
    $sqlScript | sqlcmd -S "tcp:prodcarebh.database.windows.net,1433" -d "bhcare" -U "prodcarebh" -P "Bhcare@2024" -I
    
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host "Update completed successfully!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "The following appointment types have been updated from Draft to Pending:" -ForegroundColor Cyan
    Write-Host "  - Immunization" -ForegroundColor White
    Write-Host "  - Dental" -ForegroundColor White
    Write-Host "  - DOTS Consult" -ForegroundColor White
    Write-Host "  - Prenatal & Family Planning" -ForegroundColor White
    Write-Host ""
    Write-Host "These appointments will now appear in:" -ForegroundColor Cyan
    Write-Host "  - User's Ongoing Appointments section" -ForegroundColor White
    Write-Host "  - Nurse's All Appointments table" -ForegroundColor White
    Write-Host ""
} catch {
    Write-Host ""
    Write-Host "Error executing SQL script:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
