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
        /// Converts a list of <see cref="JsonElement"/> values into dynamic JSON objects
        /// backed by <see cref="DynamicJsonObject"/> and <see cref="DynamicJsonList"/>.
        /// </summary>
        /// <param name="items">
        /// The list of <see cref="JsonElement"/> instances to convert.
        /// </param>
        /// <returns>
        /// A list of dynamic representations of the provided JSON elements. Elements that
        /// cannot be converted are skipped.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="items"/> is <c>null</c>.
        /// </exception>
        public static List<dynamic> AsDynamic(this List<JsonElement> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var result = new List<dynamic>();

            foreach (var item in items)
            {
                var converted = ConvertJsonElement(item);

                if (converted != null)
                {
                    result.Add(converted);
                }
            }

            return result;
        }

        /// <summary>
        /// Converts a single <see cref="JsonElement"/> into a dynamic object backed by
        /// a <see cref="DynamicJsonObject"/>.
        /// </summary>
        /// <param name="item">
        /// The <see cref="JsonElement"/> to convert.
        /// </param>
        /// <returns>
        /// A dynamic representation of the JSON element, typically a
        /// <see cref="DynamicJsonObject"/>.
        /// </returns>
        /// <remarks>
        /// This method wraps the element in a temporary list and delegates to
        /// <see cref="AsDynamic(System.Collections.Generic.List{System.Text.Json.JsonElement})"/>
        /// to ensure consistent conversion behavior with list-based processing.
        /// </remarks>
        public static dynamic AsDynamic(this JsonElement item)
        {
            var list = new List<JsonElement> { item };
            return list.AsDynamic()[0];
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

        private static object? ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                {
                    var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = ConvertJsonElement(prop.Value);
                    }

                    return new DynamicJsonObject(dict);
                }

                case JsonValueKind.Array:
                {
                    var list = new List<object?>();

                    foreach (var arrItem in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElement(arrItem));
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