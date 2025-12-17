namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides extension methods for simplifying interactions with the
    /// <see cref="DynamicJson"/> utility API.
    /// </summary>
    /// <remarks>
    /// These extensions enable fluent conversion of JSON strings into dynamic
    /// JSON objects using a concise and intuitive syntax.
    /// </remarks>
    public static class DynamicJsonExtensions
    {
        /// <summary>
        /// Converts a JSON string into a dynamic JSON representation, optionally applying
        /// a custom key sanitization filter.
        /// </summary>
        /// <param name="json">
        /// The JSON string to convert. Must not be <c>null</c>, empty, or whitespace.
        /// </param>
        /// <param name="sanitizationFilter">
        /// An optional predicate that determines which characters are retained when sanitizing
        /// JSON object property names. If <c>null</c>, the default alphanumeric sanitizer is used.
        /// </param>
        /// <returns>
        /// A dynamic representation of the parsed JSON structure, such as a
        /// <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/>.
        /// </returns>
        /// <remarks>
        /// This is a convenience wrapper around
        /// <see cref="DynamicJson.FromJson(string, Func{char, bool}?)"/>, enabling fluent
        /// conversion from JSON strings to dynamic structures.
        /// </remarks>
        public static dynamic ToDynamic (this string json, Func<char, bool>? sanitizationFilter = null)
        {
            return DynamicJson.FromJson(json, sanitizationFilter);
        }
    }
}