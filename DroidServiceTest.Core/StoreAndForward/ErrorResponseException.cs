using System;

namespace DroidServiceTest.Core.StoreAndForward
{
    public class ErrorResponseException : Exception
    {
        public ErrorResponseException()
        {
        }

        public ErrorResponseException(string message)
            : base(message) { }

        public ErrorResponseException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}
