using System;
using System.Text;

namespace DroidServiceTest.Core.Logging.Model
{

    /// <summary>
    /// Message format options:
    ///             ${userid} - Addes UserID if available
    ///             ${application.name} - Add application name if available 
    ///             ${application.version} - Add application version if available
    ///             ${device.id} - Add device id if available
    ///             ${message}" - Message added a log event time
    ///             ${level} - Add message level (debug, warn, etc...)
    ///             ${date} - Add date
    ///             ${membername} - Adds the class name
    ///             ${sourcefile} - Adds the class file
    ///             ${sourcefullfile} - Adds the class file name, including path
    ///             ${sourcelinenumber} - Adds the line number
    /// </summary>
    public class LogMessage
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public string Application { get; set; }
        public string ApplicationVersion { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
        public string DeviceId { get; set; }
        public MessageLevel MessageLevel { get; set; }
        public string MemberName { get; set; }
        public string SourceFile { get; set; }
        public int SourceLineNumber { get; set; }
        public string LoggerName { get; set; }

        private static readonly char[] SourceFileNamePathSeparators =
        {
            '\\',
            '/'
        };

        internal const string OptionUserId = "${userid}";
        internal const string OptionApplicationName = "${application.name}";
        internal const string OptionApplicationVersion = "${application.version}";
        internal const string OptionDeviceId = "${device.id}";
        internal const string OptionMessage = "${message}";
        internal const string OptionLevel = "${level}";
        internal const string OptionDate = "${date}";
        internal const string OptionMemberName = "${membername}";
        internal const string OptionSourceFile = "${sourcefile}";
        internal const string OptionSourceFullFile = "${sourcefullfile}";
        internal const string OptionSourceLineNumber = "${sourcelinenumber}";
        internal const string OptionLoggerName = "${loggername}";
        internal const string OptionExceptionMsg = "${exception.message}";
        internal const string OptionExceptionStacktrace = "${exception.stacktrace}";

        public String ToString(String format)
        {
            var ret = Message;

            // check to see if the format string contains any format options
            if (!format.Contains("${")) return ret;

            var builder = new StringBuilder(format);

            ret = builder.Replace(OptionUserId, UserId)
                .Replace(OptionApplicationName, Application)
                .Replace(OptionApplicationVersion, ApplicationVersion)
                .Replace(OptionDeviceId, DeviceId)
                .Replace(OptionMessage, Message)
                .Replace(OptionLevel, MessageLevel.ToString().Substring(0,1))//"Debug" => "D", "Warn" => "W"
                .Replace(OptionDate, Timestamp.ToString("MM/dd/yyyy HH:mm:ss:fff"))
                .Replace(OptionMemberName, MemberName)
                .Replace(OptionSourceFullFile, SourceFile ?? "")
                .Replace(OptionSourceFile, SourceFile == null ? "" : SourceFile.Substring(SourceFile.LastIndexOfAny(SourceFileNamePathSeparators)+1))
                .Replace(OptionSourceLineNumber, Convert.ToString(SourceLineNumber))
                .Replace(OptionLoggerName, LoggerName)
                .Replace(OptionExceptionMsg, (Exception == null ? "" : "Exception: " + Exception.Message))
                .Replace(OptionExceptionStacktrace, Exception == null ? "" : "StackTrace: " + Exception.StackTrace)
                .ToString();
            return ret;
        }

        public static String ToLayoutHeader(String format)
        {
            var ret = format;
            if (!format.Contains("${")) return ret;
            var builder = new StringBuilder(format);
            ret = builder.Replace(OptionUserId, "UserId")
                .Replace(OptionApplicationName, "Application")
                .Replace(OptionApplicationVersion, "ApplicationVersion")
                .Replace(OptionDeviceId, "DeviceId")
                .Replace(OptionMessage, "Message")
                .Replace(OptionLevel, "MessageLevel")
                .Replace(OptionDate, "Date")
                .Replace(OptionMemberName, "MemberName")
                .Replace(OptionSourceFile, "SourceFile")
                .Replace(OptionSourceFullFile, "SourceFullFile")
                .Replace(OptionSourceLineNumber, "SourceLineNumber")
                .Replace(OptionLoggerName, "LoggerName")
                .Replace(OptionExceptionMsg, "Exception.Message")
                .Replace(OptionExceptionStacktrace, "Exception.StackTrace")
                .ToString();
            return ret;
        }

        public override string ToString()
        {
            return ToStringBuilder.ReflectionToString(this);
        }
    }

}
