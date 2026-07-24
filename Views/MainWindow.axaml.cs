using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ProtocolSimulator.Views
{
    public partial class MainWindow : Window
    {
        private SettingWindow? _settingWindow;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SettingWindow_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_settingWindow != null)
            {
                _settingWindow.Activate();
                return;
            }

            _settingWindow = App.Services.GetRequiredService<SettingWindow>();

            _settingWindow.Closed += (_, _) =>
            {
                _settingWindow = null;
            };

            _settingWindow.ShowDialog(this);
        }
    }
}