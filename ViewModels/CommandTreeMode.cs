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
        public CommandTreeMode(string displayName, string path, Enum? command = null,string codeText = "")
        {
            DisplayName = displayName;
            HandlerFilePath = path;
            Command = command;
            CodeText = codeText;
        }

        public string DisplayName { get; }

        public string HandlerFilePath;

        public Enum? Command { get; }

        public ObservableCollection<CommandTreeMode> Children { get; } = new ObservableCollection<CommandTreeMode>();

        public bool IsCommand => Command is not null;

        public string CodeText
        {
            get;
        }

        [ObservableProperty]
        private bool _isFileExist;

        [ObservableProperty]
        private bool _isEnable;
    }
}
