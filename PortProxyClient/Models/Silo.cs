using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PortProxyClient.Models
{
    public class Silo : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private string _dataCenter = string.Empty;
        private SiloRole _role = SiloRole.Unknown;
        private SiloStatus _status = SiloStatus.Unknown;
        private DateTime _lastHealthCheck = DateTime.MinValue;

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

        public string DataCenter
        {
            get => _dataCenter;
            set
            {
                _dataCenter = value;
                OnPropertyChanged();
            }
        }

        public SiloRole Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RoleText));
                OnPropertyChanged(nameof(RoleColor));
            }
        }

        public SiloStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public DateTime LastHealthCheck
        {
            get => _lastHealthCheck;
            set
            {
                _lastHealthCheck = value;
                OnPropertyChanged();
            }
        }

        public List<AgentServer> AgentServers { get; set; } = new List<AgentServer>();
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public List<string> Environments { get; set; } = new List<string> { "Test", "Production" };

        // Health metrics 
        public int TotalServers => AgentServers.Count;
        public int OnlineServers => AgentServers.Count(s => s.Status == AgentStatus.Connected);
        public int OfflineServers => AgentServers.Count(s => s.Status == AgentStatus.Disconnected || s.Status == AgentStatus.Error);
        public double HealthPercentage => TotalServers > 0 ? (double)OnlineServers / TotalServers * 100 : 0;

        // UI Helper Properties
        public string RoleText => Role switch
        {
            SiloRole.Active => "ACTIVE",
            SiloRole.Passive => "PASSIVE",
            _ => "UNKNOWN"
        };

        public string RoleColor => Role switch
        {
            SiloRole.Active => "#4CAF50",
            SiloRole.Passive => "#FF9800",
            _ => "#757575"
        };

        public string StatusIcon => Status switch
        {
            SiloStatus.Healthy => "●",
            SiloStatus.Degraded => "◐",
            SiloStatus.Unhealthy => "○",
            _ => "?"
        };

        public string StatusColor => Status switch
        {
            SiloStatus.Healthy => "#4CAF50",
            SiloStatus.Degraded => "#FF9800",
            SiloStatus.Unhealthy => "#F44336",
            _ => "#757575"
        };

        public string HealthText => $"{OnlineServers}/{TotalServers} servers ({HealthPercentage:F0}%)";

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Id) &&
                   !string.IsNullOrWhiteSpace(Name);
        }

        public void UpdateHealthStatus()
        {
            if (TotalServers == 0)
            {
                Status = SiloStatus.Unknown;
                return;
            }

            var healthPercent = HealthPercentage;
            Status = healthPercent switch
            {
                >= 90 => SiloStatus.Healthy,
                >= 70 => SiloStatus.Degraded,
                _ => SiloStatus.Unhealthy
            };

            LastHealthCheck = DateTime.Now;
            OnPropertyChanged(nameof(TotalServers));
            OnPropertyChanged(nameof(OnlineServers));
            OnPropertyChanged(nameof(OfflineServers));
            OnPropertyChanged(nameof(HealthPercentage));
            OnPropertyChanged(nameof(HealthText));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum SiloRole
    {
        Unknown,
        Active,
        Passive
    }

    public enum SiloStatus
    {
        Unknown,
        Healthy,
        Degraded,
        Unhealthy
    }
}