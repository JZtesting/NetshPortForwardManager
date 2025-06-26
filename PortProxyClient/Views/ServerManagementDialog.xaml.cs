using System.Windows;
using System.Windows.Input;
using PortProxyClient.Commands;
using PortProxyClient.ViewModels;
using PortProxyClient.Services;

namespace PortProxyClient.Views
{
    public partial class ServerManagementDialog : Window
    {
        public ServerManagementViewModel ViewModel { get; private set; }

        public ServerManagementDialog(ServerService serverService, DnsService dnsService, IAgentCommunicationService agentCommunicationService)
        {
            InitializeComponent();
            
            ViewModel = new ServerManagementViewModel(serverService, dnsService, agentCommunicationService);
            DataContext = ViewModel;
            
            // Set up keyboard shortcuts
            InputBindings.Add(new KeyBinding(new RelayCommand(() => Close()), Key.Escape, ModifierKeys.None));
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}