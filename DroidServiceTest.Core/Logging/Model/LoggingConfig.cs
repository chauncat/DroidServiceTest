using System.Collections.Generic;

namespace DroidServiceTest.Core.Logging.Model
{
    public class LoggingConfig
    {
        public LoggingConfig()
        {
            Rules = new List<LoggingRule>();
            Targets = new List<LoggingTarget>();
        }
        public List<LoggingRule> Rules { get; set; }
        public List<LoggingTarget> Targets { get; set; }

        public string CustomLogFileName { get; set; }

    }
}
