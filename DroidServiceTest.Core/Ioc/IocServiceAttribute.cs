using System;

namespace DroidServiceTest.Core.Ioc
{
    /// <summary>
    /// Attribute classed to mark which classes should be auto
    /// loaded into the container.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IocServiceAttribute : Attribute
    {
        public IocServiceAttribute()
        {
            AsSelf = true;
        }
        /// <summary>
        /// If true the container will return a single instance 
        /// for all requests.  Otherwise a new instance will be 
        /// returned for each request.
        /// Default value is false.
        /// </summary>
        public bool AsSingleton { get; set; }

        /// <summary>
        /// If true the container will register the service 
        /// under its concrete name otherwise it will register 
        /// under the interfaces implemented.
        /// Default value is true.
        /// </summary>
        public bool AsSelf { get; set; }
        /// <summary>
        /// If true registers the component with the container 
        /// using the interfaces that are implemented
        /// Default value is false.
        /// </summary>
        public bool AsInterface { get; set; }
    }
}