using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolSimulator.Models
{
    public class SerialPortSettings
    {
        public string PortName { get; set; } = string.Empty;

        public int BaudRate { get; set; } = 2400;

        public int DataBits { get; set; } = 8;

        public Parity Parity { get; set; } = Parity.Even;

        public StopBits StopBits { get; set; } = StopBits.One;

        public int ReadTimeout { get; set; } = 1000;

        public int WriteTimeout { get; set; } = 1000;
    }
}
