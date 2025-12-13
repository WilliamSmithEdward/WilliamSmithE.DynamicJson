using System.Reflection;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides extension methods for mapping <see cref="SafeDynamicObject"/> instances
    /// and related values into strongly typed model classes.
    /// </summary>
    /// <remarks>
    /// These extensions enable convenient conversion of dynamic JSON structures into
    /// concrete CLR types using best-effort property matching and value conversion.
    /// </remarks>
    public static class SafeDynamicObjectCastingExtensions
    {
        /// <summary>
        /// Attempts to map the values of the specified <see cref="SafeDynamicObject"/> to a new
        /// instance of the type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <param name="source">
        /// The <see cref="SafeDynamicObject"/> whose values will be used for mapping.
        /// </param>
        /// <returns>
        /// A new instance of <typeparamref name="T"/> with matching writable properties
        /// populated from the dynamic object's values. If a property cannot be mapped or
        /// converted, it is skipped. Never returns <c>null</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Property matching is performed using case-insensitive comparison between the names
        /// of the target type's public instance properties and the keys exposed by
        /// <see cref="SafeDynamicObject.Properties"/>.
        /// </para>
        /// <para>
        /// If a value is <c>null</c>, the corresponding target property is explicitly set to
        /// <c>null</c>. For non-null values, the method attempts assignment using either direct
        /// type compatibility or <see cref="Convert.ChangeType(object?, Type)"/>.
        /// </para>
        /// <para>
        /// Conversion errors are swallowed silently to preserve best-effort behavior. Only
        /// writable properties on the target type are considered.
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Attempts to map the specified value to a new instance of the type
        /// <typeparamref name="T"/> when possible.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <param name="value">
        /// The value to map. May be a <see cref="SafeDynamicObject"/> or an instance of
        /// <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        /// If <paramref name="value"/> is a <see cref="SafeDynamicObject"/>, a new instance of
        /// <typeparamref name="T"/> populated from its values; if it is already a
        /// <typeparamref name="T"/>, the same instance is returned; otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        /// This method provides a convenient way to consume values that may already be
        /// strongly typed or may originate from a dynamic JSON mapping, without requiring
        /// the caller to branch on the underlying type.
        /// </remarks>
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