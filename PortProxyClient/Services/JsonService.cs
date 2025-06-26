using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PortProxyClient.Models;

namespace PortProxyClient.Services
{
    public static class JsonService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task<bool> ExportRulesAsync(string filePath, List<PortForwardRule> rules)
        {
            try
            {
                var exportData = new
                {
                    ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Version = "1.0",
                    TotalRules = rules.Count,
                    Rules = rules.Select(rule => new
                    {
                        ListenPort = rule.ListenPort,
                        ListenAddress = rule.ListenAddress,
                        ForwardPort = rule.ForwardPort,
                        ForwardAddress = rule.ForwardAddress,
                        Protocol = rule.Protocol.ToString()
                    }).ToArray()
                };

                var json = JsonSerializer.Serialize(exportData, JsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<(bool Success, List<PortForwardRule> Rules, string Error)> ImportRulesAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, new List<PortForwardRule>(), "File does not exist");

                var json = await File.ReadAllTextAsync(filePath);
                
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (!root.TryGetProperty("rules", out var rulesElement))
                    return (false, new List<PortForwardRule>(), "Invalid file format: missing 'rules' property");

                var rules = new List<PortForwardRule>();

                foreach (var ruleElement in rulesElement.EnumerateArray())
                {
                    try
                    {
                        var rule = new PortForwardRule();

                        if (ruleElement.TryGetProperty("listenPort", out var listenPort))
                            rule.ListenPort = listenPort.GetString() ?? "";

                        if (ruleElement.TryGetProperty("listenAddress", out var listenAddress))
                            rule.ListenAddress = listenAddress.GetString() ?? "";

                        if (ruleElement.TryGetProperty("forwardPort", out var forwardPort))
                            rule.ForwardPort = forwardPort.GetString() ?? "";

                        if (ruleElement.TryGetProperty("forwardAddress", out var forwardAddress))
                            rule.ForwardAddress = forwardAddress.GetString() ?? "";

                        if (ruleElement.TryGetProperty("protocol", out var protocol))
                        {
                            if (Enum.TryParse<ProtocolType>(protocol.GetString(), out var protocolType))
                                rule.Protocol = protocolType;
                        }

                        if (rule.IsValid())
                            rules.Add(rule);
                    }
                    catch
                    {
                        // Skip invalid rules
                        continue;
                    }
                }

                return (true, rules, "");
            }
            catch (Exception ex)
            {
                return (false, new List<PortForwardRule>(), ex.Message);
            }
        }
    }
}