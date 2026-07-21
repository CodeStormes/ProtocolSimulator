using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NLog;
using ProtocolSimulator.Models;

namespace ProtocolSimulator.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty] private string _title = "ProtocolSimulator";

        [ObservableProperty]
        public ObservableCollection<LogItem> _logItems = new ObservableCollection<LogItem>();

        [ObservableProperty]
        public ObservableCollection<str> _commandList = new();

        private readonly ILogger<MainWindowViewModel> _logger;

        public MainWindowViewModel(ILogger<MainWindowViewModel> logger)
        {
            _logger = logger;
        }

        [RelayCommand]
        private void Start()
        {
             
        }
    }
}