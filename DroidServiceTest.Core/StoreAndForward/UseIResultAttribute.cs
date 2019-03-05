using System;

namespace DroidServiceTest.Core.StoreAndForward
{
    /// <summary>
    /// Use this attribute to let ServiceProxy know that response 
    /// will called by another method call.
    /// Service will be expected to implement IResults interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class UseIResultAttribute : Attribute
    {

    }
}