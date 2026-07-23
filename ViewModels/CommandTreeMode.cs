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
        public CommandTreeMode(string displayName, string path, MBusDataType? command = null)
        {
            DisplayName = displayName;
            HandlerFilePath = path;
            Command = command;
        }

        public string DisplayName { get; }

        public string HandlerFilePath;

        public MBusDataType? Command { get; }

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
        private bool _isFileExist;

        [ObservableProperty]
        private bool _isEnable;
    }
}
