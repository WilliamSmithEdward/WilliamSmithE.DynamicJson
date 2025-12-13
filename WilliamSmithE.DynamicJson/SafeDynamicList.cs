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
    /// <see cref="SafeDynamicObject"/>, or another <see cref="SafeDynamicList"/>,
    /// depending on the structure of the underlying JSON.
    /// </para>
    /// <para>
    /// The class provides dynamic indexing, LINQ compatibility through
    /// <see cref="IEnumerable{T}"/>, and utility methods for mapping items
    /// to strongly typed models.
    /// </para>
    /// </remarks>
    public class SafeDynamicList(IList<object?> items) : DynamicObject, IEnumerable<object?>
    {
        private readonly IList<object?> items = items ?? throw new ArgumentNullException(nameof(items));

        /// <summary>
        /// Attempts to invoke a dynamic member on the <see cref="SafeDynamicList"/>.
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
        /// <see cref="SafeDynamicObject"/> or a <see cref="SafeDynamicList"/>.
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
        /// Returns the first element in the list mapped to the specified type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <param name="nullOnConversionError">
        /// If <c>true</c>, mapping errors return <c>null</c> instead of throwing an exception.
        /// </param>
        /// <returns>
        /// The first element successfully mapped to <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the list is empty, or when the first element cannot be converted
        /// to <typeparamref name="T"/> and <paramref name="nullOnConversionError"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// This method examines the list in order and attempts to map the first
        /// <see cref="SafeDynamicObject"/> it encounters to the specified type using
        /// <see cref="SafeDynamicObject.AsType{T}"/>.
        /// Non-object items are ignored.
        /// </remarks>
        public T? First<T>(bool nullOnConversionError = false) where T : class, new()
        {
            foreach (var item in items)
            {
                if (item is SafeDynamicObject sdo)
                {
                    var mapped = sdo.AsType<T>();

                    if (mapped != null)
                        return mapped;

                    if (nullOnConversionError)
                        return null;

                    throw new InvalidOperationException($"The first element in the list could not be converted to type {typeof(T).Name}.");
                }
            }

            throw new InvalidOperationException("Sequence contains no elements.");
        }

        /// <summary>
        /// Returns the first element in the list that can be mapped to the specified
        /// type <typeparamref name="T"/>, or <c>null</c> if no such element exists.
        /// </summary>
        /// <typeparam name="T">
        /// The target class type to map into. Must have a public parameterless constructor.
        /// </typeparam>
        /// <param name="nullOnConversionError">
        /// If <c>true</c>, mapping errors return <c>null</c> instead of skipping the element.
        /// </param>
        /// <returns>
        /// The first successfully mapped <typeparamref name="T"/> instance, or <c>null</c>
        /// if none can be mapped.
        /// </returns>
        /// <remarks>
        /// This method iterates through the list and attempts to map each
        /// <see cref="SafeDynamicObject"/> using <see cref="SafeDynamicObject.AsType{T}"/>.
        /// Non-object items are ignored.
        /// 
        /// Unlike <c>First{T}</c>, this method never throws when the list is empty or
        /// when no elements can be converted.
        /// </remarks>
        public T? FirstOrDefault<T>(bool nullOnConversionError = false) where T : class, new()
        {
            foreach (var item in items)
            {
                if (item is SafeDynamicObject sdo)
                {
                    var mapped = sdo.AsType<T>();

                    if (mapped != null)
                        return mapped;

                    if (nullOnConversionError)
                        return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Converts all <see cref="SafeDynamicObject"/> elements in the list into
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
        /// <see cref="SafeDynamicObject"/> using <see cref="SafeDynamicObject.AsType{T}"/>.
        /// Non-object items are ignored, and elements that cannot be mapped are skipped
        /// without throwing exceptions.
        /// </remarks>
        public List<T> ToList<T>() where T : class, new()
        {
            var result = new List<T>();

            foreach (var item in items)
            {
                if (item is SafeDynamicObject sdo)
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
        /// <see cref="SafeDynamicList"/>.  
        /// The index must be a single non-negative integer within the bounds of the list.
        /// Invalid indexes do not throw exceptions.
        /// </remarks>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            if (indexes.Length == 1 && indexes[0] is int idx && idx >= 0 && idx < items.Count)
            {

                result = items[idx];
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Converts this <see cref="SafeDynamicList"/> into a plain list containing
        /// only raw CLR values.
        /// </summary>
        /// <returns>
        /// A <see cref="List{T}"/> of objects where each element is a primitive value,
        /// a nested dictionary, or a list suitable for JSON serialization.
        /// </returns>
        /// <remarks>
        /// This method recursively unwraps <see cref="SafeDynamicObject"/> and
        /// <see cref="SafeDynamicList"/> instances into raw structures using
        /// <see cref="Raw.ToRawValue(object?)"/>, producing a representation that
        /// mirrors the original JSON.
        /// </remarks>
        public List<object?> ToRawArray()
        {
            var list = new List<object?>();

            foreach (var item in items)
            {
                list.Add(Raw.ToRawValue(item));
            }

            return list;
        }

        /// <summary>
        /// Serializes this <see cref="SafeDynamicList"/> to a JSON string.
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
        /// Returns an enumerator that iterates through the elements of the
        /// <see cref="SafeDynamicList"/>.
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
        /// <see cref="SafeDynamicList"/>.
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

                if (item is SafeDynamicObject sdo)
                {

                    return sdo;
                }

                if (item is SafeDynamicList sdl)
                {

                    return sdl;
                }
            }

            return null;
        }
    }
}