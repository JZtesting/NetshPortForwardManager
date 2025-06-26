using System.Text.Json;
using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Main service for coordinating agent operations
/// </summary>
public class AgentService : IAgentService
{
    private readonly INetshExecutor _netshExecutor;
    private readonly IEncryptionService _encryptionService;
    private readonly IFailoverService _failoverService;
    private readonly ILogger<AgentService> _logger;
    private readonly DateTime _startTime;

    public AgentService(
        INetshExecutor netshExecutor, 
        IEncryptionService encryptionService,
        IFailoverService failoverService,
        ILogger<AgentService> logger)
    {
        _netshExecutor = netshExecutor;
        _encryptionService = encryptionService;
        _failoverService = failoverService;
        _logger = logger;
        _startTime = DateTime.UtcNow;
    }

    public async Task<EncryptedMessage> ProcessCommandAsync(EncryptedMessage encryptedMessage, string secretKey)
    {
        try
        {
            // Decrypt the incoming message
            var decryptedMessage = await _encryptionService.DecryptMessageAsync(encryptedMessage, secretKey);
            _logger.LogInformation("Processing decrypted command message");

            // Deserialize the command (JsonPropertyName attributes handle camelCase)
            var command = JsonSerializer.Deserialize<AgentCommand>(decryptedMessage);
            if (command == null)
            {
                throw new InvalidOperationException("Failed to deserialize command");
            }

            _logger.LogInformation("Executing command: {Command} for rule: {Description}", 
                command.Command, command.Description);

            // Execute the command based on type
            AgentResponse response = command.Command.ToLowerInvariant() switch
            {
                "add" => await _netshExecutor.AddRuleAsync(command),
                "delete" => await _netshExecutor.DeleteRuleAsync(command),
                "list" => await _netshExecutor.ListRulesAsync(),
                "reset" => await _netshExecutor.ResetAllRulesAsync(),
                _ => new AgentResponse
                {
                    CommandId = command.CommandId,
                    Success = false,
                    Message = $"Unknown command: {command.Command}"
                }
            };

            // Serialize the response (JsonPropertyName attributes handle camelCase)
            var responseJson = JsonSerializer.Serialize(response);

            // Encrypt and return the response
            var encryptedResponse = await _encryptionService.EncryptMessageAsync(responseJson, secretKey);
            
            _logger.LogInformation("Command {Command} completed with success: {Success}", 
                command.Command, response.Success);

            return encryptedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command");
            
            // Return encrypted error response
            var errorResponse = new AgentResponse
            {
                CommandId = "error",
                Success = false,
                Message = $"Command processing error: {ex.Message}",
                Timestamp = DateTime.UtcNow
            };

            var errorJson = JsonSerializer.Serialize(errorResponse);
            return await _encryptionService.EncryptMessageAsync(errorJson, secretKey);
        }
    }

    public async Task<AgentStatus> GetStatusAsync()
    {
        try
        {
            // Get current rules count
            var listResponse = await _netshExecutor.ListRulesAsync();
            var rulesCount = listResponse.Success ? listResponse.Rules.Count : 0;

            return new AgentStatus
            {
                IsOnline = true,
                StartTime = _startTime,
                Uptime = DateTime.UtcNow.Subtract(_startTime),
                ActiveRulesCount = rulesCount,
                LastHealthCheck = DateTime.UtcNow,
                Version = "1.0.0" // TODO: Get from assembly
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent status");
            return new AgentStatus
            {
                IsOnline = false,
                StartTime = _startTime,
                Uptime = DateTime.UtcNow.Subtract(_startTime),
                LastHealthCheck = DateTime.UtcNow,
                Version = "1.0.0"
            };
        }
    }

    public async Task<bool> ConfigureFailoverAsync(FailoverConfiguration config)
    {
        try
        {
            _logger.LogInformation("Configuring failover with {MappingCount} server mappings", config.ServerMappings.Count);
            return await _failoverService.ConfigureAsync(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring failover");
            return false;
        }
    }

    public async Task<FailoverStatus> GetFailoverStatusAsync()
    {
        try
        {
            return await _failoverService.GetStatusAsync();
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
            return await _failoverService.ExecuteManualFailoverAsync(failToB);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing manual failover");
            return false;
        }
    }
}