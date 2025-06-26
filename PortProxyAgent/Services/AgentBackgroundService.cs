using Microsoft.Extensions.Options;
using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Background service for agent registration, heartbeats, and updates
/// </summary>
public class AgentBackgroundService : BackgroundService
{
    private readonly IRegistrationService _registrationService;
    private readonly IUpdateService _updateService;
    private readonly AgentConfiguration _config;
    private readonly ILogger<AgentBackgroundService> _logger;

    public AgentBackgroundService(
        IRegistrationService registrationService,
        IUpdateService updateService,
        IOptions<AgentConfiguration> config,
        ILogger<AgentBackgroundService> logger)
    {
        _registrationService = registrationService;
        _updateService = updateService;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent background service starting");

        // Initial registration if auto-register is enabled
        if (_config.AutoRegister)
        {
            _logger.LogInformation("Auto-registration enabled, attempting to register with central manager");
            
            // Wait a bit for the service to fully start
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            
            var registered = await _registrationService.RegisterAsync();
            if (registered)
            {
                _logger.LogInformation("Successfully auto-registered with central manager");
            }
            else
            {
                _logger.LogWarning("Auto-registration failed, will continue without central manager connection");
            }
        }

        // Start heartbeat and update check loops
        var heartbeatInterval = TimeSpan.FromMinutes(_config.HealthCheckIntervalMinutes);
        var updateCheckInterval = TimeSpan.FromHours(_config.UpdateCheckIntervalHours);
        var lastUpdateCheck = DateTime.MinValue;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(heartbeatInterval, stoppingToken);
                
                if (!stoppingToken.IsCancellationRequested)
                {
                    // Send heartbeat
                    await _registrationService.HeartbeatAsync();
                    
                    // Check for updates if enabled and interval has passed
                    if (_config.UpdateCheckIntervalHours > 0 && 
                        DateTime.Now - lastUpdateCheck >= updateCheckInterval)
                    {
                        _logger.LogInformation("Checking for agent updates...");
                        
                        var (updateAvailable, latestVersion, downloadUrl) = await _updateService.CheckForUpdatesAsync();
                        lastUpdateCheck = DateTime.Now;
                        
                        if (updateAvailable && !string.IsNullOrWhiteSpace(downloadUrl))
                        {
                            _logger.LogInformation("Update available: {Version}, downloading and installing...", latestVersion);
                            
                            var installSuccess = await _updateService.InstallUpdateAsync(downloadUrl);
                            if (installSuccess)
                            {
                                _logger.LogInformation("Update installation started, agent will restart automatically");
                                // The installer will terminate this process
                                return;
                            }
                            else
                            {
                                _logger.LogWarning("Update installation failed");
                            }
                        }
                        else if (updateAvailable)
                        {
                            _logger.LogInformation("Update available but no download URL provided");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent background service loop");
                // Continue running despite errors
            }
        }

        // Unregister on shutdown
        try
        {
            _logger.LogInformation("Agent background service stopping, unregistering from central manager");
            await _registrationService.UnregisterAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering from central manager during shutdown");
        }

        _logger.LogInformation("Agent background service stopped");
    }
}