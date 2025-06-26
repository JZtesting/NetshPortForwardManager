using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Interface for agent failover functionality
/// </summary>
public interface IFailoverService
{
    /// <summary>
    /// Configure failover settings
    /// </summary>
    /// <param name="config">Failover configuration</param>
    /// <returns>True if configuration was applied successfully</returns>
    Task<bool> ConfigureAsync(FailoverConfiguration config);

    /// <summary>
    /// Get current failover status
    /// </summary>
    /// <returns>Current failover status</returns>
    Task<FailoverStatus> GetStatusAsync();

    /// <summary>
    /// Manually execute failover
    /// </summary>
    /// <param name="failToB">True to fail to B servers, false to fail back to A servers</param>
    /// <returns>True if failover was executed successfully</returns>
    Task<bool> ExecuteManualFailoverAsync(bool failToB);

    /// <summary>
    /// Start background health monitoring
    /// </summary>
    Task StartMonitoringAsync();

    /// <summary>
    /// Stop background health monitoring
    /// </summary>
    Task StopMonitoringAsync();
}