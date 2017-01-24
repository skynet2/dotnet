using System;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// Common extension methods to use only in this project
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Answers true if this String is neither null or empty.
        /// </summary>
        internal static bool HasValue(this string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Chops off a string at the specified length and accounts for smaller length
        /// </summary>
        /// <param name="s"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string Truncate(this string s, int maxLength)
        {
            return s != null && s.Length > maxLength ? s.Substring(0, maxLength) : s;
        }

        /// <summary>
        /// Serializes <paramref name="o"/> to a JSON string.
        /// </summary>
        /// <param name="o">the instance to serialise</param>
        /// <returns>the resulting JSON object as a string</returns>
        internal static string ToJson(this object o)
        {
            return o == null ? null : new JavaScriptSerializer().Serialize(o);
        }

        /// <summary>
        /// Deserializes <paramref name="s"/> to an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="s">The string to deserialize</param>
        /// <returns>The object resulting from the given string</returns>
        internal static T FromJson<T>(this string s) where T : class 
        {
            return s.HasValue() ? new JavaScriptSerializer().Deserialize<T>(s) : null;
        }
    }
}
