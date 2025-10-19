# PowerShell script to add columns to StaffMembers table
$ServerName = "bhcareserverprod.database.windows.net"
$DatabaseName = "bhcareDB"
$Username = "bhcareprod"
$Password = "prodcarebh.123"

$ConnectionString = "Server=tcp:$ServerName,1433;Initial Catalog=$DatabaseName;Persist Security Info=False;User ID=$Username;Password=$Password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "Connecting to database..." -ForegroundColor Yellow

try {
    $Connection = New-Object System.Data.SqlClient.SqlConnection
    $Connection.ConnectionString = $ConnectionString
    $Connection.Open()
    
    Write-Host "Connected successfully!" -ForegroundColor Green
    
    # Array of SQL commands to execute
    $commands = @(
        "IF COL_LENGTH('dbo.StaffMembers', 'FirstName') IS NULL ALTER TABLE [dbo].[StaffMembers] ADD [FirstName] nvarchar(max) NOT NULL DEFAULT ''",
        "IF COL_LENGTH('dbo.StaffMembers', 'MiddleName') IS NULL ALTER TABLE [dbo].[StaffMembers] ADD [MiddleName] nvarchar(max) NULL",
        "IF COL_LENGTH('dbo.StaffMembers', 'Gender') IS NULL ALTER TABLE [dbo].[StaffMembers] ADD [Gender] nvarchar(max) NOT NULL DEFAULT ''",
        "IF COL_LENGTH('dbo.StaffMembers', 'DateOfBirth') IS NULL ALTER TABLE [dbo].[StaffMembers] ADD [DateOfBirth] datetime2 NOT NULL DEFAULT '1990-01-01'",
        "IF COL_LENGTH('dbo.StaffMembers', 'Address') IS NULL ALTER TABLE [dbo].[StaffMembers] ADD [Address] nvarchar(max) NOT NULL DEFAULT ''",
        "IF COL_LENGTH('dbo.StaffMembers', 'CivilStatus') IS NULL ALTER TABLE [dbo].[StaffMembers] ADD [CivilStatus] nvarchar(max) NOT NULL DEFAULT ''"
    )
    
    # Execute each command
    foreach ($sql in $commands) {
        Write-Host "Executing: $($sql.Substring(0, [Math]::Min(50, $sql.Length)))..." -ForegroundColor Cyan
        $Command = $Connection.CreateCommand()
        $Command.CommandText = $sql
        $Command.ExecuteNonQuery() | Out-Null
        Write-Host "  ✓ Success" -ForegroundColor Green
    }
    
    # Handle Name to LastName rename
    Write-Host "Checking Name column..." -ForegroundColor Cyan
    $CheckCommand = $Connection.CreateCommand()
    $CheckCommand.CommandText = "SELECT CASE WHEN COL_LENGTH('dbo.StaffMembers', 'Name') IS NOT NULL THEN 1 ELSE 0 END"
    $nameExists = $CheckCommand.ExecuteScalar()
    
    if ($nameExists -eq 1) {
        Write-Host "Renaming Name to LastName..." -ForegroundColor Cyan
        $RenameCommand = $Connection.CreateCommand()
        $RenameCommand.CommandText = "EXEC sp_rename 'StaffMembers.Name', 'LastName', 'COLUMN'"
        $RenameCommand.ExecuteNonQuery() | Out-Null
        Write-Host "  ✓ Renamed successfully" -ForegroundColor Green
    } else {
        # Check if LastName exists
        $CheckLastName = $Connection.CreateCommand()
        $CheckLastName.CommandText = "SELECT CASE WHEN COL_LENGTH('dbo.StaffMembers', 'LastName') IS NOT NULL THEN 1 ELSE 0 END"
        $lastNameExists = $CheckLastName.ExecuteScalar()
        
        if ($lastNameExists -eq 0) {
            Write-Host "Adding LastName column..." -ForegroundColor Cyan
            $AddLastName = $Connection.CreateCommand()
            $AddLastName.CommandText = "ALTER TABLE [dbo].[StaffMembers] ADD [LastName] nvarchar(max) NOT NULL DEFAULT ''"
            $AddLastName.ExecuteNonQuery() | Out-Null
            Write-Host "  ✓ Added successfully" -ForegroundColor Green
        } else {
            Write-Host "  ✓ LastName already exists" -ForegroundColor Green
        }
    }
    
    Write-Host "`n✅ All columns added successfully!" -ForegroundColor Green
    Write-Host "You can now run the application with: dotnet run" -ForegroundColor Yellow
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    if ($Connection.State -eq 'Open') {
        $Connection.Close()
        Write-Host "`nConnection closed." -ForegroundColor Gray
    }
}
