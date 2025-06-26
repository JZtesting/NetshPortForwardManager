using System.Text.Json.Serialization;

namespace PortProxyAgent.Models;

/// <summary>
/// Configuration for agent failover functionality
/// </summary>
public class FailoverConfiguration
{
    /// <summary>
    /// Whether failover monitoring is enabled
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Health check URL for A servers (primary)
    /// </summary>
    [JsonPropertyName("healthUrlA")]
    public string HealthUrlA { get; set; } = string.Empty;

    /// <summary>
    /// Health check URL for B servers (failover)
    /// </summary>
    [JsonPropertyName("healthUrlB")]
    public string HealthUrlB { get; set; } = string.Empty;

    /// <summary>
    /// Interval between health checks in seconds
    /// </summary>
    [JsonPropertyName("checkIntervalSeconds")]
    public int CheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// HTTP timeout for health checks in seconds
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Mapping of A server IDs to B server IDs
    /// Key: A server ID, Value: B server ID
    /// </summary>
    [JsonPropertyName("serverMappings")]
    public Dictionary<string, string> ServerMappings { get; set; } = new();

    /// <summary>
    /// Whether the agent is currently failed over to B servers
    /// </summary>
    [JsonPropertyName("currentlyFailedOver")]
    public bool CurrentlyFailedOver { get; set; } = false;

    /// <summary>
    /// Last time health check was performed
    /// </summary>
    [JsonPropertyName("lastHealthCheck")]
    public DateTime LastHealthCheck { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Last health status from A endpoint
    /// </summary>
    [JsonPropertyName("lastHealthA")]
    public string LastHealthA { get; set; } = string.Empty;

    /// <summary>
    /// Last health status from B endpoint
    /// </summary>
    [JsonPropertyName("lastHealthB")]
    public string LastHealthB { get; set; } = string.Empty;

    /// <summary>
    /// Number of consecutive health check failures for A
    /// </summary>
    [JsonPropertyName("consecutiveFailuresA")]
    public int ConsecutiveFailuresA { get; set; } = 0;

    /// <summary>
    /// Number of consecutive health check failures for B
    /// </summary>
    [JsonPropertyName("consecutiveFailuresB")]
    public int ConsecutiveFailuresB { get; set; } = 0;

    /// <summary>
    /// Last error message from health checks
    /// </summary>
    [JsonPropertyName("lastError")]
    public string LastError { get; set; } = string.Empty;

    /// <summary>
    /// Validate the failover configuration
    /// </summary>
    public bool IsValid()
    {
        if (!Enabled)
            return true; // If disabled, always valid

        return !string.IsNullOrWhiteSpace(HealthUrlA) &&
               !string.IsNullOrWhiteSpace(HealthUrlB) &&
               CheckIntervalSeconds > 0 &&
               TimeoutSeconds > 0 &&
               ServerMappings.Count > 0;
    }

    /// <summary>
    /// Get validation errors
    /// </summary>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (Enabled)
        {
            if (string.IsNullOrWhiteSpace(HealthUrlA))
                errors.Add("Health URL A is required when failover is enabled");

            if (string.IsNullOrWhiteSpace(HealthUrlB))
                errors.Add("Health URL B is required when failover is enabled");

            if (CheckIntervalSeconds <= 0)
                errors.Add("Check interval must be greater than 0");

            if (TimeoutSeconds <= 0)
                errors.Add("Timeout must be greater than 0");

            if (ServerMappings.Count == 0)
                errors.Add("At least one server mapping is required");
        }

        return errors;
    }
}

/// <summary>
/// Current failover status information
/// </summary>
public class FailoverStatus
{
    /// <summary>
    /// Whether failover monitoring is currently enabled
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Whether currently failed over to B servers
    /// </summary>
    [JsonPropertyName("currentlyFailedOver")]
    public bool CurrentlyFailedOver { get; set; }

    /// <summary>
    /// Last health check timestamp
    /// </summary>
    [JsonPropertyName("lastHealthCheck")]
    public DateTime LastHealthCheck { get; set; }

    /// <summary>
    /// Current health status of A endpoint
    /// </summary>
    [JsonPropertyName("healthStatusA")]
    public string HealthStatusA { get; set; } = string.Empty;

    /// <summary>
    /// Current health status of B endpoint
    /// </summary>
    [JsonPropertyName("healthStatusB")]
    public string HealthStatusB { get; set; } = string.Empty;

    /// <summary>
    /// Number of rules currently managed
    /// </summary>
    [JsonPropertyName("rulesManaged")]
    public int RulesManaged { get; set; }

    /// <summary>
    /// Last failover event timestamp
    /// </summary>
    [JsonPropertyName("lastFailoverTime")]
    public DateTime? LastFailoverTime { get; set; }

    /// <summary>
    /// Last error message
    /// </summary>
    [JsonPropertyName("lastError")]
    public string LastError { get; set; } = string.Empty;

    /// <summary>
    /// Health check interval in seconds
    /// </summary>
    [JsonPropertyName("checkIntervalSeconds")]
    public int CheckIntervalSeconds { get; set; }
}