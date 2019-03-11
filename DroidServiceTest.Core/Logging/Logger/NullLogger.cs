using System;
using System.Runtime.CompilerServices;
using DroidServiceTest.Core.Logging.Model;

namespace DroidServiceTest.Core.Logging.Logger
{
    [Target("Null")]
    public class NullLogger : ILogger
    {
        public string Name { get; set; }
        public void LogMessage(LogMessage message) { }
        public bool IsTraceEnabled() { return true; }
        public bool IsDebugEnabled() { return true; }
        public bool IsInfoEnabled() { return true; }
        public bool IsWarnEnabled() { return true; }
        public bool IsErrorEnabled() { return true; }
        public bool IsFatalEnabled() { return true; }
        public MessageLevel LogLevel { get; set; }
        public LoggingTarget TargetConfig { get; set; }

        public void Trace(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
        public void Trace(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
        public void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
        public void Debug(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
        public void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
        public void Info(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
        public void Warn(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
        public void Warn(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
        public void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
        public void Error(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
        public void Fatal(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
        public void Fatal(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0) { }
    }
}
