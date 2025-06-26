using System.Text.Json;
using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Background service for agent failover monitoring and execution
/// </summary>
public class FailoverBackgroundService : BackgroundService
{
    private readonly IFailoverService _failoverService;
    private readonly ILogger<FailoverBackgroundService> _logger;
    private readonly string _configPath;

    public FailoverBackgroundService(
        IFailoverService failoverService,
        ILogger<FailoverBackgroundService> logger)
    {
        _failoverService = failoverService;
        _logger = logger;
        _configPath = "failover_config.json";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Failover background service starting");

        try
        {
            // Load existing configuration if available
            await LoadConfigurationAsync();

            // Start monitoring
            await _failoverService.StartMonitoringAsync();

            // Keep running until cancelled
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in failover background service");
        }
        finally
        {
            try
            {
                await _failoverService.StopMonitoringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping failover monitoring");
            }
        }

        _logger.LogInformation("Failover background service stopped");
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                _logger.LogInformation("Loading existing failover configuration from {Path}", _configPath);
                
                var json = await File.ReadAllTextAsync(_configPath);
                var config = JsonSerializer.Deserialize<FailoverConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (config != null)
                {
                    await _failoverService.ConfigureAsync(config);
                    _logger.LogInformation("Failover configuration loaded successfully");
                }
            }
            else
            {
                _logger.LogInformation("No existing failover configuration found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading failover configuration");
        }
    }
}