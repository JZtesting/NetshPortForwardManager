using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using PortProxyClient.Commands;
using PortProxyClient.Models;
using PortProxyClient.Services;

namespace PortProxyClient.ViewModels
{
    public class ServerManagementViewModel : INotifyPropertyChanged
    {
        private readonly ServerService _serverService;
        private readonly DnsService _dnsService;
        private readonly IAgentCommunicationService _agentCommunicationService;
        
        private int _selectedTabIndex = 0;
        private AgentServer? _selectedAgentServer;
        private TargetServer? _selectedTargetServer;
        private Silo? _selectedSilo;
        private AgentServer _editingAgentServer;
        private TargetServer _editingTargetServer;
        private Silo _editingSilo;
        private string _statusText = "Ready";
        private string _agentServerValidationMessage = string.Empty;
        private string _targetServerValidationMessage = string.Empty;
        private string _siloValidationMessage = string.Empty;
        private bool _isPerformingHealthCheck = false;
        private TargetServerPairingInfo? _selectedPairTargetServer;
        private string _currentPairStatus = string.Empty;
        

        public ServerManagementViewModel(ServerService serverService, DnsService dnsService, IAgentCommunicationService agentCommunicationService)
        {
            _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
            _dnsService = dnsService ?? throw new ArgumentNullException(nameof(dnsService));
            _agentCommunicationService = agentCommunicationService ?? throw new ArgumentNullException(nameof(agentCommunicationService));
            
            _editingAgentServer = CreateNewAgentServer();
            _editingTargetServer = CreateNewTargetServer();
            _editingSilo = CreateNewSilo();
            
            // Setup PropertyChanged events for editing objects
            _editingAgentServer.PropertyChanged += EditingAgentServer_PropertyChanged;
            _editingTargetServer.PropertyChanged += EditingTargetServer_PropertyChanged;
            _editingSilo.PropertyChanged += (s, e) => { ValidateSilo(); CommandManager.InvalidateRequerySuggested(); };
            
            // Legacy support
            
            // Initialize Agent Server commands
            NewAgentServerCommand = new RelayCommand(() => ClearAgentServerForm());
            SaveAgentServerCommand = new AsyncRelayCommand(async () => await SaveAgentServerAsync(), () => CanSaveAgentServer());
            DeleteAgentServerCommand = new AsyncRelayCommand(async () => await DeleteAgentServerAsync(), () => CanDeleteAgentServer());
            TestAgentConnectionCommand = new AsyncRelayCommand(async () => await TestAgentConnectionAsync(), () => CanTestAgentConnection());
            GenerateSecretKeyCommand = new RelayCommand(() => GenerateSecretKey());
            ConfigureFailoverCommand = new RelayCommand(() => ConfigureFailover(), () => CanConfigureFailover());
            
            // Initialize Target Server commands
            NewTargetServerCommand = new RelayCommand(() => ClearTargetServerForm());
            SaveTargetServerCommand = new AsyncRelayCommand(async () => await SaveTargetServerAsync(), () => CanSaveTargetServer());
            DeleteTargetServerCommand = new AsyncRelayCommand(async () => await DeleteTargetServerAsync(), () => CanDeleteTargetServer());
            ResolveDnsCommand = new AsyncRelayCommand(async () => await ResolveDnsAsync(), () => CanResolveDns());
            CreateFailoverPairCommand = new AsyncRelayCommand(async () => await CreateOrRemoveFailoverPairAsync(), () => CanCreateOrRemoveFailoverPair());
            
            
            // Initialize Silo commands
            NewSiloCommand = new RelayCommand(() => ClearSiloForm());
            SaveSiloCommand = new AsyncRelayCommand(async () => await SaveSiloAsync(), () => CanSaveSilo());
            DeleteSiloCommand = new AsyncRelayCommand(async () => await DeleteSiloAsync(), () => CanDeleteSilo());
            
            ExportConfigCommand = new AsyncRelayCommand(async () => await ExportConfigAsync());
            ImportConfigCommand = new AsyncRelayCommand(async () => await ImportConfigAsync());
            
            ValidateAgentServer();
            ValidateTargetServer();
            ValidateSilo();
        }

        #region Properties

        // New collections
        public ObservableCollection<AgentServer> AgentServers => _serverService.AgentServers;
        public ObservableCollection<TargetServer> TargetServers => _serverService.TargetServers;
        public ObservableCollection<Silo> Silos => _serverService.Silos;
        

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }

        // AgentServer properties
        public AgentServer? SelectedAgentServer
        {
            get => _selectedAgentServer;
            set
            {
                _selectedAgentServer = value;
                OnPropertyChanged();
                if (value != null)
                    LoadAgentServerForEditing(value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public TargetServer? SelectedTargetServer
        {
            get => _selectedTargetServer;
            set
            {
                _selectedTargetServer = value;
                OnPropertyChanged();
                if (value != null)
                    LoadTargetServerForEditing(value);
                CommandManager.InvalidateRequerySuggested();
            }
        }


        public Silo? SelectedSilo
        {
            get => _selectedSilo;
            set
            {
                _selectedSilo = value;
                OnPropertyChanged();
                if (value != null)
                    LoadSiloForEditing(value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public AgentServer EditingAgentServer
        {
            get => _editingAgentServer;
            set
            {
                _editingAgentServer = value;
                OnPropertyChanged();
            }
        }
        
        public TargetServer EditingTargetServer
        {
            get => _editingTargetServer;
            set
            {
                _editingTargetServer = value;
                OnPropertyChanged();
            }
        }


        public Silo EditingSilo
        {
            get => _editingSilo;
            set
            {
                _editingSilo = value;
                OnPropertyChanged();
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public string AgentServerValidationMessage
        {
            get => _agentServerValidationMessage;
            set
            {
                _agentServerValidationMessage = value;
                OnPropertyChanged();
            }
        }
        
        public string TargetServerValidationMessage
        {
            get => _targetServerValidationMessage;
            set
            {
                _targetServerValidationMessage = value;
                OnPropertyChanged();
            }
        }


        public string SiloValidationMessage
        {
            get => _siloValidationMessage;
            set
            {
                _siloValidationMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsPerformingHealthCheck
        {
            get => _isPerformingHealthCheck;
            set
            {
                _isPerformingHealthCheck = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public TargetServerPairingInfo? SelectedPairTargetServer
        {
            get => _selectedPairTargetServer;
            set
            {
                _selectedPairTargetServer = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string CurrentPairStatus
        {
            get => _currentPairStatus;
            set
            {
                _currentPairStatus = value;
                OnPropertyChanged();
            }
        }

        public List<TargetServerPairingInfo> AvailableTargetServersForPairing
        {
            get
            {
                if (SelectedTargetServer == null)
                    return new List<TargetServerPairingInfo>();

                // Return all target servers except the currently selected one with pairing info
                return TargetServers
                    .Where(ts => ts.Id != SelectedTargetServer.Id)
                    .Select(ts => new TargetServerPairingInfo
                    {
                        Server = ts,
                        IsPaired = _serverService.IsServerPaired(ts.Id),
                        PairedWith = _serverService.GetPairedServer(ts.Id)?.Name ?? string.Empty
                    })
                    .ToList();
            }
        }

        public bool IsCurrentServerPaired => SelectedTargetServer != null && _serverService.IsServerPaired(SelectedTargetServer.Id);

        public string PairButtonText => IsCurrentServerPaired ? "Unpair" : "Pair";

        public List<SiloRole> AvailableRoles => Enum.GetValues<SiloRole>().ToList();
        public List<string> AvailableEnvironments => new() { "Development", "Test", "Staging", "Production" };

        #endregion

        #region Commands

        // AgentServer commands
        public ICommand NewAgentServerCommand { get; }
        public ICommand SaveAgentServerCommand { get; }
        public ICommand DeleteAgentServerCommand { get; }
        public ICommand TestAgentConnectionCommand { get; }
        public ICommand GenerateSecretKeyCommand { get; }
        public ICommand ConfigureFailoverCommand { get; }
        
        // TargetServer commands
        public ICommand NewTargetServerCommand { get; }
        public ICommand SaveTargetServerCommand { get; }
        public ICommand DeleteTargetServerCommand { get; }
        public ICommand ResolveDnsCommand { get; }
        public ICommand CreateFailoverPairCommand { get; }
        
        // Silo commands
        public ICommand NewSiloCommand { get; }
        public ICommand SaveSiloCommand { get; }
        public ICommand DeleteSiloCommand { get; }
        
        
        // Utility commands
        public ICommand ExportConfigCommand { get; }
        public ICommand ImportConfigCommand { get; }

        #endregion


        #region Silo Management

        private async Task SaveSiloAsync()
        {
            try
            {
                bool success;
                string action;
                
                if (SelectedSilo == null) // Adding new silo
                {
                    StatusText = "Adding silo...";
                    var newSilo = CloneSilo(EditingSilo);
                    success = await _serverService.AddSiloAsync(newSilo);
                    action = "added";
                    if (success)
                    {
                        ClearSiloForm(); // Clear form after successful add
                    }
                }
                else // Updating existing silo
                {
                    StatusText = "Updating silo...";
                    var updatedSilo = CloneSilo(EditingSilo);
                    updatedSilo.Id = SelectedSilo.Id;
                    success = await _serverService.UpdateSiloAsync(updatedSilo);
                    action = "updated";
                }
                
                StatusText = success 
                    ? $"Silo '{EditingSilo.Name}' {action} successfully" 
                    : $"Failed to {action.TrimEnd('d')} silo";
            }
            catch (Exception ex)
            {
                StatusText = $"Error saving silo: {ex.Message}";
            }
        }

        private async Task DeleteSiloAsync()
        {
            try
            {
                if (SelectedSilo == null) return;
                
                var siloName = SelectedSilo.Name; // Store name before deletion
                StatusText = "Deleting silo...";
                var success = await _serverService.DeleteSiloAsync(SelectedSilo.Id);
                
                if (success)
                {
                    StatusText = $"Silo '{siloName}' deleted successfully";
                    SelectedSilo = null;
                    EditingSilo = CreateNewSilo();
                    ValidateSilo();
                }
                else
                {
                    StatusText = "Failed to delete silo";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error deleting silo: {ex.Message}";
            }
        }

        #endregion

        #region Import/Export

        private async Task ExportConfigAsync()
        {
            try
            {
                // This would open a save file dialog in the view
                StatusText = "Export functionality requires file dialog implementation in view";
            }
            catch (Exception ex)
            {
                StatusText = $"Error exporting configuration: {ex.Message}";
            }
        }

        private async Task ImportConfigAsync()
        {
            try
            {
                // This would open an open file dialog in the view
                StatusText = "Import functionality requires file dialog implementation in view";
            }
            catch (Exception ex)
            {
                StatusText = $"Error importing configuration: {ex.Message}";
            }
        }

        #endregion

        #region Command CanExecute Methods

        private bool CanSaveSilo() => !string.IsNullOrEmpty(SiloValidationMessage) == false;
        private bool CanDeleteSilo() => SelectedSilo != null;

        #endregion

        #region Validation


        private void ValidateSilo()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(EditingSilo.Name))
                errors.Add("Silo name is required");

            SiloValidationMessage = string.Join("; ", errors);
        }

        #endregion

        #region Helper Methods


        private Silo CreateNewSilo() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Role = SiloRole.Unknown,
            Status = SiloStatus.Unknown
        };


        private void LoadSiloForEditing(Silo silo)
        {
            EditingSilo = CloneSilo(silo);
            ValidateSilo();
        }

        private void ClearSiloForm()
        {
            // Clear the existing instance to maintain property bindings
            EditingSilo.Id = Guid.NewGuid().ToString();
            EditingSilo.Name = string.Empty;
            EditingSilo.Description = string.Empty;
            EditingSilo.DataCenter = string.Empty;
            EditingSilo.Role = SiloRole.Unknown;
            EditingSilo.Status = SiloStatus.Unknown;
            SelectedSilo = null; // Clear selection to ensure we're in "add" mode
            ValidateSilo();
        }


        private static Silo CloneSilo(Silo original) => new()
        {
            Id = original.Id,
            Name = original.Name,
            Description = original.Description,
            DataCenter = original.DataCenter,
            Role = original.Role,
            Status = original.Status,
            LastHealthCheck = original.LastHealthCheck,
            Properties = new Dictionary<string, string>(original.Properties),
            Environments = new List<string>(original.Environments)
        };

        #endregion

        #region AgentServer Helper Methods

        private AgentServer CreateNewAgentServer()
        {
            return new AgentServer
            {
                Id = Guid.NewGuid().ToString(),
                Name = string.Empty,
                AgentUrl = "http://",
                SecretKey = string.Empty,
                Description = string.Empty,
                Environment = "Test",
                Status = AgentStatus.Disconnected
            };
        }

        private void LoadAgentServerForEditing(AgentServer agentServer)
        {
            // Clone the server data into a completely separate object to avoid reference issues
            var newEditingServer = new AgentServer
            {
                Id = agentServer.Id,
                Name = agentServer.Name,
                AgentUrl = agentServer.AgentUrl,
                SecretKey = agentServer.SecretKey,
                Description = agentServer.Description,
                Environment = agentServer.Environment,
                SiloId = agentServer.SiloId,
                Status = agentServer.Status
            };

            // Disconnect old PropertyChanged events and connect new ones
            if (_editingAgentServer != null)
            {
                _editingAgentServer.PropertyChanged -= EditingAgentServer_PropertyChanged;
            }

            _editingAgentServer = newEditingServer;
            _editingAgentServer.PropertyChanged += EditingAgentServer_PropertyChanged;
            
            OnPropertyChanged(nameof(EditingAgentServer));
            ValidateAgentServer();
        }

        private void EditingAgentServer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            ValidateAgentServer();
            CommandManager.InvalidateRequerySuggested();
        }

        private void ValidateAgentServer()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(EditingAgentServer.Name))
                errors.Add("Agent server name is required");

            if (string.IsNullOrWhiteSpace(EditingAgentServer.AgentUrl) || !Uri.IsWellFormedUriString(EditingAgentServer.AgentUrl, UriKind.Absolute))
                errors.Add("Valid agent URL is required");

            if (string.IsNullOrWhiteSpace(EditingAgentServer.SecretKey))
                errors.Add("Secret key is required");

            AgentServerValidationMessage = string.Join("; ", errors);
        }

        private void GenerateSecretKey()
        {
            EditingAgentServer.SecretKey = Guid.NewGuid().ToString("N");
        }

        private void ClearAgentServerForm()
        {
            // Create a completely new instance to ensure clean state
            var newEditingServer = CreateNewAgentServer();

            // Disconnect old PropertyChanged events and connect new ones
            if (_editingAgentServer != null)
            {
                _editingAgentServer.PropertyChanged -= EditingAgentServer_PropertyChanged;
            }

            _editingAgentServer = newEditingServer;
            _editingAgentServer.PropertyChanged += EditingAgentServer_PropertyChanged;
            
            SelectedAgentServer = null; // Clear selection to ensure we're in "add" mode
            OnPropertyChanged(nameof(EditingAgentServer));
            ValidateAgentServer();
        }

        // AgentServer CRUD operations
        private async Task SaveAgentServerAsync()
        {
            try
            {
                bool success;
                string action;
                
                if (SelectedAgentServer == null) // Adding new server
                {
                    success = await _serverService.AddAgentServerAsync(EditingAgentServer);
                    action = "added";
                    if (success)
                    {
                        ClearAgentServerForm(); // Clear form after successful add
                    }
                }
                else // Updating existing server
                {
                    success = await _serverService.UpdateAgentServerAsync(EditingAgentServer);
                    action = "updated";
                }
                
                StatusText = success 
                    ? $"Agent server '{EditingAgentServer.Name}' {action} successfully" 
                    : $"Failed to {action.TrimEnd('d')} agent server";
            }
            catch (Exception ex)
            {
                StatusText = $"Error saving agent server: {ex.Message}";
            }
        }

        private async Task DeleteAgentServerAsync()
        {
            try
            {
                if (SelectedAgentServer == null) return;
                
                var agentName = SelectedAgentServer.Name;
                var success = await _serverService.DeleteAgentServerAsync(SelectedAgentServer.Id);
                
                if (success)
                {
                    StatusText = $"Agent server '{agentName}' deleted successfully";
                    SelectedAgentServer = null;
                    ClearAgentServerForm();
                }
                else
                {
                    StatusText = "Failed to delete agent server";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error deleting agent server: {ex.Message}";
            }
        }

        private async Task TestAgentConnectionAsync()
        {
            try
            {
                if (SelectedAgentServer == null) return;
                
                StatusText = "Testing agent connection...";
                var success = await _serverService.TestAgentConnectionAsync(SelectedAgentServer.Id);
                StatusText = success ? "Agent connection test successful" : "Agent connection test failed";
            }
            catch (Exception ex)
            {
                StatusText = $"Agent connection test error: {ex.Message}";
            }
        }

        // Command Can Execute methods for AgentServer
        private bool CanSaveAgentServer() => string.IsNullOrEmpty(AgentServerValidationMessage);
        private bool CanDeleteAgentServer() => SelectedAgentServer != null;
        private bool CanTestAgentConnection() => SelectedAgentServer != null;

        #endregion

        #region TargetServer Helper Methods

        private TargetServer CreateNewTargetServer()
        {
            return new TargetServer
            {
                Id = Guid.NewGuid().ToString(),
                Name = string.Empty,
                DnsName = string.Empty,
                Description = string.Empty,
                Environment = "Test",
                IsDnsResolved = false
            };
        }

        private void LoadTargetServerForEditing(TargetServer targetServer)
        {
            // Clone the server data into a completely separate object to avoid reference issues
            var newEditingServer = new TargetServer
            {
                Id = targetServer.Id,
                Name = targetServer.Name,
                DnsName = targetServer.DnsName,
                IpAddress = targetServer.IpAddress,
                Description = targetServer.Description,
                Environment = targetServer.Environment,
                IsDnsResolved = targetServer.IsDnsResolved,
                LastDnsCheck = targetServer.LastDnsCheck
            };

            // Disconnect old PropertyChanged events and connect new ones
            if (_editingTargetServer != null)
            {
                _editingTargetServer.PropertyChanged -= EditingTargetServer_PropertyChanged;
            }

            _editingTargetServer = newEditingServer;
            _editingTargetServer.PropertyChanged += EditingTargetServer_PropertyChanged;
            
            OnPropertyChanged(nameof(EditingTargetServer));
            
            // Update pairing status
            UpdatePairingStatus();
            UpdatePairingProperties();
            
            ValidateTargetServer();
        }

        private void EditingTargetServer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            ValidateTargetServer();
            CommandManager.InvalidateRequerySuggested();
        }

        private void ValidateTargetServer()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(EditingTargetServer.Name))
                errors.Add("Target server name is required");

            if (string.IsNullOrWhiteSpace(EditingTargetServer.DnsName))
                errors.Add("DNS name or IP address is required");

            TargetServerValidationMessage = string.Join("; ", errors);
        }

        private void ClearTargetServerForm()
        {
            // Create a completely new instance to ensure clean state
            var newEditingServer = CreateNewTargetServer();

            // Disconnect old PropertyChanged events and connect new ones
            if (_editingTargetServer != null)
            {
                _editingTargetServer.PropertyChanged -= EditingTargetServer_PropertyChanged;
            }

            _editingTargetServer = newEditingServer;
            _editingTargetServer.PropertyChanged += EditingTargetServer_PropertyChanged;
            
            SelectedTargetServer = null; // Clear selection to ensure we're in "add" mode
            OnPropertyChanged(nameof(EditingTargetServer));
            
            // Clear pairing selection and update status
            SelectedPairTargetServer = null;
            UpdatePairingStatus();
            UpdatePairingProperties();
            
            ValidateTargetServer();
        }

        // TargetServer CRUD operations
        private async Task SaveTargetServerAsync()
        {
            try
            {
                bool success;
                string action;
                
                if (SelectedTargetServer == null) // Adding new server
                {
                    success = await _serverService.AddTargetServerAsync(EditingTargetServer);
                    action = "added";
                    if (success)
                    {
                        ClearTargetServerForm(); // Clear form after successful add
                    }
                }
                else // Updating existing server
                {
                    success = await _serverService.UpdateTargetServerAsync(EditingTargetServer);
                    action = "updated";
                }
                
                StatusText = success 
                    ? $"Target server '{EditingTargetServer.Name}' {action} successfully" 
                    : $"Failed to {action.TrimEnd('d')} target server";
            }
            catch (Exception ex)
            {
                StatusText = $"Error saving target server: {ex.Message}";
            }
        }

        private async Task DeleteTargetServerAsync()
        {
            try
            {
                if (SelectedTargetServer == null) return;
                
                var targetName = SelectedTargetServer.Name;
                var success = await _serverService.DeleteTargetServerAsync(SelectedTargetServer.Id);
                
                if (success)
                {
                    StatusText = $"Target server '{targetName}' deleted successfully";
                    SelectedTargetServer = null;
                    ClearTargetServerForm();
                }
                else
                {
                    StatusText = "Failed to delete target server";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error deleting target server: {ex.Message}";
            }
        }

        private async Task ResolveDnsAsync()
        {
            try
            {
                if (SelectedTargetServer == null) return;
                
                StatusText = "Resolving DNS...";
                var result = await _dnsService.ResolveDnsAsync(SelectedTargetServer.DnsName);
                
                if (result.Success)
                {
                    SelectedTargetServer.IpAddress = result.ResolvedIp;
                    SelectedTargetServer.IsDnsResolved = true;
                    SelectedTargetServer.LastDnsCheck = DateTime.Now;
                    StatusText = $"DNS resolved: {result.ResolvedIp}";
                    LoadTargetServerForEditing(SelectedTargetServer); // Refresh editing form
                }
                else
                {
                    StatusText = $"DNS resolution failed: {result.Error}";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"DNS resolution error: {ex.Message}";
            }
        }

        // Command Can Execute methods for TargetServer
        private bool CanSaveTargetServer() => string.IsNullOrEmpty(TargetServerValidationMessage);
        private bool CanDeleteTargetServer() => SelectedTargetServer != null;
        private bool CanResolveDns() => SelectedTargetServer != null && !string.IsNullOrWhiteSpace(SelectedTargetServer.DnsName);

        #endregion

        #region Server Pairing Methods

        private async Task CreateOrRemoveFailoverPairAsync()
        {
            try
            {
                if (SelectedTargetServer == null)
                    return;

                // If server is paired, unpair it
                if (_serverService.IsServerPaired(SelectedTargetServer.Id))
                {
                    var serverPair = _serverService.GetServerPair(SelectedTargetServer.Id);
                    if (serverPair != null)
                    {
                        var success = await _serverService.DeleteServerPairAsync(serverPair.Id);
                        if (success)
                        {
                            StatusText = $"Server unpaired successfully";
                            SelectedPairTargetServer = null;
                            UpdatePairingStatus();
                            UpdatePairingProperties();
                            TargetServerValidationMessage = string.Empty;
                        }
                        else
                        {
                            TargetServerValidationMessage = "Failed to unpair server";
                        }
                    }
                }
                else
                {
                    // Create new pair
                    if (SelectedPairTargetServer == null)
                    {
                        TargetServerValidationMessage = "Please select a server to pair with";
                        return;
                    }

                    // Check if target server is already paired
                    if (_serverService.IsServerPaired(SelectedPairTargetServer.Server.Id))
                    {
                        var existingPair = _serverService.GetPairedServer(SelectedPairTargetServer.Server.Id);
                        TargetServerValidationMessage = $"Selected server is already paired with '{existingPair?.Name}'";
                        return;
                    }

                    var success = await _serverService.CreateServerPairAsync(
                        SelectedTargetServer.Id, 
                        SelectedPairTargetServer.Server.Id);

                    if (success)
                    {
                        StatusText = $"Failover pair created: {SelectedTargetServer.Name} ↔ {SelectedPairTargetServer.Server.Name}";
                        SelectedPairTargetServer = null;
                        UpdatePairingStatus();
                        UpdatePairingProperties();
                        TargetServerValidationMessage = string.Empty;
                    }
                    else
                    {
                        TargetServerValidationMessage = "Failed to create server pair";
                    }
                }
            }
            catch (Exception ex)
            {
                TargetServerValidationMessage = $"Error: {ex.Message}";
            }
        }

        private bool CanCreateOrRemoveFailoverPair()
        {
            if (SelectedTargetServer == null)
                return false;
                
            // If server is paired, we can always unpair
            if (_serverService.IsServerPaired(SelectedTargetServer.Id))
                return true;
                
            // If not paired, we need a target server selected
            return SelectedPairTargetServer != null;
        }

        private void UpdatePairingProperties()
        {
            OnPropertyChanged(nameof(AvailableTargetServersForPairing));
            OnPropertyChanged(nameof(IsCurrentServerPaired));
            OnPropertyChanged(nameof(PairButtonText));
        }

        private void UpdatePairingStatus()
        {
            if (SelectedTargetServer == null)
            {
                CurrentPairStatus = string.Empty;
                return;
            }

            if (_serverService.IsServerPaired(SelectedTargetServer.Id))
            {
                var pairedServer = _serverService.GetPairedServer(SelectedTargetServer.Id);
                CurrentPairStatus = pairedServer != null 
                    ? $"Paired with: {pairedServer.Name} ({pairedServer.DnsName})"
                    : "Paired (partner not found)";
            }
            else
            {
                CurrentPairStatus = "Not paired";
            }
        }

        #endregion

        #region Failover Methods

        private void ConfigureFailover()
        {
            if (SelectedAgentServer == null)
                return;

            try
            {
                var dialog = new Views.FailoverConfigurationDialog(SelectedAgentServer, _agentCommunicationService, _serverService);
                
                // Set the owner to the current window (we'll need to pass this in or find it)
                var result = dialog.ShowDialog();
                
                if (result == true)
                {
                    StatusText = "Failover configuration saved successfully";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error opening failover configuration: {ex.Message}";
            }
        }

        private bool CanConfigureFailover() => SelectedAgentServer != null;

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Helper class for displaying target servers with pairing information in the dropdown
    /// </summary>
    public class TargetServerPairingInfo
    {
        public TargetServer Server { get; set; } = null!;
        public bool IsPaired { get; set; }
        public string PairedWith { get; set; } = string.Empty;
        
        // Properties for UI binding
        public string Name => Server.Name;
        public string DnsName => Server.DnsName;
        public string PairingStatus => IsPaired ? $"(paired with {PairedWith})" : string.Empty;
    }
}