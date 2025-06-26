using System;
using System.Collections.Generic;

namespace PortProxyClient.Models;

/// <summary>
/// Result of an operation executed on a remote agent
/// </summary>
public class AgentOperationResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result message or error description
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Raw output from the agent command
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of operation execution (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Command ID for tracking
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// Exception details if operation failed due to communication error
    /// </summary>
    public string? ExceptionDetails { get; set; }
}

/// <summary>
/// Result of listing rules from a remote agent
/// </summary>
public class AgentRulesResult : AgentOperationResult
{
    /// <summary>
    /// List of active port forwarding rules on the agent
    /// </summary>
    public List<AgentPortForwardRule> Rules { get; set; } = new List<AgentPortForwardRule>();
}

/// <summary>
/// Port forwarding rule as reported by agent
/// </summary>
public class AgentPortForwardRule
{
    public string ListenPort { get; set; } = string.Empty;
    public string ListenAddress { get; set; } = string.Empty;
    public string ConnectPort { get; set; } = string.Empty;
    public string ConnectAddress { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
}