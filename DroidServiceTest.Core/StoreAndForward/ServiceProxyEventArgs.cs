using System;

namespace DroidServiceTest.Core.StoreAndForward
{
    public enum ServiceProxyCallStatus
    {
        CompletedSuccessfully,
        CompletedWithError,
        FailedToSend,
        NotDestinedForTarget
    }

    public class ServiceProxyEventArgs : EventArgs
    {
        public String MethodName { get; set; }
        public ServiceProxyCallStatus Status { get; set; }
        public Exception ServiceException { get; set; }
        public Object ReturnValue { get; set; }
        public Object[] CallParameters { get; set; }
    }
}
