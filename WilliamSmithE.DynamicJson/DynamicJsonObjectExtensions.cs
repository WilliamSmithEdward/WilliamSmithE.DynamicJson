using System.Text.Json;

namespace WilliamSmithE.DynamicJson
{
    public static class DynamicJsonObjectExtensions
    {
        /// <summary>
        /// Converts a CLR object into a dynamic JSON representation by
        /// serializing it to JSON and parsing it with the DynamicJson API.
        /// </summary>
        /// <param name="value">
        /// The object to convert. Must not be <c>null</c>.
        /// </param>
        /// <returns>
        /// A dynamic JSON structure representing the serialized object.
        /// This is typically a <see cref="DynamicJsonObject"/> or
        /// <see cref="DynamicJsonList"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="value"/> is <c>null</c>.
        /// </exception>
        public static dynamic ToDynamic(this object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            // If already a JSON dynamic wrapper, return as-is
            if (value is DynamicJsonObject || value is DynamicJsonList)
                return value;

            // If it's a JSON string, use existing pipeline
            if (value is string s)
                return s.ToDynamic();

            // Otherwise: serialize → parse → wrap
            var json = JsonSerializer.Serialize(value);

            return DynamicJson.FromJson(json);
        }
    }
}