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
        /// Converts a JSON string into a dynamic representation backed by
        /// <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/>.
        /// </summary>
        /// <param name="json">
        /// The JSON string to convert.
        /// </param>
        /// <returns>
        /// A dynamic object representing the parsed JSON structure.
        /// </returns>
        /// <remarks>
        /// This extension method provides a convenient shorthand for calling
        /// <see cref="DynamicJson.FromJson(string)"/>.
        /// </remarks>
        public static dynamic ToDynamic (this string json)
        {
            return DynamicJson.FromJson(json);
        }
    }
}