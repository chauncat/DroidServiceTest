using DroidServiceTest.Core.Logging.Model;

namespace DroidServiceTest.Core.Logging.Logger
{
    public interface ILoggerConfig
    {
        LoggingTarget TargetConfig { get; set; }

    }
}
