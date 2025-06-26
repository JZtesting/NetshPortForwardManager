using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PortProxyAgent.Models;
using PortProxyAgent.Services;

namespace PortProxyAgent.Controllers;

/// <summary>
/// Controller for handling encrypted agent commands
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<AgentController> _logger;
    private readonly AgentConfiguration _config;

    public AgentController(
        IAgentService agentService, 
        ILogger<AgentController> logger,
        IOptions<AgentConfiguration> config)
    {
        _agentService = agentService;
        _logger = logger;
        _config = config.Value;
    }

    /// <summary>
    /// Execute an encrypted command
    /// </summary>
    /// <param name="encryptedMessage">The encrypted message containing the command</param>
    /// <returns>Encrypted response</returns>
    [HttpPost("execute")]
    public async Task<ActionResult<EncryptedMessage>> ExecuteCommand([FromBody] EncryptedMessage encryptedMessage)
    {
        try
        {
            _logger.LogInformation("Received encrypted command from {ClientIP}", HttpContext.Connection.RemoteIpAddress);

            // Get the secret key from configuration
            if (string.IsNullOrEmpty(_config.SecretKey))
            {
                _logger.LogError("Agent secret key not configured");
                return StatusCode(500, "Agent not configured");
            }

            // Process the command
            var response = await _agentService.ProcessCommandAsync(encryptedMessage, _config.SecretKey);
            
            _logger.LogInformation("Command processed successfully");
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized command attempt from {ClientIP}", HttpContext.Connection.RemoteIpAddress);
            return Unauthorized("Invalid message authentication");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get agent health status
    /// </summary>
    /// <returns>Agent status information</returns>
    [HttpGet("status")]
    public async Task<ActionResult<AgentStatus>> GetStatus()
    {
        try
        {
            var status = await _agentService.GetStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent status");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Simple ping endpoint for connectivity testing
    /// </summary>
    /// <returns>Pong response with timestamp</returns>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            Message = "Pong",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            AgentName = _config.Name,
            Port = _config.Port,
            MaxRules = _config.MaxRules
        });
    }

    /// <summary>
    /// Configure failover settings for this agent
    /// </summary>
    /// <param name="config">Failover configuration</param>
    /// <returns>Configuration result</returns>
    [HttpPost("configure-failover")]
    public async Task<ActionResult> ConfigureFailover([FromBody] FailoverConfiguration config)
    {
        try
        {
            _logger.LogInformation("Received failover configuration from {ClientIP}", HttpContext.Connection.RemoteIpAddress);

            // Validate configuration
            if (!config.IsValid())
            {
                var errors = config.GetValidationErrors();
                _logger.LogWarning("Invalid failover configuration: {Errors}", string.Join(", ", errors));
                return BadRequest(new { Errors = errors });
            }

            // Apply configuration
            var result = await _agentService.ConfigureFailoverAsync(config);
            
            if (result)
            {
                _logger.LogInformation("Failover configuration applied successfully");
                return Ok(new { Message = "Failover configuration applied successfully" });
            }
            else
            {
                _logger.LogError("Failed to apply failover configuration");
                return StatusCode(500, "Failed to apply failover configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring failover");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get current failover status
    /// </summary>
    /// <returns>Current failover status</returns>
    [HttpGet("failover-status")]
    public async Task<ActionResult<FailoverStatus>> GetFailoverStatus()
    {
        try
        {
            var status = await _agentService.GetFailoverStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failover status");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Manually trigger failover (override health checks)
    /// </summary>
    /// <param name="failToB">True to fail to B servers, false to fail back to A servers</param>
    /// <returns>Failover result</returns>
    [HttpPost("manual-failover")]
    public async Task<ActionResult> ManualFailover([FromBody] bool failToB)
    {
        try
        {
            _logger.LogInformation("Manual failover requested from {ClientIP}, failToB: {FailToB}", 
                HttpContext.Connection.RemoteIpAddress, failToB);

            var result = await _agentService.ExecuteManualFailoverAsync(failToB);
            
            if (result)
            {
                _logger.LogInformation("Manual failover executed successfully");
                return Ok(new { Message = "Manual failover executed successfully" });
            }
            else
            {
                _logger.LogError("Manual failover failed");
                return StatusCode(500, "Manual failover failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing manual failover");
            return StatusCode(500, "Internal server error");
        }
    }
}