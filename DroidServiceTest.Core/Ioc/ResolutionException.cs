namespace DroidServiceTest.Core.Ioc
{
    public class ResolutionException : System.Exception
    {
        public ResolutionException(string message, System.Exception rootException)
            : base(message, rootException)
        {
        }
        public ResolutionException(string message)
            : base(message)
        {
        }
        public ResolutionException()
        {
        }
    }
}