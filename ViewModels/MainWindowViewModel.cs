using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;
using NLog;
using ProtocolSimulator.Models;
using ProtocolSimulator.Models.DataType;
using ProtocolSimulator.Models.Events;
using ProtocolSimulator.Services;
using ProtocolSimulator.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;

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

        private readonly Action<LogItem> _logReceivedHandler;

        private readonly SerialPortService _serialPortService;

        public MainWindowViewModel(ILogger<MainWindowViewModel> logger,SerialPortService serialPortService)
        {
            _logger = logger;
            _serialPortService = serialPortService;
            CommandList = InitialCommandList();

            _logReceivedHandler += OnLog;
            NlogRuleTarget.OnLogReceived += _logReceivedHandler;
            _serialPortService.BytesReceived += SerialPortService_BytesReceived;
        }

        private void OnLog(LogItem logItem)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LogItems.Add(logItem);
                if (LogItems.Count > 1000)
                {
                    LogItems.RemoveAt(0);
                }
            });
        }

        private ObservableCollection<CommandTreeMode> InitialCommandList()
        {
            Dictionary<string, CommandTreeMode> result = new();

            foreach (MBusDataType command in Enum.GetValues(typeof(MBusDataType)))
            {
                string[] commamdNode = command.ToString().Split("_");
                string commandType = commamdNode[0];
                string commandName = commamdNode[1];

                if (!result.TryGetValue(commandType, out CommandTreeMode node))
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

            if (value?.IsCommand != true)
            {
                CurrentCommandText = string.Empty;
                return;
            }

            if (value.IsFileExist)
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
            string script = File.ReadAllText(SelectedCommand.HandlerFilePath);

            return script;
        }

        [RelayCommand]
        private void Save()
        {
            try
            {
                File.WriteAllText(Path.Combine(SelectedCommand.HandlerFilePath), CurrentCommandText);
                _logger.LogInformation("文件已存储到路径{path}", SelectedCommand.HandlerFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex.Message);
            }
        }

        [RelayCommand]
        private void Start()
        {
            try
            {
                if (SelectedCommand?.IsCommand != true)
                {
                    _logger.LogWarning("请选择一个具体命令节点。");
                    return;
                }

                if (!_serialPortService.IsOpen)
                {
                    _logger.LogWarning("串口未打开，请先在设置窗口中连接串口。");
                    return;
                }

                byte[] requestBytes = Utils.LuaScriptHelper.BuildCommandFromLua(SelectedCommand.HandlerFilePath);

                _serialPortService.Write(requestBytes);

                _logger.LogInformation("已发送：{bytes}", Utils.LuaScriptHelper.ToHexText(requestBytes));
            }
            catch (Exception ex)
            {
                _logger.LogError("发送失败：{message}", ex.Message);
            }
        }

        private void SerialPortService_BytesReceived(object? sender, SerialBytesReceivedEventArgs eventArgs)
        {
            string hexText = BitConverter.ToString(eventArgs.Data).Replace("-", " ");

            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                LogItems.Add(new LogItem
                {
                    DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    Message = $"RX: {hexText}",
                    LevelColor = Avalonia.Media.Brushes.DodgerBlue
                });
            });
        }
    }
}