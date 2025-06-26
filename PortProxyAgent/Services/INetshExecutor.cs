using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Interface for executing netsh commands
/// </summary>
public interface INetshExecutor
{
    /// <summary>
    /// Add a port proxy rule
    /// </summary>
    Task<AgentResponse> AddRuleAsync(AgentCommand command);

    /// <summary>
    /// Delete a port proxy rule
    /// </summary>
    Task<AgentResponse> DeleteRuleAsync(AgentCommand command);

    /// <summary>
    /// List all current port proxy rules
    /// </summary>
    Task<AgentResponse> ListRulesAsync();

    /// <summary>
    /// Reset all port proxy rules
    /// </summary>
    Task<AgentResponse> ResetAllRulesAsync();
}