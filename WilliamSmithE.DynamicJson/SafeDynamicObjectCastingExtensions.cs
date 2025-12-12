using System.Reflection;

namespace WilliamSmithE.DynamicJson
{
    public static class SafeDynamicObjectCastingExtensions
    {
        public static T? AsType<T>(this SafeDynamicObject source)
            where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(source);

            var result = new T();
            var targetProps = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var targetProp in targetProps)
            {

                if (!targetProp.CanWrite)
                {
                    continue;
                }

                // Find matching key in the dynamic object's properties (case-insensitive)
                foreach (var kvp in source.Properties)
                {

                    if (!kvp.Key.Equals(targetProp.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var value = kvp.Value;

                    if (value == null)
                    {
                        targetProp.SetValue(result, null);
                        break;
                    }

                    var targetType = Nullable.GetUnderlyingType(targetProp.PropertyType) ?? targetProp.PropertyType;

                    try
                    {

                        if (targetType.IsInstanceOfType(value))
                        {

                            targetProp.SetValue(result, value);
                        }

                        else
                        {

                            var converted = Convert.ChangeType(value, targetType);
                            targetProp.SetValue(result, converted);
                        }
                    }
                    catch
                    {
                        // Best-effort: if conversion fails, just skip this property
                    }

                    break;
                }
            }

            return result;
        }

        public static T? AsType<T>(this object? value)
        where T : class, new()
        {
            if (value is SafeDynamicObject sdo)
            {
                return sdo.AsType<T>();
            }

            if (value is T alreadyTyped)
            {
                return alreadyTyped;
            }

            return null;
        }
    }
}