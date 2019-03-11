using System;
using System.Runtime.CompilerServices;
using DroidServiceTest.Core.Logging.Model;

namespace DroidServiceTest.Core.Logging.Logger
{
    /// <summary>
    /// Concrete implementations internal to the SharedCode do not need to implement most of the interface
    /// methods because they are handled by the LoggerWrapper class which is return to the user.
    /// This class was created for the concrete implementations to extend so that the interface contract
    /// can be satisfied leaving them to simply implement the desired LogMessage method and to implement
    /// specific code to their purpose.
    /// Message format options:
    ///             ${userid} - Addes UserID if available
    ///             ${application.name} - Add application name if available 
    ///             ${application.version} - Add application version if available
    ///             ${device.id} - Add device id if available
    ///             ${message}" - Message added a log event time
    ///             ${level} - Add message level (debug, warn, etc...)
    ///             ${date} - Add date
    ///             ${membername} - Adds the class name
    ///             ${sourcefile} - Adds the class file name
    ///             ${sourcefullfile} - Adds the class file name, including path
    ///             ${sourcelinenumber} - Adds the line number
    /// </summary>
    public class BaseLogger : ILogger
    {
        public virtual void LogMessage(LogMessage message)
        {
        }

        public virtual void Trace(string message, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Trace, message, null, memberName, sourceFile, sourceLineNumber));
        }

        public virtual void Trace(string message, Exception exception, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Trace, message, exception, memberName, sourceFile, sourceLineNumber));
        }

        public virtual void Debug(string message, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Debug, message, null, memberName, sourceFile, sourceLineNumber));
        }

        public virtual void Debug(string message, Exception exception, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Debug, message, exception, memberName, sourceFile, sourceLineNumber));
        }

        public virtual void Info(string message, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Info, message, null, memberName, sourceFile, sourceLineNumber));
        }

        public virtual void Info(string message, Exception exception, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Info, message, exception, memberName, sourceFile, sourceLineNumber));
        }

        public virtual void Warn(string message, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Warn, message, null, memberName, sourceFile, sourceLineNumber));
        }

        public virtual void Warn(string message, Exception exception, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Warn, message, exception, memberName, sourceFile, sourceLineNumber));
        }

        public virtual void Error(string message, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Error, message, null, memberName, sourceFile, sourceLineNumber));
        }

        public virtual void Error(string message, Exception exception, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Error, message, exception, memberName, sourceFile, sourceLineNumber));
        }

        public virtual void Fatal(string message, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Fatal, message, null, memberName, sourceFile, sourceLineNumber));
        }

        public virtual void Fatal(string message, Exception exception, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogMessage(CreateLogMessage(MessageLevel.Fatal, message, exception, memberName, sourceFile, sourceLineNumber));
        }

        public virtual bool IsTraceEnabled()
        {
            return false;
        }

        public virtual bool IsDebugEnabled()
        {
            return false;
        }

        public virtual bool IsInfoEnabled()
        {
            return false;
        }

        public virtual bool IsWarnEnabled()
        {
            return false;
        }

        public virtual bool IsErrorEnabled()
        {
            return false;
        }

        public virtual bool IsFatalEnabled()
        {
            return false;
        }

        protected LogMessage CreateLogMessage(MessageLevel messageLevel, string message, Exception e, string memberName,
            string sourceFile, int sourceLineNumber)
        {
            var msg = new LogMessage();
            // Note - logging in the 3 classes below should be minimal and should not exist in the areas that this uses 
            //        them. (IApplication.Name/Version, IDevice.DeviceId & IUser.UserId)
            //        If you log in these areas there will be a race condition in the LogFactor at the creation of the Instance object. 
            //var user = Container.Instance.TryResolveOrDefault<IUser>();
            //var device = Container.Instance.TryResolveOrDefault<IDevice>();
            //var app = Container.Instance.TryResolveOrDefault<IApplication>();

            //msg.Application = app == null ? "" : app.Name;
            //msg.ApplicationVersion = app == null ? "" : app.Version;
            //msg.UserId = user == null ? "" : user.UserId;
            msg.Timestamp = DateTime.Now;
            msg.MessageLevel = messageLevel;
            msg.Message = message;
            msg.Exception = e;
            //msg.DeviceId = device == null ? "" : device.DeviceId;
            msg.MemberName = memberName;
            msg.SourceLineNumber = sourceLineNumber;
            msg.SourceFile = sourceFile;

            return msg;
        }

    }
}
