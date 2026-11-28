@echo off
rem Usage:
rem   set SUFFIX=abc123
rem   set SQL_ADMIN_PASSWORD=<same password as before>
rem   set AZURE_JWT_KEY=<a NEW random key, 40+ characters, different from dev>
rem   scripts\azure-configure.cmd

if "%SUFFIX%"=="" (echo Set SUFFIX first & exit /b 1)
if "%SQL_ADMIN_PASSWORD%"=="" (echo Set SQL_ADMIN_PASSWORD first & exit /b 1)
if "%AZURE_JWT_KEY%"=="" (echo Set AZURE_JWT_KEY first & exit /b 1)

az webapp config appsettings set --name cloudtask-api-%SUFFIX% --resource-group rg-cloudtask --settings "ConnectionStrings__DefaultConnection=Server=tcp:cloudtask-sql-%SUFFIX%.database.windows.net,1433;Database=CloudTaskDb;User ID=cloudtaskadmin;Password=%SQL_ADMIN_PASSWORD%;Encrypt=True;TrustServerCertificate=False" "Jwt__Key=%AZURE_JWT_KEY%" "Database__MigrateOnStartup=true" > nul
echo Settings applied.