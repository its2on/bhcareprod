# PowerShell script to run the HEEADSSS SQL fix for missing columns
# This will add the missing sexuality columns to the HEEADSSSAssessments table

$serverName = "bhcareserverprod.database.windows.net"
$databaseName = "bhcareDB"
$username = "bhcareprod"
$password = "prodcarebh.123"
$sqlFile = "FixHEEADSSSColumns.sql"

Write-Host "Connecting to database to fix missing HEEADSSS columns..." -ForegroundColor Yellow

try {
    # Read the SQL file content
    $sqlContent = Get-Content -Path $sqlFile -Raw
    
    # Replace the USE statement with the correct database name
    $sqlContent = $sqlContent -replace "USE \[bhcareDB\];", "USE [$databaseName];"
    
    Write-Host "SQL Content to execute:" -ForegroundColor Cyan
    Write-Host $sqlContent -ForegroundColor White
    
    Write-Host "`nPlease run this SQL script manually in SQL Server Management Studio or Azure Data Studio" -ForegroundColor Green
    Write-Host "Connection details:" -ForegroundColor Yellow
    Write-Host "Server: $serverName" -ForegroundColor White
    Write-Host "Database: $databaseName" -ForegroundColor White
    Write-Host "Username: $username" -ForegroundColor White
    
}
catch {
    Write-Host "Error reading SQL file: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nAfter running the SQL script, restart the application with 'dotnet run'" -ForegroundColor Green
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
