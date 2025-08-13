# MCP Server Docker Deployment

This Docker setup provides a containerized MCP (Model Context Protocol) server with OpenWebUI integration using best practices.

## What's Included

- **MCP Server**: .NET 9.0 MCP server with file system and development tools
- **MCPO Proxy**: Uses `uvx mcpo` (best practice) to expose MCP server as REST API
- **OpenWebUI**: CUDA-enabled web interface with GPU support and auth disabled

## Features

- ✅ **Modern Python tooling**: Uses `uvx mcpo` instead of global installation
- ✅ **GPU acceleration**: CUDA-enabled OpenWebUI with GPU support
- ✅ **No authentication**: `WEBUI_AUTH=False` for easy development
- ✅ **30+ development tools**: File operations, Git, testing, linting, and more
- ✅ **Production ready**: Multi-stage Docker builds and optimized images

## Quick Start

1. **Ensure GPU support** (if using CUDA features):
   ```bash
   # Install NVIDIA Container Toolkit if not already installed
   # https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/
   ```

2. **Build and run the services**:
   ```bash
   docker-compose up -d --build
   ```

3. **Access the applications**:
   - **OpenWebUI**: http://localhost:3000 (CUDA-enabled, no auth required)
   - **MCP API Documentation**: http://localhost:8000/docs
   - **File Tools API**: http://localhost:8000/file-tools/docs

4. **Check service health**:
   ```bash
   docker-compose ps
   docker-compose logs -f mcp-proxy
   ```

## Local Development Equivalent

The Docker setup replicates these local commands:

```bash
# MCP Server (what runs in container)
uvx mcpo --port 8000 -- dotnet "/app/mcp.server/bin/Release/net9.0/mcp.server.dll"

# OpenWebUI (what runs in container)  
docker run -d -p 3000:8080 --gpus all \
  -v open-webui:/app/backend/data \
  -e WEBUI_AUTH=False \
  --name open-webui \
  ghcr.io/open-webui/open-webui:cuda
```

## Services

### MCP Proxy (`mcp-proxy`)
- **Technology**: Uses `uvx mcpo` (modern Python tool execution)
- **Port**: 8000
- **Features**: Exposes 30+ development tools via REST API
- **Health**: Built-in monitoring and restart policies

### OpenWebUI (`open-webui`)
- **Image**: `ghcr.io/open-webui/open-webui:cuda` (GPU-enabled)
- **Port**: 3000 → 8080 (container)
- **GPU**: Full NVIDIA GPU support with CUDA
- **Auth**: Disabled (`WEBUI_AUTH=False`) for development ease
- **Storage**: Persistent data in Docker volume

## Available Tools

The MCP server provides these development tools:

### 📁 File Operations
- `read_file`, `write_file`, `list_files`, `find_files`
- `backup_config_file`, `validate_json`

### 🔧 Git Operations  
- `get_git_status`, `get_git_log`, `get_git_diff`
- `get_git_blame`, `show_commit`, `get_current_branch`

### 🏗️ Development
- `build_project`, `run_tests`, `lint_project`
- `install_dependencies`, `get_project_stats`

### 🔍 Search & Analysis
- `search_in_files`, `find_functions`, `find_todos`
- `get_file_history`, `get_development_processes`

### ⚙️ System & Config
- `get_system_config`, `get_environment_variables`
- `set_environment_variable`, `monitor_process`

## Configuration

### Environment Variables (`.env`)
```bash
# MCP Configuration
MCPO_HOST=0.0.0.0
MCPO_PORT=8000

# OpenWebUI Configuration  
WEBUI_AUTH=False
OLLAMA_BASE_URL=http://host.docker.internal:11434

# GPU Support
NVIDIA_VISIBLE_DEVICES=all
```

### GPU Requirements
- NVIDIA GPU with CUDA support
- NVIDIA Container Toolkit installed
- Docker with GPU support enabled

## Development Workflow

### Rebuilding after code changes:
```bash
docker-compose down
docker-compose up -d --build
```

### Viewing logs:
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f mcp-proxy
docker-compose logs -f open-webui
```

### Testing MCP tools:
```bash
# List available tools
curl http://localhost:8000/file-tools/openapi.json

# Test a tool
curl -X POST http://localhost:8000/file-tools/get_system_config \
  -H "Content-Type: application/json" \
  -d "{}"
```

## Troubleshooting

### GPU Issues
```bash
# Check GPU availability in container
docker run --rm --gpus all nvidia/cuda:11.8-base-ubuntu22.04 nvidia-smi

# Verify GPU is available to OpenWebUI
docker-compose exec open-webui nvidia-smi
```

### MCP Connection Issues
```bash
# Check MCP server logs
docker-compose logs mcp-proxy

# Test .NET application directly
docker-compose exec mcp-proxy dotnet /app/mcp.server/bin/Release/net9.0/mcp.server.dll
```

### Port Conflicts
```bash
# Check what's using ports
netstat -tulpn | grep :3000
netstat -tulpn | grep :8000

# Stop conflicting services
docker stop $(docker ps -q --filter "publish=3000")
```

## Production Deployment

For production use:

1. **Enable authentication**:
   ```yaml
   environment:
     - WEBUI_AUTH=True
   ```

2. **Use secrets for sensitive data**:
   ```yaml
   secrets:
     - ollama_api_key
   ```

3. **Add resource limits**:
   ```yaml
   deploy:
     resources:
       limits:
         memory: 4G
         cpus: '2'
   ```

4. **Use external networks**:
   ```yaml
   networks:
     - external_network
   ```

## File Structure

```
├── Dockerfile.mcp          # Multi-stage .NET build with uvx mcpo
├── docker-compose.yml      # GPU-enabled orchestration  
├── .dockerignore           # Optimized build context
├── .env                    # Environment configuration
├── mcp.server/             # .NET MCP server source
├── mcp.tools/              # MCP tools implementation
└── DOCKER_README.md        # This documentation
```

## Best Practices Implemented

- ✅ **Modern tooling**: `uvx` for Python tool execution
- ✅ **Multi-stage builds**: Optimized Docker images
- ✅ **GPU support**: Native CUDA acceleration
- ✅ **Development friendly**: Auth disabled, verbose logging
- ✅ **Production ready**: Health checks, restart policies
- ✅ **Secure defaults**: Minimal attack surface
