
using DroidServiceTest.Core.Ioc;
using DroidServiceTest.Core.Logging.Model;

namespace DroidServiceTest.Core.Logging.Logger
{
     [Target("Platform")]
     [IocService(AsSelf = false, AsInterface = true, AsSingleton = true)]
    public class PlatformLogger : BaseLogger
    {
         #region Implementation of ILogger

         private readonly ILogger _myLogger;
         public PlatformLogger()
         {
             var platform = Container.Instance.Resolve<IPlatformService>();
             if (platform != null)
             {
                 _myLogger = platform.PlatformLogger;
             }
         }
         override public void LogMessage(LogMessage message)
         {
             _myLogger.LogMessage(message);
         }

         #endregion
    }
}
