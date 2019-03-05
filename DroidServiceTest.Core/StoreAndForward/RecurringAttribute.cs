using System;

namespace DroidServiceTest.Core.StoreAndForward
{
    /// <summary>
    /// Use this attribute to create a recurring method when passed to ServiceProxy
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RecurringAttribute : Attribute
    {

    }
}