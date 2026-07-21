using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolSimulator.Models
{
    public class LogItem
    {
        public string Message { get; set; }

        public string Level { get; set; }

        public string DateTime { get; set; }

        public IBrush LevelColor { get; set; }
    }
}
