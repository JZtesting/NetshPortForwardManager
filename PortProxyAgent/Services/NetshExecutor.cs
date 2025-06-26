using System.Diagnostics;
using System.Text.RegularExpressions;
using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Service for executing netsh interface portproxy commands
/// </summary>
public class NetshExecutor : INetshExecutor
{
    private readonly ILogger<NetshExecutor> _logger;

    public NetshExecutor(ILogger<NetshExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<AgentResponse> AddRuleAsync(AgentCommand command)
    {
        try
        {
            var netshCommand = $"interface portproxy add {command.Protocol} " +
                             $"listenport={command.ListenPort} listenaddress={command.ListenAddress} " +
                             $"connectport={command.ConnectPort} connectaddress={command.ConnectAddress}";

            _logger.LogInformation("Adding port proxy rule: {Description}", command.Description);
            _logger.LogDebug("Executing: netsh {Command}", netshCommand);

            var result = await ExecuteNetshCommandAsync(netshCommand);

            return new AgentResponse
            {
                CommandId = command.CommandId,
                Success = result.Success,
                Message = result.Success 
                    ? $"Successfully added rule: {command.ListenAddress}:{command.ListenPort} -> {command.ConnectAddress}:{command.ConnectPort}"
                    : $"Failed to add rule: {result.Error}",
                Output = result.Output
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding port proxy rule");
            return new AgentResponse
            {
                CommandId = command.CommandId,
                Success = false,
                Message = $"Exception occurred: {ex.Message}",
                Output = string.Empty
            };
        }
    }

    public async Task<AgentResponse> DeleteRuleAsync(AgentCommand command)
    {
        try
        {
            var netshCommand = $"interface portproxy delete {command.Protocol} " +
                             $"listenport={command.ListenPort} listenaddress={command.ListenAddress}";

            _logger.LogInformation("Deleting port proxy rule: {Description}", command.Description);
            _logger.LogDebug("Executing: netsh {Command}", netshCommand);

            var result = await ExecuteNetshCommandAsync(netshCommand);

            return new AgentResponse
            {
                CommandId = command.CommandId,
                Success = result.Success,
                Message = result.Success 
                    ? $"Successfully deleted rule: {command.ListenAddress}:{command.ListenPort}"
                    : $"Failed to delete rule: {result.Error}",
                Output = result.Output
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting port proxy rule");
            return new AgentResponse
            {
                CommandId = command.CommandId,
                Success = false,
                Message = $"Exception occurred: {ex.Message}",
                Output = string.Empty
            };
        }
    }

    public async Task<AgentResponse> ListRulesAsync()
    {
        try
        {
            _logger.LogInformation("Listing current port proxy rules");
            var result = await ExecuteNetshCommandAsync("interface portproxy show all");

            var rules = new List<PortProxyRule>();

            if (result.Success && !string.IsNullOrEmpty(result.Output))
            {
                rules = ParseNetshOutput(result.Output);
            }

            return new AgentResponse
            {
                CommandId = Guid.NewGuid().ToString(),
                Success = result.Success,
                Message = result.Success 
                    ? $"Found {rules.Count} port proxy rules"
                    : $"Failed to list rules: {result.Error}",
                Output = result.Output,
                Rules = rules
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing port proxy rules");
            return new AgentResponse
            {
                CommandId = Guid.NewGuid().ToString(),
                Success = false,
                Message = $"Exception occurred: {ex.Message}",
                Output = string.Empty
            };
        }
    }

    public async Task<AgentResponse> ResetAllRulesAsync()
    {
        try
        {
            _logger.LogWarning("Resetting ALL port proxy rules");
            var result = await ExecuteNetshCommandAsync("interface portproxy reset");

            return new AgentResponse
            {
                CommandId = Guid.NewGuid().ToString(),
                Success = result.Success,
                Message = result.Success 
                    ? "Successfully reset all port proxy rules"
                    : $"Failed to reset rules: {result.Error}",
                Output = result.Output
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting port proxy rules");
            return new AgentResponse
            {
                CommandId = Guid.NewGuid().ToString(),
                Success = false,
                Message = $"Exception occurred: {ex.Message}",
                Output = string.Empty
            };
        }
    }

    private async Task<(bool Success, string Output, string Error)> ExecuteNetshCommandAsync(string arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            var success = process.ExitCode == 0;

            _logger.LogDebug("Netsh command completed with exit code: {ExitCode}", process.ExitCode);

            return (success, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute netsh command");
            return (false, string.Empty, ex.Message);
        }
    }

    private List<PortProxyRule> ParseNetshOutput(string output)
    {
        var rules = new List<PortProxyRule>();

        try
        {
            // Parse netsh output format
            // Windows netsh interface portproxy show all format:
            // Listen on ipv4:             Connect to ipv4:
            // 
            // Address         Port        Address         Port
            // --------        ----        --------        ----
            // 0.0.0.0         1813        192.168.123.214 1813
            
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            bool inDataSection = false;
            string currentProtocol = "v4tov4"; // Default protocol
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                // Detect protocol from section headers
                if (line.Contains("Listen on ipv4") && line.Contains("Connect to ipv4"))
                {
                    currentProtocol = "v4tov4";
                    inDataSection = false;
                    continue;
                }
                else if (line.Contains("Listen on ipv4") && line.Contains("Connect to ipv6"))
                {
                    currentProtocol = "v4tov6";
                    inDataSection = false;
                    continue;
                }
                else if (line.Contains("Listen on ipv6") && line.Contains("Connect to ipv4"))
                {
                    currentProtocol = "v6tov4";
                    inDataSection = false;
                    continue;
                }
                else if (line.Contains("Listen on ipv6") && line.Contains("Connect to ipv6"))
                {
                    currentProtocol = "v6tov6";
                    inDataSection = false;
                    continue;
                }
                
                // Skip header row and dashes
                if (line.Contains("Address") && line.Contains("Port"))
                {
                    inDataSection = false;
                    continue;
                }
                if (line.Contains("----"))
                {
                    inDataSection = true;
                    continue;
                }
                
                // Parse data rows
                if (inDataSection)
                {
                    // Parse line format: "0.0.0.0         1813        192.168.123.214 1813"
                    // Split by whitespace and filter empty entries
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (parts.Length >= 4)
                    {
                        rules.Add(new PortProxyRule
                        {
                            Protocol = currentProtocol,
                            ListenAddress = parts[0],
                            ListenPort = parts[1],
                            ConnectAddress = parts[2],
                            ConnectPort = parts[3]
                        });
                        
                        _logger.LogDebug("Parsed rule: {Protocol} {ListenAddress}:{ListenPort} -> {ConnectAddress}:{ConnectPort}", 
                            currentProtocol, parts[0], parts[1], parts[2], parts[3]);
                    }
                }
            }

            _logger.LogInformation("Parsed {Count} rules from netsh output", rules.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing netsh output");
        }

        return rules;
    }
}