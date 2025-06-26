using System.Windows;
using System.Windows.Input;
using PortProxyClient.Models;
using PortProxyClient.Services;
using PortProxyClient.ViewModels;

namespace PortProxyClient.Views
{
    /// <summary>
    /// Interaction logic for FailoverConfigurationDialog.xaml
    /// </summary>
    public partial class FailoverConfigurationDialog : Window
    {
        private FailoverConfigurationViewModel ViewModel => (FailoverConfigurationViewModel)DataContext;

        public new bool DialogResult { get; private set; }

        public FailoverConfigurationDialog(AgentServer agentServer, IAgentCommunicationService agentCommunicationService, ServerService serverService)
        {
            InitializeComponent();
            DataContext = new FailoverConfigurationViewModel(agentServer, agentCommunicationService, serverService);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var success = await ViewModel.SaveConfigurationAsync();
            if (success)
            {
                DialogResult = true;
                Close();
            }
            // If save failed, stay open and show validation message
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}