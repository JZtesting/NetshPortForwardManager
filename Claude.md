# NetSh Port Forward Manager

A Windows desktop application that provides a graphical interface for managing Windows netsh port forwarding rules, eliminating the need to use command-line interface for common port forwarding operations.

## Project Overview

### Purpose
Create a user-friendly GUI application that wraps Windows `netsh interface portproxy` commands, allowing users to easily manage port forwarding rules without memorizing command syntax.

### Target Platform
- Windows 10/11
- Requires Administrator privileges (netsh commands require elevation)

## Core Features

### 1. View Existing Rules
- Display all current port forwarding rules in a table/grid format
- Show: Listen Port, Listen Address, Connect Port, Connect Address, Protocol
- Refresh capability to reload current rules
- Search/filter functionality

### 2. Add New Rules
- Form-based interface for creating new port forwarding rules
- Input validation for IP addresses and port ranges
- Protocol selection (IPv4/IPv6)
- Preview command before execution

### 3. Remove Rules
- Select and delete existing rules
- Bulk delete capability
- Confirmation dialogs for safety

### 4. Edit Existing Rules
- Modify destination hosts/ports for existing rules
- Update listen addresses/ports
- Apply changes with validation

### 5. Rule Management
- Import/Export rule configurations
- Save rule templates for common setups
- Rule status monitoring (active/inactive)

## Technical Requirements

### Technology Stack Options

#### Option 1: Windows Forms (.NET)
```csharp
// Example structure
public class PortForwardRule
{
    public string ListenPort { get; set; }
    public string ListenAddress { get; set; }
    public string ConnectPort { get; set; }
    public string ConnectAddress { get; set; }
    public string Protocol { get; set; }
}
```

#### Option 2: WPF (.NET)
```xml
<!-- Modern UI with data binding -->
<DataGrid ItemsSource="{Binding PortForwardRules}" 
          AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Listen Port" 
                           Binding="{Binding ListenPort}"/>
        <!-- Additional columns -->
    </DataGrid.Columns>
</DataGrid>
```

#### Option 3: Electron + Web Technologies
```javascript
// Cross-platform option using web technologies
const { exec } = require('child_process');

class NetshManager {
    async getPortForwardRules() {
        return new Promise((resolve, reject) => {
            exec('netsh interface portproxy show all', 
                 (error, stdout, stderr) => {
                // Parse output and return structured data
            });
        });
    }
}
```

## Core NetSh Commands to Wrap

### View Rules
```batch
netsh interface portproxy show all
netsh interface portproxy show v4tov4
netsh interface portproxy show v4tov6
netsh interface portproxy show v6tov4
netsh interface portproxy show v6tov6
```

### Add Rule
```batch
netsh interface portproxy add v4tov4 listenport=<port> listenaddress=<address> connectport=<port> connectaddress=<address>
```

### Remove Rule
```batch
netsh interface portproxy delete v4tov4 listenport=<port> listenaddress=<address>
```

### Reset All Rules
```batch
netsh interface portproxy reset
```

## User Interface Design

### Main Window Layout
```
┌─────────────────────────────────────────────────────────┐
│ File  Edit  View  Tools  Help                          │
├─────────────────────────────────────────────────────────┤
│ [Add Rule] [Edit] [Delete] [Refresh] [Import] [Export]  │
├─────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────┐ │
│ │ Listen Port │ Listen Addr │ Connect Port │ Connect  │ │
│ │     8080    │   0.0.0.0   │     80       │ 10.0.0.1 │ │
│ │     3389    │  localhost  │     3389     │ 10.0.0.2 │ │
│ │     ...     │    ...      │     ...      │   ...    │ │
│ └─────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────┤
│ Status: Ready | Rules: 5 active                        │
└─────────────────────────────────────────────────────────┘
```

### Add/Edit Rule Dialog
```
┌─────────────────────────────────────┐
│ Add Port Forward Rule               │
├─────────────────────────────────────┤
│ Listen Port:    [____8080____]      │
│ Listen Address: [__0.0.0.0___]      │
│ Connect Port:   [_____80_____]      │
│ Connect Address:[_10.0.0.100_]      │
│ Protocol:       [v4tov4 ▼]          │
├─────────────────────────────────────┤
│ Preview: netsh interface portproxy  │
│ add v4tov4 listenport=8080...       │
├─────────────────────────────────────┤
│           [OK] [Cancel]             │
└─────────────────────────────────────┘
```

## Implementation Plan

### Phase 1: Core Functionality
1. Set up project structure and development environment
2. Implement netsh command execution wrapper
3. Create main window with rule display
4. Add basic CRUD operations (Create, Read, Update, Delete)

### Phase 2: Enhanced Features
1. Input validation and error handling
2. Rule templates and presets
3. Import/Export functionality
4. Configuration persistence

### Phase 3: Polish and Distribution
1. Administrator privilege handling
2. Installer creation
3. Documentation and help system
4. Testing on different Windows versions

## Security Considerations

### Administrator Requirements
- Application must run with elevated privileges
- Implement UAC prompt handling
- Validate all user inputs to prevent command injection

### Input Validation
```csharp
public bool ValidateIPAddress(string ip)
{
    return IPAddress.TryParse(ip, out _);
}

public bool ValidatePort(string port)
{
    return int.TryParse(port, out int p) && p > 0 && p <= 65535;
}
```

## Configuration and Data Storage

### Application Settings
- Window position and size
- Default values for new rules
- Recent rule templates
- Application preferences

### Rule Templates
```json
{
  "templates": [
    {
      "name": "Web Server Redirect",
      "listenPort": "8080",
      "listenAddress": "0.0.0.0",
      "connectPort": "80",
      "connectAddress": "10.0.0.100",
      "protocol": "v4tov4"
    }
  ]
}
```

## Error Handling

### Common Scenarios
- Insufficient privileges
- Port already in use
- Invalid IP addresses or ports
- Network interface not available
- Netsh command failures

### User Feedback
- Clear error messages with suggested solutions
- Status indicators for rule states
- Progress feedback for operations
- Logging for troubleshooting

## Testing Strategy

### Unit Tests
- Command parsing and validation
- IP address and port validation
- Rule data structure operations

### Integration Tests
- Netsh command execution
- Administrator privilege detection
- Windows version compatibility

### User Acceptance Tests
- Complete workflow testing
- Edge case handling
- Performance with large rule sets

## Distribution

### Packaging Options
1. **Standalone Executable**: Single .exe with all dependencies
2. **MSI Installer**: Professional installation experience
3. **Microsoft Store**: Modern distribution channel
4. **Portable Version**: No installation required

### System Requirements
- Windows 10 1809 or later
- .NET Framework 4.8 or .NET 6+ Runtime
- Administrator access
- Windows Firewall service running

## Future Enhancements

### Advanced Features
- Rule scheduling (temporary rules)
- Traffic monitoring and statistics
- Rule groups and categories
- Command-line interface for automation
- PowerShell module integration

### Integration Options
- Windows Service mode for persistent rules
- REST API for remote management
- Integration with Windows Admin Center
- Support for other port forwarding methods (SSH tunnels, etc.)

## Development Setup

### Prerequisites
```bash
# For .NET development
dotnet --version  # Verify .NET SDK installation
git clone <repository-url>
cd netsh-port-forward-manager
dotnet restore
dotnet build
```

### Project Structure
```
NetshPortForwardManager/
├── src/
│   ├── NetshPortForwardManager/
│   │   ├── Models/
│   │   │   └── PortForwardRule.cs
│   │   ├── Services/
│   │   │   └── NetshService.cs
│   │   ├── ViewModels/
│   │   │   └── MainViewModel.cs
│   │   └── Views/
│   │       └── MainWindow.xaml
│   └── NetshPortForwardManager.Tests/
├── docs/
├── installer/
└── README.md
```

This specification provides a complete roadmap for developing your netsh port forwarding GUI application. Choose the technology stack that best fits your development preferences and start with Phase 1 to build a working prototype.