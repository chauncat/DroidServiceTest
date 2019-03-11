using DroidServiceTest.Core.Ioc;
using DroidServiceTest.Core.Services;
using Java.IO;
using Android.Util;
using Environment = Android.OS.Environment;

namespace ServiceTest.Droid.Services
{
    [IocService(AsInterface = true, AsSingleton = true, AsSelf = false)]
    public class ExternalStorageHelper : IExternalStorageHelper
    {
        private bool _available, _writeable;
        private MediaState _currentState;
        private static readonly object LockObject = new object();

        private void CheckState(string pathStr)
        {
            const string TAG = "XPO_ExternalStorageHelper";
            try
            {
                Log.Debug(TAG, $"Check State Called: {pathStr}");
                var path = new File(pathStr);

#pragma warning disable 618
                var state = Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop ? Environment.GetExternalStorageState(path) : Environment.GetStorageState(path);
#pragma warning restore 618

                Log.Debug(TAG, $"State: {state}");

                // Good state
                if (state.Equals(Environment.MediaMounted))
                {
                    _available = _writeable = true;
                    _currentState = MediaState.Mounted;
                    return;
                }

                // Kinda bad state - should not be read only
                if (state.Equals(Environment.MediaMountedReadOnly))
                {
                    _available = true;
                    _writeable = false;
                    _currentState = MediaState.ReadOnly;
                    return;
                }

                // All the following are error/bad states 
                if (state.Equals(Environment.MediaRemoved)) _currentState = MediaState.Removed;
                else if (state.Equals(Environment.MediaUnmounted)) _currentState = MediaState.Unmounted;
                else if (state.Equals(Environment.MediaChecking)) _currentState = MediaState.DiskCheckInProgress;
                else if (state.Equals(Environment.MediaNofs)) _currentState = MediaState.NoFilesystem;
                else if (state.Equals(Environment.MediaShared)) _currentState = MediaState.Shared;
                else if (state.Equals(Environment.MediaBadRemoval)) _currentState = MediaState.BadRemoval;
                else if (state.Equals(Environment.MediaUnmountable)) _currentState = MediaState.Unmountable;
                else _currentState = MediaState.Unknown;

                _available = _writeable = false;
            }
            catch (Java.Lang.Exception ex)
            {
                Log.Error(TAG, $"Java Ex: {ex.Message}");
            }
            catch (System.Exception ex)
            {
                Log.Error(TAG, $".Net Ex: {ex.Message}");
            }
            finally
            {
                Log.Debug(TAG, $"CurrentState: {_currentState}");
            }

        }

        public bool IsAvailable(string path)
        {
            lock (LockObject)
            {
                CheckState(path);
                return _available;
            }
        }

        public bool IsWriteable(string path)
        {
            lock (LockObject)
            {
                CheckState(path);
                return _writeable;
            }
        }

        public bool IsAvailableAndWriteable(string path)
        {
            lock (LockObject)
            {
                CheckState(path);
                return _available && _writeable;
            }
        }

        public MediaState GetCurrentState(string path)
        {
            lock (LockObject)
            {
                CheckState(path);
                return _currentState;
            }
        }
    }
}