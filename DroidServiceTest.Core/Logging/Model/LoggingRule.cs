using System;

namespace DroidServiceTest.Core.Logging.Model
{
    public class LoggingRule
    {
        private string _name;
        private MatchMode _loggerNameMatchMode;
        private string _loggerNameMatchArgument;

        public MessageLevel MinLevel { get; set; }
        public bool Final { get; set; }
        public string WriteTo { get; set; }
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
                int firstPos = _name.IndexOf('*');
                int lastPos = _name.LastIndexOf('*');

                // text
                if (firstPos < 0)
                {
                    _loggerNameMatchMode = MatchMode.Equals;
                    _loggerNameMatchArgument = value;
                    return;
                }

                // *Text or Text* or Te*xt
                if (firstPos == lastPos)
                {
                    string before = Name.Substring(0, firstPos);
                    string after = Name.Substring(firstPos + 1);

                    // *Text
                    if (before.Length > 0)
                    {
                        _loggerNameMatchMode = MatchMode.StartsWith;
                        _loggerNameMatchArgument = before;
                        return;
                    }

                    // Text*
                    if (after.Length > 0)
                    {
                        _loggerNameMatchMode = MatchMode.EndsWith;
                        _loggerNameMatchArgument = after;
                        return;
                    }

                    return;
                }

                // *text*
                if (firstPos == 0 && lastPos == Name.Length - 1)
                {
                    string text = Name.Substring(1, Name.Length - 2);
                    _loggerNameMatchMode = MatchMode.Contains;
                    _loggerNameMatchArgument = text;
                    return;
                }

                _loggerNameMatchMode = MatchMode.None;
                _loggerNameMatchArgument = string.Empty;
            }
        }
        public bool NameMatches(string loggerName)
        {
            if (Name == null) return false;
            if (loggerName == null) return false;

            switch (_loggerNameMatchMode)
            {
                case MatchMode.All:
                    return true;

                default:
                case MatchMode.None:
                    return false;

                case MatchMode.Equals:
                    return loggerName.Equals(_loggerNameMatchArgument, StringComparison.Ordinal);

                case MatchMode.StartsWith:
                    return loggerName.StartsWith(_loggerNameMatchArgument, StringComparison.Ordinal);

                case MatchMode.EndsWith:
                    return loggerName.EndsWith(_loggerNameMatchArgument, StringComparison.Ordinal);

                case MatchMode.Contains:
                    return loggerName.IndexOf(_loggerNameMatchArgument, StringComparison.Ordinal) >= 0;
            }
        }


        internal enum MatchMode
        {
            All,
            None,
            Equals,
            StartsWith,
            EndsWith,
            Contains,
        }
    }
}
