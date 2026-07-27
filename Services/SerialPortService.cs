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

        public async Task SendWakeupAsync(int durationMilliseconds)
        {
            if(_serialPort == null || !_serialPort.IsOpen)
            {
                throw new InvalidOperationException("串口未打开，无法发送唤醒信号");
            }

            int parityBits = _serialPort.Parity == Parity.None ? 0 : 1;
            int stropBits = _serialPort.StopBits == StopBits.Two ? 2 : 1;

            int bitsPerByte = 1 + _serialPort.DataBits + parityBits + stropBits;

            int wakeupByteCount = (int)Math.Ceiling(_serialPort.BaudRate * (durationMilliseconds / 1000.0) / bitsPerByte);

            byte[] wakeupBytes = new byte[wakeupByteCount];

            for(int index = 0;index < wakeupBytes.Length; index++)
            {
                wakeupBytes[index] = 0x55;
            }

            _serialPort.Write(wakeupBytes, 0, wakeupBytes.Length);

            await _serialPort.BaseStream.FlushAsync();

            await Task.Delay(30);
        }

        public void DiscardInBuffer()
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                return;
            }

            _serialPort.DiscardInBuffer();
        }

        private readonly List<byte> _receiveBuffer = new();

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                return;
            }

            int bytesToRead = _serialPort.BytesToRead;

            if (bytesToRead <= 0)
            {
                return;
            }

            byte[] receivedBytes = new byte[bytesToRead];

            int bytesRead = _serialPort.Read(receivedBytes, 0, receivedBytes.Length);

            if (bytesRead <= 0)
            {
                return;
            }

            lock (_receiveBuffer)
            {
                for (int index = 0; index < bytesRead; index++)
                {
                    _receiveBuffer.Add(receivedBytes[index]);
                }

                while (TakeMbusFrame(_receiveBuffer, out byte[] frame))
                {
                    BytesReceived?.Invoke(this, new SerialBytesReceivedEventArgs(frame));
                }
            }
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

        private static bool TakeMbusFrame(List<byte> buffer, out byte[] frame)
        {
            frame = Array.Empty<byte>();

            while (buffer.Count > 0 && buffer[0] != 0x68 && buffer[0] != 0xE5 && buffer[0] != 0x10)
            {
                buffer.RemoveAt(0);
            }

            if (buffer.Count == 0)
            {
                return false;
            }

            if (buffer[0] == 0xE5)
            {
                frame = new byte[] { 0xE5 };
                buffer.RemoveAt(0);
                return true;
            }

            if (buffer[0] == 0x10)
            {
                if (buffer.Count < 5)
                {
                    return false;
                }

                frame = buffer.Take(5).ToArray();
                buffer.RemoveRange(0, 5);
                return true;
            }

            if (buffer.Count < 4)
            {
                return false;
            }

            if (buffer[0] != 0x68 || buffer[3] != 0x68 || buffer[1] != buffer[2])
            {
                buffer.RemoveAt(0);
                return false;
            }

            int payloadLength = buffer[1];
            int frameLength = 4 + payloadLength + 2;

            if (buffer.Count < frameLength)
            {
                return false;
            }

            if (buffer[frameLength - 1] != 0x16)
            {
                buffer.RemoveAt(0);
                return false;
            }

            frame = buffer.Take(frameLength).ToArray();
            buffer.RemoveRange(0, frameLength);
            return true;
        }
    }
}
