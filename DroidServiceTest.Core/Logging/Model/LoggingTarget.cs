namespace DroidServiceTest.Core.Logging.Model
{
    public class LoggingTarget
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Layout { get; set; }
        public string FileName { get; set; }
        public long ArchiveAboveSize { get; set; }
        public string ArchiveFileName { get; set; }
        public int MaxArchiveFiles { get; set; }
        public string DbName { get; set; }
        public string EmailHostPort { get; set; }
        public string EmailFromAddress { get; set; }
        public string EmailToAddress { get; set; }
        public string EmailSubject { get; set; }
        /// <summary>
        /// This is used to determine if the logger will write to the secondary storage location or not.
        /// Default is false
        /// </summary>
        public bool InternalStorage { get; set; }
    }
}