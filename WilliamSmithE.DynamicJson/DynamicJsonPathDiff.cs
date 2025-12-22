using System.Collections;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides path-aware diffing utilities for JSON-like values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class computes structural differences between two values while
    /// emitting explicit <see cref="JsonPath"/> information for each change.
    /// It is intended for diagnostics, auditing, logging, and explainability,
    /// not for mutation or navigation.
    /// </para>
    /// <para>
    /// All comparisons operate on normalized raw CLR representations produced
    /// by <see cref="Raw.ToRawObject(object?)"/>. Dynamic wrapper types
    /// (<see cref="DynamicJsonObject"/> and <see cref="DynamicJsonList"/>) are
    /// unwrapped prior to comparison.
    /// </para>
    /// <para>
    /// Diff semantics intentionally mirror the core dynamic diff behavior:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// Objects are compared key-by-key and recurse into child paths.
    /// </description></item>
    /// <item><description>
    /// Arrays are treated as atomic values and are replaced wholesale when changed.
    /// </description></item>
    /// <item><description>
    /// Primitive values and mismatched types produce a single change at the
    /// current path.
    /// </description></item>
    /// </list>
    /// <para>
    /// This class does not store path information on dynamic values themselves.
    /// All paths are computed ephemerally during traversal.
    /// </para>
    /// </remarks>
    public static class DynamicJsonPathDiff
    {
        /// <summary>
        /// Computes a path-aware structural diff between two JSON-like values.
        /// </summary>
        /// <param name="original">
        /// The original value to compare. May be a <see cref="DynamicJsonObject"/>,
        /// <see cref="DynamicJsonList"/>, or any raw CLR value produced by the dynamic
        /// JSON pipeline.
        /// </param>
        /// <param name="updated">
        /// The updated value to compare against. May be a <see cref="DynamicJsonObject"/>,
        /// <see cref="DynamicJsonList"/>, or any raw CLR value produced by the dynamic
        /// JSON pipeline.
        /// </param>
        /// <returns>
        /// A read-only list of <see cref="DiffEntry"/> instances, each describing a
        /// structural change at a specific <see cref="JsonPath"/>.  
        /// The list is empty when no differences are detected.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method performs a recursive comparison of the two values after
        /// normalizing them via <see cref="Raw.ToRawObject(object?)"/>.
        /// </para>
        /// <para>
        /// Diff semantics match the core dynamic diff behavior:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// Objects are diffed key-by-key, producing child paths for added, removed,
        /// or modified properties.
        /// </description></item>
        /// <item><description>
        /// Arrays are treated as atomic values and are replaced wholesale when changed.
        /// </description></item>
        /// <item><description>
        /// Primitive values and mismatched types produce a single change at the
        /// current path.
        /// </description></item>
        /// </list>
        /// <para>
        /// The returned paths are observational and are not stored on the dynamic
        /// JSON values themselves.
        /// </para>
        /// </remarks>
        public static IReadOnlyList<DiffEntry> DiffWithPaths(object? original, object? updated)
        {
            var originalRaw = Raw.ToRawObject(original);
            var updatedRaw = Raw.ToRawObject(updated);

            var results = new List<DiffEntry>();
            DiffInternal(results, JsonPath.Root, originalRaw, updatedRaw);
            return results;
        }

        private static void DiffInternal(
            List<DiffEntry> results,
            JsonPath path,
            object? original,
            object? updated)
        {
            if (AreEqual(original, updated))
                return;

            if (original is null && updated is not null)
            {
                results.Add(new DiffEntry(path, null, Raw.ToRawObject(updated), DiffKind.Added));
                return;
            }

            if (original is not null && updated is null)
            {
                results.Add(new DiffEntry(path, Raw.ToRawObject(original), null, DiffKind.Removed));
                return;
            }

            if (original is IDictionary<string, object?> od && updated is IDictionary<string, object?> ud)
            {
                var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var k in od.Keys) allKeys.Add(k);
                foreach (var k in ud.Keys) allKeys.Add(k);

                foreach (var key in allKeys)
                {
                    var hasOld = od.TryGetValue(key, out var oldValue);
                    var hasNew = ud.TryGetValue(key, out var newValue);

                    var childPath = path.Property(key);

                    if (!hasNew)
                    {
                        results.Add(new DiffEntry(childPath, Raw.ToRawObject(oldValue), null, DiffKind.Removed));
                        continue;
                    }

                    if (!hasOld)
                    {
                        results.Add(new DiffEntry(childPath, null, Raw.ToRawObject(newValue), DiffKind.Added));
                        continue;
                    }

                    DiffInternal(results, childPath, oldValue, newValue);
                }

                return;
            }

            // Match existing diff semantics: arrays are atomic (replace wholesale).
            if (original is IList || updated is IList)
            {
                results.Add(new DiffEntry(path, Raw.ToRawObject(original), Raw.ToRawObject(updated), DiffKind.Modified));
                return;
            }

            // Primitives / mismatched types: modified at this path.
            results.Add(new DiffEntry(path, Raw.ToRawObject(original), Raw.ToRawObject(updated), DiffKind.Modified));
        }

        private static bool AreEqual(object? a, object? b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            if (a is IDictionary<string, object?> ad && b is IDictionary<string, object?> bd)
            {
                if (ad.Count != bd.Count)
                    return false;

                foreach (var kvp in ad)
                {
                    if (!bd.TryGetValue(kvp.Key, out var otherValue))
                        return false;

                    if (!AreEqual(kvp.Value, otherValue))
                        return false;
                }

                return true;
            }

            if (a is IList al && b is IList bl)
            {
                if (al.Count != bl.Count)
                    return false;

                for (int i = 0; i < al.Count; i++)
                {
                    if (!AreEqual(al[i], bl[i]))
                        return false;
                }

                return true;
            }

            return Equals(a, b);
        }
    }
}