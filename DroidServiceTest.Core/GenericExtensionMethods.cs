using System;
using System.Collections.Generic;

namespace DroidServiceTest.Core
{
    public static class GenericExtensionMethods
    {
        public static bool IsNull<T>(this T value)
        {
            var type = typeof(T);

            return (Nullable.GetUnderlyingType(type) != null && EqualityComparer<T>.Default.Equals(value, default(T))) || (object)value == null;
        }

        public static string UnwindException(this Exception ex)
        {
            try
            {
                if (ex == null) return string.Empty;
                var message = string.Format("{0}Message: {1}{2}Stacktrace: {3}", Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace);

                return message + ex.InnerException.UnwindException();
            }
            catch (Exception e)
            {
                return "Error creating log message: " + e.Message;
            }
        }

        private static T CastTo<T>(this object value, T targetType)
        {
            // targetType above is just for compiler magic
            // to infer the type to cast x to
            return (T)value;
        }
    }
}