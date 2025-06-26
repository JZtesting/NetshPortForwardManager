using System.Text.Json;
using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Service for handling agent failover operations
/// </summary>
public class FailoverService : IFailoverService
{
    private readonly HttpClient _httpClient;
    private readonly INetshExecutor _netshExecutor;
    private readonly ILogger<FailoverService> _logger;
    private readonly string _configPath;
    
    private FailoverConfiguration _config;
    private Timer? _healthCheckTimer;
    private readonly object _lockObject = new object();

    public FailoverService(
        HttpClient httpClient,
        INetshExecutor netshExecutor,
        ILogger<FailoverService> logger)
    {
        _httpClient = httpClient;
        _netshExecutor = netshExecutor;
        _logger = logger;
        _configPath = "failover_config.json";
        _config = new FailoverConfiguration();
    }

    public async Task<bool> ConfigureAsync(FailoverConfiguration config)
    {
        try
        {
            _logger.LogInformation("Configuring failover with {MappingCount} server mappings", config.ServerMappings.Count);

            lock (_lockObject)
            {
                _config = config;
            }

            // Save configuration to disk
            await SaveConfigurationAsync();

            // Restart monitoring with new settings
            await StopMonitoringAsync();
            if (config.Enabled)
            {
                await StartMonitoringAsync();
            }

            _logger.LogInformation("Failover configuration applied successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring failover");
            return false;
        }
    }

    public async Task<FailoverStatus> GetStatusAsync()
    {
        try
        {
            FailoverConfiguration currentConfig;
            lock (_lockObject)
            {
                currentConfig = _config;
            }

            // Get current rule count
            var rulesResult = await _netshExecutor.ListRulesAsync();
            var ruleCount = rulesResult.Success ? rulesResult.Rules.Count : 0;

            return new FailoverStatus
            {
                Enabled = currentConfig.Enabled,
                CurrentlyFailedOver = currentConfig.CurrentlyFailedOver,
                LastHealthCheck = currentConfig.LastHealthCheck,
                HealthStatusA = currentConfig.LastHealthA,
                HealthStatusB = currentConfig.LastHealthB,
                RulesManaged = ruleCount,
                LastError = currentConfig.LastError,
                CheckIntervalSeconds = currentConfig.CheckIntervalSeconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failover status");
            return new FailoverStatus
            {
                Enabled = false,
                LastError = ex.Message
            };
        }
    }

    public async Task<bool> ExecuteManualFailoverAsync(bool failToB)
    {
        try
        {
            _logger.LogInformation("Executing manual failover, failToB: {FailToB}", failToB);

            FailoverConfiguration currentConfig;
            lock (_lockObject)
            {
                currentConfig = _config;
            }

            if (!currentConfig.Enabled)
            {
                _logger.LogWarning("Failover not enabled, cannot execute manual failover");
                return false;
            }

            var success = await ExecuteFailoverAsync(failToB, "Manual override");

            if (success)
            {
                lock (_lockObject)
                {
                    _config.CurrentlyFailedOver = failToB;
                }
                await SaveConfigurationAsync();
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing manual failover");
            return false;
        }
    }

    public Task StartMonitoringAsync()
    {
        try
        {
            FailoverConfiguration currentConfig;
            lock (_lockObject)
            {
                currentConfig = _config;
            }

            if (!currentConfig.Enabled)
            {
                _logger.LogInformation("Failover monitoring is disabled");
                return Task.CompletedTask;
            }

            if (_healthCheckTimer != null)
            {
                _logger.LogInformation("Health check timer already running");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Starting failover health monitoring with {Interval} second interval", 
                currentConfig.CheckIntervalSeconds);

            var interval = TimeSpan.FromSeconds(currentConfig.CheckIntervalSeconds);
            _healthCheckTimer = new Timer(async _ => await PerformHealthCheckAsync(), 
                null, TimeSpan.Zero, interval);

            _logger.LogInformation("Failover monitoring started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting failover monitoring");
        }
        
        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync()
    {
        try
        {
            if (_healthCheckTimer != null)
            {
                _logger.LogInformation("Stopping failover monitoring");
                _healthCheckTimer.Dispose();
                _healthCheckTimer = null;
                _logger.LogInformation("Failover monitoring stopped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping failover monitoring");
        }
        
        return Task.CompletedTask;
    }

    private async Task PerformHealthCheckAsync()
    {
        try
        {
            FailoverConfiguration currentConfig;
            lock (_lockObject)
            {
                currentConfig = _config;
            }

            if (!currentConfig.Enabled)
                return;

            _logger.LogDebug("Performing health check...");

            // Check health of both endpoints
            var healthA = await CheckHealthEndpointAsync(currentConfig.HealthUrlA, currentConfig.TimeoutSeconds);
            var healthB = await CheckHealthEndpointAsync(currentConfig.HealthUrlB, currentConfig.TimeoutSeconds);

            _logger.LogDebug("Health check results - A: {HealthA}, B: {HealthB}", healthA, healthB);

            // Update configuration with results
            lock (_lockObject)
            {
                _config.LastHealthCheck = DateTime.UtcNow;
                _config.LastHealthA = healthA;
                _config.LastHealthB = healthB;
                _config.LastError = string.Empty;

                // Update consecutive failure counts
                if (healthA == "Alive")
                    _config.ConsecutiveFailuresA = 0;
                else
                    _config.ConsecutiveFailuresA++;

                if (healthB == "Alive")
                    _config.ConsecutiveFailuresB = 0;
                else
                    _config.ConsecutiveFailuresB++;
            }

            // Determine if failover is needed
            bool shouldFailover = DetermineFailoverNeeded(healthA, healthB, currentConfig.CurrentlyFailedOver);

            if (shouldFailover)
            {
                var failToB = (healthA == "Dead" && healthB == "Alive");
                var reason = failToB ? "A endpoint is Dead, B endpoint is Alive" : "A endpoint is Alive, B endpoint is Dead";
                
                _logger.LogInformation("Triggering automatic failover: {Reason}", reason);
                
                var success = await ExecuteFailoverAsync(failToB, reason);
                
                if (success)
                {
                    lock (_lockObject)
                    {
                        _config.CurrentlyFailedOver = failToB;
                    }
                    await SaveConfigurationAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
            
            lock (_lockObject)
            {
                _config.LastError = ex.Message;
            }
        }
    }

    private async Task<string> CheckHealthEndpointAsync(string url, int timeoutSeconds)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var response = await _httpClient.GetStringAsync(url, cts.Token);
            
            var trimmedResponse = response.Trim();
            return trimmedResponse == "Alive" || trimmedResponse == "Dead" ? trimmedResponse : "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health check failed for {Url}", url);
            return "Dead";
        }
    }

    private static bool DetermineFailoverNeeded(string healthA, string healthB, bool currentlyFailedOver)
    {
        // Fail to B when A is dead and B is alive, and we're not already failed over
        if (healthA == "Dead" && healthB == "Alive" && !currentlyFailedOver)
            return true;

        // Fail back to A when A is alive and B is dead, and we're currently failed over
        if (healthA == "Alive" && healthB == "Dead" && currentlyFailedOver)
            return true;

        return false;
    }

    private async Task<bool> ExecuteFailoverAsync(bool failToB, string reason)
    {
        try
        {
            _logger.LogInformation("Executing failover to {Target} servers. Reason: {Reason}", 
                failToB ? "B" : "A", reason);

            FailoverConfiguration currentConfig;
            lock (_lockObject)
            {
                currentConfig = _config;
            }

            // Get current rules
            var rulesResult = await _netshExecutor.ListRulesAsync();
            if (!rulesResult.Success)
            {
                _logger.LogError("Failed to get current rules for failover: {Error}", rulesResult.Message);
                return false;
            }

            int rulesProcessed = 0;
            int rulesChanged = 0;

            foreach (var rule in rulesResult.Rules)
            {
                rulesProcessed++;
                
                // Determine what the target should be
                string targetAddress = DetermineTargetAddress(rule.ConnectAddress, currentConfig.ServerMappings, failToB);
                
                if (targetAddress != rule.ConnectAddress)
                {
                    // Need to update this rule's target
                    _logger.LogDebug("Switching rule target {Listen}:{Port} from {OldTarget} to {NewTarget}", 
                        rule.ListenAddress, rule.ListenPort, rule.ConnectAddress, targetAddress);

                    // NetSh requires delete + add to change target
                    var success = await UpdateRuleTargetAsync(rule, targetAddress);
                    
                    if (success)
                    {
                        rulesChanged++;
                        _logger.LogDebug("Successfully switched rule {Listen}:{Port} -> {Target}:{Port}", 
                            rule.ListenAddress, rule.ListenPort, targetAddress, rule.ConnectPort);
                    }
                    else
                    {
                        _logger.LogError("Failed to switch rule {Listen}:{Port} target", 
                            rule.ListenAddress, rule.ListenPort);
                    }
                }
            }

            _logger.LogInformation("Failover completed. Processed {Processed} rules, changed {Changed} rules", 
                rulesProcessed, rulesChanged);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing failover");
            return false;
        }
    }

    private string DetermineTargetAddress(string currentAddress, Dictionary<string, string> serverMappings, bool failToB)
    {
        // If we have a direct IP mapping, use it
        if (serverMappings.ContainsKey(currentAddress))
        {
            return failToB ? serverMappings[currentAddress] : currentAddress;
        }

        // Check if current address is a B server that should map back to A
        var reverseMapping = serverMappings.FirstOrDefault(kvp => kvp.Value == currentAddress);
        if (!reverseMapping.Equals(default(KeyValuePair<string, string>)))
        {
            return failToB ? currentAddress : reverseMapping.Key;
        }

        // No mapping found, return current address unchanged
        return currentAddress;
    }

    private async Task<bool> UpdateRuleTargetAsync(PortProxyRule rule, string newTargetAddress)
    {
        try
        {
            // Step 1: Delete the existing rule
            var deleteCommand = new AgentCommand
            {
                Command = "delete",
                ListenAddress = rule.ListenAddress,
                ListenPort = rule.ListenPort,
                CommandId = Guid.NewGuid().ToString()
            };
            
            var deleteResult = await _netshExecutor.DeleteRuleAsync(deleteCommand);
            if (!deleteResult.Success)
            {
                _logger.LogError("Failed to delete rule for target update: {Error}", deleteResult.Message);
                return false;
            }

            // Step 2: Add rule with new target
            var addCommand = new AgentCommand
            {
                Command = "add",
                ListenAddress = rule.ListenAddress,
                ListenPort = rule.ListenPort,
                ConnectAddress = newTargetAddress,
                ConnectPort = rule.ConnectPort,
                Protocol = rule.Protocol,
                Description = $"Failover target: {newTargetAddress}",
                CommandId = Guid.NewGuid().ToString()
            };
            
            var addResult = await _netshExecutor.AddRuleAsync(addCommand);
            if (!addResult.Success)
            {
                _logger.LogError("Failed to add rule with new target {Target}: {Error}", 
                    newTargetAddress, addResult.Message);
                
                // Try to restore the original rule
                var restoreCommand = new AgentCommand
                {
                    Command = "add",
                    ListenAddress = rule.ListenAddress,
                    ListenPort = rule.ListenPort,
                    ConnectAddress = rule.ConnectAddress, // Original target
                    ConnectPort = rule.ConnectPort,
                    Protocol = rule.Protocol,
                    Description = "Restored after failed failover",
                    CommandId = Guid.NewGuid().ToString()
                };
                
                await _netshExecutor.AddRuleAsync(restoreCommand);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during rule target update");
            return false;
        }
    }

    private async Task SaveConfigurationAsync()
    {
        try
        {
            FailoverConfiguration configToSave;
            lock (_lockObject)
            {
                configToSave = _config;
            }

            var json = JsonSerializer.Serialize(configToSave, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(_configPath, json);
            _logger.LogDebug("Failover configuration saved to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving failover configuration");
        }
    }
}