using System;
using System.Collections;
using System.Collections.Generic;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides utilities for merging JSON-like values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All merge operations work on the same raw CLR representations used by the
    /// dynamic JSON pipeline: dictionaries, lists, primitives, and <c>null</c>.
    /// Values that are instances of <see cref="DynamicJsonObject"/> or
    /// <see cref="DynamicJsonList"/> are first normalized via
    /// <see cref="Raw.ToRawObject(object?)"/>.
    /// </para>
    /// <para>
    /// Merging combines the contents of two JSON-like values. By default, object
    /// properties from the right-hand value override or extend those from the
    /// left-hand value, while arrays and primitives from the right-hand value
    /// replace the corresponding left-hand values.
    /// </para>
    /// </remarks>
    public static class DynamicJsonMerge
    {
        /// <summary>
        /// Merges two JSON-like values into a new raw CLR structure.
        /// </summary>
        /// <param name="left">
        /// The base value. Its fields are used as the starting point.
        /// May be a dynamic wrapper, a raw CLR value, or <c>null</c>.
        /// </param>
        /// <param name="right">
        /// The overlay value. Its fields override or extend those from
        /// <paramref name="left"/>. May be a dynamic wrapper, a raw CLR value,
        /// or <c>null</c>.
        /// </param>
        /// <param name="concatArrays">
        /// When <c>true</c>, arrays present in both values are concatenated
        /// (left elements followed by right elements). When <c>false</c>, an
        /// array from <paramref name="right"/> replaces the array from
        /// <paramref name="left"/>.
        /// </param>
        /// <returns>
        /// A new raw CLR structure representing the merged result, or
        /// <c>null</c> when the merged value is <c>null</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// When both values are objects, their keys are combined:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// Keys present only in <paramref name="left"/> are kept.
        /// </description></item>
        /// <item><description>
        /// Keys present only in <paramref name="right"/> are added.
        /// </description></item>
        /// <item><description>
        /// Keys present in both are merged recursively when both values are
        /// objects; otherwise the value from <paramref name="right"/> wins.
        /// </description></item>
        /// </list>
        /// <para>
        /// When values are arrays, behavior depends on <paramref name="concatArrays"/>:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// <c>false</c> (default): the array from <paramref name="right"/>
        /// replaces the array from <paramref name="left"/>.
        /// </description></item>
        /// <item><description>
        /// <c>true</c>: elements from both arrays are concatenated into a new
        /// list, with left elements first.
        /// </description></item>
        /// </list>
        /// <para>
        /// For primitives or mismatched types, the value from
        /// <paramref name="right"/> replaces <paramref name="left"/> unless
        /// <paramref name="right"/> is <c>null</c>, in which case
        /// <paramref name="left"/> is preserved.
        /// </para>
        /// </remarks>
        public static object? Merge(
            object? left,
            object? right,
            bool concatArrays = false)
        {
            var leftRaw = Raw.ToRawObject(left);
            var rightRaw = Raw.ToRawObject(right);

            return MergeInternal(leftRaw, rightRaw, concatArrays);
        }

        /// <summary>
        /// Merges two JSON-like values and returns the result as a dynamic JSON
        /// structure.
        /// </summary>
        /// <param name="left">
        /// The base value. Its fields are used as the starting point.
        /// May be a dynamic wrapper, a raw CLR value, or <c>null</c>.
        /// </param>
        /// <param name="right">
        /// The overlay value. Its fields override or extend those from
        /// <paramref name="left"/>. May be a dynamic wrapper, a raw CLR value,
        /// or <c>null</c>.
        /// </param>
        /// <param name="concatArrays">
        /// When <c>true</c>, arrays present in both values are concatenated
        /// (left elements followed by right elements). When <c>false</c>, an
        /// array from <paramref name="right"/> replaces the array from
        /// <paramref name="left"/>.
        /// </param>
        /// <param name="sanitizationFilter">
        /// Optional predicate controlling key sanitization in the resulting
        /// dynamic JSON wrapper. When <c>null</c>, the default alphanumeric
        /// sanitizer is applied.
        /// </param>
        /// <returns>
        /// A <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/>
        /// representing the merged result, or <c>null</c> when the merged value
        /// is <c>null</c>.
        /// </returns>
        /// <remarks>
        /// This method mirrors the behavior of <see cref="Merge(object?, object?, bool)"/>
        /// but returns the merged value wrapped in the library's dynamic JSON
        /// model for convenient navigation and further transformation.
        /// </remarks>
        public static dynamic? MergeDynamic(
            object? left,
            object? right,
            bool concatArrays = false,
            Func<char, bool>? sanitizationFilter = null)
        {
            var merged = Merge(left, right, concatArrays);

            if (merged is null)
            {
                return null;
            }

            return merged.ToDynamic(sanitizationFilter);
        }

        /// <summary>
        /// Internal merge implementation operating on normalized raw values.
        /// </summary>
        private static object? MergeInternal(
            object? left,
            object? right,
            bool concatArrays)
        {
            // If right is null, preserve left.
            if (right is null)
            {
                return left;
            }

            // Merge dictionaries recursively.
            if (left is IDictionary<string, object?> ld &&
                right is IDictionary<string, object?> rd)
            {
                var result = new Dictionary<string, object?>(
                    ld,
                    StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in rd)
                {
                    if (result.TryGetValue(kvp.Key, out var existing))
                    {
                        result[kvp.Key] = MergeInternal(existing, kvp.Value, concatArrays);
                    }
                    else
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }

                return result;
            }

            // Merge arrays, either by replacement or concatenation.
            if (left is IList lList && right is IList rList)
            {
                if (!concatArrays)
                {
                    // Overwrite mode: right array replaces left array.
                    return right;
                }

                // Concat mode: left elements followed by right elements.
                var merged = new List<object?>(lList.Count + rList.Count);

                foreach (var item in lList)
                {
                    merged.Add(item);
                }

                foreach (var item in rList)
                {
                    merged.Add(item);
                }

                return merged;
            }

            // Primitives or mismatched types: right wins.
            return right;
        }
    }
}