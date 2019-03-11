using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DroidServiceTest.Core.Logging.Logger;
using DroidServiceTest.Core.Logging.Model;
using SQLite.Net;

namespace DroidServiceTest.Core.Logging
{
    public class NullPlatformServices : IPlatformService
    {
        private readonly SQLiteConnection _dbConnection;


        public NullPlatformServices(SQLiteConnection dbConnection, ILogger platformLogger)
        {
            _dbConnection = dbConnection;
            PlatformLogger = platformLogger;
            IsWifiConnected = false;

        }

        public Stream ConfigFile => null;

        public SQLiteConnection GetSqlConnection(string dbName)
        {
            return _dbConnection;
        }

        public void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            throw new System.NotImplementedException();
        }

        public bool IsWifiConnected { get; }
        public string GetWifiIp { get; }
        public ILogger PlatformLogger { get; }

        public string ExternalStoragePath => string.Empty;
        public string SecondaryExternalStoragePath => string.Empty;

        public void ScanFile(string filePath){}


        public bool VerifyAndCreateFile(string file)
        {
            return false;
        }

        public bool FileExists(string file)
        {
            return false;
        }

        public void DeleteFile(string file) {}
        public void DeleteSqlFile(string dbName)
        {
            
        }

        public long FileLength(string file)
        {
            return 0;
        }

        public void CopyFile(Stream sourceStream, string destFile)
        {
            
        }

        public Stream ReadFile(string file)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<string> GetDirectoryFiles(string path)
        {
            return new List<string>();
        }

        public bool InitializeMailer(LoggingTarget emailConfig)
        {
            return false;
        }
        public bool SendMail(string subject, string body)
        {
            return false;
        }
        public bool Send(string @from, string recipients, string subject, string body)
        {
            return false;
        }

        // Return device phone number
        public string DevicePhoneNumber => "";

        public string DeviceId => "";
    }
    
}