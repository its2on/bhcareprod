@echo off
echo Starting BHCARE Security Service...

REM Check if Python is installed
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Python is not installed or not in PATH
    echo Please install Python 3.7 or newer
    pause
    exit /b 1
)

REM Check if required packages are installed
echo Checking required packages...
pip show fastapi uvicorn pyjwt bcrypt python-multipart cryptography >nul 2>&1
if %errorlevel% neq 0 (
    echo Installing required packages...
    pip install fastapi uvicorn pyjwt bcrypt python-multipart cryptography
    if %errorlevel% neq 0 (
        echo Failed to install required packages
        pause
        exit /b 1
    )
)

REM Create logs directory if it doesn't exist
if not exist logs mkdir logs

REM Check if admin_credentials.env exists and copy from template if not
if not exist admin_credentials.env (
    if exist admin_credentials.env.template (
        echo Creating admin_credentials.env from template...
        copy admin_credentials.env.template admin_credentials.env
        echo Please edit admin_credentials.env with secure credentials!
    ) else (
        echo Warning: No admin credentials configuration found!
        echo Admin user will be created with random password.
        echo Check logs for generated password.
    )
)

REM Set environment variables if admin_credentials.env exists
if exist admin_credentials.env (
    echo Loading admin credentials from file...
    for /F "tokens=1,2 delims==" %%a in (admin_credentials.env) do (
        if not "%%a"=="" if not "%%a:~0,1%"=="#" (
            if "%%a"=="ADMIN_EMAIL" (
                set BHCARE_ADMIN_EMAIL=%%b
            ) else if "%%a"=="ADMIN_PASSWORD" (
                set BHCARE_ADMIN_PASSWORD=%%b
            )
        )
    )
)

REM Generate a random secret key if not set
if not defined BHCARE_SECRET_KEY (
    echo Setting random secret key for this session...
    for /f "delims=" %%a in ('python -c "import secrets; print(secrets.token_hex(32))"') do set BHCARE_SECRET_KEY=%%a
)

echo Starting security server...
start "BHCARE Security Server" cmd /c "set BHCARE_ADMIN_EMAIL=%BHCARE_ADMIN_EMAIL% && set BHCARE_ADMIN_PASSWORD=%BHCARE_ADMIN_PASSWORD% && set BHCARE_SECRET_KEY=%BHCARE_SECRET_KEY% && uvicorn security_server:app --reload --host 0.0.0.0 --port 8000 && pause"

echo.
echo Security service is running at:
echo   - Main API: http://localhost:8000
echo   - Documentation: http://localhost:8000/docs
echo.
echo Press any key to open the API documentation in your browser...
pause >nul

start "" http://localhost:8000/docs
