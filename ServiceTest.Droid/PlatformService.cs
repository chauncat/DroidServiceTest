using System;
using System.IO;
using System.Threading;
using Android.App;
using Android.Media;
using Android.Util;
using DroidServiceTest.Core;
using DroidServiceTest.Core.Ioc;
using DroidServiceTest.Core.Logging;
using DroidServiceTest.Core.Logging.Logger;
using DroidServiceTest.Core.Logging.Model;
using SQLite.Net;
using SQLite.Net.Platform.XamarinAndroid;

namespace ServiceTest.Droid
{
    [IocService(AsSelf = false, AsInterface = true, AsSingleton = true)]
    public class PlatformService : IPlatformService
    {
        private static ILogger _logger;

        /// <summary>
        /// Do not call this in the constructor - It will cause logging to stop working
        /// </summary>
        public ILogger Logger => _logger ?? (_logger = LogFactory.Instance.GetLogger<PlatformService>());

        public virtual ILogger PlatformLogger => new PlatformLogger();

        public string ExternalStoragePath => Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).AbsolutePath;

        public virtual string SecondaryExternalStoragePath
        {
            get
            {
                // GetExternalFilesDir returns primary storage path, force to use SD path which is sdcard1
                var path = Application.Context.GetExternalFilesDir(Android.OS.Environment.DirectoryDcim);
                return path.AbsolutePath.Replace("sdcard0", "sdcard1");
            }
        }

        public SQLiteConnection GetSqlConnection(string dbName)
        {
            var dbFile = Path.Combine(ExternalStoragePath, dbName);

            try
            {
                if (false == VerifyAndCreateFile(dbFile)) return null;
                ScanFile(dbFile);
                return new SQLiteConnection(new SQLitePlatformAndroid(), dbFile);
            }
            catch (Exception ex)
            {
                Logger.Error("Error creating SQLiteConnection object: " + ex.Message, ex);
            }
            return null;
        }

        public void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            Logger.Debug("Started");
            workerThreads = 0;
            completionPortThreads = 0;

            try
            {
                Logger.Debug("Before GetAvailableThreads");

                ThreadPool.GetAvailableThreads(out var worker, out var completionPort);
                workerThreads = worker;
                completionPortThreads = completionPort;
            }
            catch (Exception e)
            {
                Logger.Error("Unable to get ");
            }

            Logger.Debug("Finihsed");
        }

        public virtual System.IO.Stream ConfigFile
        {
            get
            {
                var stream = Application.Context.Assets.Open("CoreLogging.config");
                return stream;
            }
        }

        public virtual void ScanFile(string filePath)
        {

            try
            {
                MediaScannerConnection.ScanFile(Application.Context.ApplicationContext, new[] { filePath }, null, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception when calling ScanFile: {ex.Message}");
                // An exception because the scan failed should not cause a crash or even any problems with this app.
                // So, no need to throw the exception. Log it, move on. Nothing to see here. 

            }
        }

        public bool VerifyAndCreateFile(string file)
        {
            try
            {
                if (!Directory.Exists(ExternalStoragePath))
                {
                    Logger.Debug($"Directory {ExternalStoragePath} does not exist, creating.");
                    Directory.CreateDirectory(ExternalStoragePath);
                    Logger.Debug($"Directory {ExternalStoragePath} created.");
                }
                if (!File.Exists(file))
                {
                    Logger.Debug($"File {file} does not exist, creating.");
                    File.Create(file);
                    Logger.Debug($"File {file} created.");
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public System.IO.Stream ReadFile(string file)
        {
            return FileExists(file) ? File.OpenRead(file) : null;
        }

        public bool FileExists(string file)
        {
            return File.Exists(file);
        }

    }

    public class PlatformLogger : BaseLogger, ILoggerConfig
    {
        public override void LogMessage(LogMessage message)
        {
            if (message == null) return;

            var layout = TargetConfig?.Layout ?? "${date}|${level}|${application.version}|${device.id}|${userid}|${sourcefile}|${membername}|${sourcelinenumber}|${message}|${exception.message}|${exception.stacktrace}";
            var name = TargetConfig?.Name ?? "XPO_UnknownApp";

            switch (message.MessageLevel)
            {
                case MessageLevel.Trace:
                case MessageLevel.Debug:
                    Log.Debug(name, message.ToString(layout));
                    break;
                case MessageLevel.Error:
                    Log.Error(name, message.ToString(layout));
                    break;
                case MessageLevel.Fatal:
                    Log.Wtf(name, message.ToString(layout));
                    break;
                case MessageLevel.Info:
                    Log.Info(name, message.ToString(layout));
                    break;
                case MessageLevel.Warn:
                    Log.Warn(name, message.ToString(layout));
                    break;
            }
        }

        public LoggingTarget TargetConfig { get; set; }
    }
}