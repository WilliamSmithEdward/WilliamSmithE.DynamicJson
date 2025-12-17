using System.Text;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides string extension methods for sanitizing input values.
    /// </summary>
    /// <remarks>
    /// Includes both a default alphanumeric sanitizer and an overload that allows
    /// callers to supply a custom character filter.
    /// </remarks>
    internal static class KeySanitization
    {
        /// <summary>
        /// Returns a sanitized version of the input string using the specified character filter.
        /// </summary>
        /// <param name="key">
        /// The string to sanitize. If <c>null</c> or empty, an empty string is returned.
        /// </param>
        /// <param name="filter">
        /// An optional predicate that determines which characters are retained.
        /// If <c>null</c>, the default alphanumeric sanitizer is applied.
        /// </param>
        /// <returns>
        /// A new string containing only the characters for which the filter returns <c>true</c>,
        /// or an empty string when the input is <c>null</c> or empty.
        /// </returns>
        /// <remarks>
        /// This overload allows callers to define custom sanitization rules while preserving
        /// the behavior of the default alphanumeric sanitizer when no filter is provided.
        /// </remarks>
        internal static string Sanitize(this string key, Func<char, bool>? filter = null)
        {
            if (filter == null)
            {
                return key.Sanitize();
            }

            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(key.Length);

            foreach (var c in key)
            {
                if (filter(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns a sanitized version of the input string containing only alphanumeric characters.
        /// </summary>
        /// <param name="key">The input string to sanitize.</param>
        /// <returns>
        /// A new string containing only alphanumeric characters,
        /// or an empty string if the input is null or empty.
        /// </returns>
        private static string Sanitize(this string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(key.Length);

            foreach (var c in key)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}