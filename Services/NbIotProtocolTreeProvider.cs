using ProtocolSimulator.Interfaces;
using ProtocolSimulator.Models.DataType;
using ProtocolSimulator.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolSimulator.Services
{
    public class NbIotProtocolTreeProvider : IProtocolTreeProvider
    {
        public ObservableCollection<CommandTreeMode> Build(string protocolName)
        {
            var list = new ObservableCollection<CommandTreeMode>();

            foreach(var item in typeof(NbIot_FuotaDataType).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                string name = item.Name;
                var command = (NbIot_FuotaDataType)item.GetRawConstantValue();
                list.Add(new CommandTreeMode(name, $"Handlers/Lua/{protocolName}/{name}.lua", command));
            }

            return list;
        }
    }
}
