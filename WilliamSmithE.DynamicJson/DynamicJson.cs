using System.Text.Json;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides high-level utilities for parsing JSON strings into dynamic
    /// representations and serializing dynamic values back to JSON.
    /// </summary>
    /// <remarks>
    /// This class serves as the entry point for converting raw JSON into
    /// <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/> instances,
    /// as well as producing JSON output from dynamic structures.
    /// </remarks>
    public static class DynamicJson
    {
        /// <summary>
        /// Parses a JSON string and converts the root element into a dynamic representation,
        /// optionally applying a custom key sanitization filter.
        /// </summary>
        /// <param name="json">
        /// The JSON string to parse. Must not be <c>null</c>, empty, or whitespace.
        /// </param>
        /// <param name="sanitizationFilter">
        /// An optional predicate that determines which characters are retained when sanitizing
        /// JSON object property names. If <c>null</c>, the default alphanumeric sanitizer is used.
        /// The filter is applied consistently throughout all nested objects.
        /// </param>
        /// <returns>
        /// A dynamic representation of the root JSON structure. This will be a
        /// <see cref="DynamicJsonObject"/> for JSON objects or a <see cref="DynamicJsonList"/>
        /// for JSON arrays.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="json"/> is <c>null</c>, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the root JSON element is not an object or an array.
        /// </exception>
        /// <remarks>
        /// This method provides the primary entry point for converting raw JSON text into
        /// the dynamic JSON system. The optional sanitization filter allows callers to control
        /// how JSON object keys are normalized during conversion.
        /// </remarks>
        public static dynamic FromJson(string json, Func<char, bool>? sanitizationFilter = null)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            using var doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            return root.ValueKind switch
            {
                JsonValueKind.Object => root.AsDynamic(sanitizationFilter),
                JsonValueKind.Array => root.AsDynamic(sanitizationFilter),
                _ => throw new InvalidOperationException(
                        $"Unsupported JSON root type: {root.ValueKind}")
            };
        }

        /// <summary>
        /// Serializes the specified value to a JSON string after converting any dynamic
        /// wrapper types into their raw CLR representations.
        /// </summary>
        /// <param name="value">
        /// The value to serialize. May be a primitive, a <see cref="DynamicJsonObject"/>,
        /// a <see cref="DynamicJsonList"/>, or any other JSON-compatible structure.
        /// </param>
        /// <returns>
        /// A JSON string representing the provided value.
        /// </returns>
        /// <remarks>
        /// This method first normalizes the input by converting dynamic wrappers into
        /// dictionaries or lists using <see cref="Raw.ToRawObject(object?)"/>, then
        /// serializes the result with <see cref="System.Text.Json.JsonSerializer"/>.
        /// </remarks>
        public static string ToJson(object? value)
        {
            var raw = Raw.ToRawObject(value);
            return JsonSerializer.Serialize(raw);
        }
    }
}