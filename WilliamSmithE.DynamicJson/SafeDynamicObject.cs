using System.Dynamic;
using System.Reflection;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Dynamic wrapper over a dictionary of values.
    /// Missing properties return an empty string.
    /// </summary>
    public class SafeDynamicObject(IDictionary<string, object?> values) : DynamicObject
    {
        private readonly IDictionary<string, object?> values = values ?? throw new ArgumentNullException(nameof(values));
        public IReadOnlyDictionary<string, object?> Properties =>
            values as IReadOnlyDictionary<string, object?>
            ?? throw new InvalidOperationException("Backing store is not a dictionary.");

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            foreach (var kvp in values)
            {
                if (kvp.Key.Equals(binder.Name, StringComparison.OrdinalIgnoreCase))
                {
                    result = kvp.Value;
                    return true;
                }
            }

            result = string.Empty;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            ArgumentNullException.ThrowIfNull(binder);

            values[binder.Name] = value;
            return true;
        }

        public T? AsType<T>() where T : class, new()
        {
            var result = new T();

            var targetProps = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var targetProp in targetProps)
            {

                if (!targetProp.CanWrite)
                {
                    continue;
                }

                foreach (var kvp in Properties)
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
                        // best-effort: if conversion fails, just skip this property
                    }

                    break;
                }
            }

            return result;
        }
    }
}
