using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PortProxyClient.Models;

namespace PortProxyClient.Services
{
    /// <summary>
    /// Service for persisting and managing rule metadata that isn't stored in netsh
    /// </summary>
    public class RuleMetadataService
    {
        private readonly string _metadataConfigPath;
        private Dictionary<string, RuleMetadata> _ruleMetadata = new();
        
        public RuleMetadataService()
        {
            // Use relative path like other services to match their behavior
            _metadataConfigPath = "rule_metadata.json";
            System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Using metadata file path: {_metadataConfigPath}");
            System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Current working directory: {Environment.CurrentDirectory}");
        }
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Generate a unique key for a rule based on its core netsh properties
        /// </summary>
        public static string GenerateRuleKey(string listenAddress, string listenPort, string agentServerId = "local")
        {
            // Use consistent casing and format
            var normalizedAddress = listenAddress?.ToLowerInvariant() ?? "0.0.0.0";
            var normalizedPort = listenPort ?? "0";
            var normalizedAgent = agentServerId ?? "local";
            
            var key = $"{normalizedAgent}:{normalizedAddress}:{normalizedPort}";
            System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Generated key: {key} from address:{listenAddress}, port:{listenPort}, agent:{agentServerId}");
            return key;
        }

        /// <summary>
        /// Generate a unique key for a PortForwardRule
        /// </summary>
        public static string GenerateRuleKey(PortForwardRule rule)
        {
            var agentId = !string.IsNullOrWhiteSpace(rule.AgentServerId) ? rule.AgentServerId : "local";
            return GenerateRuleKey(rule.ListenAddress, rule.ListenPort, agentId);
        }

        /// <summary>
        /// Load rule metadata from disk
        /// </summary>
        public async Task LoadAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Loading metadata from: {_metadataConfigPath}");
                
                if (!File.Exists(_metadataConfigPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Metadata file does not exist, starting with empty dictionary");
                    _ruleMetadata = new Dictionary<string, RuleMetadata>();
                    return;
                }

                var json = await File.ReadAllTextAsync(_metadataConfigPath);
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Read JSON content: {json}");
                
                var metadata = JsonSerializer.Deserialize<Dictionary<string, RuleMetadata>>(json, JsonOptions);
                _ruleMetadata = metadata ?? new Dictionary<string, RuleMetadata>();
                
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Loaded {_ruleMetadata.Count} metadata entries");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Error loading rule metadata: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Stack trace: {ex.StackTrace}");
                _ruleMetadata = new Dictionary<string, RuleMetadata>();
            }
        }

        /// <summary>
        /// Save rule metadata to disk
        /// </summary>
        public async Task SaveAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_ruleMetadata, JsonOptions);
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Saving {_ruleMetadata.Count} metadata entries to {_metadataConfigPath}");
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] JSON content: {json}");
                await File.WriteAllTextAsync(_metadataConfigPath, json);
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Successfully saved metadata to file");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Error saving rule metadata: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Store metadata for a rule
        /// </summary>
        public async Task StoreRuleMetadataAsync(PortForwardRule rule)
        {
            var key = GenerateRuleKey(rule);
            
            System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Storing metadata for key: {key}");
            System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Rule details - Description: '{rule.Description}', Category: '{rule.Category}', Tags: [{string.Join(", ", rule.Tags)}]");
            
            _ruleMetadata[key] = new RuleMetadata
            {
                Description = rule.Description,
                Category = rule.Category,
                Tags = new List<string>(rule.Tags),
                TargetServerId = rule.TargetServerId,
                TargetServerName = rule.TargetServerName,
                ResolvedTargetAddress = rule.ResolvedTargetAddress,
                CreatedDate = rule.CreatedDate,
                ModifiedDate = DateTime.Now,
                UseDnsName = rule.UseDnsName,
                OriginalAddress = rule.OriginalAddress
            };
            
            System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Metadata dictionary now has {_ruleMetadata.Count} entries");
            await SaveAsync();
        }

        /// <summary>
        /// Get stored metadata for a rule
        /// </summary>
        public RuleMetadata? GetRuleMetadata(string listenAddress, string listenPort, string agentServerId = "local")
        {
            var key = GenerateRuleKey(listenAddress, listenPort, agentServerId);
            return _ruleMetadata.TryGetValue(key, out var metadata) ? metadata : null;
        }

        /// <summary>
        /// Get stored metadata for a rule
        /// </summary>
        public RuleMetadata? GetRuleMetadata(PortForwardRule rule)
        {
            var key = GenerateRuleKey(rule);
            return _ruleMetadata.TryGetValue(key, out var metadata) ? metadata : null;
        }

        /// <summary>
        /// Apply stored metadata to a rule
        /// </summary>
        public void ApplyMetadataToRule(PortForwardRule rule)
        {
            var key = GenerateRuleKey(rule);
            var metadata = GetRuleMetadata(rule);
            
            if (metadata != null)
            {
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Applying stored metadata for key: {key}");
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] Applying description: '{metadata.Description}', category: '{metadata.Category}'");
                
                rule.Description = metadata.Description;
                rule.Category = metadata.Category;
                rule.Tags.Clear();
                rule.Tags.AddRange(metadata.Tags);
                rule.TargetServerId = metadata.TargetServerId;
                rule.TargetServerName = metadata.TargetServerName;
                rule.ResolvedTargetAddress = metadata.ResolvedTargetAddress;
                rule.CreatedDate = metadata.CreatedDate;
                rule.ModifiedDate = metadata.ModifiedDate;
                rule.UseDnsName = metadata.UseDnsName;
                rule.OriginalAddress = metadata.OriginalAddress;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[RuleMetadataService] No stored metadata found for key: {key}");
            }
        }

        /// <summary>
        /// Remove metadata for a rule
        /// </summary>
        public async Task RemoveRuleMetadataAsync(PortForwardRule rule)
        {
            var key = GenerateRuleKey(rule);
            if (_ruleMetadata.Remove(key))
            {
                await SaveAsync();
            }
        }

        /// <summary>
        /// Remove metadata by key components
        /// </summary>
        public async Task RemoveRuleMetadataAsync(string listenAddress, string listenPort, string agentServerId = "local")
        {
            var key = GenerateRuleKey(listenAddress, listenPort, agentServerId);
            if (_ruleMetadata.Remove(key))
            {
                await SaveAsync();
            }
        }

        /// <summary>
        /// Get all stored metadata
        /// </summary>
        public Dictionary<string, RuleMetadata> GetAllMetadata()
        {
            return new Dictionary<string, RuleMetadata>(_ruleMetadata);
        }

        /// <summary>
        /// Clean up orphaned metadata (rules that no longer exist)
        /// </summary>
        public async Task CleanupOrphanedMetadataAsync(IEnumerable<PortForwardRule> currentRules)
        {
            var currentKeys = currentRules.Select(GenerateRuleKey).ToHashSet();
            var keysToRemove = _ruleMetadata.Keys.Where(key => !currentKeys.Contains(key)).ToList();
            
            bool hasChanges = false;
            foreach (var key in keysToRemove)
            {
                _ruleMetadata.Remove(key);
                hasChanges = true;
            }
            
            if (hasChanges)
            {
                await SaveAsync();
            }
        }
    }

    /// <summary>
    /// Metadata for a port forwarding rule that isn't stored in netsh
    /// </summary>
    public class RuleMetadata
    {
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string TargetServerId { get; set; } = string.Empty;
        public string TargetServerName { get; set; } = string.Empty;
        public string ResolvedTargetAddress { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public bool UseDnsName { get; set; } = false;
        public string OriginalAddress { get; set; } = string.Empty;
    }
}