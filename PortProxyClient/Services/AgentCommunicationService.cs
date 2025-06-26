using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PortProxyClient.Models;

namespace PortProxyClient.Services;

/// <summary>
/// Service for communicating with remote PortProxy agents
/// </summary>
public class AgentCommunicationService : IAgentCommunicationService
{
    private readonly HttpClient _httpClient;
    private readonly AgentEncryptionService _encryptionService;
    private readonly JsonSerializerOptions _jsonOptions;

    public AgentCommunicationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _encryptionService = new AgentEncryptionService();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<bool> TestConnectionAsync(AgentServer agentServer)
    {
        try
        {
            var url = $"{agentServer.AgentUrl}/api/agent/ping";
            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AgentStatusInfo?> GetAgentStatusAsync(AgentServer agentServer)
    {
        try
        {
            var url = $"{agentServer.AgentUrl}/api/agent/status";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AgentStatusInfo>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AgentOperationResult> AddRuleAsync(AgentServer agentServer, PortForwardRule rule)
    {
        var command = new AgentCommand
        {
            Command = "add",
            ListenPort = rule.ListenPort,
            ListenAddress = rule.ListenAddress,
            ConnectPort = rule.ForwardPort,
            ConnectAddress = rule.ForwardAddress,
            Protocol = rule.Protocol.ToString().ToLowerInvariant(),
            Description = rule.Description
        };

        return await ExecuteCommandAsync(agentServer, command);
    }

    public async Task<AgentOperationResult> DeleteRuleAsync(AgentServer agentServer, PortForwardRule rule)
    {
        var command = new AgentCommand
        {
            Command = "delete",
            ListenPort = rule.ListenPort,
            ListenAddress = rule.ListenAddress,
            Protocol = rule.Protocol.ToString().ToLowerInvariant(),
            Description = rule.Description
        };

        return await ExecuteCommandAsync(agentServer, command);
    }

    public async Task<AgentRulesResult> ListRulesAsync(AgentServer agentServer)
    {
        var command = new AgentCommand
        {
            Command = "list"
        };

        return await ExecuteListCommandAsync(agentServer, command);
    }

    public async Task<AgentOperationResult> ResetAllRulesAsync(AgentServer agentServer)
    {
        var command = new AgentCommand
        {
            Command = "reset",
            Description = "Reset all port forwarding rules"
        };

        return await ExecuteCommandAsync(agentServer, command);
    }

    private async Task<AgentOperationResult> ExecuteCommandAsync(AgentServer agentServer, AgentCommand command)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrEmpty(agentServer.AgentUrl))
            {
                return new AgentOperationResult
                {
                    Success = false,
                    Message = "Agent URL is not configured",
                    CommandId = command.CommandId
                };
            }

            if (string.IsNullOrEmpty(agentServer.SecretKey))
            {
                return new AgentOperationResult
                {
                    Success = false,
                    Message = "Agent secret key is not configured",
                    CommandId = command.CommandId
                };
            }

            // Serialize and encrypt the command
            var commandJson = JsonSerializer.Serialize(command, _jsonOptions);
            var encryptedMessage = await _encryptionService.EncryptMessageAsync(commandJson, agentServer.SecretKey);

            // Send to agent
            var url = $"{agentServer.AgentUrl}/api/agent/execute";
            var jsonContent = JsonSerializer.Serialize(encryptedMessage, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new AgentOperationResult
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                    Output = responseContent,
                    CommandId = command.CommandId
                };
            }

            // Decrypt the response
            var encryptedResponse = JsonSerializer.Deserialize<AgentEncryptedMessage>(responseContent, _jsonOptions);
            if (encryptedResponse == null)
            {
                return new AgentOperationResult
                {
                    Success = false,
                    Message = "Failed to deserialize encrypted response",
                    CommandId = command.CommandId
                };
            }

            var decryptedResponse = await _encryptionService.DecryptMessageAsync(encryptedResponse, agentServer.SecretKey);
            var agentResponse = JsonSerializer.Deserialize<AgentResponse>(decryptedResponse, _jsonOptions);

            if (agentResponse == null)
            {
                return new AgentOperationResult
                {
                    Success = false,
                    Message = "Failed to deserialize agent response",
                    Output = decryptedResponse,
                    CommandId = command.CommandId
                };
            }

            return new AgentOperationResult
            {
                Success = agentResponse.Success,
                Message = agentResponse.Message,
                Output = decryptedResponse, // Store the full decrypted response
                Timestamp = agentResponse.Timestamp,
                CommandId = agentResponse.CommandId
            };
        }
        catch (Exception ex)
        {
            return new AgentOperationResult
            {
                Success = false,
                Message = $"Communication error: {ex.Message}",
                CommandId = command.CommandId,
                ExceptionDetails = ex.ToString()
            };
        }
    }

    private async Task<AgentRulesResult> ExecuteListCommandAsync(AgentServer agentServer, AgentCommand command)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrEmpty(agentServer.AgentUrl))
            {
                return new AgentRulesResult
                {
                    Success = false,
                    Message = "Agent URL is not configured",
                    CommandId = command.CommandId
                };
            }

            if (string.IsNullOrEmpty(agentServer.SecretKey))
            {
                return new AgentRulesResult
                {
                    Success = false,
                    Message = "Agent secret key is not configured",
                    CommandId = command.CommandId
                };
            }

            // Serialize and encrypt the command
            var commandJson = JsonSerializer.Serialize(command, _jsonOptions);
            var encryptedMessage = await _encryptionService.EncryptMessageAsync(commandJson, agentServer.SecretKey);

            // Send to agent
            var url = $"{agentServer.AgentUrl}/api/agent/execute";
            var jsonContent = JsonSerializer.Serialize(encryptedMessage, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new AgentRulesResult
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                    Output = responseContent,
                    CommandId = command.CommandId
                };
            }

            // Decrypt the response
            var encryptedResponse = JsonSerializer.Deserialize<AgentEncryptedMessage>(responseContent, _jsonOptions);
            if (encryptedResponse == null)
            {
                return new AgentRulesResult
                {
                    Success = false,
                    Message = "Failed to deserialize encrypted response",
                    CommandId = command.CommandId
                };
            }

            var decryptedResponse = await _encryptionService.DecryptMessageAsync(encryptedResponse, agentServer.SecretKey);
            var agentResponse = JsonSerializer.Deserialize<AgentResponse>(decryptedResponse, _jsonOptions);

            if (agentResponse == null)
            {
                return new AgentRulesResult
                {
                    Success = false,
                    Message = "Failed to deserialize agent response",
                    Output = decryptedResponse,
                    CommandId = command.CommandId
                };
            }

            return new AgentRulesResult
            {
                Success = agentResponse.Success,
                Message = agentResponse.Message,
                Output = decryptedResponse,
                Timestamp = agentResponse.Timestamp,
                CommandId = agentResponse.CommandId,
                Rules = agentResponse.Rules // Include the parsed rules!
            };
        }
        catch (Exception ex)
        {
            return new AgentRulesResult
            {
                Success = false,
                Message = $"Communication error: {ex.Message}",
                CommandId = command.CommandId,
                ExceptionDetails = ex.ToString()
            };
        }
    }

    public async Task<AgentOperationResult> ConfigureFailoverAsync(AgentServer agentServer, FailoverConfiguration config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{agentServer.AgentUrl}/api/agent/configure-failover", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return new AgentOperationResult
                {
                    Success = true,
                    Message = "Failover configuration applied successfully",
                    Output = responseContent
                };
            }
            else
            {
                return new AgentOperationResult
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                    Output = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            return new AgentOperationResult
            {
                Success = false,
                Message = $"Communication error: {ex.Message}",
                ExceptionDetails = ex.ToString()
            };
        }
    }

    public async Task<FailoverStatus?> GetFailoverStatusAsync(AgentServer agentServer)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{agentServer.AgentUrl}/api/agent/failover-status");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var status = JsonSerializer.Deserialize<FailoverStatus>(responseContent, _jsonOptions);
                return status;
            }
            else
            {
                return new FailoverStatus
                {
                    Enabled = false,
                    LastError = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
                };
            }
        }
        catch (Exception ex)
        {
            return new FailoverStatus
            {
                Enabled = false,
                LastError = $"Communication error: {ex.Message}"
            };
        }
    }

    public async Task<AgentOperationResult> ExecuteManualFailoverAsync(AgentServer agentServer, bool failToB)
    {
        try
        {
            var json = JsonSerializer.Serialize(failToB, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{agentServer.AgentUrl}/api/agent/manual-failover", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return new AgentOperationResult
                {
                    Success = true,
                    Message = "Manual failover executed successfully",
                    Output = responseContent
                };
            }
            else
            {
                return new AgentOperationResult
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                    Output = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            return new AgentOperationResult
            {
                Success = false,
                Message = $"Communication error: {ex.Message}",
                ExceptionDetails = ex.ToString()
            };
        }
    }
}