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
    public class MBusProtocolTreeProvider : IProtocolTreeProvider
    {
        public ObservableCollection<CommandTreeMode> Build(string protocolName)
        {
            Dictionary<string, CommandTreeMode> result = new();

            FieldInfo[] enumFields = typeof(MBusDataType).GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (FieldInfo enumField in enumFields)
            {
                string enumName = enumField.Name;

                var command = (MBusDataType)enumField.GetRawConstantValue();

                string[] commandNode = enumName.Split("_", 2);

                string commandType = commandNode[0];
                string commandName = commandNode.Length > 1 ? commandNode[1] : enumName;

                if (!result.TryGetValue(commandType, out CommandTreeMode node))
                {
                    node = new CommandTreeMode(commandType, $"Handlers/Lua/{protocolName}/{commandName}.lua");

                    result.Add(commandType, node);
                }

                string codeText = $"0x{(ushort)command:X4}";

                node.Children.Add(new CommandTreeMode(commandName, $"Handlers/Lua/{protocolName}/{commandName}.lua", command, codeText));
            }
            return new ObservableCollection<CommandTreeMode>(result.Values);
        }
    }
}
