using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DroidServiceTest.Core.Services
{
    public enum MediaState
    {
        Unknown = 0,
        Removed,
        Unmounted,
        DiskCheckInProgress,
        NoFilesystem,
        Mounted,
        ReadOnly,
        Shared,
        BadRemoval,
        Unmountable
    }

    public interface IExternalStorageHelper
    {
        bool IsAvailable(string path);
        bool IsWriteable(string path);
        bool IsAvailableAndWriteable(string path);
        MediaState GetCurrentState(string path);

    }
}
