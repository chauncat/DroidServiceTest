using System;
using System.Threading.Tasks;
using DroidServiceTest.Core.Logging.Logger;

namespace DroidServiceTest.Core.Logging
{
    public interface ILogFactory
    {
        ILogger GetLogger(String name);
        ILogger GetLogger(Type type);
        ILogger GetLogger<T>();

    }
}