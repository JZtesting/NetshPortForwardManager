using System.Windows;
using NetshPortForwardManager.Models;
using NetshPortForwardManager.ViewModels;

namespace NetshPortForwardManager.Views
{
    public partial class AddEditRuleDialog : Window
    {
        private readonly AddEditRuleViewModel _viewModel;

        public PortForwardRule? Rule => _viewModel.Rule;

        public AddEditRuleDialog(PortForwardRule? existingRule = null)
        {
            InitializeComponent();
            _viewModel = new AddEditRuleViewModel(existingRule);
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
    }
}