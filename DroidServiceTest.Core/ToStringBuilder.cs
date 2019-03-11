using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DroidServiceTest.Core
{
    public static class ToStringBuilder
    {
        /// <summary>
        /// Logging of the object fields
        /// </summary>
        /// <param name="obj">object to log</param>
        /// <returns>string representation</returns>
        public static string ReflectionToString(object obj)
        {
            var sb = new StringBuilder("[");
            if (obj == null)
            {
                return "Object is null";
            }
            var clazz = obj.GetType();
            sb.AppendFormat(CultureInfo.CurrentCulture, "Type:<{0}>", clazz);
            //FieldInfo[] fields = clazz.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField);
            var fields = clazz.GetTypeInfo().DeclaredFields;
            var fieldInfos = fields as IList<FieldInfo> ?? fields.ToList();
            for (int i = 0; i < fieldInfos.Count(); i++)
            {
                FieldInfo f = fieldInfos[i];
                if (!f.IsStatic)
                {
                    object value = f.GetValue(obj);
                    sb.AppendFormat(CultureInfo.CurrentCulture, ", Field:<{0}> Value:<{1}>", ParseName(f.Name), f.GetValue(obj) ?? "null");
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string ParseName(string value)
        {
            if (value.Contains("__BackingField"))
            {
                int start = value.IndexOf("<", System.StringComparison.Ordinal) + 1;
                int end = value.IndexOf(">", System.StringComparison.Ordinal) - 1;
                return value.Substring(start, end);


            }
            return value;

        }
    }
}