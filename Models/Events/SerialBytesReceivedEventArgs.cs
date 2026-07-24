using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolSimulator.Models.Events
{
    public sealed class SerialBytesReceivedEventArgs : EventArgs
    {
        public SerialBytesReceivedEventArgs(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; }
    }
}
