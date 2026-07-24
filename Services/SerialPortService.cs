using ProtocolSimulator.Models;
using ProtocolSimulator.Models.Events;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolSimulator.Services
{
    public sealed class SerialPortService:IDisposable
    {
        public SerialPort? _serialPort;

        public string StatusText { get; private set; } = "Disconnected";

        public bool IsOpen => _serialPort?.IsOpen == true;

        public event EventHandler<SerialBytesReceivedEventArgs>? BytesReceived;

        public event EventHandler? ConnectionChanged;

        public SerialPortService() { }

        public string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        public void Open(SerialPortSettings setting)
        {
            Close();

            _serialPort = new SerialPort
            (
                setting.PortName,
                setting.BaudRate,
                setting.Parity,
                setting.DataBits,
                setting.StopBits
            );

            _serialPort.ReadTimeout = setting.ReadTimeout;
            _serialPort.WriteTimeout = setting.WriteTimeout;

            _serialPort.DataReceived += SerialPort_DataReceived;

            _serialPort.Open();

            StatusText = $"已连接：{setting.PortName} / {setting.BaudRate} / {setting.DataBits} / {setting.Parity} / {setting.StopBits}";
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Write(byte[] data)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                throw new InvalidOperationException("串口未打开，无法发送数据");
            }

            if (data == null || data.Length == 0)
            {
                return;
            }

            _serialPort.Write(data, 0, data.Length);
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                return;
            }

            int byteToRead = _serialPort.BytesToRead;

            if (byteToRead <= 0)
            {
                return;
            }

            byte[] buffer = new byte[byteToRead];

            int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);

            if (bytesRead <= 0)
            {
                return;
            }

            if (bytesRead != buffer.Length)
            {
                byte[] actualBytes = new byte[bytesRead];
                Array.Copy(buffer, actualBytes, bytesRead);
                buffer = actualBytes;
            }

            BytesReceived?.Invoke(this, new SerialBytesReceivedEventArgs(buffer));
        }

        public void Close()
        {
            if (_serialPort == null)
            {
                return;
            }

            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            _serialPort.DataReceived -= SerialPort_DataReceived;
            _serialPort.Dispose();
            _serialPort = null;

            StatusText = "Disconnected";
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Close();
        }

    }
}
