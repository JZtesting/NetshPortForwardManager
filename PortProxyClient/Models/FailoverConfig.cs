using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PortProxyClient.Models
{
    public class FailoverConfig : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _activeSilo = string.Empty;
        private string _passiveSilo = string.Empty;
        private FailoverMode _mode = FailoverMode.Manual;
        private int _healthCheckInterval = 30;
        private int _failureThreshold = 3;
        private double _healthThreshold = 80.0;
        private DateTime _lastFailover = DateTime.MinValue;
        private int _cooldownMinutes = 30;
        private bool _isEnabled = false;

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

        public string ActiveSilo
        {
            get => _activeSilo;
            set
            {
                _activeSilo = value;
                OnPropertyChanged();
            }
        }

        public string PassiveSilo
        {
            get => _passiveSilo;
            set
            {
                _passiveSilo = value;
                OnPropertyChanged();
            }
        }

        public FailoverMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ModeText));
            }
        }

        public int HealthCheckInterval
        {
            get => _healthCheckInterval;
            set
            {
                _healthCheckInterval = value;
                OnPropertyChanged();
            }
        }

        public int FailureThreshold
        {
            get => _failureThreshold;
            set
            {
                _failureThreshold = value;
                OnPropertyChanged();
            }
        }

        public double HealthThreshold
        {
            get => _healthThreshold;
            set
            {
                _healthThreshold = value;
                OnPropertyChanged();
            }
        }

        public DateTime LastFailover
        {
            get => _lastFailover;
            set
            {
                _lastFailover = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastFailoverText));
                OnPropertyChanged(nameof(IsInCooldown));
            }
        }

        public int CooldownMinutes
        {
            get => _cooldownMinutes;
            set
            {
                _cooldownMinutes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsInCooldown));
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        // UI Helper Properties
        public string ModeText => Mode switch
        {
            FailoverMode.Manual => "Manual",
            FailoverMode.Automatic => "Automatic",
            _ => "Unknown"
        };

        public string LastFailoverText
        {
            get
            {
                if (LastFailover == DateTime.MinValue)
                    return "Never";

                var timeSpan = DateTime.Now - LastFailover;
                if (timeSpan.TotalDays >= 1)
                    return $"{(int)timeSpan.TotalDays} days ago";
                if (timeSpan.TotalHours >= 1)
                    return $"{(int)timeSpan.TotalHours} hours ago";
                if (timeSpan.TotalMinutes >= 1)
                    return $"{(int)timeSpan.TotalMinutes} minutes ago";
                
                return "Just now";
            }
        }

        public bool IsInCooldown => LastFailover != DateTime.MinValue && 
                                   DateTime.Now < LastFailover.AddMinutes(CooldownMinutes);

        public TimeSpan CooldownRemaining => IsInCooldown ? 
            LastFailover.AddMinutes(CooldownMinutes) - DateTime.Now : 
            TimeSpan.Zero;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Id) &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(ActiveSilo) &&
                   !string.IsNullOrWhiteSpace(PassiveSilo) &&
                   ActiveSilo != PassiveSilo &&
                   HealthCheckInterval > 0 &&
                   FailureThreshold > 0 &&
                   HealthThreshold >= 0 && HealthThreshold <= 100 &&
                   CooldownMinutes >= 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum FailoverMode
    {
        Manual,
        Automatic
    }
}