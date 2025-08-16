@echo off
setlocal enabledelayedexpansion

set ACTION=%1
if "%ACTION%"=="" set ACTION=start

set MCP_PORT=8000
set WEBUI_PORT=3000
set OLLAMA_PORT=11434
set PROJECT_ROOT=%~dp0
set MCP_DLL=%PROJECT_ROOT%mcp.server\bin\Release\net9.0\mcp.server.dll

if /i "%ACTION%"=="start" goto START
if /i "%ACTION%"=="stop" goto STOP
if /i "%ACTION%"=="status" goto STATUS
goto USAGE

:START
echo Starting MCP Hybrid Deployment with Ollama...
echo.

echo Checking Ollama...
call :CHECK_OLLAMA
if !OLLAMA_RUNNING!==0 (
    echo Starting Ollama service...
    start "Ollama" ollama serve
    echo Waiting for Ollama to start...
    timeout /t 3 /nobreak >nul
    
    call :CHECK_OLLAMA
    if !OLLAMA_RUNNING!==0 (
        echo Warning: Failed to start Ollama automatically
        echo Please start Ollama manually: ollama serve
    ) else (
        echo Ollama is now running at http://localhost:%OLLAMA_PORT%
    )
) else (
    echo Ollama is already running at http://localhost:%OLLAMA_PORT%
)
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
echo Ollama API: http://localhost:%OLLAMA_PORT%
echo MCP API: http://localhost:%MCP_PORT%/docs
echo Open WebUI: http://localhost:%WEBUI_PORT%
echo.
echo Open WebUI will use Ollama for AI models automatically.
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

echo Note: Ollama service is left running (use 'ollama stop' to stop models)
echo All other services stopped.
goto END

:STATUS
echo === Service Status ===
echo.

echo Checking Ollama...
call :CHECK_OLLAMA
if !OLLAMA_RUNNING!==1 (
    echo Ollama: RUNNING at http://localhost:%OLLAMA_PORT%
    
    rem Check for available models
    for /f %%i in ('ollama list ^| find /c ":" 2^>nul') do set MODEL_COUNT=%%i
    if !MODEL_COUNT! gtr 1 (
        echo   Models available: !MODEL_COUNT! models
    ) else (
        echo   No models installed - run 'ollama pull llama3.2' to install a model
    )
    
    rem Check for running models
    ollama ps 2>nul | findstr ":" >nul
    if !ERRORLEVEL! equ 0 (
        echo   Running models:
        ollama ps | findstr -v "NAME"
    )
) else (
    echo Ollama: STOPPED
    echo   To start: ollama serve
)

echo.
echo Checking MCP Server...
curl -s http://localhost:%MCP_PORT%/docs >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo MCP Server: RUNNING at http://localhost:%MCP_PORT%
) else (
    echo MCP Server: STOPPED
)

echo.
echo Checking Open WebUI...
docker ps --format "{{.Names}}" | findstr "open-webui" >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo Open WebUI: RUNNING at http://localhost:%WEBUI_PORT%
) else (
    echo Open WebUI: STOPPED
)
goto END

:CHECK_OLLAMA
curl -s http://localhost:%OLLAMA_PORT%/api/tags >nul 2>&1
if %ERRORLEVEL% equ 0 (
    set OLLAMA_RUNNING=1
) else (
    set OLLAMA_RUNNING=0
)
goto :eof

:USAGE
echo Usage: start-hybrid.bat [start^|stop^|status]
echo.
echo   start  - Start Ollama, MCP server and Open WebUI
echo   stop   - Stop MCP server and Open WebUI (leaves Ollama running)
echo   status - Check status of all services
echo.
echo Additional Ollama commands:
echo   ollama serve     - Start Ollama service manually
echo   ollama pull ^<model^> - Download a model (e.g., ollama pull llama3.2)
echo   ollama list      - List available models
echo   ollama ps        - List running models

:END