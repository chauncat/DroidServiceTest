using System;

namespace DroidServiceTest.Core.Logging.Logger
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TargetAttribute : Attribute
    {
        private readonly String _name;
        public TargetAttribute(String name) { _name = name; }
        public String TargetName { get { return _name; } }
    }
}
