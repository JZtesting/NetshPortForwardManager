using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using PortProxyClient.Models;
using PortProxyClient.Services;

namespace PortProxyClient.ViewModels
{
    public class FailoverConfigurationViewModel : INotifyPropertyChanged
    {
        private readonly IAgentCommunicationService _agentCommunicationService;
        private readonly ServerService _serverService;
        private readonly AgentServer _agentServer;
        private FailoverConfiguration _configuration;
        private string _validationMessage = string.Empty;
        private bool _isSaving = false;
        private bool _isLoading = true;

        public FailoverConfigurationViewModel(AgentServer agentServer, IAgentCommunicationService agentCommunicationService, ServerService serverService)
        {
            _agentServer = agentServer ?? throw new ArgumentNullException(nameof(agentServer));
            _agentCommunicationService = agentCommunicationService ?? throw new ArgumentNullException(nameof(agentCommunicationService));
            _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
            
            _configuration = new FailoverConfiguration();
            
            // Load existing configuration
            _ = LoadConfigurationAsync();
        }

        #region Properties

        public string AgentServerName => _agentServer.Name;

        public bool Enabled
        {
            get => _configuration.Enabled;
            set
            {
                _configuration.Enabled = value;
                OnPropertyChanged();
                ValidateConfiguration();
            }
        }

        public string HealthUrlA
        {
            get => _configuration.HealthUrlA;
            set
            {
                _configuration.HealthUrlA = value;
                OnPropertyChanged();
                ValidateConfiguration();
            }
        }

        public string HealthUrlB
        {
            get => _configuration.HealthUrlB;
            set
            {
                _configuration.HealthUrlB = value;
                OnPropertyChanged();
                ValidateConfiguration();
            }
        }

        public int CheckIntervalSeconds
        {
            get => _configuration.CheckIntervalSeconds;
            set
            {
                _configuration.CheckIntervalSeconds = value;
                OnPropertyChanged();
                ValidateConfiguration();
            }
        }

        public int TimeoutSeconds
        {
            get => _configuration.TimeoutSeconds;
            set
            {
                _configuration.TimeoutSeconds = value;
                OnPropertyChanged();
                ValidateConfiguration();
            }
        }

        public int ServerPairsCount => _serverService.ServerPairs.Count;

        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                _validationMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasValidationError));
            }
        }

        public bool HasValidationError => !string.IsNullOrEmpty(ValidationMessage);

        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                _isSaving = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public bool CanSave => !IsSaving && !IsLoading && !HasValidationError;

        #endregion

        #region Commands

        public async Task<bool> SaveConfigurationAsync()
        {
            if (!CanSave)
                return false;

            IsSaving = true;
            try
            {
                // Update configuration with server pairs from ServerService
                _configuration.ServerMappings = _serverService.GetServerPairMappings();

                var result = await _agentCommunicationService.ConfigureFailoverAsync(_agentServer, _configuration);
                
                if (result.Success)
                {
                    ValidationMessage = string.Empty;
                    return true;
                }
                else
                {
                    ValidationMessage = $"Failed to save configuration: {result.Message}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Error saving configuration: {ex.Message}";
                return false;
            }
            finally
            {
                IsSaving = false;
            }
        }


        #endregion

        #region Private Methods

        private async Task LoadConfigurationAsync()
        {
            IsLoading = true;
            try
            {
                var status = await _agentCommunicationService.GetFailoverStatusAsync(_agentServer);
                if (status != null)
                {
                    // Load current configuration values
                    Enabled = status.Enabled;
                    CheckIntervalSeconds = status.CheckIntervalSeconds;
                    
                    // Note: Health URLs and server mappings are not returned in status
                    // They would need to be stored separately or we'd need a separate "get config" endpoint
                }
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Failed to load current configuration: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ValidateConfiguration()
        {
            if (!Enabled)
            {
                ValidationMessage = string.Empty;
                return;
            }

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(HealthUrlA))
                errors.Add("Health URL A is required when failover is enabled");

            if (string.IsNullOrWhiteSpace(HealthUrlB))
                errors.Add("Health URL B is required when failover is enabled");

            if (CheckIntervalSeconds <= 0)
                errors.Add("Check interval must be greater than 0");

            if (TimeoutSeconds <= 0)
                errors.Add("Timeout must be greater than 0");

            if (CheckIntervalSeconds <= TimeoutSeconds)
                errors.Add("Check interval must be greater than timeout");

            if (_serverService.ServerPairs.Count == 0)
                errors.Add("At least one server pair is required (configure in Target Servers tab)");

            ValidationMessage = errors.Count > 0 ? string.Join("; ", errors) : string.Empty;
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}