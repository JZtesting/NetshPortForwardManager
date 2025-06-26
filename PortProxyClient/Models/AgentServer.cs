using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;

namespace PortProxyClient.Models
{
    public class AgentServer : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _agentUrl = string.Empty;
        private string _secretKey = string.Empty;
        private string _description = string.Empty;
        private string _siloId = string.Empty;
        private string _environment = string.Empty;
        private AgentStatus _status = AgentStatus.Disconnected;
        private DateTime _lastSeen = DateTime.MinValue;
        private string _version = string.Empty;
        private int _ruleCount = 0;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public string AgentUrl
        {
            get => _agentUrl;
            set
            {
                _agentUrl = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public string SecretKey
        {
            get => _secretKey;
            set
            {
                _secretKey = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public string SiloId
        {
            get => _siloId;
            set
            {
                _siloId = value;
                OnPropertyChanged();
            }
        }

        public string Environment
        {
            get => _environment;
            set
            {
                _environment = value;
                OnPropertyChanged();
            }
        }

        public AgentStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public DateTime LastSeen
        {
            get => _lastSeen;
            set
            {
                _lastSeen = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastSeenText));
            }
        }

        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged();
            }
        }

        public int RuleCount
        {
            get => _ruleCount;
            set
            {
                _ruleCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RuleCountText));
            }
        }

        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        // UI Helper Properties
        public string StatusIcon => Status switch
        {
            AgentStatus.Connected => "●",
            AgentStatus.Disconnected => "○",
            AgentStatus.Error => "⚠",
            AgentStatus.Connecting => "⟳",
            _ => "?"
        };

        public string StatusColor => Status switch
        {
            AgentStatus.Connected => "#4CAF50",
            AgentStatus.Disconnected => "#757575",
            AgentStatus.Error => "#F44336",
            AgentStatus.Connecting => "#FF9800",
            _ => "#757575"
        };

        public string StatusText => Status switch
        {
            AgentStatus.Connected => "Connected",
            AgentStatus.Disconnected => "Disconnected",
            AgentStatus.Error => "Error",
            AgentStatus.Connecting => "Connecting",
            _ => "Unknown"
        };

        public string DisplayText => $"{Name} ({GetUrlHost()})";

        public string LastSeenText => LastSeen == DateTime.MinValue 
            ? "Never" 
            : $"{(DateTime.Now - LastSeen).TotalSeconds:F0}s ago";

        public string RuleCountText => $"{RuleCount} rules";

        private string GetUrlHost()
        {
            try
            {
                if (Uri.TryCreate(AgentUrl, UriKind.Absolute, out var uri))
                {
                    return uri.Host;
                }
                return AgentUrl;
            }
            catch
            {
                return AgentUrl;
            }
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Id) &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(AgentUrl) &&
                   !string.IsNullOrWhiteSpace(SecretKey) &&
                   IsValidAgentUrl(AgentUrl);
        }

        private static bool IsValidAgentUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == "http" || uri.Scheme == "https");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum AgentStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }
}