using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.ExceptionServices;
using System.Text;

namespace WilliamSmithE.DynamicJson
{
    public class SafeDynamicList(IList<object?> items) : DynamicObject, IEnumerable<object?>
    {
        private readonly IList<object?> items = items ?? throw new ArgumentNullException(nameof(items));

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

        public IEnumerator<object?> GetEnumerator()
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }

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
