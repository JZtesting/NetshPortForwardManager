using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace PortProxyClient.Models
{
    /// <summary>
    /// Configuration for agent failover functionality (client-side model)
    /// </summary>
    public class FailoverConfiguration : INotifyPropertyChanged
    {
        private bool _enabled = false;
        private string _healthUrlA = string.Empty;
        private string _healthUrlB = string.Empty;
        private int _checkIntervalSeconds = 30;
        private int _timeoutSeconds = 10;
        private Dictionary<string, string> _serverMappings = new();

        /// <summary>
        /// Whether failover monitoring is enabled
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Health check URL for A servers (primary)
        /// </summary>
        [JsonPropertyName("healthUrlA")]
        public string HealthUrlA
        {
            get => _healthUrlA;
            set
            {
                _healthUrlA = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Health check URL for B servers (failover)
        /// </summary>
        [JsonPropertyName("healthUrlB")]
        public string HealthUrlB
        {
            get => _healthUrlB;
            set
            {
                _healthUrlB = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Interval between health checks in seconds
        /// </summary>
        [JsonPropertyName("checkIntervalSeconds")]
        public int CheckIntervalSeconds
        {
            get => _checkIntervalSeconds;
            set
            {
                _checkIntervalSeconds = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// HTTP timeout for health checks in seconds
        /// </summary>
        [JsonPropertyName("timeoutSeconds")]
        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set
            {
                _timeoutSeconds = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Mapping of A server addresses to B server addresses
        /// Key: A server address, Value: B server address
        /// </summary>
        [JsonPropertyName("serverMappings")]
        public Dictionary<string, string> ServerMappings
        {
            get => _serverMappings;
            set
            {
                _serverMappings = value ?? new Dictionary<string, string>();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Validate the failover configuration
        /// </summary>
        public bool IsValid()
        {
            if (!Enabled)
                return true; // If disabled, always valid

            return !string.IsNullOrWhiteSpace(HealthUrlA) &&
                   !string.IsNullOrWhiteSpace(HealthUrlB) &&
                   CheckIntervalSeconds > 0 &&
                   TimeoutSeconds > 0 &&
                   ServerMappings.Count > 0;
        }

        /// <summary>
        /// Get validation errors
        /// </summary>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (Enabled)
            {
                if (string.IsNullOrWhiteSpace(HealthUrlA))
                    errors.Add("Health URL A is required when failover is enabled");

                if (string.IsNullOrWhiteSpace(HealthUrlB))
                    errors.Add("Health URL B is required when failover is enabled");

                if (CheckIntervalSeconds <= 0)
                    errors.Add("Check interval must be greater than 0");

                if (TimeoutSeconds <= 0)
                    errors.Add("Timeout must be greater than 0");

                if (ServerMappings.Count == 0)
                    errors.Add("At least one server mapping is required");
            }

            return errors;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Current failover status information (client-side model)
    /// </summary>
    public class FailoverStatus
    {
        /// <summary>
        /// Whether failover monitoring is currently enabled
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Whether currently failed over to B servers
        /// </summary>
        [JsonPropertyName("currentlyFailedOver")]
        public bool CurrentlyFailedOver { get; set; }

        /// <summary>
        /// Last health check timestamp
        /// </summary>
        [JsonPropertyName("lastHealthCheck")]
        public DateTime LastHealthCheck { get; set; }

        /// <summary>
        /// Current health status of A endpoint
        /// </summary>
        [JsonPropertyName("healthStatusA")]
        public string HealthStatusA { get; set; } = string.Empty;

        /// <summary>
        /// Current health status of B endpoint
        /// </summary>
        [JsonPropertyName("healthStatusB")]
        public string HealthStatusB { get; set; } = string.Empty;

        /// <summary>
        /// Number of rules currently managed
        /// </summary>
        [JsonPropertyName("rulesManaged")]
        public int RulesManaged { get; set; }

        /// <summary>
        /// Last failover event timestamp
        /// </summary>
        [JsonPropertyName("lastFailoverTime")]
        public DateTime? LastFailoverTime { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
        [JsonPropertyName("lastError")]
        public string LastError { get; set; } = string.Empty;

        /// <summary>
        /// Health check interval in seconds
        /// </summary>
        [JsonPropertyName("checkIntervalSeconds")]
        public int CheckIntervalSeconds { get; set; }

        /// <summary>
        /// UI helper - formatted status text
        /// </summary>
        public string StatusText
        {
            get
            {
                if (!Enabled)
                    return "Disabled";

                if (!string.IsNullOrEmpty(LastError))
                    return $"Error: {LastError}";

                if (CurrentlyFailedOver)
                    return "Failed Over (B Servers Active)";

                return "Normal (A Servers Active)";
            }
        }

        /// <summary>
        /// UI helper - health summary
        /// </summary>
        public string HealthSummary
        {
            get
            {
                if (!Enabled)
                    return "Not monitoring";

                return $"A: {HealthStatusA}, B: {HealthStatusB}";
            }
        }

        /// <summary>
        /// UI helper - time since last check
        /// </summary>
        public string LastCheckText
        {
            get
            {
                if (LastHealthCheck == DateTime.MinValue)
                    return "Never";

                var timeSpan = DateTime.Now - LastHealthCheck;
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalHours < 1)
                    return $"{(int)timeSpan.TotalMinutes} minutes ago";
                if (timeSpan.TotalDays < 1)
                    return $"{(int)timeSpan.TotalHours} hours ago";

                return $"{(int)timeSpan.TotalDays} days ago";
            }
        }
    }
}