using System;
using System.IO;
using Android.App;
using Android.Media;
using Android.OS;
using DroidServiceTest.Core;
using DroidServiceTest.Core.Ioc;
using SQLite.Net;
using SQLite.Net.Platform.XamarinAndroid;
using Stream = System.IO.Stream;

namespace DroidServiceTest.Droid
{
    [IocService(AsSelf = false, AsInterface = true, AsSingleton = true)]
    public class PlatformService : IPlatformService
    {
        private static ILogger _logger;

        /// <summary>
        /// Do not call this in the constructor - It will cause logging to stop working
        /// </summary>
        public ILogger Logger => _logger ?? (_logger = new Logger());

        public string ExternalStoragePath => Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).AbsolutePath;

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

    }
}