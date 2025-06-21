using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using NetshPortForwardManager.Models;

namespace NetshPortForwardManager.ViewModels
{
    public class AddEditRuleViewModel : INotifyPropertyChanged
    {
        private PortForwardRule _rule;
        private string _validationMessage = string.Empty;

        public AddEditRuleViewModel(PortForwardRule? existingRule = null)
        {
            _rule = existingRule != null ? CloneRule(existingRule) : new PortForwardRule
            {
                ListenAddress = "0.0.0.0",
                Protocol = ProtocolType.V4ToV4
            };

            _rule.PropertyChanged += Rule_PropertyChanged;
            UpdatePreviewCommand();
        }

        public PortForwardRule Rule => _rule;

        public string ListenPort
        {
            get => _rule.ListenPort;
            set
            {
                _rule.ListenPort = value;
                OnPropertyChanged();
                UpdatePreviewCommand();
                ValidateInput();
            }
        }

        public string ListenAddress
        {
            get => _rule.ListenAddress;
            set
            {
                _rule.ListenAddress = value;
                OnPropertyChanged();
                UpdatePreviewCommand();
                ValidateInput();
            }
        }

        public string ForwardPort
        {
            get => _rule.ForwardPort;
            set
            {
                _rule.ForwardPort = value;
                OnPropertyChanged();
                UpdatePreviewCommand();
                ValidateInput();
            }
        }

        public string ForwardAddress
        {
            get => _rule.ForwardAddress;
            set
            {
                _rule.ForwardAddress = value;
                OnPropertyChanged();
                UpdatePreviewCommand();
                ValidateInput();
            }
        }

        public ProtocolType Protocol
        {
            get => _rule.Protocol;
            set
            {
                _rule.Protocol = value;
                OnPropertyChanged();
                UpdatePreviewCommand();
            }
        }

        private string _previewCommand = string.Empty;
        public string PreviewCommand
        {
            get => _previewCommand;
            set
            {
                _previewCommand = value;
                OnPropertyChanged();
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                _validationMessage = value;
                OnPropertyChanged();
            }
        }

        public bool ValidateAndSave()
        {
            ValidateInput();
            return string.IsNullOrEmpty(ValidationMessage);
        }

        private void ValidateInput()
        {
            var errors = new List<string>();

            if (!IsValidPort(ListenPort))
                errors.Add("Listen port must be between 1 and 65535");

            if (!IsValidPort(ForwardPort))
                errors.Add("Forward port must be between 1 and 65535");

            if (!IsValidAddress(ListenAddress))
                errors.Add("Listen address must be a valid IP address or localhost");

            if (!IsValidAddress(ForwardAddress))
                errors.Add("Forward address must be a valid IP address or localhost");

            ValidationMessage = string.Join("; ", errors);
        }

        private void UpdatePreviewCommand()
        {
            if (string.IsNullOrWhiteSpace(ListenPort) || 
                string.IsNullOrWhiteSpace(ListenAddress) ||
                string.IsNullOrWhiteSpace(ForwardPort) || 
                string.IsNullOrWhiteSpace(ForwardAddress))
            {
                PreviewCommand = "Fill in all fields to see command preview";
                return;
            }

            PreviewCommand = $"netsh interface portproxy add {Protocol.ToString().ToLower()} " +
                           $"listenport={ListenPort} listenaddress={ListenAddress} " +
                           $"connectport={ForwardPort} connectaddress={ForwardAddress}";
        }

        private void Rule_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
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

        private static PortForwardRule CloneRule(PortForwardRule original)
        {
            return new PortForwardRule
            {
                ListenPort = original.ListenPort,
                ListenAddress = original.ListenAddress,
                ForwardPort = original.ForwardPort,
                ForwardAddress = original.ForwardAddress,
                Protocol = original.Protocol
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}