using System;
using System.ComponentModel;
using System.Net;

namespace NetshPortForwardManager.Models
{
    public class PortForwardRule : INotifyPropertyChanged
    {
        private string _listenPort = string.Empty;
        private string _listenAddress = string.Empty;
        private string _forwardPort = string.Empty;
        private string _forwardAddress = string.Empty;
        private ProtocolType _protocol = ProtocolType.V4ToV4;

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

        public string ProtocolString => Protocol.ToString().ToLower();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsValid()
        {
            return IsValidPort(ListenPort) &&
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

            return IPAddress.TryParse(address, out _) || 
                   address.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        }
    }
}