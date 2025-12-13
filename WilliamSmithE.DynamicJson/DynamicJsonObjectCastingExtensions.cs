using System.Reflection;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides extension methods for mapping <see cref="DynamicJsonObject"/> instances
    /// and related values into strongly typed model classes.
    /// </summary>
    /// <remarks>
    /// These extensions enable convenient conversion of dynamic JSON structures into
    /// concrete CLR types using best-effort property matching and value conversion.
    /// </remarks>
    public static class DynamicJsonObjectCastingExtensions
    {
        /// <summary>
        /// Maps the values of the specified <see cref="DynamicJsonObject"/> to a new instance
        /// of the type <typeparamref name="T"/> using sanitized, case-insensitive key matching.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <param name="source">
        /// The <see cref="DynamicJsonObject"/> whose values will be used for mapping.
        /// </param>
        /// <returns>
        /// A new instance of <typeparamref name="T"/> with matching writable properties
        /// populated from the dynamic object's values. Never returns <c>null</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Property matching is performed using case-insensitive comparison between the
        /// sanitized names of the target type's public instance properties and the sanitized
        /// keys exposed by <see cref="DynamicJsonObject.Properties"/>.
        /// </para>
        /// <para>
        /// If a value is <c>null</c>, the corresponding target property is explicitly set
        /// to <c>null</c>. For non-null values, the method attempts assignment using either
        /// direct type compatibility or <see cref="Convert.ChangeType(object?, Type)"/>.
        /// </para>
        /// <para>
        /// Conversion errors are allowed to propagate to the caller. For a safe,
        /// exception-free variant, use <c>TryAsType&lt;T&gt;</c> instead.
        /// </para>
        /// </remarks>
        public static T? AsType<T>(this DynamicJsonObject source)
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

                    if (!kvp.Key.Equals(targetProp.Name.Sanitize(), StringComparison.OrdinalIgnoreCase))
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
            }

            return result;
        }

        /// <summary>
        /// Attempts to map the specified <see cref="DynamicJsonObject"/> to a new instance
        /// of the type <typeparamref name="T"/> without throwing exceptions.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <param name="source">
        /// The <see cref="DynamicJsonObject"/> whose values will be used for mapping.
        /// </param>
        /// <param name="result">
        /// When this method returns, contains the populated <typeparamref name="T"/> instance
        /// if the mapping succeeds; otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if mapping was successful and <paramref name="result"/> is non-<c>null</c>;
        /// otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method wraps <see cref="AsType{T}(DynamicJsonObject)"/> in a safe,
        /// exception-free form. Any conversion or assignment errors are silently handled,
        /// ensuring that dynamic-to-POCO mapping can be attempted without risk of throwing
        /// exceptions.
        /// </para>
        /// <para>
        /// Callers that require strict behavior can use <see cref="AsType{T}(DynamicJsonObject)"/>
        /// directly instead of this method.
        /// </para>
        /// </remarks>
        public static bool TryAsType<T>(this DynamicJsonObject source, out T? result) where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(source);

            try
            {
                var mapped = source.AsType<T>();

                if (mapped != null)
                {
                    result = mapped;
                    return true;
                }

                result = null;
                return false;
            }

            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to map the specified value to a new instance of the type
        /// <typeparamref name="T"/> when possible.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <param name="value">
        /// The value to map. May be a <see cref="DynamicJsonObject"/> or an instance of
        /// <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        /// If <paramref name="value"/> is a <see cref="DynamicJsonObject"/>, a new instance of
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
            if (value is DynamicJsonObject sdo)
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