using ProtocolSimulator.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolSimulator.Interfaces
{
    public interface IProtocolTreeProvider
    {
        ObservableCollection<CommandTreeMode> Build(string protocolName);
    }
}
