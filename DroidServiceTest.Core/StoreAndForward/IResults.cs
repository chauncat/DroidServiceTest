using System;

namespace DroidServiceTest.Core.StoreAndForward
{
    public interface IResults
    {
        Object GetResult(string requestor, Object item);
    }
}
