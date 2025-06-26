using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Service for registering agent with central manager
/// </summary>
public class RegistrationService : IRegistrationService
{
    private readonly HttpClient _httpClient;
    private readonly AgentConfiguration _config;
    private readonly ILogger<RegistrationService> _logger;
    private readonly IEncryptionService _encryptionService;

    public RegistrationService(
        HttpClient httpClient,
        IOptions<AgentConfiguration> config,
        ILogger<RegistrationService> logger,
        IEncryptionService encryptionService)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    public async Task<bool> RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.CentralManagerUrl))
        {
            _logger.LogInformation("No central manager URL configured, skipping registration");
            return true; // Not an error - just not configured
        }

        try
        {
            var registrationData = new
            {
                Name = _config.Name,
                AgentUrl = $"http://{Environment.MachineName}:{_config.Port}",
                SecretKey = _config.SecretKey,
                Environment = _config.Environment,
                SiloId = _config.SiloId,
                Version = _config.Version,
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                OSVersion = Environment.OSVersion.ToString(),
                RegisteredAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(registrationData, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_config.CentralManagerUrl}/api/agents/register", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully registered with central manager at {Url}", _config.CentralManagerUrl);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to register with central manager. Status: {Status}, Reason: {Reason}", 
                    response.StatusCode, response.ReasonPhrase);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering with central manager at {Url}", _config.CentralManagerUrl);
            return false;
        }
    }

    public async Task<bool> HeartbeatAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.CentralManagerUrl))
        {
            return true; // Not configured
        }

        try
        {
            var heartbeatData = new
            {
                Name = _config.Name,
                AgentUrl = $"http://{Environment.MachineName}:{_config.Port}",
                LastSeen = DateTime.UtcNow,
                Status = "Connected"
            };

            var json = JsonSerializer.Serialize(heartbeatData, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_config.CentralManagerUrl}/api/agents/heartbeat", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat to central manager");
            return false;
        }
    }

    public async Task<bool> UnregisterAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.CentralManagerUrl))
        {
            return true; // Not configured
        }

        try
        {
            var response = await _httpClient.DeleteAsync($"{_config.CentralManagerUrl}/api/agents/{_config.Name}");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully unregistered from central manager");
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to unregister from central manager. Status: {Status}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering from central manager");
            return false;
        }
    }
}