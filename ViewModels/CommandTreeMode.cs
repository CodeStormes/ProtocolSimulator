using CommunityToolkit.Mvvm.ComponentModel;
using ProtocolSimulator.Models.DataType;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolSimulator.ViewModels
{
    public partial class CommandTreeMode : ObservableObject
    {
        public CommandTreeMode(string displayName,MBusDataType? command = null)
        {
            DisplayName = displayName;
            Command = command;
        }

        public string DisplayName { get; }

        MBusDataType? Command { get; }

        public ObservableCollection<CommandTreeMode> Children { get; } = new ObservableCollection<CommandTreeMode>();

        public bool IsCommand => Command.HasValue;

        public string CodeText
        {
            get
            {
                if (!Command.HasValue)
                {
                    return string.Empty;
                }

                return $"0x{(ushort)Command.Value:X4}";
            }
        }

        [ObservableProperty]
        public bool _isEnable;
    }
}
