using System.Text.Json;

namespace WilliamSmithE.DynamicJson
{
    public static class DynamicJsonObjectExtensions
    {
        /// <summary>
        /// Converts a CLR object into a dynamic JSON representation, optionally applying
        /// a custom key sanitization filter.
        /// </summary>
        /// <param name="value">
        /// The object to convert. Must not be <c>null</c>.
        /// </param>
        /// <param name="sanitizationFilter">
        /// An optional predicate that determines which characters are retained when sanitizing
        /// JSON object property names. If <c>null</c>, the default alphanumeric sanitizer is used.
        /// </param>
        /// <returns>
        /// A dynamic JSON structure representing the serialized object, typically a
        /// <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/>.
        /// If <paramref name="value"/> is already a dynamic wrapper, it is returned unchanged.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="value"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// If the input is a JSON string, this method delegates to the existing dynamic
        /// conversion pipeline, ensuring consistent sanitization behavior.
        /// </para>
        /// <para>
        /// For all other CLR types, the value is serialized to JSON and then parsed into the
        /// dynamic model using <see cref="DynamicJson.FromJson(string, Func{char, bool}?)"/>.
        /// </para>
        /// </remarks>
        public static dynamic ToDynamic(this object value, Func<char, bool>? sanitizationFilter = null)
        {
            ArgumentNullException.ThrowIfNull(value);

            // If already a JSON dynamic wrapper, return as-is
            if (value is DynamicJsonObject || value is DynamicJsonList)
                return value;

            // If it's a JSON string, use existing pipeline
            if (value is string s)
                return s.ToDynamic(sanitizationFilter);

            // Otherwise: serialize → parse → wrap
            var json = JsonSerializer.Serialize(value);

            return DynamicJson.FromJson(json, sanitizationFilter);
        }
    }
}