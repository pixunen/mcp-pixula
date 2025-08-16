# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MCP-Pixula is a comprehensive Model Context Protocol (MCP) implementation built with .NET 9.0. It provides AI assistants with powerful development tools through three main components:

- **mcp.server**: MCP server hosting development tools via stdio transport
- **mcp.client**: Interactive console client with Ollama integration and Spectre.Console UI
- **mcp.tools**: Modular tools library with file management, Git operations, process management, search capabilities, and configuration handling

## Build and Development Commands

### Core Commands
```bash
# Build the entire solution
dotnet build -c Release

# Build specific projects
dotnet build mcp.server/mcp.server.csproj -c Release
dotnet build mcp.client/mcp.client.csproj -c Release
dotnet build mcp.tools/mcp.tools.csproj -c Release

# Run the MCP server (requires Python mcpo)
uvx mcpo --port 8000 -- dotnet mcp.server/bin/Release/net9.0/mcp.server.dll

# Run the interactive client
dotnet run --project mcp.client/mcp.client.csproj
```

### Deployment Options
```cmd
# Hybrid deployment (recommended) - Windows
.\start-hybrid.bat start
.\start-hybrid.bat stop
.\start-hybrid.bat status

# Manual startup
.\start-mcp-host.bat

# Open WebUI only (after MCP server is running)
docker-compose -f docker-compose.hybrid.yml up -d
```

## Architecture

### Server Architecture (mcp.server/Program.cs)
- Uses Microsoft's MCP implementation with stdio transport
- Automatically discovers tools from mcp.tools assembly using reflection
- Configured with `WithToolsFromAssembly(typeof(mcp.tools.ConfigTool).Assembly)`
- Structured logging with console output to stderr

### Tool Architecture (mcp.tools/)
- Tools are static classes decorated with `[McpServerToolType]`
- Tool methods use `[McpServerTool]` and `[Description]` attributes
- Five main tool categories:
  - **FileTool.cs**: File operations (read, write, list, find)
  - **GitTool.cs**: Git repository management (status, log, diff, blame, history)
  - **ProcessTool.cs**: Build/test automation with framework auto-detection
  - **SearchTool.cs**: Code search, function finding, project statistics
  - **ConfigTool.cs**: Configuration and environment management

### Client Architecture (mcp.client/)
- Interactive console using Spectre.Console for rich UI
- Integrates with Ollama for AI-powered interactions via OllamaSharp
- Streaming response support with visual feedback
- Uses Microsoft.Extensions.AI for LLM integration

## Key Development Patterns

### Adding New Tools
1. Create static class in mcp.tools/ with `[McpServerToolType]`
2. Add static methods with `[McpServerTool, Description("...")]`
3. Server automatically discovers and registers new tools
4. Follow existing error handling patterns with specific exceptions

### Project Detection Logic
The ProcessTool implements intelligent project type detection:
- Checks for specific files (.csproj, package.json, requirements.txt, Cargo.toml, pom.xml)
- Automatically selects appropriate build/test/lint commands
- Supports multiple project types in same repository

### Cross-Platform Considerations
- File path handling uses Path.Combine and platform-agnostic APIs
- Process execution accounts for Windows vs Unix differences
- Hybrid deployment provides optimal balance of functionality and ease of use

## Configuration Files

- **mcp.sln**: Main solution file with three projects
- **docker-compose.hybrid.yml**: Open WebUI only in Docker
- **start-hybrid.bat**: Windows hybrid deployment automation
- **start-mcp-host.bat**: Manual MCP server startup script

## Integration Points

### Claude Desktop Integration
Add to MCP configuration:
```json
{
  "mcpServers": {
    "mcp-pixula": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/mcp-pixula/mcp.server/mcp.server.csproj"]
    }
  }
}
```

### Open WebUI Integration
- Hybrid deployment runs Open WebUI on port 3000
- MCP server accessible at port 8000
- Health check endpoint: http://localhost:8000/health
- API documentation: http://localhost:8000/docs