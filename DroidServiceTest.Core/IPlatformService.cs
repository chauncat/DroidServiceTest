﻿using System.IO;
using SQLite.Net;

namespace DroidServiceTest.Core
{
    public interface IPlatformService
    {
        // Database
        /// <summary>
        /// Returns a new SQLiteConnection object. Note: This implements IDisposable so should be used in a USING or disposed when finished with it.
        /// </summary>
        /// <param name="dbName">The name of the database</param>
        /// <returns>A new SQLiteConnection object</returns>
        SQLiteConnection GetSqlConnection(string dbName);

        void GetAvailableThreads(out int workerThreads, out int completionPortThreads);
    }
}
