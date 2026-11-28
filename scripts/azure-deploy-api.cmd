@echo off
rem Usage:
rem   set SUFFIX=abc123
rem   scripts\azure-deploy-api.cmd

if "%SUFFIX%"=="" (echo Set SUFFIX first & exit /b 1)

if exist publish rmdir /s /q publish
if exist api.zip del api.zip

dotnet publish src\CloudTask.Api -c Release -o publish || exit /b 1

pushd publish
tar -a -c -f ..\api.zip *
popd

az webapp deploy --name cloudtask-api-%SUFFIX% --resource-group rg-cloudtask --src-path api.zip --type zip
echo.
echo Check: https://cloudtask-api-%SUFFIX%.azurewebsites.net/health/ready