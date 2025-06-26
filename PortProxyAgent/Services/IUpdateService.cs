namespace PortProxyAgent.Services;

/// <summary>
/// Service for checking and applying agent updates
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Check if a newer version is available
    /// </summary>
    Task<(bool UpdateAvailable, string? LatestVersion, string? DownloadUrl)> CheckForUpdatesAsync();

    /// <summary>
    /// Download and install an update
    /// </summary>
    Task<bool> InstallUpdateAsync(string downloadUrl);

    /// <summary>
    /// Get current agent version
    /// </summary>
    string GetCurrentVersion();
}