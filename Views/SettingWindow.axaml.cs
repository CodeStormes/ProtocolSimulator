using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ProtocolSimulator.ViewModels;

namespace ProtocolSimulator.Views;

public partial class SettingWindow : Window
{
    public SettingWindow()
    {
        InitializeComponent();
    }

    public SettingWindow(SettingWindowViewModel viewmodel) : this()
    {
        DataContext = viewmodel;
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        Close();
    }
}