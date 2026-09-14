using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;
using NLog;
using ProtocolSimulator.Interfaces;
using ProtocolSimulator.Models;
using ProtocolSimulator.Models.DataType;
using ProtocolSimulator.Models.Events;
using ProtocolSimulator.Services;
using ProtocolSimulator.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Zenner.Communication.Client;
using Zenner.Communication.Client.Models.Enums;
using Zenner.Communication.Core.Models.Events;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProtocolSimulator.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty] private string _title = "ProtocolSimulator";

        [ObservableProperty]
        private ObservableCollection<LogItem> _logItems = new ObservableCollection<LogItem>();

        public ObservableCollection<ProtocolMenuItem> ProtocolList { get; } = new();

        [ObservableProperty]
        private ObservableCollection<CommandTreeMode> _commandList = new();

        [ObservableProperty]
        private CommandTreeMode? _selectedCommand;

        [ObservableProperty]
        private string _currentProtocol = string.Empty;

        [ObservableProperty]
        private string _currentCommandText;

        private readonly ILogger<MainWindowViewModel> _logger;

        private readonly Action<LogItem> _logReceivedHandler;

        private readonly SerialPortService _serialPortService;

        private readonly CommunicationController _communication;

        private readonly ResponseRouter _responseRouter;

        private int _wakeupCount;

        public MainWindowViewModel(ILogger<MainWindowViewModel> logger, CommunicationController communication)
        {
            _logger = logger;
            _communication = communication;
            //CommandList = InitialCommandList();
            LoadScripts();

            _communication.BytesReceived += Communication_BytesReceived;
            _logReceivedHandler += OnLog;
            NlogRuleTarget.OnLogReceived += _logReceivedHandler;

            _responseRouter = new ResponseRouter(
                _communication,
                () => CommandList
                    .SelectMany(node => node.Children)
                    .Where(node => node.IsCommand));

            _responseRouter.ResponseSent += ResponseRouter_ResponseSent;
            _responseRouter.ResponseFailed += ResponseRouter_ResponseFailed;
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

        private void LoadScripts()
        {
            var root = Path.Combine(AppContext.BaseDirectory, "Handlers", "Lua");

            if (!Directory.Exists(root))
            {
                return;
            }

            foreach (var item in Directory.EnumerateDirectories(root))
            {
                var name = new DirectoryInfo(item).Name;
                ProtocolList.Add(new ProtocolMenuItem(name, SelectProtocolCommand));
            }

            if (ProtocolList.FirstOrDefault() is { } first)
                SelectProtocol(first);
        }

        public ObservableCollection<CommandTreeMode> BuildCommandTree()
        {
            IProtocolTreeProvider provider = CurrentProtocol.Contains("NbIot", StringComparison.OrdinalIgnoreCase) ? new NbIotProtocolTreeProvider() : new MBusProtocolTreeProvider();

            return provider.Build(CurrentProtocol);
        }

        partial void OnSelectedCommandChanged(CommandTreeMode? value)
        {
            if (value is null)
            {
                CurrentCommandText = string.Empty;
                return;
            }

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
        private void SelectProtocol(ProtocolMenuItem protocolItem)
        {
            if (CurrentProtocol == protocolItem.Name)
                return;

            _logger.LogInformation("加载协议 {protocolName}", protocolItem.Name);

            SelectedCommand = null;
            CurrentCommandText = string.Empty;

            CurrentProtocol = protocolItem.Name;

            foreach (var item in ProtocolList)
                item.IsChecked = ReferenceEquals(item, protocolItem);

            CommandList = BuildCommandTree();
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
        private async Task Start()
        {
            try
            {
                if (SelectedCommand?.IsCommand != true)
                {
                    _logger.LogWarning("请选择具体命令。");
                    return;
                }

                if (!_communication.IsConnected)
                {
                    _logger.LogWarning("请先在设置窗口连接通讯设备。");
                    return;
                }

                if (_communication.Role != CommunicationRole.Master)
                {
                    _logger.LogWarning("当前为从机模式，不能主动发送命令。");
                    return;
                }

                byte[] request = LuaScriptHelper.BuildCommandFromLua(
                    SelectedCommand.HandlerFilePath);

                switch (_communication.Mode)
                {
                    case CommunicationMode.Serial:
                        _communication.SendSerial(request);
                        break;

                    case CommunicationMode.Irda:
                        await _communication.SendIrdaAsync(request);
                        break;

                    case CommunicationMode.DirectNfc:
                        LuaNfcCommand nfcCommand =
                            LuaScriptHelper.BuildNfcCommandFromLua(
                                SelectedCommand.HandlerFilePath);

                        if (nfcCommand.Command == 0x01)
                        {
                            // Identification 是建立 NFC 会话的第一条特殊命令。
                            _communication.SendNfcIdentification();
                        }
                        else
                        {
                            _communication.SendNfc(
                                nfcCommand.Command,
                                nfcCommand.Data);
                        }

                        break;

                    default:
                        throw new InvalidOperationException("未知通讯方式。");
                }

                _logger.LogInformation(
                    "TX: {bytes}",
                    LuaScriptHelper.ToHexText(request));
            }
            catch (Exception exception)
            {
                _logger.LogError("发送失败：{message}", exception.Message);
            }
        }

        private void Communication_BytesReceived(
            object? sender,
            BytesReceivedEventArgs eventArgs)
        {
            byte[] data = eventArgs.Bytes;

            if (data.All(x => x == 0x55))
            {
                _wakeupCount = data.Length;
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    LogItems.Add(new LogItem
                    {
                        DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        Message = $"RX: Wakeup(0x55),count:{_wakeupCount}",
                        LevelColor = Avalonia.Media.Brushes.DodgerBlue
                    });
                });
                return;
            }

            string hex = BitConverter
                .ToString(eventArgs.Bytes)
                .Replace("-", " ");

            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                LogItems.Add(new LogItem
                {
                    DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    Message = $"RX: {hex}",
                    LevelColor = Avalonia.Media.Brushes.DodgerBlue
                });
            });
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

        private void ResponseRouter_ResponseSent(object? sender, SerialBytesReceivedEventArgs eventArgs)
        {
            _logger.LogInformation(
                "TX(应答): {bytes}",
                LuaScriptHelper.ToHexText(eventArgs.Data));
        }

        private void ResponseRouter_ResponseFailed(object? sender, ResponseRouterErrorEventArgs eventArgs)
        {
            _logger.LogError(
                "应答失败 [{command}]: {message}",
                eventArgs.CommandName,
                eventArgs.Message);
        }
    }
}