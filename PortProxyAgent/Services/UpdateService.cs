using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Service for checking and applying agent updates
/// </summary>
public class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly AgentConfiguration _config;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(
        HttpClient httpClient,
        IOptions<AgentConfiguration> config,
        ILogger<UpdateService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<(bool UpdateAvailable, string? LatestVersion, string? DownloadUrl)> CheckForUpdatesAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.CentralManagerUrl))
        {
            _logger.LogDebug("No central manager configured, skipping update check");
            return (false, null, null);
        }

        try
        {
            var currentVersion = GetCurrentVersion();
            var checkUrl = $"{_config.CentralManagerUrl}/api/updates/check?currentVersion={currentVersion}&agentName={_config.Name}";
            
            _logger.LogDebug("Checking for updates at {Url}", checkUrl);
            
            var response = await _httpClient.GetAsync(checkUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Update check failed with status {Status}", response.StatusCode);
                return (false, null, null);
            }

            var content = await response.Content.ReadAsStringAsync();
            var updateInfo = JsonSerializer.Deserialize<UpdateCheckResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (updateInfo == null)
            {
                _logger.LogWarning("Invalid update check response");
                return (false, null, null);
            }

            _logger.LogInformation("Update check completed. Available: {Available}, Latest: {Version}", 
                updateInfo.UpdateAvailable, updateInfo.LatestVersion);

            return (updateInfo.UpdateAvailable, updateInfo.LatestVersion, updateInfo.DownloadUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return (false, null, null);
        }
    }

    public async Task<bool> InstallUpdateAsync(string downloadUrl)
    {
        try
        {
            _logger.LogInformation("Starting update installation from {Url}", downloadUrl);

            // Download the update package
            var response = await _httpClient.GetAsync(downloadUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to download update package");
                return false;
            }

            // Save to temp location
            var tempPath = Path.Combine(Path.GetTempPath(), "PortProxyAgent_Update.msi");
            await using var fileStream = new FileStream(tempPath, FileMode.Create);
            await response.Content.CopyToAsync(fileStream);
            fileStream.Close();

            _logger.LogInformation("Update package downloaded to {Path}", tempPath);

            // Execute the installer (this will replace the current installation)
            var installArgs = $"/i \"{tempPath}\" /quiet /qn REINSTALL=ALL REINSTALLMODE=vomus";
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = installArgs,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogInformation("Starting installer with args: {Args}", installArgs);
            
            var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                // Don't wait for completion as the installer will terminate this process
                _logger.LogInformation("Update installer started successfully");
                return true;
            }
            else
            {
                _logger.LogError("Failed to start update installer");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing update");
            return false;
        }
    }

    public string GetCurrentVersion()
    {
        // Try to get version from config first, then assembly
        if (!string.IsNullOrWhiteSpace(_config.Version))
        {
            return _config.Version;
        }

        // Fallback to assembly version
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0.0";
    }

    /// <summary>
    /// Response model for update check API
    /// </summary>
    private class UpdateCheckResponse
    {
        public bool UpdateAvailable { get; set; }
        public string? LatestVersion { get; set; }
        public string? DownloadUrl { get; set; }
        public string? ReleaseNotes { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }
}