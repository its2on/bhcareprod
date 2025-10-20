@echo off
echo ========================================
echo BHCARE Database Cleanup Script
echo ========================================
echo.
echo WARNING: This will delete ALL data except the system admin account!
echo Admin account (admin@example.com) will be preserved.
echo.
echo This action cannot be undone!
echo.
set /p confirm="Are you sure you want to continue? (yes/no): "

if /i not "%confirm%"=="yes" (
    echo Operation cancelled.
    pause
    exit /b 1
)

echo.
echo Starting database cleanup...
echo.

REM Run the SQL script using sqlcmd
sqlcmd -S "tcp:bhcareserverprod.database.windows.net,1433" -d "bhcareDB" -U "bhcareprod" -P "prodcarebh.123" -i "SQL\force-cleanup.sql" -o "cleanup-log.txt"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Database cleanup completed successfully!
    echo ========================================
    echo.
    echo Check cleanup-log.txt for detailed output.
    echo.
    echo The database has been reset with only the admin account preserved.
    echo Admin login: admin@example.com
    echo Admin password: Admin@123
    echo.
) else (
    echo.
    echo ========================================
    echo Database cleanup failed!
    echo ========================================
    echo.
    echo Check cleanup-log.txt for error details.
    echo.
)

echo Press any key to exit...
pause > nul
