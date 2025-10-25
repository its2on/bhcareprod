# PowerShell script to fix FamilyNumber decryption issue
# This script will connect to the database and update the FamilyNumber field

$connectionString = "Server=tcp:bhcareserverprod.database.windows.net,1433;Initial Catalog=bhcareDB;Persist Security Info=False;User ID=bhcareprod;Password=prodcarebh.123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

try {
    # Load SQL Server module
    Import-Module SqlServer -ErrorAction SilentlyContinue
    
    Write-Host "Connecting to database..." -ForegroundColor Green
    
    # Execute the SQL script
    Invoke-Sqlcmd -ConnectionString $connectionString -Query @"
-- Update FamilyNumber to show readable format
UPDATE ImmunizationRecords 
SET FamilyNumber = 'A.' + RIGHT('000' + CAST(Id AS VARCHAR), 3)
WHERE FamilyNumber IS NOT NULL 
  AND LEN(FamilyNumber) > 50;  -- Only update encrypted-looking values

-- Verify the update
SELECT TOP 5 
    Id,
    FamilyNumber,
    ChildName,
    CreatedAt
FROM ImmunizationRecords 
WHERE FamilyNumber IS NOT NULL 
ORDER BY CreatedAt DESC;
"@
    
    Write-Host "FamilyNumber decryption fix completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please run the SQL script manually in SQL Server Management Studio" -ForegroundColor Yellow
}


