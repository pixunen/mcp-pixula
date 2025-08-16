@echo off
setlocal enabledelayedexpansion

set ACTION=%1
if "%ACTION%"=="" set ACTION=start

set MCP_PORT=8000
set WEBUI_PORT=3000
set PROJECT_ROOT=%~dp0
set MCP_DLL=%PROJECT_ROOT%mcp.server\bin\Release\net9.0\mcp.server.dll

if /i "%ACTION%"=="start" goto START
if /i "%ACTION%"=="stop" goto STOP
if /i "%ACTION%"=="status" goto STATUS
goto USAGE

:START
echo Starting MCP Hybrid Deployment...
echo.

echo Building MCP server...
cd /d "%PROJECT_ROOT%"
dotnet build mcp.server\mcp.server.csproj -c Release
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    exit /b 1
)
echo Build completed successfully.
echo.

echo Starting MCP server on port %MCP_PORT%...
start "MCP Server" powershell.exe -Command "uvx mcpo --port %MCP_PORT% -- dotnet '%MCP_DLL%'"

echo Waiting for MCP server to start...
timeout /t 5 /nobreak >nul

echo Testing MCP server...
curl -s http://localhost:%MCP_PORT%/docs >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo MCP server is running at http://localhost:%MCP_PORT%
) else (
    echo MCP server may still be starting...
)
echo.

echo Starting Open WebUI container...
docker-compose -f docker-compose.hybrid.yml up -d
if %ERRORLEVEL% equ 0 (
    echo Open WebUI is running at http://localhost:%WEBUI_PORT%
) else (
    echo Failed to start Open WebUI - make sure Docker Desktop is running
)

echo.
echo === Hybrid Deployment Started ===
echo MCP API: http://localhost:%MCP_PORT%/docs
echo Open WebUI: http://localhost:%WEBUI_PORT%
echo.
echo To stop: start-hybrid.bat stop
goto END

:STOP
echo Stopping MCP Hybrid Deployment...
echo.

echo Stopping Open WebUI container...
cd /d "%PROJECT_ROOT%"
docker-compose -f docker-compose.hybrid.yml down

echo Stopping MCP server...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":%MCP_PORT% " ^| findstr "LISTENING"') do (
    echo Stopping process %%a
    taskkill /PID %%a /F >nul 2>&1
)

echo All services stopped.
goto END

:STATUS
echo === Service Status ===
echo.

echo Checking MCP Server...
curl -s http://localhost:%MCP_PORT%/docs >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo MCP Server: RUNNING at http://localhost:%MCP_PORT%
) else (
    echo MCP Server: STOPPED
)

echo Checking Open WebUI...
docker ps --format "{{.Names}}" | findstr "open-webui" >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo Open WebUI: RUNNING at http://localhost:%WEBUI_PORT%
) else (
    echo Open WebUI: STOPPED
)
goto END

:USAGE
echo Usage: start-hybrid.bat [start^|stop^|status]
echo.
echo   start  - Start MCP server and Open WebUI
echo   stop   - Stop all services
echo   status - Check service status

:END