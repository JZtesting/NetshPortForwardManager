using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PortProxyClient.Models;

namespace PortProxyClient.Services
{
    public class NetshService
    {
        public async Task<NetshResult> GetAllRulesAsync()
        {
            return await ExecuteNetshCommandAsync("interface portproxy show all");
        }

        public async Task<List<PortForwardRule>> ParseRulesAsync()
        {
            var result = await GetAllRulesAsync();
            var rules = new List<PortForwardRule>();

            if (!result.Success)
                return rules;

            // Debug: Log the raw output
            System.Diagnostics.Debug.WriteLine($"Netsh output: {result.Output}");

            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                System.Diagnostics.Debug.WriteLine($"Processing line: '{line}'");
                var rule = ParseRuleLine(line);
                if (rule != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Parsed rule: {rule.ListenAddress}:{rule.ListenPort} -> {rule.ForwardAddress}:{rule.ForwardPort}");
                    rules.Add(rule);
                }
            }

            System.Diagnostics.Debug.WriteLine($"Total rules parsed: {rules.Count}");
            return rules;
        }

        public async Task<NetshResult> AddRuleAsync(PortForwardRule rule)
        {
            var command = $"interface portproxy add {rule.ProtocolString} " +
                         $"listenport={rule.ListenPort} " +
                         $"listenaddress={rule.ListenAddress} " +
                         $"connectport={rule.ForwardPort} " +
                         $"connectaddress={rule.ForwardAddress}";

            return await ExecuteNetshCommandAsync(command);
        }

        public async Task<NetshResult> DeleteRuleAsync(PortForwardRule rule)
        {
            var command = $"interface portproxy delete {rule.ProtocolString} " +
                         $"listenport={rule.ListenPort} " +
                         $"listenaddress={rule.ListenAddress}";

            return await ExecuteNetshCommandAsync(command);
        }

        public async Task<NetshResult> ResetAllRulesAsync()
        {
            return await ExecuteNetshCommandAsync("interface portproxy reset");
        }

        private async Task<NetshResult> ExecuteNetshCommandAsync(string arguments)
        {
            var result = new NetshResult();

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    result.Error = "Failed to start netsh process";
                    return result;
                }

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                result.Output = await outputTask;
                result.Error = await errorTask;
                result.ExitCode = process.ExitCode;
                result.Success = process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                result.Success = false;
            }

            return result;
        }

        private static PortForwardRule? ParseRuleLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || 
                line.Contains("Listen on") || 
                line.Contains("Address") ||
                line.Contains("---") ||
                line.Contains("ipv4") ||
                line.Contains("Port"))
                return null;

            // netsh output format: "listenaddress listenport connectaddress connectport"
            // Example: "0.0.0.0        8080            192.168.1.100   80"
            var parts = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
                return null;

            try
            {
                var rule = new PortForwardRule
                {
                    ListenAddress = parts[0],
                    ListenPort = parts[1],
                    ForwardAddress = parts[2],
                    ForwardPort = parts[3],
                    Protocol = ProtocolType.V4ToV4
                };

                return rule.IsValid() ? rule : null;
            }
            catch
            {
                return null;
            }
        }
    }
}