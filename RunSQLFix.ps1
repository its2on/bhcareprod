# PowerShell script to run the SQL fix for missing columns
# This will add the missing columns to the NCDRiskAssessments table

$serverName = "bhcareserverprod.database.windows.net"
$databaseName = "bhcareDB"
$username = "bhcareprod"
$password = "prodcarebh.123"
$sqlFile = "QuickFixNCDColumns.sql"

Write-Host "Connecting to database to fix missing columns..." -ForegroundColor Yellow

try {
    # Read the SQL file content
    $sqlContent = Get-Content -Path $sqlFile -Raw
    
    # Replace the USE statement with the correct database name
    $sqlContent = $sqlContent -replace "USE \[bhcareDB\];", "USE [$databaseName];"
    
    # Create connection string
    $connectionString = "Server=$serverName;Database=$databaseName;User Id=$username;Password=$password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    
    # Load SQL Server module if available
    if (Get-Module -ListAvailable -Name SqlServer) {
        Import-Module SqlServer
        Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlContent
        Write-Host "SQL fix executed successfully!" -ForegroundColor Green
    }
    else {
        Write-Host "SqlServer module not available. Please run the SQL manually." -ForegroundColor Red
        Write-Host "SQL Content to run:" -ForegroundColor Yellow
        Write-Host $sqlContent
    }
}
catch {
    Write-Host "Error executing SQL fix: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please run the QuickFixNCDColumns.sql file manually in SQL Server Management Studio" -ForegroundColor Yellow
}

Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
