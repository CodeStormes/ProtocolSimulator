using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtocolSimulator.Models;

namespace ZennerPacketInspector.Utils
{
    [Target("NlogRule")]
    public class NlogRuleTarget: TargetWithLayout
    {
        public static Action<LogItem> OnLogReceived;

        private readonly static Avalonia.Media.IBrush warnBrush = Avalonia.Media.Brushes.Orange;

        protected override void Write(LogEventInfo logEvent)
        {
            var logItem = new LogItem
            {
                DateTime = logEvent.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Level = $"[{logEvent.Level.Name}]",
                Message = logEvent.FormattedMessage,
                LevelColor = logEvent.Level == NLog.LogLevel.Info ? Avalonia.Media.Brushes.Black :
                             logEvent.Level == NLog.LogLevel.Warn ? warnBrush :
                             logEvent.Level == NLog.LogLevel.Error ? Avalonia.Media.Brushes.Red :
                             Avalonia.Media.Brushes.Black
            };

            OnLogReceived?.Invoke(logItem);
        }
    }
}
