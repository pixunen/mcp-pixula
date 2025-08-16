# MCP-Pixula

A comprehensive Model Context Protocol (MCP) implementation featuring a rich set of development tools for file management, project analysis, Git operations, process management, and configuration handling.

## 🚀 Overview

MCP-Pixula is a local MCP sandbox that provides AI assistants with powerful development tools through the Model Context Protocol. The project consists of three main components:

- **MCP Server**: Hosts and serves the development tools
- **MCP Client**: Interactive console client for testing and using the tools
- **MCP Tools Library**: Collection of development-focused tools

## 🚀 Quick Start

**Hybrid Deployment** - MCP server runs on your host machine (for full file system access) with Open WebUI in Docker. This gives you the best of both worlds - full system access for MCP tools and easy deployment for the web interface.

### Why Hybrid?

- ✅ **Full Host Access**: MCP server can read/write files and execute commands on your actual machine
- ✅ **Easy Deployment**: Open WebUI remains containerized for simple setup  
- ✅ **Best of Both Worlds**: Security isolation for the web UI, full power for the MCP tools
- ✅ **No Permission Issues**: No need to mount volumes or deal with container isolation

### Start Everything

```cmd
# Windows - Start all services
.\start-hybrid.bat start

# Check status of all services
.\start-hybrid.bat status

# Stop services
.\start-hybrid.bat stop
```

## 📋 Alternative Deployment Options

### **Manual/Native**
Run everything manually without the batch script:

```bash
# Terminal 1: MCP Server
uvx mcpo --port 8000 -- dotnet "mcp.server/bin/Release/net9.0/mcp.server.dll"

# Terminal 2: Open WebUI (optional)
docker-compose -f docker-compose.hybrid.yml up -d
```

## 📁 Project Structure

```
mcp-pixula/
├── mcp.server/             # MCP Server implementation
│   ├── Program.cs          # Server entry point and configuration
│   └── mcp.server.csproj
├── mcp.client/             # Interactive console client
│   ├── Program.cs          # Client with Ollama integration
│   └── mcp.client.csproj
├── mcp.tools/              # Tools library
│   ├── FileTool.cs         # File operations
│   ├── GitTool.cs          # Git repository management
│   ├── ProcessTool.cs      # Process and build management
│   ├── SearchTool.cs       # Code search and analysis
│   ├── ConfigTool.cs       # Configuration management
│   └── mcp.tools.csproj
├── start-hybrid.bat        # Windows hybrid deployment script
├── start-mcp-host.bat      # Manual MCP server startup script
├── docker-compose.hybrid.yml # Hybrid Docker compose (Open WebUI only)
└── mcp.sln                # Solution file
```

## 🛠️ Available MCP Tools

### 📄 File Management Tools

| Tool | Description |
|------|-------------|
| `read_file` | Reads the content of a local file |
| `write_file` | Writes content to a local file |
| `list_files` | Lists files in a directory with optional search patterns |
| `find_files` | Finds files by name pattern recursively |

### 🔍 Search & Analysis Tools

| Tool | Description |
|------|-------------|
| `search_in_files` | Searches for text content across multiple files with regex support |
| `find_functions` | Finds functions or methods containing specific text patterns |
| `find_todos` | Searches for TODO, FIXME, HACK, and other code comments |
| `get_project_stats` | Generates comprehensive project statistics (file counts, lines of code, etc.) |

### 🔧 Git Repository Tools

| Tool | Description |
|------|-------------|
| `get_git_status` | Gets git status of the repository |
| `get_git_log` | Gets recent git commits with customizable count |
| `get_current_branch` | Gets the current git branch |
| `get_git_diff` | Gets git diff for uncommitted changes |
| `get_git_blame` | Shows blame information for a specific file |
| `get_git_tracked_files` | Lists all git-tracked files |
| `show_commit` | Shows detailed commit information |
| `get_file_history` | Gets commit history for a specific file |

### ⚙️ Process & Build Management Tools

| Tool | Description |
|------|-------------|
| `run_tests` | Runs tests (auto-detects framework: .NET, npm, pytest, cargo, maven) |
| `build_project` | Builds the project (auto-detects build system) |
| `install_dependencies` | Installs project dependencies |
| `lint_project` | Runs linting/formatting tools with optional auto-fix |
| `get_development_processes` | Lists running development-related processes |
| `kill_processes_by_name` | Terminates processes by name |
| `monitor_process` | Monitors a process and waits for completion |

### 🔧 Configuration Management Tools

| Tool | Description |
|------|-------------|
| `get_environment_variables` | Gets environment variables with optional filtering |
| `set_environment_variable` | Sets an environment variable for the current process |
| `read_json_config` | Reads and parses JSON configuration files |
| `update_json_config` | Updates values in JSON configuration files |
| `find_config_files` | Finds configuration files in the project |
| `validate_json` | Validates JSON file syntax |
| `get_system_config` | Gets comprehensive system and runtime information |
| `backup_config_file` | Creates timestamped backups of configuration files |

## 🚀 Getting Started

### Prerequisites

- .NET 9.0 SDK
- Python 3.8+ (for mcpo)
- Docker Desktop (for containerized/hybrid deployments)
- **Ollama** (for AI model integration - automatically managed by the script)

### Installation & Setup

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd mcp-pixula
   ```

2. **Install Python dependencies:**
   ```bash
   pip install uv  # For running mcpo
   ```

3. **Start the hybrid deployment:**
   ```cmd
   # Windows
   .\start-hybrid.bat start
   ```

### Access Points

Once running, you can access:
- **Open WebUI**: http://localhost:3000 (with Ollama AI models)
- **MCP API Docs**: http://localhost:8000/docs
- **Ollama API**: http://localhost:11434 (AI model server)

## 🏗️ Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────┐
│                    Host Machine                          │
│                                                          │
│  ┌──────────────────────────────────────┐               │
│  │   Ollama Server (Port 11434)         │               │
│  │   - AI model hosting                 │               │
│  │   - Local LLM inference              │               │
│  │   - GPU acceleration                 │               │
│  └──────────────────────────────────────┘               │
│                      ↑                                   │
│                      │ http://host.docker.internal:11434 │
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
│  │   │  - Connects to MCP & Ollama│     │               │
│  │   │  - Chat with AI + tools    │     │               │
│  │   └────────────────────────────┘     │               │
│  └──────────────────────────────────────┘               │
└─────────────────────────────────────────────────────────┘
```

### Component Details

**MCP Server (`mcp.server`)**
- Built on Microsoft's MCP implementation
- Uses stdio transport for communication
- Automatically discovers and registers all tools from the `mcp.tools` assembly
- Provides structured logging and error handling

**MCP Client (`mcp.client`)**
- Interactive console client with rich UI using Spectre.Console
- Integrates with Ollama for AI-powered interactions
- Supports streaming responses with visual feedback
- Automatically discovers and utilizes all available MCP tools

**Tools Library (`mcp.tools`)**
- Modular design with tools organized by functionality
- Each tool class focuses on a specific domain (Git, Files, Process, etc.)
- Comprehensive error handling and validation
- Support for cross-platform operations

## 🎯 Key Features

### Intelligent Project Detection
- Automatically detects project types (.NET, Node.js, Python, Rust, Java, etc.)
- Chooses appropriate build/test/lint commands based on project structure
- Supports multiple project types in the same repository

### Advanced Search Capabilities
- Multi-file text search with regex support
- Function/method discovery across different programming languages
- Comprehensive TODO/FIXME/HACK comment analysis
- Project statistics and code metrics

### Git Integration
- Complete git workflow support
- File history tracking
- Blame information and commit analysis
- Status monitoring and diff visualization

### Configuration Management
- JSON configuration file manipulation
- Environment variable management
- Configuration file discovery and validation
- Automated backup creation

### Process Management
- Development process monitoring
- Build automation with timeout handling
- Test execution with framework detection
- Code formatting and linting automation

## 🔧 Configuration

### Client Configuration
The client can be configured by modifying `mcp.client/Program.cs`:
- Change the Ollama model: Update the `modelName` variable
- Modify server connection: Update the `StdioClientTransport` configuration

### Server Configuration
The server automatically discovers tools from the `mcp.tools` assembly. To add new tools:
1. Create a new static class in `mcp.tools`
2. Decorate it with `[McpServerToolType]`
3. Add static methods decorated with `[McpServerTool]` and `[Description]`

## 📝 Usage Examples

### Using with Claude Desktop

1. Add to your Claude Desktop MCP configuration:
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

### Using with Open WebUI

The hybrid deployment automatically configures Open WebUI to connect to both the MCP server and Ollama. Simply navigate to http://localhost:3000 after starting the services.

**Ollama Integration:**
- Open WebUI automatically connects to Ollama at http://localhost:11434
- The script checks and starts Ollama if needed
- Models available: Use any models you've installed with `ollama pull <model>`
- Example: `ollama pull llama3.2` to install Llama 3.2

### Example Prompts

- "Show me the current git status and recent commits"
- "Find all TODO comments in the project"
- "Search for functions containing 'authenticate' in all TypeScript files"
- "Get project statistics and show me the largest files"
- "Run the tests and lint the code"
- "Find all configuration files and backup the main app settings"

## 🔧 Troubleshooting

### MCP Server Issues

```cmd
# Check if MCP is running
.\start-hybrid.bat status

# Test MCP directly
curl http://localhost:8000/docs

# Check if port 8000 is in use
netstat -ano | findstr :8000
```

### Open WebUI Can't Connect to MCP

```cmd
# Verify host.docker.internal resolves from container
docker run --rm alpine ping -c 2 host.docker.internal

# Check MCP is accessible from container
docker run --rm curlimages/curl curl http://host.docker.internal:8000/docs
```

### Ollama Issues

```cmd
# Check Ollama status
ollama ps

# Start Ollama manually if needed
ollama serve

# Install a model if none available
ollama pull llama3.2
```

### Port Conflicts

```cmd
# Find what's using specific ports
netstat -ano | findstr :8000   # MCP Server
netstat -ano | findstr :3000   # Open WebUI  
netstat -ano | findstr :11434  # Ollama
```

## 🚨 Security Considerations

- **MCP server** runs on your host with full file system access
- For production use, always enable authentication in Open WebUI
- Consider using a reverse proxy with proper authentication for remote access
- The hybrid approach prioritizes functionality over isolation

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Add your tools or improvements
4. Ensure all tools follow the established patterns
5. Submit a pull request

## 📜 License

This project is provided as-is for educational and development purposes.

## 🙏 Acknowledgments

- Built using Microsoft's Model Context Protocol implementation
- Uses Ollama for AI integration
- UI powered by Spectre.Console for rich terminal experience
- Open WebUI for web-based interface
