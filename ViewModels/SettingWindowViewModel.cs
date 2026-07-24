using CommunityToolkit.Mvvm.Input;
using ProtocolSimulator.Models;
using ProtocolSimulator.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolSimulator.ViewModels
{
    public partial class SettingWindowViewModel:ViewModelBase
    {
        public ObservableCollection<string> PortsCollection { get; } = new();

        public ObservableCollection<int> BaudRates { get; } = new()
        {
            2400,
            4800,
            9600,
            19200,
            38400,
            57600,
            115200
        };

        public ObservableCollection<int> DataBitsOptions { get; } = new()
        {
            7,
            8
        };

        public ObservableCollection<Parity> ParityOptions { get; } =
            new ObservableCollection<Parity> { Parity.None, Parity.Odd, Parity.Even };

        public ObservableCollection<StopBits> StopBitsOptions { get; } =
            new ObservableCollection<StopBits> { StopBits.One, StopBits.Two };

        private readonly SerialPortService _serialPortService;

        public SettingWindowViewModel(SerialPortService serialPortService)
        {
            _serialPortService = serialPortService;
            RefreshSerialPorts();
        }

        private string _selectedPortName;

        public string SelectedPortName
        {
            get => _selectedPortName;
            set => SetProperty<string>(ref _selectedPortName, value);
        }

        private int _baudRate = 2400;

        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        private int _dataBits = 8;

        public int DataBits
        {
            get => _dataBits;
            set => SetProperty(ref _dataBits, value);
        }

        private Parity _parity = Parity.Even;

        public Parity Parity
        {
            get => _parity;
            set => SetProperty(ref _parity, value);
        }

        private StopBits _stopBits = StopBits.One;

        public StopBits StopBits
        {
            get => _stopBits;
            set => SetProperty(ref _stopBits, value);
        }

        private string _connectionStatusText = "未连接";

        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set => SetProperty(ref _connectionStatusText, value);
        }

        [RelayCommand]
        public void RefreshSerialPorts()
        {
            PortsCollection.Clear();

            foreach (string portName in _serialPortService.GetPortNames())
            {
                PortsCollection.Add(portName);
            }

            if (SelectedPortName == null && PortsCollection.Count > 0)
            {
                SelectedPortName = PortsCollection[0];
            }

            ConnectionStatusText = PortsCollection.Count == 0
                ? "没有检测到可用串口。"
                : $"检测到 {PortsCollection.Count} 个串口。";
        }

        [RelayCommand]
        private void Connect()
        {
            if (string.IsNullOrWhiteSpace(_selectedPortName))
            {
                ConnectionStatusText = "请选择串口";
                return;
            }

            _serialPortService.Open(new SerialPortSettings
            {
                PortName = SelectedPortName,
                BaudRate = BaudRate,
                DataBits = DataBits,
                Parity = Parity,
                StopBits = StopBits,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            });

            ConnectionStatusText = $"{SelectedPortName} / {BaudRate} / {DataBits} / {Parity} / {StopBits}";
        }
    }
}
