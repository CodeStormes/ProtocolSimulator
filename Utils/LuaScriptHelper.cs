using MoonSharp.Interpreter;
using ProtocolSimulator.Models.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolSimulator.Utils
{
    public static class LuaScriptHelper
    {
        public static byte[] BuildCommandFromLua(string luaFilePath)
        {
            var script = new Script();

            script.DoFile(luaFilePath);

            DynValue luaFunction = script.Globals.Get("build_command_request");

            if (luaFunction.Type != MoonSharp.Interpreter.DataType.Function)
            {
                throw new InvalidOperationException("The Lua script does not contain a valid 'build_command_request' function.");
            }

            DynValue luaResult = script.Call(luaFunction);

            if(luaResult.Type != MoonSharp.Interpreter.DataType.Table)
            {
                throw new InvalidOperationException("The 'build_command_request' function did not return a table.");
            }

            return ConvertLuaTableToByteArray(luaResult.Table);
        }

        public static LuaNfcCommand BuildNfcCommandFromLua(string luaFilePath)
        {
            var script = new Script();
            script.DoFile(luaFilePath);

            DynValue function = script.Globals.Get("build_nfc_request");

            if (function.Type != DataType.Function)
                throw new InvalidOperationException(
                    "Lua 中没有 build_nfc_request 函数。");

            DynValue result = script.Call(function);

            if (result.Type != DataType.Table)
                throw new InvalidOperationException(
                    "build_nfc_request 必须返回 table。");

            DynValue commandValue = result.Table.Get("command");
            DynValue dataValue = result.Table.Get("data");

            if (commandValue.Type != DataType.Number)
                throw new InvalidOperationException("command 必须是数字。");

            if (dataValue.Type != DataType.Table)
                throw new InvalidOperationException("data 必须是 table。");

            return new LuaNfcCommand
            {
                Command = checked((byte)commandValue.Number),
                Data = ConvertLuaTableToByteArray(dataValue.Table)
            };     
        }

        public static byte[] ConvertLuaTableToByteArray(Table luaTable)
        {
            var bytes = new List<byte>();

            for(int index = 1; ; index++)
            {
                DynValue luaValue = luaTable.Get(index);

                if(luaValue.Type == MoonSharp.Interpreter.DataType.Nil ||
                    luaValue.Type == MoonSharp.Interpreter.DataType.Void)
                {
                    break;
                }

                if (luaValue.Type != MoonSharp.Interpreter.DataType.Number)
                {
                    throw new InvalidOperationException($"Lua table 第 {index} 项不是数字，无法作为字节发送。");
                }

                int byteValue = (int)luaValue.Number;

                if (byteValue < 0 || byteValue > 255)
                {
                    throw new InvalidOperationException($"Lua table 第 {index} 项超出 byte 范围：{byteValue}。");
                }

                bytes.Add((byte)byteValue);
            }

            return bytes.ToArray();
        }

        public static string ToHexText(byte[] bytes)
        {
            return string.Join(" ", bytes.Select(byteValue => byteValue.ToString("X2")));
        }
    }
}
