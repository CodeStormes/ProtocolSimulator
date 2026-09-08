using System;

namespace ProtocolSimulator.Models.Events
{
    public sealed class ResponseRouterErrorEventArgs : EventArgs
    {
        public ResponseRouterErrorEventArgs(string commandName, string message)
        {
            CommandName = commandName;
            Message = message;
        }

        public string CommandName { get; }

        public string Message { get; }
    }
}