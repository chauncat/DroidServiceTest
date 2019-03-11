namespace DroidServiceTest.Core.Logging.Model
{
    /// <summary>
    /// Make sure each enum type starts with a unique first letter,
    /// as this letter is used in LogMessage.ToString()
    /// </summary>
    public enum MessageLevel : int
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Fatal = 5,
        None = 6
    }
}
