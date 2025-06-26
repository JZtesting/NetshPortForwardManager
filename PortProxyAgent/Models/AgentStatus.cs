namespace PortProxyAgent.Models;

/// <summary>
/// Agent status information
/// </summary>
public class AgentStatus
{
    /// <summary>
    /// Whether the agent is online and operational
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// When the agent service started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// How long the agent has been running
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Number of active port forwarding rules
    /// </summary>
    public int ActiveRulesCount { get; set; }

    /// <summary>
    /// Last time health was checked
    /// </summary>
    public DateTime LastHealthCheck { get; set; }

    /// <summary>
    /// Agent version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Additional status information
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;
}