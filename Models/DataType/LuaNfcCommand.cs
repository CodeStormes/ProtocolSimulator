using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolSimulator.Models.DataType
{
    public class LuaNfcCommand
    {
        public byte Command { get; set; }

        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
