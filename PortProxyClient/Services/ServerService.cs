using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PortProxyClient.Models;

namespace PortProxyClient.Services
{
    public class ServerService : INotifyPropertyChanged
    {
        private readonly string _agentServerConfigPath = Path.Combine(Environment.CurrentDirectory, "agentservers.json");
        private readonly string _targetServerConfigPath = Path.Combine(Environment.CurrentDirectory, "targetservers.json");
        private readonly string _siloConfigPath = Path.Combine(Environment.CurrentDirectory, "silos.json");
        private readonly string _serverPairsConfigPath = Path.Combine(Environment.CurrentDirectory, "serverpairs.json");
        private readonly DnsService _dnsService;
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ObservableCollection<AgentServer> AgentServers { get; private set; }
        public ObservableCollection<TargetServer> TargetServers { get; private set; }
        public ObservableCollection<Silo> Silos { get; private set; }
        public ObservableCollection<ServerPair> ServerPairs { get; private set; }


        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<AgentServerEventArgs>? AgentServerAdded;
        public event EventHandler<AgentServerEventArgs>? AgentServerUpdated;
        public event EventHandler<AgentServerEventArgs>? AgentServerDeleted;
        public event EventHandler<TargetServerEventArgs>? TargetServerAdded;
        public event EventHandler<TargetServerEventArgs>? TargetServerUpdated;
        public event EventHandler<TargetServerEventArgs>? TargetServerDeleted;
        public event EventHandler<SiloEventArgs>? SiloAdded;
        public event EventHandler<SiloEventArgs>? SiloUpdated;
        public event EventHandler<SiloEventArgs>? SiloDeleted;
        public event EventHandler<ServerPairEventArgs>? ServerPairAdded;
        public event EventHandler<ServerPairEventArgs>? ServerPairDeleted;


        public ServerService(DnsService? dnsService = null)
        {
            _dnsService = dnsService ?? new DnsService();
            AgentServers = new ObservableCollection<AgentServer>();
            TargetServers = new ObservableCollection<TargetServer>();
            Silos = new ObservableCollection<Silo>();
            ServerPairs = new ObservableCollection<ServerPair>();
            
            AgentServers.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AgentServers));
            TargetServers.CollectionChanged += (s, e) => OnPropertyChanged(nameof(TargetServers));
            Silos.CollectionChanged += (s, e) => OnPropertyChanged(nameof(Silos));
            ServerPairs.CollectionChanged += (s, e) => OnPropertyChanged(nameof(ServerPairs));
        }

        #region Agent Server Management

        public async Task<bool> AddAgentServerAsync(AgentServer agentServer)
        {
            if (agentServer == null || !agentServer.IsValid())
                return false;

            // Ensure unique ID
            if (string.IsNullOrWhiteSpace(agentServer.Id))
                agentServer.Id = Guid.NewGuid().ToString();
            
            if (AgentServers.Any(s => s.Id == agentServer.Id))
                return false;

            AgentServers.Add(agentServer);
            AgentServerAdded?.Invoke(this, new AgentServerEventArgs(agentServer));
            
            // Update silo relationships if server is assigned to a silo
            if (!string.IsNullOrWhiteSpace(agentServer.SiloId))
            {
                await UpdateSiloHealthAsync(agentServer.SiloId);
            }
            
            await SaveAgentServersAsync();
            return true;
        }

        public async Task<bool> UpdateAgentServerAsync(AgentServer agentServer)
        {
            if (agentServer == null || !agentServer.IsValid())
                return false;

            var existingServer = AgentServers.FirstOrDefault(s => s.Id == agentServer.Id);
            if (existingServer == null)
                return false;

            // Store old silo ID before updating
            var oldSiloId = existingServer.SiloId;

            // Update properties
            existingServer.Name = agentServer.Name;
            existingServer.AgentUrl = agentServer.AgentUrl;
            existingServer.SecretKey = agentServer.SecretKey;
            existingServer.Description = agentServer.Description;
            existingServer.SiloId = agentServer.SiloId;
            existingServer.Environment = agentServer.Environment;
            existingServer.Tags.Clear();
            existingServer.Tags.AddRange(agentServer.Tags);

            AgentServerUpdated?.Invoke(this, new AgentServerEventArgs(existingServer));
            
            // Update silo relationships for both old and new silo assignments
            if (!string.IsNullOrWhiteSpace(oldSiloId) && oldSiloId != agentServer.SiloId)
            {
                await UpdateSiloHealthAsync(oldSiloId);
            }
            if (!string.IsNullOrWhiteSpace(existingServer.SiloId))
            {
                await UpdateSiloHealthAsync(existingServer.SiloId);
            }
            
            await SaveAgentServersAsync();
            return true;
        }

        public async Task<bool> DeleteAgentServerAsync(string agentServerId)
        {
            var agentServer = AgentServers.FirstOrDefault(s => s.Id == agentServerId);
            if (agentServer == null)
                return false;

            // Remove from any silos
            var silo = Silos.FirstOrDefault(s => s.Id == agentServer.SiloId);
            if (silo != null)
            {
                await UpdateSiloHealthAsync(silo.Id);
            }

            AgentServers.Remove(agentServer);
            AgentServerDeleted?.Invoke(this, new AgentServerEventArgs(agentServer));
            
            await SaveAgentServersAsync();
            await SaveSilosAsync();
            return true;
        }

        public AgentServer? GetAgentServer(string agentServerId)
        {
            return AgentServers.FirstOrDefault(s => s.Id == agentServerId);
        }

        public List<AgentServer> GetAgentServersBySilo(string siloId)
        {
            return AgentServers.Where(s => s.SiloId == siloId).ToList();
        }

        public async Task<bool> TestAgentConnectionAsync(string agentServerId)
        {
            var agentServer = GetAgentServer(agentServerId);
            if (agentServer == null)
                return false;

            try
            {
                agentServer.Status = AgentStatus.Connecting;
                
                // Use the actual agent communication service
                var agentService = new AgentCommunicationService(new System.Net.Http.HttpClient());
                
                var connectionSuccess = await agentService.TestConnectionAsync(agentServer);
                
                if (connectionSuccess)
                {
                    agentServer.Status = AgentStatus.Connected;
                    agentServer.LastSeen = DateTime.Now;
                    
                    // Try to get agent status info
                    var statusInfo = await agentService.GetAgentStatusAsync(agentServer);
                    if (statusInfo != null)
                    {
                        agentServer.Version = statusInfo.Version;
                    }
                    
                    return true;
                }
                else
                {
                    agentServer.Status = AgentStatus.Disconnected;
                    return false;
                }
            }
            catch (Exception ex)
            {
                agentServer.Status = AgentStatus.Error;
                System.Diagnostics.Debug.WriteLine($"Agent connection test failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Target Server Management

        public async Task<bool> AddTargetServerAsync(TargetServer targetServer)
        {
            if (targetServer == null || !targetServer.IsValid())
                return false;

            // Ensure unique ID
            if (string.IsNullOrWhiteSpace(targetServer.Id))
                targetServer.Id = Guid.NewGuid().ToString();
            
            if (TargetServers.Any(s => s.Id == targetServer.Id))
                return false;

            // Resolve DNS if not already resolved
            if (string.IsNullOrWhiteSpace(targetServer.IpAddress) && !string.IsNullOrWhiteSpace(targetServer.DnsName))
            {
                var dnsResult = await _dnsService.ResolveDnsAsync(targetServer.DnsName);
                if (dnsResult.Success)
                {
                    targetServer.IpAddress = dnsResult.ResolvedIp;
                    targetServer.IsDnsResolved = true;
                    targetServer.LastDnsCheck = DateTime.Now;
                }
            }

            TargetServers.Add(targetServer);
            TargetServerAdded?.Invoke(this, new TargetServerEventArgs(targetServer));
            
            await SaveTargetServersAsync();
            return true;
        }

        public async Task<bool> UpdateTargetServerAsync(TargetServer targetServer)
        {
            if (targetServer == null || !targetServer.IsValid())
                return false;

            var existingServer = TargetServers.FirstOrDefault(s => s.Id == targetServer.Id);
            if (existingServer == null)
                return false;

            // Update properties
            existingServer.Name = targetServer.Name;
            existingServer.DnsName = targetServer.DnsName;
            existingServer.Description = targetServer.Description;
            existingServer.Environment = targetServer.Environment;
            existingServer.Tags.Clear();
            existingServer.Tags.AddRange(targetServer.Tags);

            // Re-resolve DNS if DNS name changed
            if (!string.IsNullOrWhiteSpace(existingServer.DnsName))
            {
                var dnsResult = await _dnsService.ResolveDnsAsync(existingServer.DnsName);
                if (dnsResult.Success)
                {
                    existingServer.IpAddress = dnsResult.ResolvedIp;
                    existingServer.IsDnsResolved = true;
                    existingServer.LastDnsCheck = DateTime.Now;
                }
            }

            TargetServerUpdated?.Invoke(this, new TargetServerEventArgs(existingServer));
            
            await SaveTargetServersAsync();
            return true;
        }

        public async Task<bool> DeleteTargetServerAsync(string targetServerId)
        {
            var targetServer = TargetServers.FirstOrDefault(s => s.Id == targetServerId);
            if (targetServer == null)
                return false;

            TargetServers.Remove(targetServer);
            TargetServerDeleted?.Invoke(this, new TargetServerEventArgs(targetServer));
            
            await SaveTargetServersAsync();
            return true;
        }

        public TargetServer? GetTargetServer(string targetServerId)
        {
            return TargetServers.FirstOrDefault(s => s.Id == targetServerId);
        }

        public async Task<string> ResolveTargetAddressAsync(string targetServerId)
        {
            var targetServer = GetTargetServer(targetServerId);
            if (targetServer == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(targetServer.IpAddress))
                return targetServer.IpAddress;

            var dnsResult = await _dnsService.ResolveDnsAsync(targetServer.DnsName);
            if (dnsResult.Success)
            {
                targetServer.IpAddress = dnsResult.ResolvedIp;
                targetServer.IsDnsResolved = true;
                targetServer.LastDnsCheck = DateTime.Now;
                await SaveTargetServersAsync();
                return dnsResult.ResolvedIp;
            }

            return targetServer.DnsName; // Fall back to DNS name
        }

        #endregion

        #region Silo Management

        public async Task<bool> AddSiloAsync(Silo silo)
        {
            if (silo == null || !silo.IsValid())
                return false;

            // Ensure unique ID
            if (string.IsNullOrWhiteSpace(silo.Id))
                silo.Id = Guid.NewGuid().ToString();
            
            if (Silos.Any(s => s.Id == silo.Id))
                return false;

            Silos.Add(silo);
            SiloAdded?.Invoke(this, new SiloEventArgs(silo));
            
            await SaveSilosAsync();
            return true;
        }

        public async Task<bool> UpdateSiloAsync(Silo silo)
        {
            if (silo == null || !silo.IsValid())
                return false;

            var existingSilo = Silos.FirstOrDefault(s => s.Id == silo.Id);
            if (existingSilo == null)
                return false;

            // Update properties
            existingSilo.Name = silo.Name;
            existingSilo.Description = silo.Description;
            existingSilo.DataCenter = silo.DataCenter;
            existingSilo.Role = silo.Role;
            existingSilo.Properties.Clear();
            foreach (var kvp in silo.Properties)
                existingSilo.Properties[kvp.Key] = kvp.Value;
            existingSilo.Environments.Clear();
            existingSilo.Environments.AddRange(silo.Environments);

            SiloUpdated?.Invoke(this, new SiloEventArgs(existingSilo));
            await SaveSilosAsync();
            return true;
        }

        public async Task<bool> DeleteSiloAsync(string siloId)
        {
            var silo = Silos.FirstOrDefault(s => s.Id == siloId);
            if (silo == null)
                return false;

            // Remove silo reference from all agent servers
            foreach (var agentServer in AgentServers.Where(s => s.SiloId == siloId))
            {
                agentServer.SiloId = string.Empty;
            }

            Silos.Remove(silo);
            SiloDeleted?.Invoke(this, new SiloEventArgs(silo));
            
            await SaveAgentServersAsync(); // Save agent servers to update SiloId references
            await SaveSilosAsync();
            return true;
        }

        public Silo? GetSilo(string siloId)
        {
            return Silos.FirstOrDefault(s => s.Id == siloId);
        }

        public async Task UpdateSiloHealthAsync(string siloId)
        {
            var silo = GetSilo(siloId);
            if (silo == null)
                return;

            // Update AgentServers collection in silo
            silo.AgentServers.Clear();
            var agentServers = GetAgentServersBySilo(siloId);
            silo.AgentServers.AddRange(agentServers);

            // Update health status
            silo.UpdateHealthStatus();

            await SaveSilosAsync();
        }

        #endregion

        #region Persistence

        public async Task LoadAsync()
        {
            await LoadAgentServersAsync();
            await LoadTargetServersAsync();
            await LoadSilosAsync();
            await LoadServerPairsAsync();
            await MigrateLegacyServersAsync();
            
            // Rebuild silo-server relationships
            foreach (var silo in Silos)
            {
                await UpdateSiloHealthAsync(silo.Id);
            }
        }

        public async Task SaveAllAsync()
        {
            await SaveAgentServersAsync();
            await SaveTargetServersAsync();
            await SaveSilosAsync();
            await SaveServerPairsAsync();
        }

        private async Task LoadAgentServersAsync()
        {
            try
            {
                if (!File.Exists(_agentServerConfigPath))
                    return;

                var json = await File.ReadAllTextAsync(_agentServerConfigPath);
                var agentServers = JsonSerializer.Deserialize<List<AgentServer>>(json, JsonOptions);
                
                if (agentServers != null)
                {
                    AgentServers.Clear();
                    foreach (var agentServer in agentServers.Where(s => s.IsValid()))
                    {
                        AgentServers.Add(agentServer);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading agent servers: {ex.Message}");
            }
        }

        private async Task LoadTargetServersAsync()
        {
            try
            {
                if (!File.Exists(_targetServerConfigPath))
                    return;

                var json = await File.ReadAllTextAsync(_targetServerConfigPath);
                var targetServers = JsonSerializer.Deserialize<List<TargetServer>>(json, JsonOptions);
                
                if (targetServers != null)
                {
                    TargetServers.Clear();
                    foreach (var targetServer in targetServers.Where(s => s.IsValid()))
                    {
                        TargetServers.Add(targetServer);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading target servers: {ex.Message}");
            }
        }

        private async Task LoadSilosAsync()
        {
            try
            {
                if (!File.Exists(_siloConfigPath))
                    return;

                var json = await File.ReadAllTextAsync(_siloConfigPath);
                var silos = JsonSerializer.Deserialize<List<Silo>>(json, JsonOptions);
                
                if (silos != null)
                {
                    Silos.Clear();
                    foreach (var silo in silos.Where(s => s.IsValid()))
                    {
                        Silos.Add(silo);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading silos: {ex.Message}");
            }
        }

        private async Task LoadServerPairsAsync()
        {
            try
            {
                if (!File.Exists(_serverPairsConfigPath))
                    return;

                var json = await File.ReadAllTextAsync(_serverPairsConfigPath);
                var serverPairs = JsonSerializer.Deserialize<List<ServerPair>>(json, JsonOptions);
                
                if (serverPairs != null)
                {
                    ServerPairs.Clear();
                    foreach (var pair in serverPairs)
                    {
                        ServerPairs.Add(pair);
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {serverPairs.Count} server pairs from {_serverPairsConfigPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading server pairs: {ex.Message}");
            }
        }

        private async Task MigrateLegacyServersAsync()
        {
            // Legacy migration removed as part of obsolete code cleanup
            await Task.CompletedTask;
        }

        private async Task SaveAgentServersAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(AgentServers.ToList(), JsonOptions);
                await File.WriteAllTextAsync(_agentServerConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving agent servers: {ex.Message}");
            }
        }

        private async Task SaveTargetServersAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(TargetServers.ToList(), JsonOptions);
                await File.WriteAllTextAsync(_targetServerConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving target servers: {ex.Message}");
            }
        }

        private async Task SaveSilosAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(Silos.ToList(), JsonOptions);
                await File.WriteAllTextAsync(_siloConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving silos: {ex.Message}");
            }
        }

        #endregion

        #region Server Pair Management

        /// <summary>
        /// Create a failover pair between two target servers
        /// </summary>
        public async Task<bool> CreateServerPairAsync(string serverAId, string serverBId, string description = "")
        {
            try
            {
                // Validate servers exist
                var serverA = TargetServers.FirstOrDefault(s => s.Id == serverAId);
                var serverB = TargetServers.FirstOrDefault(s => s.Id == serverBId);
                
                if (serverA == null || serverB == null)
                    return false;

                // Check if either server is already paired
                var existingPair = ServerPairs.FirstOrDefault(p => p.ContainsServer(serverAId) || p.ContainsServer(serverBId));
                if (existingPair != null)
                    return false; // Server already paired

                var pair = new ServerPair
                {
                    Id = Guid.NewGuid().ToString(),
                    ServerAId = serverAId,
                    ServerBId = serverBId,
                    Description = string.IsNullOrEmpty(description) ? $"{serverA.Name} ↔ {serverB.Name}" : description,
                    CreatedDate = DateTime.UtcNow
                };

                ServerPairs.Add(pair);
                await SaveServerPairsAsync();
                
                ServerPairAdded?.Invoke(this, new ServerPairEventArgs(pair));
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating server pair: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove a server pair
        /// </summary>
        public async Task<bool> DeleteServerPairAsync(string pairId)
        {
            try
            {
                var pair = ServerPairs.FirstOrDefault(p => p.Id == pairId);
                if (pair == null)
                    return false;

                ServerPairs.Remove(pair);
                await SaveServerPairsAsync();
                
                ServerPairDeleted?.Invoke(this, new ServerPairEventArgs(pair));
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting server pair: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the paired server for a given server ID
        /// </summary>
        public TargetServer? GetPairedServer(string serverId)
        {
            var pair = ServerPairs.FirstOrDefault(p => p.ContainsServer(serverId));
            if (pair == null)
                return null;

            var partnerId = pair.GetPartnerServerId(serverId);
            if (partnerId == null)
                return null;

            return TargetServers.FirstOrDefault(s => s.Id == partnerId);
        }

        /// <summary>
        /// Check if a server is already paired
        /// </summary>
        public bool IsServerPaired(string serverId)
        {
            return ServerPairs.Any(p => p.ContainsServer(serverId));
        }

        /// <summary>
        /// Get server pair information for a server
        /// </summary>
        public ServerPair? GetServerPair(string serverId)
        {
            return ServerPairs.FirstOrDefault(p => p.ContainsServer(serverId));
        }

        /// <summary>
        /// Get all server pairs as IP address mappings for failover configuration
        /// </summary>
        public Dictionary<string, string> GetServerPairMappings()
        {
            var mappings = new Dictionary<string, string>();
            
            foreach (var pair in ServerPairs)
            {
                var serverA = TargetServers.FirstOrDefault(s => s.Id == pair.ServerAId);
                var serverB = TargetServers.FirstOrDefault(s => s.Id == pair.ServerBId);
                
                if (serverA != null && serverB != null)
                {
                    mappings[serverA.ResolvedAddress] = serverB.ResolvedAddress;
                }
            }
            
            return mappings;
        }

        private async Task SaveServerPairsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(ServerPairs.ToList(), JsonOptions);
                await File.WriteAllTextAsync(_serverPairsConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving server pairs: {ex.Message}");
            }
        }

        #endregion


        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Event argument classes
    public class AgentServerEventArgs : EventArgs
    {
        public AgentServer AgentServer { get; }
        public AgentServerEventArgs(AgentServer agentServer) => AgentServer = agentServer;
    }

    public class TargetServerEventArgs : EventArgs
    {
        public TargetServer TargetServer { get; }
        public TargetServerEventArgs(TargetServer targetServer) => TargetServer = targetServer;
    }


    public class SiloEventArgs : EventArgs
    {
        public Silo Silo { get; }
        public SiloEventArgs(Silo silo) => Silo = silo;
    }

    public class ServerPairEventArgs : EventArgs
    {
        public ServerPair ServerPair { get; }
        public ServerPairEventArgs(ServerPair serverPair) => ServerPair = serverPair;
    }
}