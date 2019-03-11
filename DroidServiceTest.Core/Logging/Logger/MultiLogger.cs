using System.Collections.Generic;
using System.Linq;
using DroidServiceTest.Core.Logging.Model;

namespace DroidServiceTest.Core.Logging.Logger
{
    public class MultiLogger: BaseLogger, ILogger
    {
        private readonly string _name;
        private readonly List<LoggerWrapper> _loggerList = new List<LoggerWrapper>();

        public MultiLogger(string name)
        {
            _name = name;
        }

        internal int Count { get { return _loggerList.Count; } }

        internal void Add(LoggerWrapper logger) { _loggerList.Add(logger); }
        internal void Remove(LoggerWrapper logger) { _loggerList.Remove(logger); }

        public override bool IsTraceEnabled() { return _loggerList.Any((item) => item.IsTraceEnabled()); }
        public override bool IsDebugEnabled() { return _loggerList.Any((item) => item.IsDebugEnabled()); }
        public override bool IsInfoEnabled() { return _loggerList.Any((item) => item.IsInfoEnabled()); }
        public override bool IsWarnEnabled() { return _loggerList.Any((item) => item.IsWarnEnabled()); }
        public override bool IsErrorEnabled() { return _loggerList.Any((item) => item.IsErrorEnabled()); }
        public override bool IsFatalEnabled() { return _loggerList.Any((item) => item.IsFatalEnabled()); }
        public override void LogMessage(LogMessage message)
        {
            message.LoggerName = _name;
            foreach (var logger in _loggerList.Where(logger => logger.Rule.MinLevel <= message.MessageLevel))
            {
                logger.TargetLogger.LogMessage(message);
            }
        }
    }
}
