@echo off
echo Updating admin email to healthcenterbaesa@gmail.com...
dotnet run --project . -- Tools/UpdateAdminEmail.cs
echo Done!
pause
