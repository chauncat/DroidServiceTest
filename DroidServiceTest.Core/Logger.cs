using System;
using System.Runtime.CompilerServices;

namespace DroidServiceTest.Core
{
    public class Logger : ILogger
    {
        public void Trace(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "T", memberName, sourceFile, sourceLineNumber);
        }

        public void Trace(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "T", memberName, sourceFile, sourceLineNumber, exception);
        }

        public void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "D", memberName, sourceFile, sourceLineNumber);
        }

        void ILogger.Debug(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "D", memberName, sourceFile, sourceLineNumber, exception);
        }

        public void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "D", memberName, sourceFile, sourceLineNumber);
        }

        public void Info(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "I", memberName, sourceFile, sourceLineNumber, exception);
        }

        public void Warn(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "I", memberName, sourceFile, sourceLineNumber);
        }

        public void Warn(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath]string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "D", memberName, sourceFile, sourceLineNumber, exception);
        }

        public void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "E", memberName, sourceFile, sourceLineNumber);
        }

        public void Error(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "E", memberName, sourceFile, sourceLineNumber, exception);
        }

        public void Fatal(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "F", memberName, sourceFile, sourceLineNumber);
        }

        public void Fatal(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(message, "F", memberName, sourceFile, sourceLineNumber, exception);
        }

        private static readonly char[] SourceFileNamePathSeparators =
        {
            '\\',
            '/'
        };

        private static void LogMessage(string message, string type, string memberName = "", string sourceFile = "", int sourceLineNumber = 0, Exception ex = null)
        {
            var file = sourceFile == null ? "" : sourceFile.Substring(sourceFile.LastIndexOfAny(SourceFileNamePathSeparators) + 1);
            var exception = "";
            if (ex != null)
            {
                exception = $"|{ex.Message}|{ex.StackTrace}|";
            }

            System.Diagnostics.Debug.WriteLine($"{DateTime.Now:MM/dd/yyyy HH:mm:ss:fff}|{type}|{file}|{memberName}|{sourceLineNumber}|{message}|{exception}");
        }
    }

    public interface ILogger
    {
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
        void Fatal(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0);
    }
}