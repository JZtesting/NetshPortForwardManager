using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NetshPortForwardManager.Models;
using NetshPortForwardManager.Services;

namespace NetshPortForwardManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly NetshService _netshService;
        private ObservableCollection<PortForwardRule> _rules;
        private PortForwardRule? _selectedRule;
        private string _statusText;

        public MainViewModel()
        {
            _netshService = new NetshService();
            _rules = new ObservableCollection<PortForwardRule>();
            _statusText = "Initializing...";
            
            // Load rules on startup
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadRulesAsync();
            
            // If no rules loaded and we're in debug mode, add some test data
            #if DEBUG
            if (Rules.Count == 0)
            {
                AddTestRules();
            }
            #endif
        }
        
        #if DEBUG
        private void AddTestRules()
        {
            Rules.Add(new PortForwardRule
            {
                ListenPort = "8080",
                ListenAddress = "0.0.0.0",
                ForwardPort = "80",
                ForwardAddress = "192.168.1.100",
                Protocol = ProtocolType.V4ToV4
            });
            
            Rules.Add(new PortForwardRule
            {
                ListenPort = "3389",
                ListenAddress = "localhost",
                ForwardPort = "3389",
                ForwardAddress = "192.168.1.200",
                Protocol = ProtocolType.V4ToV4
            });
            
            StatusText = "Ready (Test data loaded)";
        }
        #endif

        public ObservableCollection<PortForwardRule> Rules
        {
            get => _rules;
            set
            {
                _rules = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RuleCountText));
            }
        }

        public PortForwardRule? SelectedRule
        {
            get => _selectedRule;
            set
            {
                _selectedRule = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedRule));
            }
        }

        public bool HasSelectedRule => SelectedRule != null;

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public string RuleCountText => $"Rules: {Rules.Count} active";

        public async Task LoadRulesAsync()
        {
            try
            {
                StatusText = "Loading rules...";
                
                // First get the raw netsh output for debugging
                var netshResult = await _netshService.GetAllRulesAsync();
                if (!netshResult.Success)
                {
                    StatusText = $"Failed to execute netsh: {netshResult.Error}";
                    return;
                }

                // Parse the rules
                var rules = await _netshService.ParseRulesAsync();
                
                Rules.Clear();
                foreach (var rule in rules)
                {
                    Rules.Add(rule);
                }
                
                StatusText = rules.Count > 0 ? "Ready" : "No port forwarding rules found";
            }
            catch (Exception ex)
            {
                StatusText = $"Error loading rules: {ex.Message}";
            }
        }

        public async Task<bool> AddRuleAsync(PortForwardRule rule)
        {
            try
            {
                StatusText = "Adding rule...";
                var result = await _netshService.AddRuleAsync(rule);
                
                if (result.Success)
                {
                    Rules.Add(rule);
                    StatusText = "Rule added successfully";
                    return true;
                }
                else
                {
                    StatusText = $"Failed to add rule: {result.Error}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error adding rule: {ex.Message}";
                return false;
            }
        }

        public async Task<bool> DeleteRuleAsync(PortForwardRule rule)
        {
            try
            {
                StatusText = "Deleting rule...";
                var result = await _netshService.DeleteRuleAsync(rule);
                
                if (result.Success)
                {
                    Rules.Remove(rule);
                    if (SelectedRule == rule)
                        SelectedRule = null;
                        
                    StatusText = "Rule deleted successfully";
                    return true;
                }
                else
                {
                    StatusText = $"Failed to delete rule: {result.Error}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error deleting rule: {ex.Message}";
                return false;
            }
        }

        public async Task<bool> ResetAllRulesAsync()
        {
            try
            {
                StatusText = "Resetting all rules...";
                var result = await _netshService.ResetAllRulesAsync();
                
                if (result.Success)
                {
                    Rules.Clear();
                    SelectedRule = null;
                    StatusText = "All rules reset successfully";
                    return true;
                }
                else
                {
                    StatusText = $"Failed to reset rules: {result.Error}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error resetting rules: {ex.Message}";
                return false;
            }
        }

        public async Task<bool> ExportRulesAsync(string filePath)
        {
            try
            {
                StatusText = "Exporting rules...";
                var rules = Rules.ToList();
                var success = await JsonService.ExportRulesAsync(filePath, rules);
                
                if (success)
                {
                    StatusText = $"Exported {rules.Count} rules successfully";
                    return true;
                }
                else
                {
                    StatusText = "Failed to export rules";
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error exporting rules: {ex.Message}";
                return false;
            }
        }

        public async Task<(bool Success, int ImportedCount)> ImportRulesAsync(string filePath, bool replaceExisting = false)
        {
            try
            {
                StatusText = "Importing rules...";
                var (success, importedRules, error) = await JsonService.ImportRulesAsync(filePath);
                
                if (!success)
                {
                    StatusText = $"Failed to import rules: {error}";
                    return (false, 0);
                }

                if (replaceExisting)
                {
                    Rules.Clear();
                }

                int importedCount = 0;
                foreach (var rule in importedRules)
                {
                    // Check for duplicates
                    bool isDuplicate = Rules.Any(existing => 
                        existing.ListenPort == rule.ListenPort && 
                        existing.ListenAddress == rule.ListenAddress);

                    if (!isDuplicate)
                    {
                        Rules.Add(rule);
                        importedCount++;
                    }
                }

                StatusText = $"Imported {importedCount} rules successfully";
                return (true, importedCount);
            }
            catch (Exception ex)
            {
                StatusText = $"Error importing rules: {ex.Message}";
                return (false, 0);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}