using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProtocolSimulator.ViewModels
{
    public partial class ProtocolMenuItem:ObservableObject
    {
        public ProtocolMenuItem(string protocolName,ICommand selectCommand)
        {
            Name = protocolName;
            SelectCommand = selectCommand;
        }

        public string Name { get; }

        public ICommand SelectCommand { get; }

        [ObservableProperty]
        private bool _isChecked;
    }
}
