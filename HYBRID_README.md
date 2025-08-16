# MCP Pixula - Hybrid Deployment Guide

## Overview

This hybrid approach runs the **MCP server directly on your host machine** (for full file system and command access) while keeping **Open WebUI containerized** (for easy deployment). This is the best practice when you need the MCP server to have full access to your development environment.

## Why Hybrid?

- ✅ **Full Host Access**: MCP server can read/write files and execute commands on your actual machine
- ✅ **Easy Deployment**: Open WebUI remains containerized for simple setup
- ✅ **Best of Both Worlds**: Security isolation for the web UI, full power for the MCP tools
- ✅ **No Permission Issues**: No need to mount volumes or deal with container isolation

## Quick Start

### Prerequisites

1. **Windows** with Command Prompt or PowerShell
2. **Docker Desktop** installed and running
3. **.NET 9.0 SDK** installed
4. **Python 3.8+** installed
5. **UV** (Python package manager): `pip install uv`

### Option 1: Batch Script (Recommended)

```cmd
# Start everything
.\start-hybrid.bat start

# Check status
.\start-hybrid.bat status

# Stop everything
.\start-hybrid.bat stop
```

### Option 2: Manual Start

```bash
# Terminal 1: Start MCP server on host
.\start-mcp-host.bat

# Terminal 2: Start Open WebUI in Docker
docker-compose -f docker-compose.hybrid.yml up -d
```

## Access Points

Once running, you can access:

- **Open WebUI**: http://localhost:3000 (No authentication required)
- **MCP API Docs**: http://localhost:8000/docs
- **MCP Health Check**: http://localhost:8000/health

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Host Machine                          │
│                                                          │
│  ┌──────────────────────────────────────┐               │
│  │   MCP Server (Port 8000)             │               │
│  │   - Full file system access          │               │
│  │   - Can execute host commands        │               │
│  │   - Git operations                   │               │
│  │   - Build/test/lint tools            │               │
│  └──────────────────────────────────────┘               │
│                      ↑                                   │
│                      │ http://host.docker.internal:8000  │
│  ┌──────────────────────────────────────┐               │
│  │   Docker Container                   │               │
│  │   ┌────────────────────────────┐     │               │
│  │   │  Open WebUI (Port 3000)    │     │               │
│  │   │  - Web interface           │     │               │
│  │   │  - CUDA/GPU support        │     │               │
│  │   │  - Connects to MCP & Ollama│     │               │
│  │   └────────────────────────────┘     │               │
│  └──────────────────────────────────────┘               │
└─────────────────────────────────────────────────────────┘
```

## Configuration

### MCP Server Settings

The MCP server runs with these defaults:
- **Port**: 8000
- **Host**: 0.0.0.0 (accessible from containers)
- **Access**: Full host file system and command execution

### Open WebUI Settings

Edit `docker-compose.hybrid.yml` to customize:

```yaml
environment:
  - WEBUI_AUTH=False  # Set to True for production
  - OLLAMA_BASE_URL=http://host.docker.internal:11434
```

### GPU Support

If you have an NVIDIA GPU:
1. Install NVIDIA Container Toolkit
2. The hybrid compose file already includes GPU support
3. Open WebUI will automatically use CUDA acceleration

## Available MCP Tools

The MCP server provides 30+ tools with full host access:

### File Operations
- `read_file` - Read any file on your system
- `write_file` - Write files anywhere you have permissions
- `list_files` - Browse directories
- `find_files` - Search for files by pattern

### Git Operations  
- `get_git_status` - Check repo status
- `get_git_log` - View commit history
- `get_git_diff` - See uncommitted changes
- `show_commit` - View specific commits

### Development Tools
- `build_project` - Build your projects
- `run_tests` - Execute test suites
- `lint_project` - Run linters
- `install_dependencies` - Install packages

### System Tools
- `monitor_process` - Monitor and manage processes
- `get_environment_variables` - Read env vars
- `set_environment_variable` - Set env vars
- `kill_processes_by_name` - Terminate processes

## Troubleshooting

### MCP Server Issues

```powershell
# Check if MCP is running
Get-Process -Name "uvx" -ErrorAction SilentlyContinue

# Test MCP directly
curl http://localhost:8000/health

# Check Windows Firewall
# Make sure port 8000 is not blocked
```

### Open WebUI Can't Connect to MCP

```bash
# Verify host.docker.internal resolves
docker run --rm alpine ping -c 2 host.docker.internal

# Check MCP is accessible from container
docker run --rm curlimages/curl curl http://host.docker.internal:8000/health
```

### Port Conflicts

```powershell
# Find what's using port 8000
netstat -ano | findstr :8000

# Find what's using port 3000  
netstat -ano | findstr :3000
```

## Security Considerations

⚠️ **Important**: This hybrid setup gives the MCP server full access to your host system. 

### For Development
- Current setup has `WEBUI_AUTH=False` for convenience
- MCP server binds to `0.0.0.0` for container access
- Suitable for local development only

### For Production
1. Enable authentication: `WEBUI_AUTH=True`
2. Use a reverse proxy with proper authentication
3. Restrict MCP server binding to localhost only
4. Implement proper access controls
5. Use HTTPS for all connections

## Migrating from Full Container Setup

If you were using the fully containerized setup:

1. **Stop old containers**: `docker-compose down`
2. **Remove old containers**: `docker container prune`
3. **Start hybrid setup**: `.\start-hybrid.bat start`
4. **Your Open WebUI data is preserved** in the Docker volume

## Manual Cleanup

To completely remove everything:

```cmd
# Stop services
.\start-hybrid.bat stop

# Remove Open WebUI container and volume
docker-compose -f docker-compose.hybrid.yml down -v

# Clean build artifacts (optional)
rmdir /s /q mcp.server\bin mcp.server\obj
rmdir /s /q mcp.tools\bin mcp.tools\obj
```

## Best Practices

1. **Always stop services properly** to avoid orphaned processes
2. **Monitor resource usage** - MCP operations can be CPU intensive
3. **Regular backups** of the Open WebUI volume if storing important data
4. **Keep MCP server updated** for latest tools and security fixes
5. **Use the batch script** for consistent start/stop operations

## FAQ

**Q: Why not run everything in containers?**
A: MCP server needs host access for file operations and command execution. Container isolation defeats this purpose.

**Q: Can I run this on Linux/Mac?**
A: Yes, but you'll need to adapt the scripts. The core concept works on any OS that supports Docker.

**Q: How do I add custom tools to MCP?**
A: Edit the tools in `mcp.tools/` directory and rebuild with `dotnet build -c Release`.

**Q: Can multiple users connect to the same MCP server?**
A: Yes, but be aware that all users share the same host access. Use with caution.

## Support

For issues specific to:
- **MCP Server**: Check the [MCP Pixula repository](https://github.com/ottop/mcp-pixula)
- **Open WebUI**: See [Open WebUI documentation](https://github.com/open-webui/open-webui)
- **Docker**: Consult [Docker documentation](https://docs.docker.com/)

## License

This hybrid deployment configuration is provided as-is for the MCP Pixula project.
