using System.Text.Json;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides high-level utilities for parsing JSON strings into dynamic
    /// representations and serializing dynamic values back to JSON.
    /// </summary>
    /// <remarks>
    /// This class serves as the entry point for converting raw JSON into
    /// <see cref="SafeDynamicObject"/> or <see cref="SafeDynamicList"/> instances,
    /// as well as producing JSON output from dynamic structures.
    /// </remarks>
    public static class DynamicJson
    {
        /// <summary>
        /// Parses a JSON string and converts the root element into a dynamic
        /// representation backed by <see cref="SafeDynamicObject"/> or
        /// <see cref="SafeDynamicList"/>.
        /// </summary>
        /// <param name="json">
        /// The JSON string to parse.
        /// </param>
        /// <returns>
        /// A dynamic object representing the root JSON structure. This will be a
        /// <see cref="SafeDynamicObject"/> for JSON objects or a
        /// <see cref="SafeDynamicList"/> for JSON arrays.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="json"/> is <c>null</c>, empty, or whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the JSON root element is not an object or an array.
        /// </exception>
        /// <remarks>
        /// This method standardizes JSON parsing and dynamic conversion, ensuring that
        /// objects and arrays are handled uniformly via <see cref="JsonElementDynamicExtensions.AsDynamic(System.Text.Json.JsonElement)"/>.
        /// </remarks>
        public static dynamic FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            using var doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            return root.ValueKind switch
            {
                JsonValueKind.Object => root.AsDynamic(),
                JsonValueKind.Array => root.AsDynamic(),
                _ => throw new InvalidOperationException(
                        $"Unsupported JSON root type: {root.ValueKind}")
            };
        }

        /// <summary>
        /// Serializes the specified value to a JSON string after converting any dynamic
        /// wrapper types into their raw CLR representations.
        /// </summary>
        /// <param name="value">
        /// The value to serialize. May be a primitive, a <see cref="SafeDynamicObject"/>,
        /// a <see cref="SafeDynamicList"/>, or any other JSON-compatible structure.
        /// </param>
        /// <returns>
        /// A JSON string representing the provided value.
        /// </returns>
        /// <remarks>
        /// This method first normalizes the input by converting dynamic wrappers into
        /// dictionaries or lists using <see cref="Raw.ToRawValue(object?)"/>, then
        /// serializes the result with <see cref="System.Text.Json.JsonSerializer"/>.
        /// </remarks>
        public static string ToJson(object? value)
        {
            var raw = Raw.ToRawValue(value);
            return JsonSerializer.Serialize(raw);
        }
    }
}