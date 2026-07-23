using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;
using NLog;
using ProtocolSimulator.Models;
using ProtocolSimulator.Models.DataType;

namespace ProtocolSimulator.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty] private string _title = "ProtocolSimulator";

        [ObservableProperty]
        private ObservableCollection<LogItem> _logItems = new ObservableCollection<LogItem>();

        [ObservableProperty]
        private ObservableCollection<CommandTreeMode> _commandList = new();

        [ObservableProperty]
        private CommandTreeMode _selectedCommand;

        [ObservableProperty]
        private string _currentCommandText;

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
                    node = new CommandTreeMode(commandType, $"Handlers/Lua/{commandName}.lua");

                    result.Add(commandType, node);
                }

                node.Children.Add(new CommandTreeMode(commandName, $"Handlers/Lua/{commandName}.lua", command));
            }
            return new ObservableCollection<CommandTreeMode>(result.Values);
        }

        partial void OnSelectedCommandChanged(CommandTreeMode? value)
        {
            value.IsFileExist = File.Exists(value.HandlerFilePath);

            if(value?.IsCommand != true)
            {
                CurrentCommandText = string.Empty;
                return;
            }

            if(value.IsFileExist)
            {
                CurrentCommandText = LoadLuaText(); 
            }
            else
            {
                CurrentCommandText = $"File {value.HandlerFilePath} is not exist! You can add your code in this textbox then save.";
            }
        }

        private string LoadLuaText()
        {
            var lua = new Script();

            return "";
        }

        [RelayCommand]
        private void Save()
        {

        }

        [RelayCommand]
        private void Start()
        {
             
        }
    }
}