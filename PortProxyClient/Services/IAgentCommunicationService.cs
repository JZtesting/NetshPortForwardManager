using System.Threading.Tasks;
using PortProxyClient.Models;

namespace PortProxyClient.Services;

/// <summary>
/// Interface for communicating with remote PortProxy agents
/// </summary>
public interface IAgentCommunicationService
{
    /// <summary>
    /// Test connection to an agent server
    /// </summary>
    /// <param name="agentServer">The agent server to test</param>
    /// <returns>True if connection is successful</returns>
    Task<bool> TestConnectionAsync(AgentServer agentServer);

    /// <summary>
    /// Get status information from an agent
    /// </summary>
    /// <param name="agentServer">The agent server to query</param>
    /// <returns>Agent status information</returns>
    Task<AgentStatusInfo?> GetAgentStatusAsync(AgentServer agentServer);

    /// <summary>
    /// Add a port forwarding rule on an agent
    /// </summary>
    /// <param name="agentServer">The agent server to execute on</param>
    /// <param name="rule">The port forwarding rule to add</param>
    /// <returns>Operation result</returns>
    Task<AgentOperationResult> AddRuleAsync(AgentServer agentServer, PortForwardRule rule);

    /// <summary>
    /// Delete a port forwarding rule on an agent
    /// </summary>
    /// <param name="agentServer">The agent server to execute on</param>
    /// <param name="rule">The port forwarding rule to delete</param>
    /// <returns>Operation result</returns>
    Task<AgentOperationResult> DeleteRuleAsync(AgentServer agentServer, PortForwardRule rule);

    /// <summary>
    /// List all port forwarding rules on an agent
    /// </summary>
    /// <param name="agentServer">The agent server to query</param>
    /// <returns>List of active rules</returns>
    Task<AgentRulesResult> ListRulesAsync(AgentServer agentServer);

    /// <summary>
    /// Reset all port forwarding rules on an agent
    /// </summary>
    /// <param name="agentServer">The agent server to execute on</param>
    /// <returns>Operation result</returns>
    Task<AgentOperationResult> ResetAllRulesAsync(AgentServer agentServer);

    /// <summary>
    /// Configure failover settings on an agent
    /// </summary>
    /// <param name="agentServer">The agent server to configure</param>
    /// <param name="config">Failover configuration</param>
    /// <returns>Operation result</returns>
    Task<AgentOperationResult> ConfigureFailoverAsync(AgentServer agentServer, FailoverConfiguration config);

    /// <summary>
    /// Get current failover status from an agent
    /// </summary>
    /// <param name="agentServer">The agent server to query</param>
    /// <returns>Current failover status</returns>
    Task<FailoverStatus?> GetFailoverStatusAsync(AgentServer agentServer);

    /// <summary>
    /// Manually trigger failover on an agent
    /// </summary>
    /// <param name="agentServer">The agent server to execute on</param>
    /// <param name="failToB">True to fail to B servers, false to fail back to A servers</param>
    /// <returns>Operation result</returns>
    Task<AgentOperationResult> ExecuteManualFailoverAsync(AgentServer agentServer, bool failToB);
}