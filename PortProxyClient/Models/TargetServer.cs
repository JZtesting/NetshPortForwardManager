using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;

namespace PortProxyClient.Models
{
    public class TargetServer : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _dnsName = string.Empty;
        private string _ipAddress = string.Empty;
        private string _description = string.Empty;
        private string _environment = string.Empty;
        private DateTime _lastDnsCheck = DateTime.MinValue;
        private bool _isDnsResolved = false;

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

        public string DnsName
        {
            get => _dnsName;
            set
            {
                _dnsName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(ResolvedAddress));
            }
        }

        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResolvedAddress));
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

        public string Environment
        {
            get => _environment;
            set
            {
                _environment = value;
                OnPropertyChanged();
            }
        }

        public DateTime LastDnsCheck
        {
            get => _lastDnsCheck;
            set
            {
                _lastDnsCheck = value;
                OnPropertyChanged();
            }
        }

        public bool IsDnsResolved
        {
            get => _isDnsResolved;
            set
            {
                _isDnsResolved = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResolutionStatus));
                OnPropertyChanged(nameof(ResolutionColor));
            }
        }

        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        // UI Helper Properties
        public string DisplayText => $"{Name} ({DnsName})";

        public string ResolvedAddress => !string.IsNullOrWhiteSpace(IpAddress) 
            ? IpAddress 
            : DnsName;

        public string ResolutionStatus => IsDnsResolved ? "✓" : "?";

        public string ResolutionColor => IsDnsResolved ? "#4CAF50" : "#FF9800";

        public string TagsText => Tags.Count > 0 ? string.Join(", ", Tags) : "None";

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Id) &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(DnsName) &&
                   IsValidDnsNameOrIp(DnsName);
        }

        private static bool IsValidDnsNameOrIp(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Check if it's a valid IP address
            if (IPAddress.TryParse(value, out _))
                return true;

            // Check if it's localhost
            if (value.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            // Basic DNS name validation
            return Uri.CheckHostName(value) != UriHostNameType.Unknown;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}