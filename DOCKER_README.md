# MCP Server Docker Deployment

This Docker setup provides a containerized MCP (Model Context Protocol) server with OpenWebUI integration.

## What's Included

- **MCP Server**: .NET 9.0 MCP server with file system tools
- **MCPO Proxy**: Exposes the MCP server as a REST API
- **OpenWebUI**: Web interface for interacting with AI models

## Quick Start

1. **Build and run the services**:
   ```bash
   docker-compose up -d --build
   ```

2. **Access the applications**:
   - OpenWebUI: http://localhost:3000
   - MCP API: http://localhost:8000

3. **Check service health**:
   ```bash
   docker-compose ps
   docker-compose logs mcp-proxy
   ```

## Services

### MCP Proxy (`mcp-proxy`)
- Runs on port 8000
- Exposes MCP server functionality via REST API
- Health check endpoint: `/health`

### OpenWebUI (`open-webui`)
- Runs on port 3000
- Configured to connect to MCP proxy
- Data persisted in Docker volume

## Configuration

Environment variables can be configured in `.env` file:

- `MCPO_HOST`: Host for MCP proxy (default: 0.0.0.0)
- `MCPO_PORT`: Port for MCP proxy (default: 8000) 
- `OLLAMA_BASE_URL`: Ollama server URL for OpenWebUI
- `OPENWEBUI_PORT`: Port for OpenWebUI (default: 3000)

## Development

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

### Debugging the MCP server:
```bash
# Run MCP proxy interactively
docker-compose run --rm mcp-proxy /bin/bash

# Check if .NET runtime works
docker-compose run --rm mcp-proxy dotnet --version

# Test MCP server directly
docker-compose run --rm mcp-proxy dotnet /app/mcp-server/mcp.server.dll
```

## Troubleshooting

### MCP Proxy fails to start
1. Check logs: `docker-compose logs mcp-proxy`
2. Verify .NET application is built correctly
3. Ensure health check endpoint is accessible

### OpenWebUI can't connect to MCP
1. Verify MCP proxy is healthy: `curl http://localhost:8000/health`
2. Check network connectivity between services
3. Review OpenWebUI configuration

### Build failures
1. Ensure all .NET dependencies are restored
2. Check Docker build context includes all necessary files
3. Verify .NET SDK version compatibility

## File Structure

```
├── Dockerfile.mcp          # Multi-stage build for MCP server
├── docker-compose.yml      # Service orchestration
├── .dockerignore           # Exclude unnecessary files from build
├── .env                    # Environment configuration
├── mcp.server/             # .NET MCP server source
├── mcp.tools/              # MCP tools implementation
└── mcp.client/             # MCP client (optional)
```
