using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using DroidServiceTest.Core.Ioc;
using DroidServiceTest.Core.Logging.Logger;
using DroidServiceTest.Core.Logging.Model;


namespace DroidServiceTest.Core.Logging
{
    /// <summary>
    /// type = platform will using PlatformLogger from LogPlatformServices
    /// <![CDATA[
    /// LogFactory will need to be initialized similar to below at application startup
    ///        Container.Instance.Initialize((cc) =>
    ///        {
    ///            cc.RegisterTypeAs<TestPlatformServices, IPlatformServices>(true);
    ///            //OPTIONAL - LogFactory will be used if no type found in container
    ///            cc.RegisterTypeAs<LogFactory, ILogFactory>(true);
    ///
    ///        });
    /// ]]>
    /// </summary>

    public sealed class LogFactory : ILogFactory
    {
        private readonly List<LoggerWrapper> _loggers = new List<LoggerWrapper>();
        private readonly Dictionary<string, ILogger> _concreteLoggers = new Dictionary<string, ILogger>();
        private Dictionary<string, TypeInfo> _loggerTypes = new Dictionary<string, TypeInfo>();
        private const string PlatformLoggerType = "platform";
        private readonly NullPlatformServices _defaultService = new NullPlatformServices(null, new NullLogger());
        private static ILogFactory _instance;
        private static readonly object LockObject = new object();
        private static readonly SemaphoreSlim _initFileCreateLock = new SemaphoreSlim(1, 1);


        private LogFactory()
        {
            Initialize();
        }

        public static ILogFactory Instance
        {
            get
            {
                // http://csharpindepth.com/Articles/General/Singleton.aspx
                // Using 2nd Version of singleton - simple thread-safety
                // Version 4 - thread safety w/o locks will crash if there are inter-dependencies whereas version 2
                //             will have a race condition at the line below. The race condition is easier to debug
                //             be cause the crash isn't clear as to what the problem is. 
                _initFileCreateLock.Wait();
                try
                {
                    if (_instance == null)
                    {
                        _instance = Container.Instance.TryResolve<ILogFactory>(new LogFactory());
                    }
                }
                finally
                {
                    _initFileCreateLock.Release();
                }

                return _instance;
            }
            private set => _instance = value;
        }

        internal void Reset()
        {
            Instance = null;
        }

        /// <summary>
        /// Get a logger by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ILogger GetLogger(string name)
        {
            // first check to see if any of the registered loggers have rules that match
            if (_loggers.Any(e => e.Rule.NameMatches(name)))
            {
                var matchedLoggers = _loggers.Where(e => e.Rule.NameMatches(name)).ToList();

                // if any of the matching loggers are Final
                if (matchedLoggers.Any(e => e.Rule.Final))
                {
                    // then return that single logger
                    return matchedLoggers.First(e => e.Rule.Final);
                }

                // otherwise return all matching loggers
                var ret = new MultiLogger(name);
                foreach (var logger in matchedLoggers)
                {
                    ret.Add(logger);
                }
                return ret;
            }

            return Container.Instance.TryResolve<IPlatformService>(_defaultService).PlatformLogger ?? new NullLogger();
        }

        /// <summary>
        /// Get a logger by class type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ILogger GetLogger(Type type) { return GetLogger(type.FullName); }
        public ILogger GetLogger<T>() { return GetLogger(typeof(T).FullName); }

        /// <summary>
        /// Initialize the factory.
        /// </summary>
        private void Initialize()
        {
            var psvc = Container.Instance.TryResolve<IPlatformService>(_defaultService);
            var loggingConfig = InitializeLoggerConfig();
            _loggerTypes = InitializeLoggerTypeInfo();
            if (loggingConfig == null)
            {
                loggingConfig = new LoggingConfig();
            }

            var platformlogger = psvc.PlatformLogger;
            if (!String.IsNullOrEmpty(loggingConfig.CustomLogFileName))
            {
                platformlogger.Debug($"Searching for custom log file: {loggingConfig.CustomLogFileName}");
                var customConfig = LoadCustomConfig(loggingConfig.CustomLogFileName);
                if (customConfig != null)
                {
                    platformlogger.Debug($"Loading custom log file: {loggingConfig.CustomLogFileName}");
                    loggingConfig = customConfig;
                }
                else platformlogger.Debug("Loading internal log file because the CustomLogFile file could not be loaded.");
            }
            else
            {
                platformlogger.Debug("Loading internal log file because the CustomLogFile name is empty");
            }
            AddLoggerWrappers(loggingConfig);

        }


        /// <summary>
        /// This will attempt to open a file from the internal SD card with the given fileNam.
        /// If found then the files contents will be loaded instead of the default configuration for the logger.
        /// </summary>
        /// <param name="fileName">The name of the file to search for, for example: Messaging.config</param>
        /// <param name="retryCount">If retryCount is >= 2 null will be returned</param>
        private LoggingConfig LoadCustomConfig(string fileName, int retryCount = 0)
        {
            try
            {
                var result = ReadCustomConfigFile(fileName);
                return result != null ? DeserializeLoggingConfig(result) : null;
            }
            catch
            {
                Container.Instance.TryResolve<IPlatformService>(_defaultService).PlatformLogger.Debug($"Unable to load Custom Log file '{fileName}' - retryCount: {retryCount}");
                return retryCount >= 2 ? null : LoadCustomConfig(fileName, ++retryCount);
            }
        }

        /// <summary>
        /// This will attempt to open a file from the internal SD card with the given fileNam.
        /// If found then the files contents will be loaded as alternative configuration for the logger.
        /// </summary>
        /// <param name="fileName">The name of the file to search for, for example: Messaging.config</param>
        /// <returns>
        /// The stream which will be loaded 
        /// </returns>
        private Stream ReadCustomConfigFile(string fileName)
        {
            var platformService = Container.Instance.TryResolve<IPlatformService>(_defaultService);
            var path = platformService.ExternalStoragePath;
            var fName = Path.Combine(path, fileName);


            // Verify the file exists - return if not
            var exists = platformService.FileExists(fName);
            if (!exists)
            {
                platformService.PlatformLogger.Debug($"File doesn't exist: {fName}");
                return null;
            }

            Stream stream;
            try
            {
                // Open File
                platformService.PlatformLogger.Debug($"Opening Custom Config: {fName}");
                stream = platformService.ReadFile(fName);
            }
            catch (Exception fileEx)
            {
                platformService.PlatformLogger.Debug($"Error opening {fName}: {fileEx.Message}");
                throw;
            }
            return stream;
        }

        /// <summary>
        /// Each rule found in loggingConfig will be added to a new LoggerWrapper and then added to the _loggers list.
        /// </summary>
        /// <param name="loggingConfig">Rules to add to _loggers</param>
        private void AddLoggerWrappers(LoggingConfig loggingConfig)
        {
            foreach (var rule in loggingConfig.Rules)
            {
                try
                {
                    // If the LoggingConfig does not have a Target for the current rule then continue on.
                    if (!loggingConfig.Targets.Any(t => t.Name.Equals(rule.WriteTo))) continue;

                    ILogger logger;
                    if (_concreteLoggers.ContainsKey(rule.WriteTo))
                    {
                        logger = _concreteLoggers[rule.WriteTo];
                    }
                    else
                    {
                        var target = loggingConfig.Targets.First(t => t.Name.Equals(rule.WriteTo));
                        if (string.Compare(target.Type, PlatformLoggerType, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            logger = Container.Instance.TryResolve<IPlatformService>(_defaultService).PlatformLogger;
                        }
                        else
                        {
                            logger = (ILogger)Activator.CreateInstance(_loggerTypes[target.Type].AsType());
                        }
                        if (logger != null && typeof(ILoggerConfig).GetTypeInfo().IsAssignableFrom(logger.GetType().GetTypeInfo()))
                        {
                            ((ILoggerConfig)logger).TargetConfig = target;
                            _concreteLoggers.Add(rule.WriteTo, logger);
                        }
                    }

                    _loggers.Add(new LoggerWrapper
                    {
                        Rule = rule,
                        TargetLogger = logger ?? Container.Instance.TryResolve<IPlatformService>(_defaultService).PlatformLogger ?? new NullLogger()
                    });
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Get types from attributed classes
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, TypeInfo> InitializeLoggerTypeInfo()
        {
            var ret = new Dictionary<string, TypeInfo>();
            var definedTypes = GetType().GetTypeInfo().Assembly.DefinedTypes;
            foreach (var type in definedTypes)
            {
                var attributes = type.GetCustomAttributes<TargetAttribute>(true);
                foreach (var attribute in attributes)
                {
                    ret.Add(attribute.TargetName, type);
                }
            }
            return ret;
        }

        /// <summary>
        /// deserialize the json
        /// </summary>
        /// <returns></returns>
        private LoggingConfig InitializeLoggerConfig()
        {
            var platformServices = Container.Instance.TryResolve<IPlatformService>(_defaultService);

            return platformServices != null ? DeserializeLoggingConfig(platformServices.ConfigFile) : null;
        }

        private static LoggingConfig DeserializeLoggingConfig(Stream stream)
        {
            try
            {
                using (var sr = new StreamReader(stream))
                {
                    var json = sr.ReadToEnd();
                    return JsonConvert.DeserializeObject<LoggingConfig>(json, new StringEnumConverter());
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to open or read load config file.{Environment.NewLine}\t{e.Message.Replace(Environment.NewLine, $"{Environment.NewLine}\t")}");
                return null;
            }
        }

    }
}