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
        /// of the type <typeparamref name="T"/>, using optional key sanitization for property matching.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <param name="source">
        /// The <see cref="DynamicJsonObject"/> whose values will be used for mapping. Must not be <c>null</c>.
        /// </param>
        /// <param name="sanitizationFilter">
        /// An optional predicate that determines which characters are retained when sanitizing
        /// target property names for comparison with JSON keys. If <c>null</c>, the default
        /// alphanumeric sanitizer is applied.
        /// </param>
        /// <returns>
        /// A new instance of <typeparamref name="T"/> with matching writable properties populated
        /// from the dynamic object's values. Never returns <c>null</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="source"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Property matching is performed by sanitizing each target property name using the
        /// specified <paramref name="sanitizationFilter"/> (or the default sanitizer when
        /// <c>null</c>) and comparing the result to the keys exposed by
        /// <see cref="DynamicJsonObject.Properties"/> in a case-insensitive manner.
        /// </para>
        /// <para>
        /// If a value is <c>null</c>, the corresponding property is explicitly set to <c>null</c>.
        /// For non-null values, the method attempts assignment using either direct type compatibility
        /// or <see cref="Convert.ChangeType(object?, Type)"/> when a conversion is required.
        /// Conversion errors are allowed to propagate to the caller.
        /// </para>
        /// </remarks>
        public static T? AsType<T>(this DynamicJsonObject source, Func<char, bool>? sanitizationFilter = null)
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

                    if (!kvp.Key.Equals(targetProp.Name.Sanitize(sanitizationFilter), StringComparison.OrdinalIgnoreCase))
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
        /// Attempts to map the specified <see cref="DynamicJsonObject"/> to a new instance of
        /// the type <typeparamref name="T"/>, using optional key sanitization for property matching,
        /// without throwing exceptions.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <param name="source">
        /// The <see cref="DynamicJsonObject"/> whose values will be used for mapping.
        /// Must not be <c>null</c>.
        /// </param>
        /// <param name="result">
        /// When this method returns, contains the populated <typeparamref name="T"/> instance
        /// if the mapping succeeds; otherwise <c>null</c>.
        /// </param>
        /// <param name="sanitizationFilter">
        /// An optional predicate that determines which characters are retained when sanitizing
        /// target property names for comparison with JSON keys. If <c>null</c>, the default
        /// alphanumeric sanitizer is applied.
        /// </param>
        /// <returns>
        /// <c>true</c> if mapping succeeds and <paramref name="result"/> is not <c>null</c>;
        /// otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="source"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// This method wraps <see cref="AsType{T}(DynamicJsonObject, Func{char, bool}?)"/> in
        /// a safe, exception-free form. Any conversion or assignment errors are caught and
        /// treated as a failed mapping attempt.
        /// </remarks>
        public static bool TryAsType<T>(this DynamicJsonObject source, out T? result, Func<char, bool>? sanitizationFilter = null) where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(source);

            try
            {
                var mapped = source.AsType<T>(sanitizationFilter);

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
        /// Attempts to map the specified value to an instance of the type <typeparamref name="T"/>.
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
        /// This method provides a convenient way to consume values that may originate from either
        /// dynamic JSON structures or already typed objects without requiring the caller to branch
        /// on the underlying type.
        /// </remarks>
        public static T? AsType<T>(this object? value, Func<char, bool>? sanitizationFilter = null) where T : class, new()
        {
            if (value is DynamicJsonObject sdo)
            {
                return sdo.AsType<T>(sanitizationFilter);
            }

            if (value is T alreadyTyped)
            {
                return alreadyTyped;
            }

            return null;
        }
    }
}