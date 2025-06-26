using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Service for registering agent with central manager
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Register this agent with the central manager
    /// </summary>
    Task<bool> RegisterAsync();

    /// <summary>
    /// Send heartbeat to central manager
    /// </summary>
    Task<bool> HeartbeatAsync();

    /// <summary>
    /// Unregister from central manager
    /// </summary>
    Task<bool> UnregisterAsync();
}