using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using PortProxyClient.Models;
using PortProxyClient.Services;

namespace PortProxyClient.ViewModels
{
    public class AddEditRuleViewModel : INotifyPropertyChanged
    {
        private PortForwardRule _rule;
        private string _validationMessage = string.Empty;
        private readonly ServerService? _serverService;
        private AgentServer? _selectedAgentServer;
        private TargetServer? _selectedTargetServer;
        private string _tagsText = string.Empty;

        public AddEditRuleViewModel(PortForwardRule? existingRule = null, ServerService? serverService = null)
        {
            _serverService = serverService;
            _rule = existingRule != null ? CloneRule(existingRule) : new PortForwardRule
            {
                ListenAddress = "0.0.0.0",
                Protocol = ProtocolType.V4ToV4,
                Description = string.Empty,
                Category = string.Empty
            };
            
            // Initialize tags text from rule tags
            _tagsText = string.Join(", ", _rule.Tags);

            // Initialize server selection if we have a server service
            if (_serverService != null)
            {
                AvailableAgentServers = _serverService.AgentServers;
                AvailableTargetServers = _serverService.TargetServers;
                
                // If editing an existing rule with server info, try to find the servers
                if (!string.IsNullOrEmpty(_rule.AgentServerId))
                {
                    _selectedAgentServer = _serverService.GetAgentServer(_rule.AgentServerId);
                }
                if (!string.IsNullOrEmpty(_rule.TargetServerId))
                {
                    _selectedTargetServer = _serverService.GetTargetServer(_rule.TargetServerId);
                }
            }
            else
            {
                AvailableAgentServers = new ObservableCollection<AgentServer>();
                AvailableTargetServers = new ObservableCollection<TargetServer>();
            }

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

        // New rule properties
        public string Description
        {
            get => _rule.Description;
            set
            {
                _rule.Description = value;
                OnPropertyChanged();
                ValidateInput();
            }
        }
        
        public string Category
        {
            get => _rule.Category;
            set
            {
                _rule.Category = value;
                OnPropertyChanged();
            }
        }
        
        public string TagsText
        {
            get => _tagsText;
            set
            {
                _tagsText = value;
                OnPropertyChanged();
                UpdateTagsFromText();
            }
        }
        
        // Server Selection Properties
        public ObservableCollection<AgentServer> AvailableAgentServers { get; private set; }
        public ObservableCollection<TargetServer> AvailableTargetServers { get; private set; }
        
        public AgentServer? SelectedAgentServer
        {
            get => _selectedAgentServer;
            set
            {
                _selectedAgentServer = value;
                OnPropertyChanged();
                ValidateInput();
            }
        }
        
        public TargetServer? SelectedTargetServer
        {
            get => _selectedTargetServer;
            set
            {
                _selectedTargetServer = value;
                OnPropertyChanged();
                OnTargetServerSelectionChanged();
            }
        }

        public string WindowTitle => string.IsNullOrEmpty(_rule.Description) ? "Add Rule" : $"Edit: {_rule.Description}";
        
        public bool IsValid => string.IsNullOrEmpty(ValidationMessage);
        
        public bool ValidateAndSave()
        {
            // Update rule with server information before validating
            UpdateRuleFromServerSelection();
            ValidateInput();
            return IsValid;
        }
        
        private void OnTargetServerSelectionChanged()
        {
            if (SelectedTargetServer != null)
            {
                // Use resolved IP if available, otherwise use DNS name
                if (!string.IsNullOrEmpty(SelectedTargetServer.IpAddress))
                {
                    ForwardAddress = SelectedTargetServer.IpAddress;
                }
                else
                {
                    ForwardAddress = SelectedTargetServer.DnsName;
                }
            }
            
            UpdatePreviewCommand();
            ValidateInput();
        }
        
        private void UpdateRuleFromServerSelection()
        {
            // Update agent server information
            if (SelectedAgentServer != null)
            {
                _rule.AgentServerId = SelectedAgentServer.Id;
                _rule.AgentServerName = SelectedAgentServer.Name;
            }
            else
            {
                _rule.AgentServerId = string.Empty;
                _rule.AgentServerName = string.Empty;
            }
            
            // Update target server information
            if (SelectedTargetServer != null)
            {
                _rule.TargetServerId = SelectedTargetServer.Id;
                _rule.TargetServerName = SelectedTargetServer.Name;
                _rule.ResolvedTargetAddress = SelectedTargetServer.IpAddress ?? SelectedTargetServer.DnsName;
            }
            else
            {
                _rule.TargetServerId = string.Empty;
                _rule.TargetServerName = string.Empty;
                _rule.ResolvedTargetAddress = string.Empty;
            }
            
            // Update legacy compatibility properties
            _rule.ServerId = _rule.AgentServerId;
            _rule.ServerName = _rule.AgentServerName;
            _rule.OriginalAddress = SelectedTargetServer?.DnsName ?? string.Empty;
            _rule.UseDnsName = !string.IsNullOrEmpty(_rule.OriginalAddress);
        }

        private void UpdateTagsFromText()
        {
            _rule.Tags.Clear();
            if (!string.IsNullOrWhiteSpace(_tagsText))
            {
                var tags = _tagsText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(t => t.Trim())
                                   .Where(t => !string.IsNullOrEmpty(t))
                                   .ToList();
                _rule.Tags.AddRange(tags);
            }
        }
        
        private void ValidateInput()
        {
            var errors = new List<string>();

            // Required fields
            if (string.IsNullOrWhiteSpace(Description))
                errors.Add("Description is required");
            
            if (SelectedAgentServer == null)
                errors.Add("Agent server selection is required");
                
            if (SelectedTargetServer == null)
                errors.Add("Target server selection is required");

            // Port validation
            if (!IsValidPort(ListenPort))
                errors.Add("Listen port must be between 1 and 65535");

            if (!IsValidPort(ForwardPort))
                errors.Add("Forward port must be between 1 and 65535");

            // Address validation
            if (!IsValidAddress(ListenAddress))
                errors.Add("Listen address must be a valid IP address or localhost");

            if (!IsValidAddress(ForwardAddress))
                errors.Add("Forward address must be a valid IP address or localhost");

            ValidationMessage = string.Join("; ", errors);
            OnPropertyChanged(nameof(IsValid));
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

        public Visibility ValidationVisibility => string.IsNullOrEmpty(ValidationMessage) ? Visibility.Collapsed : Visibility.Visible;
        
        private static PortForwardRule CloneRule(PortForwardRule original)
        {
            var cloned = new PortForwardRule
            {
                ListenPort = original.ListenPort,
                ListenAddress = original.ListenAddress,
                ForwardPort = original.ForwardPort,
                ForwardAddress = original.ForwardAddress,
                Protocol = original.Protocol,
                Description = original.Description,
                Category = original.Category,
                AgentServerId = original.AgentServerId,
                TargetServerId = original.TargetServerId,
                AgentServerName = original.AgentServerName,
                TargetServerName = original.TargetServerName,
                ResolvedTargetAddress = original.ResolvedTargetAddress,
                // Legacy compatibility
                ServerId = original.ServerId,
                ServerName = original.ServerName,
                UseDnsName = original.UseDnsName,
                OriginalAddress = original.OriginalAddress,
                CreatedDate = original.CreatedDate,
                ModifiedDate = DateTime.Now
            };
            
            // Clone tags
            cloned.Tags.AddRange(original.Tags);
            
            return cloned;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}