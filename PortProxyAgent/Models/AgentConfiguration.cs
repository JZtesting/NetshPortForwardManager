namespace PortProxyAgent.Models;

/// <summary>
/// Configuration options for the PortProxy Agent
/// </summary>
public class AgentConfiguration
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Agent";

    /// <summary>
    /// Secret key for encrypting/decrypting messages
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Port to listen on for HTTP requests
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>
    /// Agent name/identifier
    /// </summary>
    public string Name { get; set; } = System.Environment.MachineName;

    /// <summary>
    /// Maximum number of port forwarding rules allowed
    /// </summary>
    public int MaxRules { get; set; } = 100;

    /// <summary>
    /// Whether to require administrator privileges for netsh commands
    /// </summary>
    public bool RequireAdminPrivileges { get; set; } = true;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Health check interval in minutes
    /// </summary>
    public int HealthCheckIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Central manager URL for registration (optional)
    /// </summary>
    public string? CentralManagerUrl { get; set; }

    /// <summary>
    /// Environment tag (Production, Staging, Development)
    /// </summary>
    public string Environment { get; set; } = "Production";

    /// <summary>
    /// Silo ID for failover grouping (optional)
    /// </summary>
    public string? SiloId { get; set; }

    /// <summary>
    /// Auto-register with central manager on startup
    /// </summary>
    public bool AutoRegister { get; set; } = false;

    /// <summary>
    /// Check for updates interval in hours (0 = disabled)
    /// </summary>
    public int UpdateCheckIntervalHours { get; set; } = 24;

    /// <summary>
    /// Agent version (set during build)
    /// </summary>
    public string Version { get; set; } = "1.0.0";
}