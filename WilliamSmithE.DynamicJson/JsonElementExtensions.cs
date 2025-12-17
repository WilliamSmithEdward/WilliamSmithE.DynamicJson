using System.Text.Json;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides extension methods for converting <see cref="JsonElement"/> values
    /// and related collections into dynamic objects backed by
    /// <see cref="DynamicJsonObject"/> and <see cref="DynamicJsonList"/>.
    /// </summary>
    /// <remarks>
    /// These extensions enable seamless transformation of parsed JSON structures
    /// into navigable dynamic representations while preserving type information and
    /// supporting downstream mapping into strongly typed models.
    /// </remarks>
    public static class JsonElementExtensions
    {
        /// <summary>
        /// Converts a list of <see cref="JsonElement"/> values into dynamic JSON structures,
        /// optionally applying a custom key sanitization filter.
        /// </summary>
        /// <param name="items">
        /// The collection of <see cref="JsonElement"/> instances to convert. Must not be <c>null</c>.
        /// </param>
        /// <param name="sanitizationFilter">
        /// An optional predicate that determines which characters are retained when sanitizing
        /// JSON object property names. If <c>null</c>, the default alphanumeric sanitizer is used.
        /// </param>
        /// <returns>
        /// A list of dynamic representations of the provided JSON elements, such as
        /// <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/>.
        /// Elements that cannot be converted are skipped.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="items"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// Each <see cref="JsonElement"/> in the sequence is recursively transformed, and the
        /// specified sanitization filter is applied consistently throughout all nested objects.
        /// </remarks>
        public static List<dynamic> AsDynamic(this List<JsonElement> items, Func<char, bool>? sanitizationFilter = null)
        {
            ArgumentNullException.ThrowIfNull(items);

            var result = new List<dynamic>();

            foreach (var item in items)
            {
                var converted = ConvertJsonElement(item, sanitizationFilter);

                if (converted != null)
                {
                    result.Add(converted);
                }
            }

            return result;
        }

        /// <summary>
        /// Converts a single <see cref="JsonElement"/> into a dynamic JSON representation,
        /// optionally applying a custom key sanitization filter.
        /// </summary>
        /// <param name="item">
        /// The <see cref="JsonElement"/> to convert.
        /// </param>
        /// <param name="sanitizationFilter">
        /// An optional predicate used to determine which characters are retained when
        /// sanitizing JSON object property names. If <c>null</c>, the default alphanumeric
        /// sanitizer is applied.
        /// </param>
        /// <returns>
        /// A dynamic representation of the JSON element, typically a <see cref="DynamicJsonObject"/>
        /// or <see cref="DynamicJsonList"/>, depending on the structure of the underlying JSON.
        /// </returns>
        /// <remarks>
        /// This method wraps the element in a temporary list and delegates to the list-based
        /// <c>AsDynamic</c> overload to ensure consistent sanitization and conversion behavior.
        /// </remarks>
        public static dynamic AsDynamic(this JsonElement item, Func<char, bool>? sanitizationFilter = null)
        {
            var list = new List<JsonElement> { item };
            return list.AsDynamic(sanitizationFilter)[0];
        }

        /// <summary>
        /// Returns the first element in the sequence that is a
        /// <see cref="DynamicJsonObject"/>, or <c>null</c> if none exist.
        /// </summary>
        /// <param name="source">
        /// The sequence to search.
        /// </param>
        /// <returns>
        /// The first <see cref="DynamicJsonObject"/> in the sequence, or <c>null</c>
        /// if no such element is found.
        /// </returns>
        /// <remarks>
        /// This method does not throw if <paramref name="source"/> is <c>null</c>;
        /// it simply returns <c>null</c>.
        /// Non-object elements are ignored.
        /// </remarks>
        public static dynamic? First(this IEnumerable<object?> source)
        {
            if (source == null)
                return null;

            foreach (var item in source)
            {
                if (item is DynamicJsonObject sdo)
                    return sdo;
            }

            return null;
        }

        /// <summary>
        /// Recursively converts a <see cref="JsonElement"/> into its corresponding CLR
        /// representation, producing <see cref="DynamicJsonObject"/> for JSON objects and
        /// <see cref="DynamicJsonList"/> for JSON arrays.
        /// </summary>
        /// <param name="element">
        /// The <see cref="JsonElement"/> to convert.
        /// </param>
        /// <param name="sanitizationFilter">
        /// An optional predicate used to determine which characters are retained when
        /// sanitizing JSON object property names. This filter is passed through to all
        /// nested <see cref="DynamicJsonObject"/> instances. If <c>null</c>, default
        /// alphanumeric sanitization is applied.
        /// </param>
        /// <returns>
        /// A CLR value representing the JSON structure:
        /// <list type="bullet">
        /// <item><description>
        /// <see cref="DynamicJsonObject"/> for JSON objects
        /// </description></item>
        /// <item><description>
        /// <see cref="DynamicJsonList"/> for JSON arrays
        /// </description></item>
        /// <item><description>
        /// <see cref="string"/>, <see cref="long"/>,
        /// <see cref="double"/>, <see cref="decimal"/>,
        /// <see cref="bool"/>, <see cref="DateTime"/>, or <c>null</c>
        /// for primitive values
        /// </description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// When converting object properties, duplicate JSON keys are resolved by appending
        /// numeric suffixes (e.g., <c>Name</c>, <c>Name2</c>, <c>Name3</c>).
        /// </para>
        /// <para>
        /// The sanitization filter is not applied during dictionary construction here.
        /// Instead, it is passed to <see cref="DynamicJsonObject"/>, which performs
        /// sanitization on the finalized property names to ensure consistent behavior.
        /// </para>
        /// </remarks>
        private static object? ConvertJsonElement(JsonElement element, Func<char, bool>? sanitizationFilter)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                {
                    var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                    foreach (var prop in element.EnumerateObject())
                    {
                        var value = ConvertJsonElement(prop.Value, sanitizationFilter);

                        var baseName = prop.Name;
                        var finalName = baseName;

                        int i = 2;

                        while (dict.ContainsKey(finalName))
                        {
                            finalName = baseName + i.ToString();
                            i++;
                        }

                        dict[finalName] = value;
                    }

                    return new DynamicJsonObject(dict, sanitizationFilter);
                }

                case JsonValueKind.Array:
                {
                    var list = new List<object?>();

                    foreach (var arrItem in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElement(arrItem, sanitizationFilter));
                    }

                    return new DynamicJsonList(list);
                }

                case JsonValueKind.String:
                    if (element.TryGetDateTime(out var dt)) return dt;
                    return element.GetString();

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var l)) return l;
                    if (element.TryGetDouble(out var d)) return d;
                    return element.GetDecimal();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;

                default:
                    return element.GetRawText();
            }
        }
    }
}