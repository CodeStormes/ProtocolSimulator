using ProtocolSimulator.Models.Events;
using ProtocolSimulator.Utils;
using ProtocolSimulator.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Zenner.Communication.Client;
using Zenner.Communication.Client.Models.Enums;
using Zenner.Communication.Core.Models.Events;

namespace ProtocolSimulator.Services
{
    public sealed class ResponseRouter : IDisposable
    {
        private readonly Func<IEnumerable<CommandTreeMode>> _commandProvider;

        public event EventHandler<SerialBytesReceivedEventArgs>? ResponseSent;

        public event EventHandler<ResponseRouterErrorEventArgs>? ResponseFailed;

        public event EventHandler<SerialBytesReceivedEventArgs>? RequestReceived;

        private readonly CommunicationController _communication;
        private readonly List<byte> _receiveBuffer = new();
        private readonly object _receiveLock = new();

        public ResponseRouter(
            CommunicationController communication,
            Func<IEnumerable<CommandTreeMode>> commandProvider)
        {
            _communication = communication;
            _commandProvider = commandProvider;
            _communication.BytesReceived += OnBytesReceived;
            _communication.ConnectionChanged += OnConnectionChanged;
        }

        private void OnConnectionChanged(object? sender, EventArgs eventArgs)
        {
            lock (_receiveLock)
                _receiveBuffer.Clear();
        }

        private void OnBytesReceived(
            object? sender,
            BytesReceivedEventArgs args)
        {
            if (_communication.Role != CommunicationRole.Slave)
                return;

            if (_communication.Mode != CommunicationMode.Serial &&
                _communication.Mode != CommunicationMode.Irda)
                return;

            var frames = new List<byte[]>();

            lock (_receiveLock)
            {
                _receiveBuffer.AddRange(args.Bytes);

                while (MbusFrameParser.TryTakeFrame(
                    _receiveBuffer,
                    out byte[] frame))
                {
                    frames.Add(frame);
                }
            }

            foreach (byte[] frame in frames)
            {
                if (!IsCompanySpecificRequest(frame))
                    continue;

                RequestReceived?.Invoke(
                    this,
                    new SerialBytesReceivedEventArgs(frame));

                RouteFrame(frame);
            }
        }

        private static bool IsCompanySpecificRequest(byte[] frame)
        {
            return frame.Length >= 14 &&
                   frame[4] == 0x53 &&
                   frame[5] == 0xFE &&
                   frame[6] == 0x51 &&
                   frame[7] == 0x0F;
        }

        private void RouteFrame(byte[] request)
        {
            CommandTreeMode? selectedCommand =
                GetSelectedResponseCommand();

            if (selectedCommand == null)
                return;

            try
            {
                byte[] response = LuaScriptHelper.BuildResponseFromLua(
                    selectedCommand.HandlerFilePath,
                    request);

                if (response.Length == 0)
                    return;

                _communication.SendSlaveResponse(response);

                ResponseSent?.Invoke(
                    this,
                    new SerialBytesReceivedEventArgs(response));
            }
            catch (Exception exception)
            {
                ResponseFailed?.Invoke(
                    this,
                    new ResponseRouterErrorEventArgs(
                        selectedCommand.DisplayName,
                        exception.Message));
            }
        }


        private CommandTreeMode? GetSelectedResponseCommand()
        {
            CommandTreeMode[] selectedCommands = _commandProvider()
                .Where(node => node.IsCommand && node.IsEnable)
                .ToArray();

            if (selectedCommands.Length == 0)
            {
                ResponseFailed?.Invoke(
                    this,
                    new ResponseRouterErrorEventArgs(
                        "未选择响应",
                        "请勾选一个要发送的从机响应。"));

                return null;
            }

            if (selectedCommands.Length > 1)
            {
                ResponseFailed?.Invoke(
                    this,
                    new ResponseRouterErrorEventArgs(
                        "选择冲突",
                        "一次请求只能发送一个响应，请只勾选一个命令。"));

                return null;
            }

            return selectedCommands[0];
        }

        public void Dispose()
        {
            _communication.BytesReceived -= OnBytesReceived;
            _communication.ConnectionChanged -= OnConnectionChanged;

            lock (_receiveLock)
                _receiveBuffer.Clear();
        }
    }
}