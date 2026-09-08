using Zenner.Communication.Avalonia.ViewModels;

namespace ProtocolSimulator.ViewModels
{
    public sealed class SettingWindowViewModel : ViewModelBase
    {
        public CommunicationPanelViewModel CommunicationPanel { get; }

        public SettingWindowViewModel(
            CommunicationPanelViewModel communicationPanel)
        {
            CommunicationPanel = communicationPanel;
        }
    }
}
