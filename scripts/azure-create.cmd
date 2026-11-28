@echo off
rem Usage (in your terminal, NOT in this file):
rem   set SUFFIX=abc123
rem   set SQL_ADMIN_PASSWORD=<a strong password>
rem   scripts\azure-create.cmd

if "%SUFFIX%"=="" (echo Set SUFFIX first & exit /b 1)
if "%SQL_ADMIN_PASSWORD%"=="" (echo Set SQL_ADMIN_PASSWORD first & exit /b 1)

set RG=rg-cloudtask
set LOC=canadacentral

az group create --name %RG% --location %LOC%
az appservice plan create --name plan-cloudtask --resource-group %RG% --sku B1 --is-linux
az webapp create --name cloudtask-api-%SUFFIX% --resource-group %RG% --plan plan-cloudtask --runtime "DOTNETCORE:8.0"
az sql server create --name cloudtask-sql-%SUFFIX% --resource-group %RG% --location %LOC% --admin-user cloudtaskadmin --admin-password "%SQL_ADMIN_PASSWORD%"
az sql db create --name CloudTaskDb --server cloudtask-sql-%SUFFIX% --resource-group %RG% --edition Basic
az sql server firewall-rule create --server cloudtask-sql-%SUFFIX% --resource-group %RG% --name AllowAzure --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0