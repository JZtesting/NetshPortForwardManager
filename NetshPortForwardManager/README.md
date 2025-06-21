# NetSh Port Forward Manager

A Windows WPF application that provides a graphical interface for managing Windows netsh port forwarding rules.

## Features

- View existing port forwarding rules in a table format
- Add new port forwarding rules with validation
- Edit existing rules
- Delete individual rules or reset all rules
- Real-time command preview
- Input validation for IP addresses and ports
- Administrator privilege handling

## Requirements

- Windows 10/11
- .NET 6.0 Runtime
- Administrator privileges (required for netsh commands)

## Building

1. Ensure you have .NET 6.0 SDK installed
2. Clone or download the project
3. Run the following commands:

```bash
dotnet restore
dotnet build
```

## Running

The application must be run with administrator privileges to execute netsh commands.

1. Build the project
2. Right-click on the executable and select "Run as administrator"
3. Or run from an elevated command prompt:

```bash
dotnet run
```

## Project Structure

```
NetshPortForwardManager/
├── Models/
│   ├── PortForwardRule.cs      # Data model for port forwarding rules
│   ├── ProtocolType.cs         # Enum for protocol types
│   └── NetshResult.cs          # Result wrapper for netsh operations
├── Services/
│   ├── NetshService.cs         # Service for executing netsh commands
│   └── ValidationService.cs    # Input validation utilities
├── ViewModels/
│   ├── MainViewModel.cs        # Main window view model
│   └── AddEditRuleViewModel.cs # Add/Edit dialog view model
├── Views/
│   ├── MainWindow.xaml         # Main application window
│   └── AddEditRuleDialog.xaml  # Add/Edit rule dialog
├── App.xaml                    # Application definition
└── app.manifest                # UAC elevation manifest
```

## Usage

1. **View Rules**: Current port forwarding rules are displayed in the main grid
2. **Add Rule**: Click "Add Rule" to create a new port forwarding rule
3. **Edit Rule**: Select a rule and click "Edit Rule" to modify it
4. **Delete Rule**: Select a rule and click "Delete Rule" to remove it
5. **Refresh**: Click "Refresh" to reload current rules from netsh
6. **Reset All**: Use "Edit" → "Reset All Rules" to remove all port forwarding rules

## Supported netsh Commands

The application wraps these netsh commands:

- `netsh interface portproxy show all` - View all rules
- `netsh interface portproxy add v4tov4` - Add IPv4 to IPv4 rule
- `netsh interface portproxy delete v4tov4` - Delete specific rule
- `netsh interface portproxy reset` - Reset all rules

## Security Notes

- The application requires administrator privileges
- All input is validated to prevent command injection
- Only supports standard netsh portproxy commands