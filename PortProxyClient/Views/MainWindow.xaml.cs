using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using PortProxyClient.Models;
using PortProxyClient.ViewModels;
using PortProxyClient.Views;

namespace PortProxyClient.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        private async void AddRule_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] AddRule_Click: Opening dialog");
            var dialog = new AddEditRuleDialog(null, _viewModel.ServerService);
            var dialogResult = dialog.ShowDialog();
            
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Dialog result: {dialogResult}, Rule null: {dialog.Rule == null}");
            
            if (dialogResult == true && dialog.Rule != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Calling AddRuleAsync with rule: {dialog.Rule.Description}");
                await _viewModel.AddRuleAsync(dialog.Rule);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] Dialog was cancelled or rule was null");
            }
        }

        private async void EditRule_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRule == null)
            {
                MessageBox.Show("Please select a rule to edit.", "No Rule Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new AddEditRuleDialog(_viewModel.SelectedRule, _viewModel.ServerService);
            if (dialog.ShowDialog() == true && dialog.Rule != null)
            {
                // Save metadata for the edited rule before deleting the old one
                await _viewModel.SaveRuleMetadataAsync(dialog.Rule);
                await _viewModel.DeleteRuleAsync(_viewModel.SelectedRule);
                await _viewModel.AddRuleAsync(dialog.Rule);
            }
        }

        private async void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRule == null)
            {
                MessageBox.Show("Please select a rule to delete.", "No Rule Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the rule for port {_viewModel.SelectedRule.ListenPort}?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.DeleteRuleAsync(_viewModel.SelectedRule);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadRulesAsync();
        }

        private async void ResetAllRules_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset ALL port forwarding rules? This cannot be undone.",
                "Confirm Reset All", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.ResetAllRulesAsync();
            }
        }

        private async void ImportRules_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Import Port Forward Rules",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var result = MessageBox.Show(
                    "Do you want to replace existing rules?\n\nYes = Replace all rules\nNo = Add to existing rules\nCancel = Cancel import",
                    "Import Options",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                    return;

                bool replaceExisting = result == MessageBoxResult.Yes;
                var (success, importedCount) = await _viewModel.ImportRulesAsync(openFileDialog.FileName, replaceExisting);

                if (success)
                {
                    MessageBox.Show(
                        $"Successfully imported {importedCount} rules.",
                        "Import Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Failed to import rules. Please check the file format and try again.",
                        "Import Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private async void ExportRules_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Rules.Count == 0)
            {
                MessageBox.Show("No rules to export.", "Export Rules", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Title = "Export Port Forward Rules",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"PortForwardRules_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var success = await _viewModel.ExportRulesAsync(saveFileDialog.FileName);

                if (success)
                {
                    MessageBox.Show(
                        $"Successfully exported {_viewModel.Rules.Count} rules to:\n{saveFileDialog.FileName}",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Failed to export rules. Please check the file path and try again.",
                        "Export Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("NetSh Port Forward Manager v1.0\n\nA graphical interface for managing Windows netsh port forwarding rules.",
                "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #region Window Chrome Event Handlers

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Only handle agent server selection, not silo selection
            if (e.NewValue is AgentServer selectedAgentServer)
            {
                _viewModel.SelectedTreeViewServer = selectedAgentServer;
            }
            else
            {
                _viewModel.SelectedTreeViewServer = null;
            }
        }

        private async void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the clicked item from the TreeView
            if (sender is TreeView treeView && treeView.SelectedItem is AgentServer agentServer)
            {
                // Connect to the double-clicked agent server
                await _viewModel.ConnectToServerAsync(agentServer);
                e.Handled = true;
            }
        }

        #endregion
    }
}