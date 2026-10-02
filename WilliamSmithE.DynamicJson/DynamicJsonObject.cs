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
    /// <param name="values">
    /// The property names and values. Each name is sanitized; when two names sanitize to the
    /// same key, the later one gets a numeric suffix (<c>Name</c>, <c>Name2</c>, <c>Name3</c>).
    /// Must not be <c>null</c>, or <see cref="ArgumentNullException"/> is thrown.
    /// </param>
    /// <param name="sanitizationFilter">
    /// An optional predicate that decides which characters of a property name are kept.
    /// If <c>null</c>, letters and digits (<see cref="char.IsLetterOrDigit(char)"/>) are kept.
    /// </param>
    public class DynamicJsonObject(IDictionary<string, object?> values, Func<char, bool>? sanitizationFilter = null) : DynamicObject
    {
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
        /// Gets a newline-separated list of this object's key/value pairs, with each
        /// entry formatted as <c>Key: Value</c>.
        /// </summary>
        /// <value>
        /// A string where each line represents one property of the
        /// <see cref="DynamicJsonObject"/> in the format <c>Key: Value</c>.  
        /// Keys correspond to the sanitized names stored in <see cref="Properties"/>.
        /// </value>
        /// <remarks>
        /// This property provides a shallow textual representation.  
        /// Nested <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/> values
        /// are rendered using their default <c>ToString()</c> output.
        /// </remarks>
        public string KeyValuePairsAsString =>
            string.Join(Environment.NewLine, Properties.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

        /// <summary>
        /// Attempts to retrieve a dynamic member value using a case-insensitive
        /// lookup against this object's sanitized property keys.
        /// </summary>
        /// <param name="binder">
        /// Provides information about the requested member, including the name
        /// specified by the dynamic call site.
        /// </param>
        /// <param name="result">
        /// When this method returns, contains the value associated with the
        /// requested member if found; otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// Always returns <c>true</c>, indicating that the dynamic member access
        /// was handled, even when no matching key exists.
        /// </returns>
        /// <remarks>
        /// The requested member name is compared, ignoring case, with the keys as
        /// they were sanitized when this object was built. The member name itself
        /// is not sanitized, so <c>obj.first_name</c> does not find the key
        /// <c>firstname</c>. Missing members do not throw; they return <c>null</c>.
        /// </remarks>
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            ArgumentNullException.ThrowIfNull(binder);

            var targetKey = binder.Name;

            foreach (var kvp in Properties)
            {
                if (kvp.Key.Equals(targetKey, StringComparison.OrdinalIgnoreCase))
                {
                    result = kvp.Value;
                    return true;
                }
            }

            result = null;
            return true;
        }

        /// <summary>
        /// Assigns a value to a dynamic member, matching existing keys case-insensitively.
        /// </summary>
        /// <param name="binder">
        /// Contains information about the member being assigned, including the
        /// name supplied at the dynamic call site.
        /// </param>
        /// <param name="value">
        /// The value to associate with the specified member name.
        /// </param>
        /// <returns>
        /// Always returns <c>true</c>, indicating the assignment was handled.
        /// </returns>
        /// <remarks>
        /// The member name is not sanitized: an existing key that matches it, ignoring
        /// case, is overwritten, and otherwise a new key is added exactly as written.
        /// Dynamic assignment never throws due to missing members.
        /// </remarks>
        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            ArgumentNullException.ThrowIfNull(binder);

            values[binder.Name] = value;
            return true;
        }

        /// <summary>
        /// Maps this dynamic JSON object to a new instance of the type
        /// <typeparamref name="T"/> using sanitized, case-insensitive property
        /// name matching.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless
        /// constructor.
        /// </typeparam>
        /// <param name="sanitizationFilter">
        /// An optional predicate that decides which characters of each target property
        /// name are kept before it is compared with the keys. If <c>null</c>, letters and
        /// digits are kept.
        /// </param>
        /// <returns>
        /// A new instance of <typeparamref name="T"/> with any matching writable
        /// properties populated from this object's values. This method never
        /// returns <c>null</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Property names on the target type are sanitized before comparison to
        /// ensure consistent matching with the sanitized keys stored internally
        /// for this dynamic JSON object. Matching is performed in a
        /// case-insensitive manner.
        /// </para>
        /// <para>
        /// For each matching property, the value is assigned either directly when
        /// the runtime type is compatible, or via <see cref="Convert.ChangeType(object?, Type)"/>
        /// when a conversion is required. If a value is <c>null</c>, the
        /// corresponding property is explicitly set to <c>null</c>.
        /// </para>
        /// <para>
        /// Conversion errors are allowed to propagate to the caller. A nested
        /// <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/> value mapped
        /// to a property of another type throws <see cref="InvalidCastException"/>.
        /// For an exception-free variant, use <see cref="TryAsType{T}(out T)"/>.
        /// </para>
        /// </remarks>
        public T? AsType<T>(Func<char, bool>? sanitizationFilter = null) where T : class, new()
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
        /// Attempts to map this dynamic JSON object to a new instance of the type
        /// <typeparamref name="T"/> without throwing exceptions.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <param name="result">
        /// When this method returns, contains the populated instance of
        /// <typeparamref name="T"/> if mapping succeeds; otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the mapping was successful and <paramref name="result"/> is not
        /// <c>null</c>; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method wraps <see cref="AsType{T}"/> in a safe, exception-free form.
        /// Any conversion, assignment, or type mismatch errors are caught and treated
        /// as a failed mapping attempt. For stricter behavior, call
        /// <c>AsType&lt;T&gt;</c> directly.
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
        /// <see cref="Raw.ToRawObject(object?)"/>, producing a representation that
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
                dict[kvp.Key] = Raw.ToRawObject(kvp.Value);
            }

            return dict;
        }

        /// <summary>
        /// Attempts to retrieve a value associated with the specified
        /// property name from the underlying JSON object.
        /// </summary>
        /// <param name="name">
        /// The property name to look up, in its sanitized form (<c>orderid</c> for the
        /// JSON key <c>order-id</c>). Comparison is case-insensitive.
        /// </param>
        /// <param name="value">
        /// When this method returns, contains the value associated with
        /// <paramref name="name"/> if it exists; otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the property exists; otherwise <c>false</c>.
        /// </returns>
        public bool TryGetValue(string name, out object? value)
        {
            return values.TryGetValue(name, out value);
        }

        /// <summary>
        /// Serializes this <see cref="DynamicJsonObject"/> to a JSON string.
        /// </summary>
        /// <returns>
        /// Compact JSON produced from <see cref="ToRawObject"/> with
        /// <see cref="System.Text.Json.JsonSerializer"/>, using the sanitized keys.
        /// </returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(ToRawObject());
        }

        /// <summary>
        /// Creates a deep copy of this <see cref="DynamicJsonObject"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="DynamicJsonObject"/> that is a copy of the original.</returns>
        /// <remarks>
        /// The copy is made by serializing to JSON and parsing again with the default
        /// sanitizer, so a custom sanitization filter used to build this object is not
        /// applied to the copy.
        /// </remarks>
        public DynamicJsonObject Clone() => DynamicJson.FromJson(ToJson());

        /// <summary>
        /// The internal value dictionary, built from the constructor's <c>values</c>
        /// by sanitizing keys, ensuring case-insensitive lookup, and resolving
        /// duplicate keys by appending a numeric suffix.
        /// </summary>
        /// <remarks>
        /// Each key in the source dictionary is sanitized. If the sanitized
        /// key already exists, a numeric counter is appended to create a
        /// unique key (e.g., <c>Name</c>, <c>Name2</c>, <c>Name3</c>).
        /// Throws <see cref="ArgumentNullException"/> when <c>values</c> is <c>null</c>.
        /// </remarks>
        private readonly Dictionary<string, object?> values =
            values?.Aggregate(
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
                (dict, kvp) =>
                {
                    var baseKey = kvp.Key.Sanitize(sanitizationFilter);
                    var key = baseKey;

                    int counter = 2;
                    while (dict.ContainsKey(key))
                    {
                        key = $"{baseKey}{counter}";
                        counter++;
                    }

                    dict[key] = kvp.Value;
                    return dict;
                })
            ?? throw new ArgumentNullException(nameof(values));
    }
}