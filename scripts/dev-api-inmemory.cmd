@echo off
cd /d "%~dp0.."
set Database__Provider=InMemory
"C:\Program Files\dotnet\dotnet.exe" run --project backend/src/Pcc.Api/Pcc.Api.csproj --no-build --urls http://127.0.0.1:5088
