using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DroidServiceTest.Core.Ioc;
using DroidServiceTest.Core.Logging.Model;
using DroidServiceTest.Core.Services;
using PCLStorage;
using static System.String;

namespace DroidServiceTest.Core.Logging.Logger
{
    /// <summary>
    ///     Message format options:
    ///     ${userid} - Addes UserID if available
    ///     ${application.name} - Add application name if available
    ///     ${application.version} - Add application version if available
    ///     ${device.id} - Add device id if available
    ///     ${message}" - Message added a log event time
    ///     ${level} - Add message level (debug, warn, etc...)
    ///     ${date} - Add date
    ///     ${membername} - Adds the class name
    ///     ${sourcefile} - Adds the class file
    ///     ${sourcelinenumber} - Adds the line number
    /// </summary>
    [Target("File")]
    public class FileLogger : BaseLogger, ILoggerConfig
    {
        private MyLogger _debugLogger = new MyLogger();
        private const int SyncLockTimeout = 10000;
        private const int QueueLockTimeout = 300;
        private const int LogFileOperationsLockTimeout = 5000;
        private readonly SemaphoreSlim _logFileOperationsLock = new SemaphoreSlim(1, 1);
        private readonly Queue<LogMessage> _queuedLogMessages = new Queue<LogMessage>(100);
        private readonly SemaphoreSlim _queueLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
        private IFile _file;
        private Task _processQueueTask;
        private LoggingTarget _targetConfig;
        private bool _writeHeader;
        private ILogger _platformLogger;
        private IPlatformService _platformService;
        /// <summary>
        /// Path of the log files
        /// </summary>
        private string _path;
        /// <summary>
        /// Used to check if the path is available for read/write
        /// </summary>
        private IExternalStorageHelper _storageHelper;


        public LoggingTarget TargetConfig
        {
            get => _targetConfig;
            set
            {
                _targetConfig = value;
                Task.Run(Initialize);
            }
        }

        private async Task Initialize()
        {
            _platformService = Container.Instance.Resolve<IPlatformService>();
            _storageHelper = Container.Instance.Resolve<IExternalStorageHelper>();
            _platformLogger = _platformService.PlatformLogger;
            _path = TargetConfig.InternalStorage ? _platformService.ExternalStoragePath : _platformService.SecondaryExternalStoragePath;

            if (_file == null)
            {
                _file = await GetFileAsync(TargetConfig.FileName).ConfigureAwait(false); ;
                if (_writeHeader)
                {
                    await WriteHeaderAsync().ConfigureAwait(false);
                }
            }

            // If the ArchiveFileName wasn't set and the ArchiveAboveSize was set then we'll default to FileName_#.Ext
            if (TargetConfig.ArchiveAboveSize <= 0 || !IsNullOrEmpty(TargetConfig.ArchiveFileName)) return;

            var fileNameParts = TargetConfig.FileName.Split('.');
            var fileName = fileNameParts[0];
            var fileExt = Empty;

            if (fileNameParts.Length == 2)
            {
                fileExt = "." + fileNameParts[1];
            }
            else if (fileNameParts.Length > 2)
            {
                for (var i = 1; i < fileNameParts.Length; i++)
                {
                    fileExt += "." + fileNameParts[i];
                }
            }

            TargetConfig.ArchiveFileName = $"{fileName}_#{fileExt}";

        }

        public override void LogMessage(LogMessage message)
        {
            var lockTaken = false;
            try
            {
                _debugLogger.Debug("Before Queue Lock.");
                lockTaken = _queueLock.Wait(QueueLockTimeout);
                if (!lockTaken)
                {
                    _debugLogger.Debug("Failed to Acquire Queue Lock.");
                    return;
                }
                _debugLogger.Debug("After Queue Lock Acquired.");

                _queuedLogMessages.Enqueue(message);
            }
            catch (ObjectDisposedException ode)
            {
                LogErrorToPlatform(ode);
            }
            finally
            {
                if (lockTaken)
                {
                    _debugLogger.Debug("Start Queue Lock Release.");
                    _queueLock.Release();
                    _debugLogger.Debug("After Queue Lock Release.");
                }
            }

            StartQueueLogger();
        }

        public bool QueueLoggerIsRunning => (_processQueueTask != null && !_processQueueTask.IsCompleted);

        private async Task PerformLoggingAsync(LogMessage message)
        {
            if (message == null) return;
            var lockTaken = false;
            var logMessage = message.ToString(TargetConfig.Layout);
            try
            {
                _debugLogger.Debug("Before Sync Logging Lock");
                lockTaken = await _syncLock.WaitAsync(SyncLockTimeout).ConfigureAwait(false);
                if (!lockTaken)
                {
                    _debugLogger.Debug("Failed to get Sync Logging Lock");
                    return;
                }
                _debugLogger.Debug("Sync Logging Lock Acquired");

                var length = await WriteToFileAsync(logMessage).ConfigureAwait(false);
                if (ShouldArchive(length))
                {
                    await ArchiveAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _debugLogger.Debug("Start Sync Logging Lock Release");
                    _syncLock.Release();
                    _debugLogger.Debug("After Sync Logging Lock Release");
                }
            }
        }

        private async Task WriteHeaderAsync()
        {
            var logMessage = Model.LogMessage.ToLayoutHeader(TargetConfig.Layout);

            try
            {
                await WriteToFileAsync(logMessage).ConfigureAwait(false);
            }
            finally
            {
                _writeHeader = false;
            }
        }

        private void StartQueueLogger()
        {
            if (!QueueLoggerIsRunning)
            {
                _processQueueTask = Task.Run(async () =>
                {
                    try
                    {
                        await StartQueueLoggerAsync().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        LogErrorToPlatform(e, "Error Running StartQueueLoggerAsync. ");
                    }
                });
            }
        }

        private async Task StartQueueLoggerAsync()
        {
            if (QueueLoggerIsRunning) return;
            var lockTaken = false;

            while (_queuedLogMessages.Count > 0)
            {
                LogMessage message = null;
                try
                {
                    _debugLogger.Debug("Before Queue Lock");
                    lockTaken = await _queueLock.WaitAsync(QueueLockTimeout).ConfigureAwait(false);
                    if (!lockTaken)
                    {
                        _debugLogger.Debug("Failed to Get Queue Lock");
                        continue;
                    }
                    _debugLogger.Debug("Queue Lock Acquired");

                    if (_queuedLogMessages.Count == 0) break;

                    try
                    {
                        message = _queuedLogMessages.Dequeue();
                    }
                    catch (InvalidOperationException e)
                    {
                        LogErrorToPlatform(e);
                    }
                }
                catch (Exception e)
                {
                    LogErrorToPlatform(e);
                }
                finally
                {
                    if (lockTaken)
                    {
                        _debugLogger.Debug("Start Queue Lock Release");
                        _queueLock.Release();
                        _debugLogger.Debug("After Queue Lock Released");
                    }
                }

                try
                {
                    await PerformLoggingAsync(message).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    LogErrorToPlatform(e);
                }

            }
        }

        private async Task<long> WriteToFileAsync(string message)
        {
            long fileLength = 0;
            var lockTaken = false;

            try
            {
                _debugLogger.Debug("Before File Log Lock");
                lockTaken = await _logFileOperationsLock.WaitAsync(LogFileOperationsLockTimeout).ConfigureAwait(false);
                if (!lockTaken)
                {
                    _debugLogger.Debug("Failed to acquire file log lock");
                    return fileLength;
                }
                _debugLogger.Debug("File log lock acquired");

                if (_file != null)
                {
                    using (var stream = await _file.OpenAsync(FileAccess.ReadAndWrite).ConfigureAwait(false))
                    {
                        using (var streamWriter = new StreamWriter(stream))
                        {
                            fileLength = streamWriter.BaseStream.Seek(0, SeekOrigin.End);
                            streamWriter.WriteLine(message);
                            fileLength += message.Length + 2;
                            streamWriter.Flush();
                            stream.Flush();
                        }
                    }
                }
                else // Unable to log to file, will log to platform instead.
                {
                    _debugLogger.Debug(message);
                }
            }
            catch (NullReferenceException e)
            {
                LogErrorToPlatform(e);
            }
            catch (Exception ex)
            {
                LogErrorToPlatform(ex, $"Original Log Message: {message}");
            }
            finally
            {
                if (lockTaken)
                {
                    _debugLogger.Debug("Start file log Release");
                    _logFileOperationsLock.Release();
                    _debugLogger.Debug("After file log release");
                }
            }

            return fileLength;
        }

        private async Task ArchiveAsync()
        {
            var fileCount = 1;
            var filename = TargetConfig.ArchiveFileName.Replace("#", fileCount.ToString());
            filename = Path.Combine(_path, filename);

            while (await FileExistsAsync(filename).ConfigureAwait(false))
            {
                if (fileCount >= TargetConfig.MaxArchiveFiles)
                {
                    await RollLogsAsync().ConfigureAwait(false);
                    break;
                }
                fileCount += 1;
                filename = Path.Combine(_path, TargetConfig.ArchiveFileName.Replace("#", fileCount.ToString()));
            }

            await MoveLogAsync(TargetConfig.FileName, filename).ConfigureAwait(false);
            _file = await GetFileAsync(TargetConfig.FileName).ConfigureAwait(false);
            if (_writeHeader) await WriteHeaderAsync().ConfigureAwait(false);
        }

        private async Task RollLogsAsync()
        {
            var fileCount = 1;
            while (fileCount < TargetConfig.MaxArchiveFiles)
            {
                var targetFile = TargetConfig.ArchiveFileName.Replace("#", fileCount.ToString());
                var sourceFile = TargetConfig.ArchiveFileName.Replace("#", (fileCount + 1).ToString());
                await MoveLogAsync(sourceFile, targetFile).ConfigureAwait(false);
                fileCount += 1;
            }
        }

        private async Task MoveLogAsync(string oldPath, string newPath)
        {
            var lockTaken = false;
            try
            {
                _debugLogger.Debug("Before file log lock.");
                lockTaken = await _logFileOperationsLock.WaitAsync(LogFileOperationsLockTimeout).ConfigureAwait(false);
                if (!lockTaken)
                {
                    _debugLogger.Debug("Failed to acquire file lock.");
                    return;
                }

                _debugLogger.Debug("Acquired file log lock.");

                oldPath = Path.Combine(_path, oldPath);
                newPath = Path.Combine(_path, newPath);

                if (await FileSystem.Current.LocalStorage.CheckExistsAsync(oldPath).ConfigureAwait(false) != ExistenceCheckResult.FileExists) return;

                var file = await FileSystem.Current.LocalStorage.GetFileAsync(oldPath).ConfigureAwait(false);
                if (file == null) return;

                await file.MoveAsync(newPath).ConfigureAwait(false);
                if (await FileSystem.Current.LocalStorage.CheckExistsAsync(newPath).ConfigureAwait(false) == ExistenceCheckResult.FileExists)
                {
                    _platformService.ScanFile(newPath);
                }

            }
            catch (ObjectDisposedException ode)
            {
                LogErrorToPlatform(ode);
            }
            catch (Exception e)
            {
                LogErrorToPlatform(e);
            }
            finally
            {
                if (lockTaken)
                {
                    _debugLogger.Debug("Start file log release");
                    _logFileOperationsLock.Release();
                    _debugLogger.Debug("After file log release");
                }
            }
        }

        private async Task<bool> FileExistsAsync(string file)
        {
            return await FileSystem.Current.LocalStorage.CheckExistsAsync(Path.Combine(_path, file)).ConfigureAwait(false) == ExistenceCheckResult.FileExists;
        }

        /// <summary>
        /// Attempts to get the file.
        /// The file will be created if it does not already exist.
        /// </summary>
        /// <param name="file">The name of the file</param>
        /// <returns>A PCLStorage file object or null if unable to access the file.</returns>
        private async Task<IFile> GetFileAsync(string file)
        {
            var lockTaken = false;
            try
            {
                _debugLogger.Debug("Before file log lock..");
                lockTaken = await _logFileOperationsLock.WaitAsync(LogFileOperationsLockTimeout).ConfigureAwait(false);
                if (!lockTaken)
                {
                    _debugLogger.Debug("Failed to acquired file log lock..");
                    return null;
                }
                _debugLogger.Debug("File log lock acquired..");

                // Check if the SD Card is available for read/write 
                if (!_storageHelper.IsAvailableAndWriteable(_path))
                {
                    var msg = Format("Error accessing SD card, please contact the helpdesk.\n\nPlease inform the helpdesk that the external SD card media state is '{0}'", _storageHelper.GetCurrentState(_path)).Replace(@"\n", Environment.NewLine);
                    LogErrorToPlatform(null, msg);
                    //MessageHub.Instance.Publish(new FileLoggerError(msg, "SD Card Error"));
                    return null;
                }


                // Get the folder (create it if it doesn't exist)
                var folder = await FileSystem.Current.LocalStorage.CreateFolderAsync(_path, CreationCollisionOption.OpenIfExists).ConfigureAwait(false);

                // If this is a new file we'll want to write the file header
                _writeHeader = !await FileExistsAsync(file).ConfigureAwait(false);

                // Get/Create the file
                var ret = await folder.CreateFileAsync(file, CreationCollisionOption.OpenIfExists).ConfigureAwait(false);

                // Necessary for 'droid - adds file to media content provider - Only necessary if file is new, and file is new if we're writing the header.
                if (_writeHeader)
                {
                    _platformService.ScanFile(ret.Path);
                }

                return ret;
            }
            catch (ObjectDisposedException ode)
            {
                LogErrorToPlatform(ode);
            }
            catch (Exception ex)
            {
                LogErrorToPlatform(ex, ex.Message);
            }
            finally
            {
                if (lockTaken)
                {
                    _debugLogger.Debug("Start File log Lock release..");
                    _logFileOperationsLock.Release();
                    _debugLogger.Debug("After file log lock release..");
                }
            }
            return null;
        }

        private bool ShouldArchive(long fileSize)
        {
            return TargetConfig.ArchiveAboveSize > 0 && TargetConfig.ArchiveAboveSize <= fileSize;
        }

        private void LogErrorToPlatform(Exception e, string msg = null)
        {
            if (msg != null) _platformLogger.Error(msg);
            if (e != null) _platformLogger.Error(e.Message, e);
        }

        public class FileLoggerError
        {
            public FileLoggerError(string msg, string title)
            {
                Message = msg;
                Title = title;
            }
            public string Message { get; }
            public string Title { get; }
        }
    }
}