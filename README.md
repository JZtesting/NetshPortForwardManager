# NetSh Port Forward Manager

A Windows WPF application that provides a graphical interface for managing Windows netsh port forwarding rules with import/export functionality.

## 🚀 Key Features

### Rule Management
- **View Rules**: Display all current port forwarding rules in an organized table
- **Add Rules**: Create new port forwarding rules with real-time validation
- **Edit Rules**: Modify existing rules with full validation
- **Delete Rules**: Remove individual rules or reset all rules at once
- **Auto-Load**: Automatically loads existing netsh rules on startup

### Import/Export
- **Export to JSON**: Save all current rules to a JSON file with metadata
- **Import from JSON**: Load rules from JSON files with duplicate detection
- **Backup & Restore**: Easy backup and restoration of rule configurations
- **Merge Options**: Choose to replace existing rules or add to current set

### User Experience
- **Real-time Validation**: IP addresses and port numbers validated as you type
- **Command Preview**: See the actual netsh command before execution
- **Status Updates**: Real-time feedback on all operations
- **Error Handling**: Clear error messages with helpful suggestions
- **Administrator Mode**: Handles UAC elevation automatically

## 📋 Requirements

- **OS**: Windows 10/11
- **Runtime**: .NET 6.0 Runtime
- **Privileges**: Administrator access (required for netsh commands)
- **Dependencies**: No additional software required

## 🎯 How to Use

### Basic Operations

1. **Launch**: Run as Administrator (UAC prompt will appear)
2. **View**: Current rules load automatically in the main table
3. **Add Rule**: 
   - Click "Add Rule" button
   - Fill in Listen Port, Listen Address, Forward Port, Forward Address
   - Select protocol type (IPv4 to IPv4 is most common)
   - Click OK to create the rule
4. **Edit Rule**: 
   - Select a rule from the table
   - Click "Edit Rule" button
   - Modify fields as needed
   - Click OK to apply changes
5. **Delete Rule**: 
   - Select a rule from the table
   - Click "Delete Rule" button
   - Confirm deletion

### Import/Export Rules

#### Export Rules
1. Click "Export" button or use File → Export Rules
2. Choose save location and filename
3. All current rules saved to JSON file with timestamp

#### Import Rules
1. Click "Import" button or use File → Import Rules
2. Select JSON file to import
3. Choose import mode:
   - **Replace**: Remove all existing rules and import new ones
   - **Add**: Keep existing rules and add imported ones (duplicates skipped)
4. Review import results

### Example JSON Format
```json
{
  "exportDate": "2024-06-21 10:30:00",
  "version": "1.0",
  "totalRules": 2,
  "rules": [
    {
      "listenPort": "8080",
      "listenAddress": "0.0.0.0",
      "forwardPort": "80", 
      "forwardAddress": "192.168.1.100",
      "protocol": "V4ToV4"
    }
  ]
}
```

## 🛠️ Common Use Cases

### Web Server Forwarding
- **Listen**: Port 8080 on all interfaces (0.0.0.0)
- **Forward**: To port 80 on internal server (192.168.1.100)
- **Use**: Access internal web server from external network

### Remote Desktop Forwarding  
- **Listen**: Port 3389 on localhost
- **Forward**: To port 3389 on target machine (192.168.1.200)
- **Use**: RDP through a jump host

### Development Server Access
- **Listen**: Port 3000 on all interfaces
- **Forward**: To port 3000 on development machine
- **Use**: Access local dev server from other devices

## ⚠️ Important Notes

### Security Considerations
- **Administrator Required**: All netsh commands require elevated privileges
- **Input Validation**: All IP addresses and ports are validated to prevent injection
- **Network Security**: Port forwarding can expose internal services - use carefully

### Troubleshooting
- **No Rules Loading**: Check if running as Administrator
- **Command Fails**: Verify Windows Firewall service is running
- **Import Errors**: Check JSON file format matches expected structure
- **Port Conflicts**: Ensure listen ports aren't already in use

## 🔧 Technical Details

### Supported Protocols
- **V4ToV4**: IPv4 to IPv4 (most common)
- **V4ToV6**: IPv4 to IPv6
- **V6ToV4**: IPv6 to IPv4  
- **V6ToV6**: IPv6 to IPv6

### NetSh Commands Used
```cmd
# View all rules
netsh interface portproxy show all

# Add rule
netsh interface portproxy add v4tov4 listenport=8080 listenaddress=0.0.0.0 connectport=80 connectaddress=192.168.1.100

# Delete rule  
netsh interface portproxy delete v4tov4 listenport=8080 listenaddress=0.0.0.0

# Reset all rules
netsh interface portproxy reset
```

### File Locations
- **Executable**: `bin/Debug/net6.0-windows/NetshPortForwardManager.exe`
- **Exports**: Default to user's Documents folder with timestamp
- **Configuration**: Stored in Windows netsh, not in files

## 🏗️ Building from Source

```bash
# Prerequisites: .NET 6.0 SDK installed
git clone <repository>
cd NetshPortForwardManager
dotnet restore
dotnet build

# Run (requires Administrator)
dotnet run
```

## 📄 License

This application provides a GUI wrapper for Windows netsh commands and requires appropriate Windows licensing for the underlying functionality.
