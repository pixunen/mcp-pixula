@echo off
REM Start MCP Server on Host (Windows Batch Script)
REM This runs the MCP server locally with full host access

echo Starting MCP Server on host (port 8000)...
echo.
echo This will give the MCP server full access to your file system.
echo Press Ctrl+C to stop the server.
echo.

REM Build the MCP server in Release mode first
echo Building MCP server...
cd /d "%~dp0"
dotnet build mcp.server\mcp.server.csproj -c Release

if %ERRORLEVEL% NEQ 0 (
    echo Build failed! Please check the error messages above.
    pause
    exit /b 1
)

echo.
echo Starting MCP server with mcpo...
uvx mcpo --port 8000 -- dotnet "%~dp0mcp.server\bin\Release\net9.0\mcp.server.dll"
