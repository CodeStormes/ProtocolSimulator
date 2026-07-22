using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NLog;
using ProtocolSimulator.Models;
using ProtocolSimulator.Models.DataType;

namespace ProtocolSimulator.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty] private string _title = "ProtocolSimulator";

        [ObservableProperty]
        public ObservableCollection<LogItem> _logItems = new ObservableCollection<LogItem>();

        [ObservableProperty]
        public ObservableCollection<CommandTreeMode> _commandList = new();

        [ObservableProperty]
        public CommandTreeMode _selectedCommand;

        private readonly ILogger<MainWindowViewModel> _logger;

        public MainWindowViewModel(ILogger<MainWindowViewModel> logger)
        {
            _logger = logger;
            CommandList = InitialCommandList();
        }

        private ObservableCollection<CommandTreeMode> InitialCommandList()
        {
            Dictionary<string, CommandTreeMode> result = new();

            foreach (MBusDataType command in Enum.GetValues(typeof(MBusDataType)))
            {
                string[] commamdNode = command.ToString().Split("_");
                string commandType = commamdNode[0];
                string commandName = commamdNode[1];

                if(!result.TryGetValue(commandType,out CommandTreeMode node))
                {
                    node = new CommandTreeMode(commandType);

                    result.Add(commandType, node);
                }

                node.Children.Add(new CommandTreeMode(commandName, command));
            }
            return new ObservableCollection<CommandTreeMode>(result.Values);
        }

        [RelayCommand]
        private void Start()
        {
             
        }
    }
}