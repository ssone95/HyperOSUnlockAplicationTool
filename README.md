# HyperOSUnlocker (HOSUnlocker)

A cross-platform .NET console application designed to automate the timing-critical process of applying for Xiaomi/HyperOS bootloader unlock permissions.

---

## ?? Disclaimer

> **IMPORTANT: Please read carefully before using this software.**

- This project is a **personal hobby project** developed for educational and personal use purposes only.
- **No affiliation**: This project has **NO affiliation, endorsement, or connection** with Xiaomi Corporation, Xiaomi Inc., or any of its subsidiaries, partners, or related entities.
- **No commercial purpose**: This software is provided free of charge. There are no expected earnings, financial gains, sponsorships, or monetization of any kind.
- **Use at your own risk**: The author(s) assume **NO responsibility or liability** for any damages, losses, account bans, device issues, warranty voidance, or any other consequences resulting from the use of this software.
- **No guarantees**: This software is provided "AS IS" without warranty of any kind, express or implied. Success is not guaranteed, and the unlock process depends entirely on Xiaomi's server-side decisions.
- **Legal compliance**: Users are solely responsible for ensuring their use of this software complies with all applicable laws, regulations, and terms of service in their jurisdiction.
- **Bootloader unlocking risks**: Unlocking your device's bootloader may void your warranty, compromise device security, and could potentially brick your device if done incorrectly.

**By using this software, you acknowledge that you have read, understood, and agree to this disclaimer.**

---

## ?? What is HOSUnlocker?

HOSUnlocker is a tool that helps automate the process of applying for Xiaomi/HyperOS bootloader unlock permissions at precise server-side timing windows. The Xiaomi unlock system operates on Beijing time, and this tool monitors clock thresholds to submit unlock requests at optimal moments.

### Key Features

- **Two Operation Modes**: Interactive TUI (Terminal User Interface) or Headless console mode
- **Cross-Platform**: Runs on Windows, Linux, and macOS
- **Docker Support**: Run in containers for 24/7 unattended operation
- **Precise Timing**: Uses NTP synchronization for accurate Beijing time calculations
- **Auto-Retry**: Configurable automatic retry attempts across multiple days
- **Multiple Tokens**: Support for multiple account tokens with configurable time shifts
- **Resilient**: Built-in retry policies for network failures using Polly

---

## ?? Why Does This Exist?

Xiaomi's bootloader unlock system requires:
1. Waiting a specific period (often 7+ days) after binding your device
2. Submitting an unlock request at the right server-side timing window
3. Multiple attempts may be needed due to server-side quotas and timing

This tool automates the tedious process of manually checking and submitting requests at precise times, especially useful when:
- You're in a different timezone than Beijing (UTC+8)
- You need to submit requests at inconvenient hours
- You want to run unattended retry attempts over multiple days

---

## ?? Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for building) or .NET 10 Runtime (for running)
- A valid Xiaomi account with a device bound for bootloader unlock
- Your account's authentication token(s)

### Configuration

1. Copy `appsettings.sample.json` to `appsettings.json`
2. Edit `appsettings.json` with your token(s):

```json
{
  "Tokens": [
    {
      "Index": 1,
      "Token": "YOUR_TOKEN_HERE"
    }
  ],
  "TokenShifts": [
    3200, 2500, 1900, 1400, 900, 400, 100,
    -100, -400, -900, -1400, -1900, -2500, -3200
  ],
  "AutoRunOnStart": false,
  "HeadlessMode": false,
  "MaxAutoRetries": 5,
  "MaxApiRetries": 3,
  "ApiRetryWaitTimeMs": 100,
  "MultiplyApiRetryWaitTimeByAttempt": true
}
```

### Building from Source

```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/HyperOSUnlocker.git
cd HyperOSUnlocker

# Build the project
dotnet build -c Release

# Run the application
dotnet run --project HOSUnlocker
```

---

## ?? Usage

### Operation Modes

#### 1. TUI Mode (Interactive)

The default mode with a terminal-based user interface powered by Terminal.Gui:

```bash
# Windows
dotnet run --project HOSUnlocker

# Or run the compiled executable
HOSUnlocker.exe          # Windows
./HOSUnlocker            # Linux/macOS
```

#### 2. Headless Mode (Console)

For servers, automation, or when TUI is not available:

```bash
# Run in headless mode
dotnet run --project HOSUnlocker -- --headless

# Headless with auto-run (no user prompts)
dotnet run --project HOSUnlocker -- --headless --auto-run
```

### Command-Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `--headless` | Run in headless mode (no TUI) | `false` |
| `--auto-run` | Automatically start monitoring without prompts | `false` |
| `--max-retries <n>` | Max auto-retry attempts (1-365) | `5` |
| `--max-api-retries <n>` | Max API/NTP retry attempts (0-10) | `3` |
| `--api-retry-wait <ms>` | Base retry wait time in ms (1-1000) | `100` |
| `--fixed-retry-wait` | Use fixed wait time instead of multiplying by attempt | `false` |
| `--help`, `-h` | Show help message | - |

### Examples

```bash
# Interactive TUI mode
HOSUnlocker

# Headless mode with auto-run (ideal for servers/containers)
HOSUnlocker --headless --auto-run

# Custom retry settings
HOSUnlocker --headless --auto-run --max-retries 10 --max-api-retries 5

# Override API retry timing
HOSUnlocker --headless --api-retry-wait 200 --fixed-retry-wait
```

---

## ??? Platform-Specific Instructions

### Windows

```powershell
# Option 1: Run with .NET CLI
dotnet run --project HOSUnlocker

# Option 2: Run compiled executable
.\HOSUnlocker.exe

# Option 3: Headless in background (PowerShell)
Start-Process -NoNewWindow -FilePath ".\HOSUnlocker.exe" -ArgumentList "--headless", "--auto-run"
```

### Linux

```bash
# Run with .NET CLI
dotnet run --project HOSUnlocker

# Run compiled executable
./HOSUnlocker

# Run headless in background with nohup
nohup ./HOSUnlocker --headless --auto-run > hosunlocker.log 2>&1 &

# Or use systemd service (recommended for long-running)
# Create /etc/systemd/system/hosunlocker.service
```

### macOS

```bash
# Run with .NET CLI
dotnet run --project HOSUnlocker

# Run compiled executable
./HOSUnlocker

# Run in background
nohup ./HOSUnlocker --headless --auto-run > hosunlocker.log 2>&1 &
```

---

## ?? Docker

HOSUnlocker includes Docker support for containerized deployment, ideal for running on servers or NAS devices.

### Building the Docker Image

```bash
# From the repository root
docker build -t hosunlocker -f HOSUnlocker/Dockerfile .
```

### Running with Docker

```bash
# Run with mounted configuration
docker run -d \
  --name hosunlocker \
  -v /path/to/your/appsettings.json:/app/appsettings.json:ro \
  -e TZ=Your/Timezone \
  hosunlocker

# View logs
docker logs -f hosunlocker
```

### Docker Compose Example

```yaml
version: '3.8'
services:
  hosunlocker:
    build:
      context: .
      dockerfile: HOSUnlocker/Dockerfile
    container_name: hosunlocker
    restart: unless-stopped
    environment:
      - TZ=Europe/London
    volumes:
      - ./appsettings.json:/app/appsettings.json:ro
```

> **Note**: When running in a container, the application automatically detects the container environment and forces headless mode.

---

## ?? Configuration Reference

### Configuration File (`appsettings.json`)

| Property | Type | Description |
|----------|------|-------------|
| `Tokens` | Array | Array of token objects with `Index` and `Token` properties |
| `TokenShifts` | Array | Time shifts in milliseconds for threshold calculations |
| `AutoRunOnStart` | Boolean | Start monitoring automatically without user prompts |
| `HeadlessMode` | Boolean | Run without TUI (console output only) |
| `MaxAutoRetries` | Integer | Maximum retry attempts across days (1-365) |
| `MaxApiRetries` | Integer | API call retry attempts (0-10) |
| `ApiRetryWaitTimeMs` | Integer | Base wait time between retries in ms (1-1000) |
| `MultiplyApiRetryWaitTimeByAttempt` | Boolean | Multiply wait time by attempt number |

### Token Shifts

Token shifts define the time offsets (in milliseconds) around the target unlock window. Multiple shifts increase the chances of hitting the correct server-side timing:

```json
"TokenShifts": [3200, 2500, 1900, 1400, 900, 400, 100, -100, -400, -900, -1400, -1900, -2500, -3200]
```

---

## ?? Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| "Configuration is invalid" | Ensure `appsettings.json` exists and contains valid token(s) |
| "Cookie expired or invalid" | Your token has expired; obtain a new one |
| TUI not displaying correctly | Try running in headless mode with `--headless` |
| Time sync issues | Ensure your system clock is accurate or NTP is accessible |

### Logs

Logs are output to the console and can be redirected to a file:

```bash
HOSUnlocker --headless --auto-run > hosunlocker.log 2>&1
```

---

## ?? License

This project is provided for personal and educational use. See the [LICENSE](../LICENSE) file for details.

---

## ?? Acknowledgments

This project uses the following open-source libraries:
- [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) - Terminal UI toolkit
- [Polly](https://github.com/App-vNext/Polly) - Resilience and transient fault handling
- [GuerrillaNtp](https://github.com/robertvazan/guerrillantp) - NTP client for .NET

---

**Remember**: This tool is provided as-is for personal use. Always ensure you understand the implications of unlocking your device's bootloader before proceeding.
