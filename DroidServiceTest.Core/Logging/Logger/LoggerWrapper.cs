using DroidServiceTest.Core.Logging.Model;

namespace DroidServiceTest.Core.Logging.Logger
{
    public class LoggerWrapper : BaseLogger, ILogger
    {
        internal LoggerWrapper(){}
        internal LoggingRule Rule { get; set; }
        internal ILogger TargetLogger { get; set; }
        public override bool IsTraceEnabled() { return Rule.MinLevel == MessageLevel.Trace; }
        public override bool IsDebugEnabled() { return Rule.MinLevel <= MessageLevel.Debug; }
        public override bool IsInfoEnabled() { return Rule.MinLevel <= MessageLevel.Info; }
        public override bool IsWarnEnabled() { return Rule.MinLevel <= MessageLevel.Warn; }
        public override bool IsErrorEnabled() { return Rule.MinLevel <= MessageLevel.Error; }
        public override bool IsFatalEnabled() { return Rule.MinLevel <= MessageLevel.Fatal; }
        public override void LogMessage(LogMessage message) { if (Rule.MinLevel <= message.MessageLevel) { TargetLogger.LogMessage(message); } }
    }
}
