using System.Windows;
using System.Windows.Input;
using PortProxyClient.Models;
using PortProxyClient.Services;
using PortProxyClient.ViewModels;

namespace PortProxyClient.Views
{
    public partial class AddEditRuleDialog : Window
    {
        private readonly AddEditRuleViewModel _viewModel;

        public PortForwardRule? Rule => _viewModel.Rule;

        public AddEditRuleDialog(PortForwardRule? existingRule = null, ServerService? serverService = null)
        {
            InitializeComponent();
            _viewModel = new AddEditRuleViewModel(existingRule, serverService);
            DataContext = _viewModel;

            Title = existingRule == null ? "Add Port Forward Rule" : "Edit Port Forward Rule";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ValidateAndSave())
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #region Window Chrome Event Handlers

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion
    }
}