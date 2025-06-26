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
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly NetshService _netshService;
        private readonly ServerService _serverService;
        private readonly DnsService _dnsService;
        private readonly IAgentCommunicationService _agentCommunicationService;
        private readonly RuleMetadataService _ruleMetadataService;
        private ObservableCollection<PortForwardRule> _rules;
        private PortForwardRule? _selectedRule;
        private Silo? _selectedSilo;
        private AgentServer? _selectedTreeViewServer;
        private string _statusText;

        public MainViewModel()
        {
            _netshService = new NetshService();
            _dnsService = new DnsService();
            _serverService = new ServerService(_dnsService);
            _ruleMetadataService = new RuleMetadataService();
// Use real agent communication service for actual testing
            _agentCommunicationService = new AgentCommunicationService(new System.Net.Http.HttpClient());
            _rules = new ObservableCollection<PortForwardRule>();
            _statusText = "Initializing...";
            
            // Initialize commands
            ManageServersCommand = new RelayCommand(OpenServerManagement);
            ConnectToServerCommand = new AsyncRelayCommand<AgentServer>(async (server) => await ConnectToServerAsync(server));
            
            // Subscribe to agent server events for automatic rule reloading
            _serverService.AgentServerUpdated += async (s, e) => await OnAgentServerUpdated(e.AgentServer);
            
            // Load data on startup
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            // Load servers and silos first
            await _serverService.LoadAsync();
            
            // Load rule metadata
            System.Diagnostics.Debug.WriteLine("[MainViewModel] Loading rule metadata service...");
            await _ruleMetadataService.LoadAsync();
            
            await LoadRulesAsync();
        }

        public ObservableCollection<PortForwardRule> Rules
        {
            get => _rules;
            set
            {
                _rules = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RuleCountText));
            }
        }

        public PortForwardRule? SelectedRule
        {
            get => _selectedRule;
            set
            {
                _selectedRule = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedRule));
            }
        }

        public bool HasSelectedRule => SelectedRule != null;

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public string RuleCountText => $"Rules: {Rules.Count} total";

        // Server Management Properties
        public ObservableCollection<AgentServer> AgentServers => _serverService.AgentServers;
        public ObservableCollection<TargetServer> TargetServers => _serverService.TargetServers;
        public ObservableCollection<Silo> Silos => _serverService.Silos;

        
        public Silo? SelectedSilo
        {
            get => _selectedSilo;
            set
            {
                _selectedSilo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedSilo));
            }
        }
        public bool HasSelectedSilo => SelectedSilo != null;
        
        public AgentServer? SelectedTreeViewServer
        {
            get => _selectedTreeViewServer;
            set
            {
                _selectedTreeViewServer = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedTreeViewServer));
                OnPropertyChanged(nameof(FilteredRules));
                OnPropertyChanged(nameof(SelectedServerDisplayName));
                OnPropertyChanged(nameof(RuleCountText));
                
                // Refresh failover status when server selection changes
                _ = RefreshFailoverStatusAsync();
            }
        }
        
        public bool HasSelectedTreeViewServer => SelectedTreeViewServer != null;
        public string SelectedServerDisplayName => SelectedTreeViewServer?.DisplayText ?? "No server selected";

        // Failover Status Properties  
        public FailoverStatus? SelectedAgentFailoverStatus { get; private set; }
        public bool HasFailoverStatus => SelectedAgentFailoverStatus != null;
        
        public ObservableCollection<PortForwardRule> FilteredRules
        {
            get
            {
                // Always show all rules for now - we can add filtering later if needed
                return Rules;
            }
        }
        
        
        public ObservableCollection<AgentServer> SelectedSiloAgentServers
        {
            get
            {
                if (SelectedSilo == null)
                    return new ObservableCollection<AgentServer>();
                
                var siloServers = AgentServers.Where(s => s.SiloId == SelectedSilo.Id).ToList();
                return new ObservableCollection<AgentServer>(siloServers);
            }
        }


        // Commands
        public ICommand ManageServersCommand { get; }
        public ICommand ConnectToServerCommand { get; }

        // Services (for use by other components)
        public ServerService ServerService => _serverService;
        public IAgentCommunicationService AgentCommunicationService => _agentCommunicationService;
        
        /// <summary>
        /// Save metadata for an existing rule (called from Edit Rule dialog)
        /// </summary>
        public async Task SaveRuleMetadataAsync(PortForwardRule rule)
        {
            await _ruleMetadataService.StoreRuleMetadataAsync(rule);
        }

        public async Task LoadRulesAsync()
        {
            try
            {
                StatusText = "Loading rules...";
                var allRules = new List<PortForwardRule>();
                
                System.Diagnostics.Debug.WriteLine("Starting LoadRulesAsync...");
                
                // Check if running as administrator
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                var isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                System.Diagnostics.Debug.WriteLine($"Running as Administrator: {isAdmin}");
                
                // Load local netsh rules
                System.Diagnostics.Debug.WriteLine("Calling netsh service...");
                var netshResult = await _netshService.GetAllRulesAsync();
                System.Diagnostics.Debug.WriteLine($"Netsh result - Success: {netshResult.Success}, Output: '{netshResult.Output}', Error: '{netshResult.Error}'");
                if (netshResult.Success)
                {
                    var localRules = await _netshService.ParseRulesAsync();
                    System.Diagnostics.Debug.WriteLine($"Loaded {localRules.Count} local netsh rules");
                    foreach (var rule in localRules)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Rule: {rule.ListenAddress}:{rule.ListenPort} -> {rule.ForwardAddress}:{rule.ForwardPort}");
                        
                        // Apply stored metadata to local rules as well
                        _ruleMetadataService.ApplyMetadataToRule(rule);
                    }
                    allRules.AddRange(localRules);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load netsh rules: {netshResult.Error}");
                    StatusText = $"Warning: Failed to load local netsh rules: {netshResult.Error}";
                }

                // Load rules from all connected agent servers
                var connectedAgents = AgentServers.Where(a => a.Status == AgentStatus.Connected).ToList();
                foreach (var agentServer in connectedAgents)
                {
                    try
                    {
                        StatusText = $"Loading rules from {agentServer.Name}...";
                        var agentRulesResult = await _agentCommunicationService.ListRulesAsync(agentServer);
                        
                        if (agentRulesResult.Success && agentRulesResult.Rules != null)
                        {
                            // Convert AgentPortForwardRule to PortForwardRule and apply stored metadata
                            foreach (var agentRule in agentRulesResult.Rules)
                            {
                                var rule = new PortForwardRule
                                {
                                    ListenPort = agentRule.ListenPort,
                                    ListenAddress = agentRule.ListenAddress,
                                    ForwardPort = agentRule.ConnectPort,
                                    ForwardAddress = agentRule.ConnectAddress,
                                    Protocol = ParseProtocolFromString(agentRule.Protocol),
                                    AgentServerId = agentServer.Id,
                                    AgentServerName = agentServer.Name,
                                    // Set default values that will be overridden by stored metadata
                                    Description = $"Rule from {agentServer.Name}",
                                    Category = "Agent"
                                };
                                
                                // Apply stored metadata if available
                                _ruleMetadataService.ApplyMetadataToRule(rule);
                                
                                allRules.Add(rule);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading rules from agent {agentServer.Name}: {ex.Message}");
                    }
                }
                
                Rules.Clear();
                foreach (var rule in allRules)
                {
                    Rules.Add(rule);
                }
                
                // Clean up orphaned metadata (rules that no longer exist)
                // Only clean up if we actually loaded some rules to avoid deleting all metadata due to parsing failures
                if (allRules.Count > 0)
                {
                    await _ruleMetadataService.CleanupOrphanedMetadataAsync(allRules);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] Skipping metadata cleanup - no rules loaded (possible parsing failure)");
                }
                
                System.Diagnostics.Debug.WriteLine($"Total rules loaded: {allRules.Count}");
                
                // Refresh filtered rules
                OnPropertyChanged(nameof(FilteredRules));
                
                StatusText = allRules.Count > 0 
                    ? $"Ready - {allRules.Count} rules loaded ({connectedAgents.Count} agents connected)"
                    : "No port forwarding rules found";
            }
            catch (Exception ex)
            {
                StatusText = $"Error loading rules: {ex.Message}";
            }
        }

        public async Task<bool> AddRuleAsync(PortForwardRule rule)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] AddRuleAsync called with rule - Description: '{rule.Description}', AgentServerId: '{rule.AgentServerId}'");
                StatusText = "Adding rule...";
                
                // Check if rule should be executed on a remote agent
                if (!string.IsNullOrEmpty(rule.AgentServerId))
                {
                    var agentServer = AgentServers.FirstOrDefault(a => a.Id == rule.AgentServerId);
                    if (agentServer != null)
                    {
                        var result = await _agentCommunicationService.AddRuleAsync(agentServer, rule);
                        
                        if (result.Success)
                        {
                            // Store metadata for the new rule
                            System.Diagnostics.Debug.WriteLine($"[MainViewModel] Storing metadata for new agent rule: {rule.Description}");
                            await _ruleMetadataService.StoreRuleMetadataAsync(rule);
                            
                            Rules.Add(rule);
                            OnPropertyChanged(nameof(FilteredRules));
                            StatusText = $"Rule added successfully on agent {agentServer.Name}";
                            return true;
                        }
                        else
                        {
                            StatusText = $"Failed to add rule on agent {agentServer.Name}: {result.Message}";
                            return false;
                        }
                    }
                    else
                    {
                        StatusText = "Agent server not found";
                        return false;
                    }
                }
                else
                {
                    // Execute locally using NetshService
                    var result = await _netshService.AddRuleAsync(rule);
                    
                    if (result.Success)
                    {
                        // Store metadata for the new rule
                        System.Diagnostics.Debug.WriteLine($"[MainViewModel] Storing metadata for new local rule: {rule.Description}");
                        await _ruleMetadataService.StoreRuleMetadataAsync(rule);
                        
                        Rules.Add(rule);
                        OnPropertyChanged(nameof(FilteredRules));
                        StatusText = "Rule added successfully (local)";
                        return true;
                    }
                    else
                    {
                        StatusText = $"Failed to add rule: {result.Error}";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error adding rule: {ex.Message}";
                return false;
            }
        }

        public async Task<bool> DeleteRuleAsync(PortForwardRule rule)
        {
            try
            {
                StatusText = "Deleting rule...";
                
                // Check if rule should be executed on a remote agent
                if (!string.IsNullOrEmpty(rule.AgentServerId))
                {
                    var agentServer = AgentServers.FirstOrDefault(a => a.Id == rule.AgentServerId);
                    if (agentServer != null)
                    {
                        var result = await _agentCommunicationService.DeleteRuleAsync(agentServer, rule);
                        
                        if (result.Success)
                        {
                            // Remove metadata for the deleted rule
                            await _ruleMetadataService.RemoveRuleMetadataAsync(rule);
                            
                            Rules.Remove(rule);
                            if (SelectedRule == rule)
                                SelectedRule = null;
                            OnPropertyChanged(nameof(FilteredRules));
                            StatusText = $"Rule deleted successfully from agent {agentServer.Name}";
                            return true;
                        }
                        else
                        {
                            StatusText = $"Failed to delete rule from agent {agentServer.Name}: {result.Message}";
                            return false;
                        }
                    }
                    else
                    {
                        StatusText = "Agent server not found";
                        return false;
                    }
                }
                else
                {
                    // Execute locally using NetshService
                    var result = await _netshService.DeleteRuleAsync(rule);
                    
                    if (result.Success)
                    {
                        // Remove metadata for the deleted rule
                        await _ruleMetadataService.RemoveRuleMetadataAsync(rule);
                        
                        Rules.Remove(rule);
                        if (SelectedRule == rule)
                            SelectedRule = null;
                        OnPropertyChanged(nameof(FilteredRules));
                        StatusText = "Rule deleted successfully (local)";
                        return true;
                    }
                    else
                    {
                        StatusText = $"Failed to delete rule: {result.Error}";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error deleting rule: {ex.Message}";
                return false;
            }
        }

        public async Task<bool> ResetAllRulesAsync()
        {
            try
            {
                StatusText = "Resetting all rules...";
                var result = await _netshService.ResetAllRulesAsync();
                
                if (result.Success)
                {
                    Rules.Clear();
                    SelectedRule = null;
                    OnPropertyChanged(nameof(FilteredRules));
                    StatusText = "All rules reset successfully";
                    return true;
                }
                else
                {
                    StatusText = $"Failed to reset rules: {result.Error}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error resetting rules: {ex.Message}";
                return false;
            }
        }

        public async Task<bool> ExportRulesAsync(string filePath)
        {
            try
            {
                StatusText = "Exporting rules...";
                var rules = Rules.ToList();
                var success = await JsonService.ExportRulesAsync(filePath, rules);
                
                if (success)
                {
                    StatusText = $"Exported {rules.Count} rules successfully";
                    return true;
                }
                else
                {
                    StatusText = "Failed to export rules";
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error exporting rules: {ex.Message}";
                return false;
            }
        }

        public async Task<(bool Success, int ImportedCount)> ImportRulesAsync(string filePath, bool replaceExisting = false)
        {
            try
            {
                StatusText = "Importing rules...";
                var (success, importedRules, error) = await JsonService.ImportRulesAsync(filePath);
                
                if (!success)
                {
                    StatusText = $"Failed to import rules: {error}";
                    return (false, 0);
                }

                if (replaceExisting)
                {
                    Rules.Clear();
                }

                int importedCount = 0;
                foreach (var rule in importedRules)
                {
                    // Check for duplicates
                    bool isDuplicate = Rules.Any(existing => 
                        existing.ListenPort == rule.ListenPort && 
                        existing.ListenAddress == rule.ListenAddress);

                    if (!isDuplicate)
                    {
                        Rules.Add(rule);
                        importedCount++;
                    }
                }

                StatusText = $"Imported {importedCount} rules successfully";
                return (true, importedCount);
            }
            catch (Exception ex)
            {
                StatusText = $"Error importing rules: {ex.Message}";
                return (false, 0);
            }
        }

        // Server Management Methods
        public async Task ConnectToServerAsync(AgentServer? agentServer)
        {
            if (agentServer == null) return;

            try
            {
                StatusText = $"Connecting to {agentServer.Name}...";
                
                // Test the connection
                var success = await _serverService.TestAgentConnectionAsync(agentServer.Id);
                
                if (success)
                {
                    StatusText = $"Connected to {agentServer.Name} successfully";
                    
                    // Reload rules to include rules from this newly connected server
                    await LoadRulesAsync();
                    
                    // Select this server in the tree view
                    SelectedTreeViewServer = agentServer;
                }
                else
                {
                    StatusText = $"Failed to connect to {agentServer.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error connecting to {agentServer.Name}: {ex.Message}";
            }
        }

        private async void OpenServerManagement()
        {
            var dialog = new Views.ServerManagementDialog(_serverService, _dnsService, _agentCommunicationService);
            dialog.ShowDialog();
            
            // Refresh server-related properties after dialog closes
            OnPropertyChanged(nameof(AgentServers));
            OnPropertyChanged(nameof(TargetServers));
            OnPropertyChanged(nameof(Silos));
            OnPropertyChanged(nameof(SelectedSiloAgentServers));
            
            // Force refresh of silo-server relationships
            foreach (var silo in _serverService.Silos)
            {
                await _serverService.UpdateSiloHealthAsync(silo.Id);
            }
            
            // Reload rules to reflect any changes in server connections
            await LoadRulesAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OnAgentServerUpdated(AgentServer agentServer)
        {
            // If an agent server status changed to Connected, reload rules
            if (agentServer.Status == AgentStatus.Connected)
            {
                StatusText = $"Agent {agentServer.Name} connected - reloading rules...";
                await LoadRulesAsync();
            }
        }

        private static ProtocolType ParseProtocolFromString(string protocol)
        {
            return protocol?.ToLowerInvariant() switch
            {
                "v4tov4" or "ipv4" => ProtocolType.V4ToV4,
                "v4tov6" => ProtocolType.V4ToV6,
                "v6tov4" => ProtocolType.V6ToV4,
                "v6tov6" or "ipv6" => ProtocolType.V6ToV6,
                _ => ProtocolType.V4ToV4
            };
        }

        private async Task RefreshFailoverStatusAsync()
        {
            try
            {
                if (SelectedTreeViewServer == null)
                {
                    SelectedAgentFailoverStatus = null;
                    OnPropertyChanged(nameof(SelectedAgentFailoverStatus));
                    OnPropertyChanged(nameof(HasFailoverStatus));
                    return;
                }

                // Get failover status from the selected agent server
                var status = await _agentCommunicationService.GetFailoverStatusAsync(SelectedTreeViewServer);
                SelectedAgentFailoverStatus = status;
                
                OnPropertyChanged(nameof(SelectedAgentFailoverStatus));
                OnPropertyChanged(nameof(HasFailoverStatus));
            }
            catch (Exception ex)
            {
                // If we can't get failover status, just set it to null
                // This might happen if the agent doesn't support failover or is offline
                SelectedAgentFailoverStatus = null;
                OnPropertyChanged(nameof(SelectedAgentFailoverStatus));
                OnPropertyChanged(nameof(HasFailoverStatus));
                
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to get failover status: {ex.Message}");
            }
        }
    }

}