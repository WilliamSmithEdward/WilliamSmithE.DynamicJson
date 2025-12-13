using System.Dynamic;
using System.Reflection;
using System.Text.Json;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Represents a dynamic wrapper around a JSON-derived object, enabling
    /// case-insensitive property access and safe navigation of JSON data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each entry in the underlying dictionary may represent a primitive value,
    /// a nested <see cref="DynamicJsonObject"/>, or a <see cref="DynamicJsonList"/>,
    /// depending on the structure of the original JSON.
    /// </para>
    /// <para>
    /// The class implements dynamic member access through
    /// <see cref="DynamicObject"/>, allowing usage such as <c>obj.PropertyName</c>
    /// without throwing exceptions for missing members.
    /// </para>
    /// <para>
    /// The object also provides utility methods for converting the dynamic
    /// structure into strongly typed models and for producing raw CLR structures
    /// suitable for serialization.
    /// </para>
    /// </remarks>
    public class DynamicJsonObject(IDictionary<string, object?> values) : DynamicObject
    {
        private readonly IDictionary<string, object?> values = values ?? throw new ArgumentNullException(nameof(values));
        /// <summary>
        /// Gets a read-only view of the key/value pairs contained in this
        /// <see cref="DynamicJsonObject"/>.
        /// </summary>
        /// <remarks>
        /// The underlying value store must be backed by an <see cref="IDictionary{TKey, TValue}"/>.
        /// If it is not, an <see cref="InvalidOperationException"/> is thrown.
        /// 
        /// Keys are compared using <see cref="StringComparer.OrdinalIgnoreCase"/> during
        /// dynamic member access and internal lookups.
        /// 
        /// This property exposes the raw values, which may include primitives,
        /// nested <see cref="DynamicJsonObject"/> instances, or <see cref="DynamicJsonList"/> 
        /// instances, depending on the structure of the original JSON.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the backing store is not a dictionary.
        /// </exception>
        public IReadOnlyDictionary<string, object?> Properties =>
            values as IReadOnlyDictionary<string, object?>
            ?? throw new InvalidOperationException("Backing store is not a dictionary.");

        /// <summary>
        /// Attempts to retrieve a member value dynamically using case-insensitive
        /// property name matching.
        /// </summary>
        /// <param name="binder">
        /// Provides information about the member being accessed, including the
        /// requested member name.
        /// </param>
        /// <param name="result">
        /// When this method returns, contains the value associated with the
        /// requested member if a matching key exists; otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// Always returns <c>true</c>, indicating that member access was handled,
        /// even when the requested property does not exist.
        /// </returns>
        /// <remarks>
        /// This method searches the underlying value dictionary using case-insensitive
        /// comparison. If a matching property is found, its value is returned. If not,
        /// <c>null</c> is assigned to <paramref name="result"/>, ensuring that dynamic
        /// access never throws for missing members. Callers requiring strict behavior
        /// should use explicit accessors instead of dynamic member access.
        /// </remarks>
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

            result = null;
            return true;
        }

        /// <summary>
        /// Attempts to set a dynamic member value using the specified binder name.
        /// </summary>
        /// <param name="binder">
        /// Provides information about the member being assigned, including the
        /// member name.
        /// </param>
        /// <param name="value">
        /// The value to assign to the member.
        /// </param>
        /// <returns>
        /// Always returns <c>true</c>.
        /// </returns>
        /// <remarks>
        /// This method stores the provided value in the underlying dictionary using
        /// the binder's member name as the key. Assignment is case-sensitive and
        /// overwrites any existing entry for the same key.
        /// </remarks>
        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            ArgumentNullException.ThrowIfNull(binder);

            values[binder.Name] = value;
            return true;
        }

        /// <summary>
        /// Attempts to map the values of this <see cref="DynamicJsonObject"/> to a new
        /// instance of the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <returns>
        /// A new instance of <typeparamref name="T"/> with matching writable properties
        /// populated from this object's values. If a property cannot be mapped or converted,
        /// it is skipped. Never returns <c>null</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Property matching is performed using case-insensitive comparison between the names
        /// of the target type's public instance properties and the keys of this object's
        /// underlying value dictionary.
        /// </para>
        /// <para>
        /// If a property value is <c>null</c>, the corresponding target property is explicitly
        /// set to <c>null</c>. For non-null values, the method attempts assignment using either
        /// direct type compatibility or <see cref="Convert.ChangeType(object?, Type)"/>.
        /// </para>
        /// <para>
        /// Conversion errors are swallowed silently to preserve best-effort behavior. Only
        /// writable properties on the target type are considered.
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Attempts to map this <see cref="DynamicJsonObject"/> to an instance of the
        /// specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <param name="result">
        /// When this method returns, contains the populated <typeparamref name="T"/> instance
        /// if the mapping succeeds; otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if mapping was successful; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method wraps <see cref="AsType{T}"/> in a safe, exception-free form.
        /// Any conversion or assignment errors are silently handled, ensuring that
        /// dynamic-to-POCO mapping can be attempted without risk of throwing exceptions.
        /// </remarks>
        public bool TryAsType<T>(out T? result) where T : class, new()
        {
            try
            {
                result = AsType<T>();

                if (result != null)
                    return true;

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
        /// Converts this <see cref="DynamicJsonObject"/> into a plain dictionary
        /// containing only raw CLR values.
        /// </summary>
        /// <returns>
        /// A <see cref="Dictionary{TKey, TValue}"/> whose keys match the dynamic
        /// object's properties and whose values are primitives, nested dictionaries,
        /// or lists suitable for JSON serialization.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method recursively unwraps <see cref="DynamicJsonObject"/> and
        /// <see cref="DynamicJsonList"/> instances into raw structures via
        /// <see cref="Raw.ToRawValue(object?)"/>, producing a representation that
        /// mirrors the original JSON.
        /// </para>
        /// <para>
        /// The returned dictionary uses <see cref="StringComparer.OrdinalIgnoreCase"/>
        /// to preserve the case-insensitive behavior of dynamic access.
        /// </para>
        /// </remarks>
        public Dictionary<string, object?> ToRawObject()
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in Properties)
            {
                dict[kvp.Key] = Raw.ToRawValue(kvp.Value);
            }

            return dict;
        }

        /// <summary>
        /// Serializes this <see cref="DynamicJsonObject"/> to a JSON string.
        /// </summary>
        /// <returns>
        /// A JSON representation of the object produced by converting all values
        /// to their raw CLR equivalents.
        /// </returns>
        /// <remarks>
        /// This method first converts the dynamic object into a raw dictionary via
        /// <see cref="ToRawObject"/> and then serializes it using
        /// <see cref="System.Text.Json.JsonSerializer"/>.
        /// </remarks>
        public string ToJson()
        {
            return JsonSerializer.Serialize(ToRawObject());
        }
    }
}