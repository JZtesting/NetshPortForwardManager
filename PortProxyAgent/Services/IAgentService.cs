using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Interface for coordinating agent operations
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Process an encrypted command message
    /// </summary>
    /// <param name="encryptedMessage">The encrypted message containing the command</param>
    /// <param name="secretKey">The secret key for decryption</param>
    /// <returns>Encrypted response message</returns>
    Task<EncryptedMessage> ProcessCommandAsync(EncryptedMessage encryptedMessage, string secretKey);

    /// <summary>
    /// Get current agent status
    /// </summary>
    /// <returns>Agent status information</returns>
    Task<AgentStatus> GetStatusAsync();

    /// <summary>
    /// Configure failover settings for this agent
    /// </summary>
    /// <param name="config">Failover configuration</param>
    /// <returns>True if configuration was applied successfully</returns>
    Task<bool> ConfigureFailoverAsync(FailoverConfiguration config);

    /// <summary>
    /// Get current failover status
    /// </summary>
    /// <returns>Current failover status information</returns>
    Task<FailoverStatus> GetFailoverStatusAsync();

    /// <summary>
    /// Manually execute failover (override health checks)
    /// </summary>
    /// <param name="failToB">True to fail to B servers, false to fail back to A servers</param>
    /// <returns>True if failover was executed successfully</returns>
    Task<bool> ExecuteManualFailoverAsync(bool failToB);
}