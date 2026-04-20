# HTTP Server & Local Llama Configuration

## Overview
Added HTTP server management capabilities and local Llama model configuration to CLI-Intelligence.

## Features Implemented

### 1. HTTP Server Management Screen
**Location**: `cli-intelligence/Screens/ServerScreen.cs`

**Capabilities**:
- Start/Stop HTTP server in background
- Configure server URL (default: http://localhost:5080)
- Configure service name
- Configure version
- View server status and available endpoints

**Available Endpoints**:
- `GET /` - Service identity card
- `GET /health` - Liveness probe
- `GET /ping` - Round-trip latency check
- `POST /echo` - JSON echo validation
- `GET /headers` - Request header inspection
- `GET /ip` - Client IP address

**Access**: Main Menu → "HTTP Server"

### 2. Local Llama Configuration
**Location**: `appsettings.json` → "Llama" section

**Configuration Options**:
```json
{
  "Llama": {
	"Enabled": false,
	"Url": "http://localhost:8080",
	"Model": "llama3",
	"ContextLength": 4096,
	"Temperature": 0.7,
	"TopP": 0.9,
	"MaxTokens": 2048,
	"TimeoutSeconds": 120
  }
}
```

**Parameters**:
- `Enabled` - Set to `true` to use local model instead of OpenRouter
- `Url` - HTTP endpoint of local Llama server (llama.cpp, Ollama, etc.)
- `Model` - Model name or path
- `ContextLength` - Maximum context window (tokens)
- `Temperature` - Sampling temperature (0.0 = deterministic, 1.0 = creative)
- `TopP` - Top-p sampling threshold
- `MaxTokens` - Maximum tokens to generate per response
- `TimeoutSeconds` - Request timeout

**Compatible Servers**:
- llama.cpp server
- Ollama
- Any OpenAI-compatible local LLM server

### 3. Updated Help Screen
**Location**: `cli-intelligence/Screens/HelpContent.cs`

Now includes documentation for:
- HTTP server capabilities and endpoints
- Local Llama model configuration instructions
- Setup requirements

## Usage

### Starting the HTTP Server
1. Launch CLI-Intelligence
2. Select "HTTP Server" from main menu
3. Configure URL/settings if needed
4. Select "Start Server"
5. Server runs in background until stopped

### Configuring Local Llama
1. Set up local Llama server (llama.cpp, Ollama, etc.)
2. Edit `appsettings.json`
3. Set `Llama.Enabled: true`
4. Configure `Llama.Url` to match your server
5. Adjust model parameters as needed
6. Restart CLI-Intelligence

## Technical Details

### Server Management
- Uses ASP.NET Core Kestrel (CreateSlimBuilder)
- Background execution via Task.Run with CancellationToken
- Graceful shutdown on stop
- Serilog integration for request logging

### Configuration Persistence
- Changes saved to both runtime and source `appsettings.json`
- Uses `AppSession.SaveConfig()` method
- Reflection-based property updates for server settings

### Server Settings Properties
Changed `ServerSection` properties from `{ get; init; }` to `{ get; set; }` to allow runtime modification.

## Files Modified
1. `cli-intelligence/Screens/ServerScreen.cs` (NEW)
2. `cli-intelligence/Models/AppConfig.cs` (MODIFIED - added LlamaSection, changed ServerSection properties)
3. `cli-intelligence/appsettings.json` (MODIFIED - added Llama configuration)
4. `cli-intelligence/Screens/RootMenuScreen.cs` (MODIFIED - added "HTTP Server" menu option)
5. `cli-intelligence/Screens/HelpContent.cs` (MODIFIED - added server/Llama documentation)

## Build Status
✅ Build successful - all changes compile without errors
