using System.Text.Json;

namespace WilliamSmithE.DynamicJson
{
    public static class JsonElementDynamicExtensions
    {
        public static List<dynamic> AsDynamic(this List<JsonElement> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var result = new List<dynamic>();

            foreach (var item in items)
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                foreach (var prop in item.EnumerateObject())
                {
                    if (prop.NameEquals("fields"))
                    {
                        foreach (var fieldProp in prop.Value.EnumerateObject())
                        {
                            dict[fieldProp.Name] = ConvertJsonElement(fieldProp.Value);
                        }
                    }

                    else
                    {
                        dict[prop.Name] = ConvertJsonElement(prop.Value);
                    }
                }

                result.Add(new SafeDynamicObject(dict));
            }

            return result;
        }

        public static dynamic AsDynamic(this JsonElement item)
        {
            var list = new List<JsonElement> { item };
            return list.AsDynamic()[0];
        }

        public static IEnumerable<dynamic> AsDynamics(this IEnumerable<object?> source)
        {
            if (source == null)
                yield break;

            foreach (var item in source)
            {
                if (item is SafeDynamicObject sdo)
                    yield return sdo;
            }
        }

        public static dynamic? First(this IEnumerable<object?> source)
        {
            if (source == null)
                return null;

            foreach (var item in source)
            {
                if (item is SafeDynamicObject sdo)
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

                        return new SafeDynamicObject(dict);
                    }

                case JsonValueKind.Array:
                    {
                        var list = new List<object?>();

                        foreach (var arrItem in element.EnumerateArray())
                        {
                            list.Add(ConvertJsonElement(arrItem));
                        }

                        return new SafeDynamicList(list);
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