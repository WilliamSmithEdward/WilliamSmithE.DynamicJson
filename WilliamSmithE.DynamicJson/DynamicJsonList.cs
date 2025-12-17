using System.Collections;
using System.Dynamic;
using System.Text.Json;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Represents a dynamic wrapper around a list of JSON-derived values,
    /// supporting safe dynamic access, indexing, enumeration, and conversion
    /// to strongly typed objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each element in the list may be a primitive value, a
    /// <see cref="DynamicJsonObject"/>, or another <see cref="DynamicJsonList"/>,
    /// depending on the structure of the underlying JSON.
    /// </para>
    /// <para>
    /// The class provides dynamic indexing, LINQ compatibility through
    /// <see cref="IEnumerable{T}"/>, and utility methods for mapping items
    /// to strongly typed models.
    /// </para>
    /// </remarks>
    public class DynamicJsonList(IList<object?> items) : DynamicObject, IEnumerable<object?>
    {
        /// <summary>
        /// Gets the number of items contained in this <see cref="DynamicJsonList"/>.
        /// </summary>
        public int Count => items.Count;

        private readonly IList<object?> items = items ?? throw new ArgumentNullException(nameof(items));

        /// <summary>
        /// Attempts to invoke a dynamic member on the <see cref="DynamicJsonList"/>.
        /// </summary>
        /// <param name="binder">
        /// Provides information about the invoked member, including the method name.
        /// </param>
        /// <param name="args">
        /// The arguments supplied for the invocation. Not used by this implementation.
        /// </param>
        /// <param name="result">
        /// When this method returns, contains the result of the invocation if handled;
        /// otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the invocation is recognized and processed; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This implementation supports the dynamic invocation of a <c>First()</c> method,
        /// returning the first element in the list that is either a
        /// <see cref="DynamicJsonObject"/> or a <see cref="DynamicJsonList"/>.
        /// All other method names are ignored.
        /// </remarks>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            if (binder.Name.Equals("First", StringComparison.OrdinalIgnoreCase))
            {

                result = First();
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Converts all <see cref="DynamicJsonObject"/> elements in the list into
        /// instances of the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <returns>
        /// A <see cref="List{T}"/> containing all elements that could be successfully
        /// mapped to <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>
        /// This method iterates through the list and attempts to map each
        /// <see cref="DynamicJsonObject"/> using <see cref="DynamicJsonObject.AsType{T}"/>.
        /// Non-object items are ignored, and elements that cannot be mapped are skipped
        /// without throwing exceptions.
        /// </remarks>
        public List<T> ToList<T>() where T : class, new()
        {
            var result = new List<T>();

            foreach (var item in items)
            {
                if (item is DynamicJsonObject sdo)
                {
                    var mapped = sdo.AsType<T>();

                    if (mapped != null)
                    {
                        result.Add(mapped);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Converts all primitive (scalar) values in the list into a strongly typed
        /// <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The target scalar type to extract from the list. Only elements whose runtime
        /// type matches <typeparamref name="T"/> are included.
        /// </typeparam>
        /// <returns>
        /// A <see cref="List{T}"/> containing all elements in the list that can be
        /// assigned to <typeparamref name="T"/>. Elements of other types are ignored.
        /// </returns>
        /// <remarks>
        /// This method is intended for JSON arrays that contain primitive values such as
        /// strings, numbers, or booleans. It does not attempt type conversion and does
        /// not process <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/>
        /// instances.
        /// <para>
        /// For example, given a JSON array <c>["user", "admin"]</c>, calling
        /// <c>ToScalarList&lt;string&gt;()</c> will return a list containing the two
        /// strings.
        /// </para>
        /// </remarks>
        public List<T> ToScalarList<T>()
        {
            var list = new List<T>();

            foreach (var item in items)
            {
                if (item is T value)
                    list.Add(value);
            }

            return list;
        }

        /// <summary>
        /// Attempts to access an element in the list via dynamic index notation.
        /// </summary>
        /// <param name="binder">
        /// Provides information about the indexing operation.
        /// </param>
        /// <param name="indexes">
        /// The index arguments supplied for the operation. Must contain exactly one
        /// integer index.
        /// </param>
        /// <param name="result">
        /// When this method returns, contains the element at the specified index if the
        /// index is valid; otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the index is valid and the element is returned; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method enables dynamic index access such as <c>list[0]</c> on a
        /// <see cref="DynamicJsonList"/>.  
        /// The index must be a single non-negative integer within the bounds of the list.
        /// Invalid indexes throw exceptions.
        /// </remarks>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            // Validate index argument
            if (indexes.Length != 1 || indexes[0] is not int idx)
                throw new ArgumentException("DynamicJsonList indexer requires a single integer index.");

            // Bounds check with explicit error message
            if (idx < 0 || idx >= items.Count)
                throw new IndexOutOfRangeException(
                    $"Index {idx} is out of range for this DynamicJsonList. Valid indices are 0 to {items.Count - 1}."
                );

            // Return the value when valid
            result = items[idx];
            return true; // IMPORTANT: never return false
        }

        /// <summary>
        /// Converts this <see cref="DynamicJsonList"/> into a plain list containing
        /// only raw CLR values.
        /// </summary>
        /// <returns>
        /// A <see cref="List{T}"/> of objects where each element is a primitive value,
        /// a nested dictionary, or a list suitable for JSON serialization.
        /// </returns>
        /// <remarks>
        /// This method recursively unwraps <see cref="DynamicJsonObject"/> and
        /// <see cref="DynamicJsonList"/> instances into raw structures using
        /// <see cref="Raw.ToRawObject(object?)"/>, producing a representation that
        /// mirrors the original JSON.
        /// </remarks>
        public List<object?> ToRawArray()
        {
            var list = new List<object?>();

            foreach (var item in items)
            {
                list.Add(Raw.ToRawObject(item));
            }

            return list;
        }

        /// <summary>
        /// Serializes this <see cref="DynamicJsonList"/> to a JSON string.
        /// </summary>
        /// <returns>
        /// A JSON representation of the list produced by converting all elements
        /// to their raw CLR equivalents.
        /// </returns>
        /// <remarks>
        /// This method first converts the dynamic list into a raw array via
        /// <see cref="ToRawArray"/> and then serializes it using
        /// <see cref="System.Text.Json.JsonSerializer"/>.
        /// </remarks>
        public string ToJson()
        {
            return JsonSerializer.Serialize(ToRawArray());
        }

        /// <summary>
        /// Creates a deep copy of this <see cref="DynamicJsonList"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="DynamicJsonList"/> that is a copy of the original.</returns>
        public DynamicJsonList Clone() => DynamicJson.FromJson(ToJson());

        /// <summary>
        /// Returns an enumerator that iterates through the elements of the
        /// <see cref="DynamicJsonList"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the list.
        /// </returns>
        /// <remarks>
        /// This enables <c>foreach</c> iteration and LINQ operations over the
        /// underlying collection of dynamic items.
        /// </remarks>
        public IEnumerator<object?> GetEnumerator()
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Returns a non-generic enumerator that iterates through the
        /// <see cref="DynamicJsonList"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> for iterating through the list.
        /// </returns>
        /// <remarks>
        /// This method provides the non-generic implementation required by
        /// <see cref="IEnumerable"/> and delegates to the generic
        /// <see cref="GetEnumerator"/> method.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private dynamic? First()
        {
            foreach (var item in items)
            {

                if (item is DynamicJsonObject sdo)
                {

                    return sdo;
                }

                if (item is DynamicJsonList sdl)
                {

                    return sdl;
                }
            }

            return null;
        }
    }
}