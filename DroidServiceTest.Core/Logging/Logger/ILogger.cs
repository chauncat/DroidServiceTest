using System;
using System.Runtime.CompilerServices;
using DroidServiceTest.Core.Logging.Model;

namespace DroidServiceTest.Core.Logging.Logger
{
    public interface ILogger
    {
        void LogMessage(LogMessage message);
        void Trace(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Trace(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Debug(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Info(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Warn(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Warn(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Error(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Fatal(string message, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Fatal(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);

        bool IsTraceEnabled();
        bool IsDebugEnabled();
        bool IsInfoEnabled();
        bool IsWarnEnabled();
        bool IsErrorEnabled();
        bool IsFatalEnabled();

    }
}
