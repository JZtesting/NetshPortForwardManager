using System.Text.Json.Serialization;

namespace PortProxyAgent.Models;

/// <summary>
/// Command structure for agent operations
/// </summary>
public class AgentCommand
{
    /// <summary>
    /// Command type (Add, Delete, List)
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Listen port for port forwarding rule
    /// </summary>
    [JsonPropertyName("listenPort")]
    public string ListenPort { get; set; } = string.Empty;

    /// <summary>
    /// Listen address for port forwarding rule
    /// </summary>
    [JsonPropertyName("listenAddress")]
    public string ListenAddress { get; set; } = string.Empty;

    /// <summary>
    /// Connect port for port forwarding rule
    /// </summary>
    [JsonPropertyName("connectPort")]
    public string ConnectPort { get; set; } = string.Empty;

    /// <summary>
    /// Connect address for port forwarding rule
    /// </summary>
    [JsonPropertyName("connectAddress")]
    public string ConnectAddress { get; set; } = string.Empty;

    /// <summary>
    /// Protocol type (v4tov4, v4tov6, v6tov4, v6tov6)
    /// </summary>
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "v4tov4";

    /// <summary>
    /// Rule description for logging and tracking
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Command ID for tracking responses
    /// </summary>
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Response structure for agent command execution
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// Command ID that this response corresponds to
    /// </summary>
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the command was executed successfully
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Result message or error description
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Raw output from netsh command
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of command execution (UTC)
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// For list commands, contains current port proxy rules
    /// </summary>
    [JsonPropertyName("rules")]
    public List<PortProxyRule> Rules { get; set; } = new List<PortProxyRule>();
}

/// <summary>
/// Simplified port proxy rule structure for agent responses
/// </summary>
public class PortProxyRule
{
    [JsonPropertyName("listenPort")]
    public string ListenPort { get; set; } = string.Empty;
    
    [JsonPropertyName("listenAddress")]
    public string ListenAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("connectPort")]
    public string ConnectPort { get; set; } = string.Empty;
    
    [JsonPropertyName("connectAddress")]
    public string ConnectAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;
}