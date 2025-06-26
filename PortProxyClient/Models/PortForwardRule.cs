using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text.Json.Serialization;

namespace PortProxyClient.Models
{
    public class PortForwardRule : INotifyPropertyChanged
    {
        private string _listenPort = string.Empty;
        private string _listenAddress = string.Empty;
        private string _forwardPort = string.Empty;
        private string _forwardAddress = string.Empty;
        private ProtocolType _protocol = ProtocolType.V4ToV4;
        private string _description = string.Empty;
        private string _category = string.Empty;
        private string _agentServerId = string.Empty;
        private string _targetServerId = string.Empty;
        private string _agentServerName = string.Empty;
        private string _targetServerName = string.Empty;
        private string _resolvedTargetAddress = string.Empty;

        // Legacy properties for backward compatibility
        private string _serverId = string.Empty;
        private string _serverName = string.Empty;
        private bool _useDnsName = false;
        private string _originalAddress = string.Empty;

        public string ListenPort
        {
            get => _listenPort;
            set
            {
                _listenPort = value;
                OnPropertyChanged(nameof(ListenPort));
            }
        }

        public string ListenAddress
        {
            get => _listenAddress;
            set
            {
                _listenAddress = value;
                OnPropertyChanged(nameof(ListenAddress));
            }
        }

        public string ForwardPort
        {
            get => _forwardPort;
            set
            {
                _forwardPort = value;
                OnPropertyChanged(nameof(ForwardPort));
            }
        }

        public string ForwardAddress
        {
            get => _forwardAddress;
            set
            {
                _forwardAddress = value;
                OnPropertyChanged(nameof(ForwardAddress));
            }
        }

        public ProtocolType Protocol
        {
            get => _protocol;
            set
            {
                _protocol = value;
                OnPropertyChanged(nameof(Protocol));
            }
        }


        public bool UseDnsName
        {
            get => _useDnsName;
            set
            {
                _useDnsName = value;
                OnPropertyChanged(nameof(UseDnsName));
            }
        }

        public string OriginalAddress
        {
            get => _originalAddress;
            set
            {
                _originalAddress = value;
                OnPropertyChanged(nameof(OriginalAddress));
            }
        }

        // New properties for enhanced rule management
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }

        public List<string> Tags { get; set; } = new List<string>();

        public string AgentServerId
        {
            get => _agentServerId;
            set
            {
                _agentServerId = value;
                OnPropertyChanged(nameof(AgentServerId));
            }
        }

        public string TargetServerId
        {
            get => _targetServerId;
            set
            {
                _targetServerId = value;
                OnPropertyChanged(nameof(TargetServerId));
            }
        }

        public string AgentServerName
        {
            get => _agentServerName;
            set
            {
                _agentServerName = value;
                OnPropertyChanged(nameof(AgentServerName));
            }
        }

        public string TargetServerName
        {
            get => _targetServerName;
            set
            {
                _targetServerName = value;
                OnPropertyChanged(nameof(TargetServerName));
            }
        }

        public string ResolvedTargetAddress
        {
            get => _resolvedTargetAddress;
            set
            {
                _resolvedTargetAddress = value;
                OnPropertyChanged(nameof(ResolvedTargetAddress));
            }
        }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        public string ProtocolString => Protocol.ToString().ToLower();

        // UI Helper Properties
        public string DisplayAddress => !string.IsNullOrWhiteSpace(ResolvedTargetAddress)
            ? ResolvedTargetAddress
            : (UseDnsName && !string.IsNullOrWhiteSpace(OriginalAddress) 
                ? OriginalAddress 
                : ForwardAddress);

        public string ServerInfo => !string.IsNullOrWhiteSpace(ServerName) 
            ? $"{ServerName} ({DisplayAddress})" 
            : DisplayAddress;

        public string TagsText => Tags.Count > 0 ? string.Join(", ", Tags) : string.Empty;

        public string CategoryBadgeColor => Category?.ToLower() switch
        {
            "web" => "#2196F3",
            "database" => "#4CAF50", 
            "admin" => "#FF9800",
            "api" => "#9C27B0",
            "monitoring" => "#607D8B",
            _ => "#757575"
        };

        public string ShortDescription => Description?.Length > 50 
            ? Description.Substring(0, 47) + "..." 
            : Description ?? string.Empty;

        // Backward compatibility properties
        [JsonIgnore]
        public string ServerId
        {
            get => !string.IsNullOrWhiteSpace(_agentServerId) ? _agentServerId : _serverId;
            set
            {
                if (!string.IsNullOrWhiteSpace(_agentServerId))
                    _agentServerId = value;
                else
                    _serverId = value;
                OnPropertyChanged(nameof(ServerId));
            }
        }

        [JsonIgnore]
        public string ServerName
        {
            get => !string.IsNullOrWhiteSpace(_agentServerName) ? _agentServerName : _serverName;
            set
            {
                if (!string.IsNullOrWhiteSpace(_agentServerName))
                    _agentServerName = value;
                else
                    _serverName = value;
                OnPropertyChanged(nameof(ServerName));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Description) &&
                   IsValidPort(ListenPort) &&
                   IsValidPort(ForwardPort) &&
                   IsValidAddress(ListenAddress) &&
                   IsValidAddress(ForwardAddress);
        }

        private static bool IsValidPort(string port)
        {
            return int.TryParse(port, out int p) && p > 0 && p <= 65535;
        }

        private static bool IsValidAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            // Check if it's a valid IP address
            if (IPAddress.TryParse(address, out _))
                return true;

            // Check if it's localhost
            if (address.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            // Check if it's a valid hostname/DNS name
            return Uri.CheckHostName(address) != UriHostNameType.Unknown;
        }
    }
}