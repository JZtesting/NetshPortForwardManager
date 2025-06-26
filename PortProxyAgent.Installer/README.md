# PortProxy Agent Installer

This directory contains the Windows Installer (MSI) package for the PortProxy Agent service, designed for enterprise deployment across multiple Windows servers.

## Overview

The PortProxy Agent is a Windows service that enables remote management of network port forwarding rules via an encrypted HTTP API. The installer provides:

- **Automated Service Installation**: Installs and configures the agent as a Windows service
- **Configuration Management**: Interactive setup for agent configuration
- **Security**: Automatic secret key generation and secure communication setup
- **Central Management**: Optional integration with central management console
- **Enterprise Deployment**: Bulk deployment scripts for multiple servers

## Prerequisites

### Development Environment
- **WiX Toolset v3.11+**: Download from [https://wixtoolset.org/](https://wixtoolset.org/)
- **Visual Studio 2022**: With .NET 8.0 SDK
- **Windows SDK**: For signing tools (optional)

### Target Servers
- **Windows Server 2019+** or **Windows 10/11**
- **.NET 8.0 Runtime**: Will be installed if missing
- **Administrator Privileges**: Required for netsh commands and service installation
- **Network Access**: Port 8080 (default) must be available

## Building the Installer

### Method 1: PowerShell Script (Recommended)
```powershell
.\build-installer.ps1 -Configuration Release -Version "1.0.0"
```

### Method 2: Manual Build
```bash
# Build the agent first
dotnet build ..\PortProxyAgent\PortProxyAgent.csproj -c Release

# Build the installer
msbuild PortProxyAgent.Installer.wixproj /p:Configuration=Release
```

## Installation Options

### Interactive Installation
Double-click `PortProxyAgent.msi` and follow the setup wizard:

1. **License Agreement**: Accept the license terms
2. **Agent Configuration**: Configure agent settings
   - Agent Name (defaults to computer name)
   - Port (default: 8080)
   - Secret Key (auto-generated or custom)
   - Central Manager URL (optional)
   - Environment (Production/Staging/Development)
   - Silo ID (for failover grouping)
   - Auto-register option
3. **Installation**: Complete the installation

### Silent Installation
```cmd
msiexec /i PortProxyAgent.msi /quiet ^
  AGENT_NAME="ServerName" ^
  AGENT_PORT="8080" ^
  SECRET_KEY="YourSecretKey" ^
  CENTRAL_MANAGER_URL="http://manager.company.com" ^
  ENVIRONMENT="Production" ^
  SILO_ID="Silo1" ^
  AUTO_REGISTER="true"
```

### Bulk Deployment
```powershell
.\deploy-agent.ps1 -TargetServers @("server1", "server2", "server3") ^
  -CentralManagerUrl "http://manager.company.com" ^
  -Environment "Production" ^
  -SiloId "Silo1" ^
  -AutoRegister $true
```

## Configuration Parameters

| Parameter | Description | Default | Required |
|-----------|-------------|---------|----------|
| `AGENT_NAME` | Unique agent identifier | Computer name | Yes |
| `AGENT_PORT` | HTTP listener port | 8080 | Yes |
| `SECRET_KEY` | Encryption key (32+ chars) | Auto-generated | Yes |
| `CENTRAL_MANAGER_URL` | Manager console URL | None | No |
| `ENVIRONMENT` | Environment tag | Production | No |
| `SILO_ID` | Failover group ID | None | No |
| `AUTO_REGISTER` | Auto-register with manager | false | No |

## Service Management

### Service Control
```cmd
# Start service
net start PortProxyAgent

# Stop service
net stop PortProxyAgent

# Restart service
net stop PortProxyAgent && net start PortProxyAgent
```

### Configuration Files
- **Install Directory**: `C:\Program Files\PortProxy\Agent\`
- **Main Config**: `appsettings.json`
- **Environment Config**: `appsettings.Production.json`
- **Logs**: Windows Event Log (Application)

### Health Check
```powershell
# Test agent health
Invoke-RestMethod -Uri "http://localhost:8080/health"

# Expected response:
# {
#   "status": "PortProxy Agent is running",
#   "name": "ServerName",
#   "port": 8080,
#   "timestamp": "2025-06-25T20:00:00Z"
# }
```

## Security Considerations

### Secret Key Management
- **Auto-Generation**: Installer generates cryptographically secure keys
- **Key Length**: Minimum 32 characters required
- **Storage**: Keys stored in encrypted configuration
- **Rotation**: Manual key rotation supported

### Network Security
- **HTTP Only**: Agent uses HTTP for internal network communication
- **Encryption**: All command payloads encrypted with AES-256
- **Authentication**: HMAC-SHA256 message authentication
- **Firewall**: Configure Windows Firewall for agent port

### Access Control
- **Service Account**: Runs as Local System (required for netsh)
- **File Permissions**: Config files protected by NTFS permissions
- **Registry**: Agent registration stored in protected registry keys

## Troubleshooting

### Common Issues

**Service Won't Start**
- Check Windows Event Log for error details
- Verify port is not in use: `netstat -an | findstr :8080`
- Ensure .NET 8.0 runtime is installed
- Verify administrator privileges

**Configuration Problems**
- Validate JSON syntax in config files
- Check secret key length (minimum 32 chars)
- Verify port number is valid (1-65535)

**Network Connectivity**
- Test port accessibility: `telnet server 8080`
- Check Windows Firewall rules
- Verify network routing

**Central Manager Registration**
- Check manager URL accessibility
- Verify secret key matches between agent and manager
- Review network proxy settings

### Log Locations
- **Windows Event Log**: Application → PortProxyAgent
- **Debug Logs**: Enable in `appsettings.json` (Development only)

### Support Commands
```powershell
# Check service status
Get-Service PortProxyAgent

# View recent events
Get-EventLog -LogName Application -Source PortProxyAgent -Newest 10

# Test configuration
& "C:\Program Files\PortProxy\Agent\PortProxyAgent.exe" --validate-config

# Manual registration (if auto-register failed)
& "C:\Program Files\PortProxy\Agent\PortProxyAgent.exe" --register
```

## Uninstallation

### Interactive Uninstall
- Control Panel → Programs → PortProxy Agent → Uninstall

### Silent Uninstall
```cmd
msiexec /x {ProductGUID} /quiet
```

### Complete Removal
```powershell
# Stop and remove service
sc stop PortProxyAgent
sc delete PortProxyAgent

# Remove files (if needed)
Remove-Item "C:\Program Files\PortProxy\Agent" -Recurse -Force

# Remove registry entries
Remove-Item "HKLM:\SOFTWARE\PortProxy\Agent" -Recurse -Force
```

## Enterprise Deployment Guide

### Deployment Architecture
```
Central Manager
     │
     ├── Production Silo 1
     │   ├── Agent Server A (Active)
     │   └── Agent Server B (Passive)
     │
     └── Production Silo 2
         ├── Agent Server C (Active)
         └── Agent Server D (Passive)
```

### Best Practices
1. **Standardized Configuration**: Use consistent naming and port assignments
2. **Secret Key Management**: Generate unique keys per environment
3. **Testing**: Deploy to staging environment first
4. **Monitoring**: Set up health check monitoring
5. **Documentation**: Maintain inventory of deployed agents

### Automation Examples

**Group Policy Deployment**
Create a GPO to deploy the MSI package across multiple servers automatically.

**PowerShell DSC Configuration**
```powershell
Configuration PortProxyAgentConfig {
    Import-DscResource -ModuleName PSDesiredStateConfiguration
    
    Package PortProxyAgent {
        Name = "PortProxy Agent"
        Path = "\\deploy\share\PortProxyAgent.msi"
        ProductId = "{ProductGUID}"
        Arguments = "AGENT_PORT=8080 ENVIRONMENT=Production"
    }
}
```

**Ansible Playbook**
```yaml
- name: Deploy PortProxy Agent
  win_package:
    path: \\deploy\share\PortProxyAgent.msi
    arguments: 
      - AGENT_PORT=8080
      - ENVIRONMENT=Production
      - AUTO_REGISTER=true
    state: present
```

## Version History

- **v1.0.0**: Initial release with basic installer functionality
- **v1.1.0**: Added central manager integration
- **v1.2.0**: Enhanced security and bulk deployment tools

## Support

For technical support and questions:
- **Documentation**: Check project README and wiki
- **Issues**: Submit bug reports via GitHub issues
- **Enterprise Support**: Contact system administrator

---

*PortProxy Agent Installer - Enterprise Network Management Solution*