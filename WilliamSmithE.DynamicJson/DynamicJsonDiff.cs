using System;
using System.Collections;
using System.Collections.Generic;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides utilities for computing structural diffs between JSON-like values
    /// and applying patch objects using a JSON-merge style algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All public methods operate on the same raw CLR representations used by the
    /// dynamic JSON pipeline:
    /// dictionaries (<see cref="IDictionary{TKey, TValue}"/>),
    /// lists (<see cref="IList"/>), primitives, and <c>null</c>.
    /// Values that are instances of <see cref="DynamicJsonObject"/> or
    /// <see cref="DynamicJsonList"/> are first normalized via
    /// <see cref="Raw.ToRawObject(object?)"/>.
    /// </para>
    /// <para>
    /// Diffs are represented as JSON-merge style patches:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// Object patches are dictionaries keyed by property name.
    /// </description></item>
    /// <item><description>
    /// A <c>null</c> value in a patch dictionary removes the corresponding key.
    /// </description></item>
    /// <item><description>
    /// Non-object patch values (including arrays and primitives) replace the
    /// target value wholesale.
    /// </description></item>
    /// </list>
    /// </remarks>
    public static class DynamicJsonDiff
    {
        /// <summary>
        /// Computes a patch that transforms <paramref name="original"/> into
        /// <paramref name="updated"/>.
        /// </summary>
        /// <param name="original">
        /// The original value. May be a dynamic wrapper, a raw dictionary,
        /// a list, a primitive, or <c>null</c>.
        /// </param>
        /// <param name="updated">
        /// The updated value. May be a dynamic wrapper, a raw dictionary,
        /// a list, a primitive, or <c>null</c>.
        /// </param>
        /// <returns>
        /// A patch object expressed as raw CLR values, or <c>null</c> when
        /// there are no differences.
        /// </returns>
        /// <remarks>
        /// <para>
        /// When both values are dictionaries, they are diffed key by key:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// Keys present only in <paramref name="original"/> are removed via a
        /// <c>null</c> entry in the patch.
        /// </description></item>
        /// <item><description>
        /// Keys present only in <paramref name="updated"/> are added with their
        /// full values.
        /// </description></item>
        /// <item><description>
        /// Keys present in both are diffed recursively.
        /// </description></item>
        /// </list>
        /// <para>
        /// When values are not both dictionaries, any difference results in a
        /// patch value that fully replaces the original.
        /// </para>
        /// </remarks>
        public static object? Diff(object? original, object? updated)
        {
            var originalRaw = Raw.ToRawObject(original);
            var updatedRaw = Raw.ToRawObject(updated);

            return DiffInternal(originalRaw, updatedRaw);
        }

        /// <summary>
        /// Applies a patch created by <see cref="Diff(object?, object?)"/> to an
        /// original value.
        /// </summary>
        /// <param name="original">
        /// The original value to patch. May be a dynamic wrapper, a raw
        /// dictionary, a list, a primitive, or <c>null</c>.
        /// </param>
        /// <param name="patch">
        /// The patch to apply. May be a patch dictionary, a primitive, a list,
        /// or <c>null</c>.
        /// </param>
        /// <returns>
        /// A new raw CLR value representing the patched result.
        /// </returns>
        /// <remarks>
        /// <para>
        /// When the patch is a dictionary, it is applied to a cloned
        /// dictionary version of <paramref name="original"/>:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// A patch value of <c>null</c> removes the corresponding key.
        /// </description></item>
        /// <item><description>
        /// A non-null patch value is applied recursively when the existing
        /// value is also a dictionary, or replaces the existing value
        /// wholesale otherwise.
        /// </description></item>
        /// </list>
        /// <para>
        /// When the patch is not a dictionary, it replaces the original value
        /// entirely.
        /// </para>
        /// </remarks>
        public static object? ApplyPatch(object? original, object? patch)
        {
            var originalRaw = Raw.ToRawObject(original);
            var patchRaw = Raw.ToRawObject(patch);

            return ApplyPatchInternal(originalRaw, patchRaw);
        }

        /// <summary>
        /// Computes a patch between two values and returns it as a dynamic JSON
        /// structure.
        /// </summary>
        /// <param name="original">
        /// The original value. May be a dynamic wrapper, a raw dictionary,
        /// a list, a primitive, or <c>null</c>.
        /// </param>
        /// <param name="updated">
        /// The updated value. May be a dynamic wrapper, a raw dictionary,
        /// a list, a primitive, or <c>null</c>.
        /// </param>
        /// <param name="sanitizationFilter">
        /// Optional sanitization filter used when constructing dynamic keys.
        /// If <c>null</c>, the default alphanumeric sanitizer is applied.
        /// </param>
        /// <returns>
        /// A dynamic JSON representation of the patch, or <c>null</c> when
        /// there are no differences.
        /// </returns>
        /// <remarks>
        /// The returned value is produced by converting the raw patch through
        /// the existing object to dynamic pipeline, so callers receive a
        /// <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/>
        /// where appropriate.
        /// </remarks>
        public static dynamic? DiffDynamic(
            object? original,
            object? updated,
            Func<char, bool>? sanitizationFilter = null)
        {
            var patch = Diff(original, updated);

            if (patch is null)
            {
                // No differences
                return null;
            }

            // Reuse the existing object -> dynamic pipeline
            return patch.ToDynamic(sanitizationFilter);
        }

        /// <summary>
        /// Applies a patch expressed as dynamic JSON and returns the result as a
        /// dynamic JSON structure.
        /// </summary>
        /// <param name="original">
        /// The original value to patch. May be a dynamic wrapper, a raw
        /// dictionary, a list, a primitive, or <c>null</c>.
        /// </param>
        /// <param name="patch">
        /// The patch to apply. May be a dynamic wrapper, a raw patch dictionary,
        /// a list, a primitive, or <c>null</c>.
        /// </param>
        /// <param name="sanitizationFilter">
        /// Optional sanitization filter used when constructing dynamic keys.
        /// If <c>null</c>, the default alphanumeric sanitizer is applied.
        /// </param>
        /// <returns>
        /// A dynamic JSON representation of the patched result, or <c>null</c>
        /// when the resulting value is <c>null</c>.
        /// </returns>
        /// <remarks>
        /// This method mirrors the behavior of <see cref="ApplyPatch"/> but
        /// returns the patched value as a dynamic JSON wrapper type instead of
        /// a raw CLR structure.
        /// </remarks>
        public static dynamic? ApplyPatchDynamic(
            object? original,
            object? patch,
            Func<char, bool>? sanitizationFilter = null)
        {
            var result = ApplyPatch(original, patch);

            if (result is null)
            {
                return null;
            }

            return result.ToDynamic(sanitizationFilter);
        }

        /// <summary>
        /// Internal diff implementation operating on normalized raw values.
        /// </summary>
        private static object? DiffInternal(object? original, object? updated)
        {
            if (AreEqual(original, updated))
            {
                return null;
            }

            if (original is IDictionary<string, object?> od &&
                updated is IDictionary<string, object?> ud)
            {
                var result = new Dictionary<string, object?>(
                    StringComparer.OrdinalIgnoreCase);

                var allKeys = new HashSet<string>(od.Keys, StringComparer.OrdinalIgnoreCase);

                allKeys.UnionWith(ud.Keys);

                foreach (var key in allKeys)
                {
                    var hasOld = od.TryGetValue(key, out var oldValue);
                    var hasNew = ud.TryGetValue(key, out var newValue);

                    if (!hasNew)
                    {
                        // Key removed
                        result[key] = null;
                        continue;
                    }

                    if (!hasOld)
                    {
                        // New key added
                        result[key] = Raw.ToRawObject(newValue);
                        continue;
                    }

                    var childPatch = DiffInternal(oldValue, newValue);

                    if (childPatch != null)
                    {
                        result[key] = childPatch;
                    }
                }

                if (result.Count == 0)
                {
                    return null;
                }

                return result;
            }

            // Arrays and primitives are treated as full replacements.
            return Raw.ToRawObject(updated);
        }

        /// <summary>
        /// Internal patch application operating on normalized raw values.
        /// </summary>
        private static object? ApplyPatchInternal(object? original, object? patch)
        {
            if (patch is null)
            {
                return null;
            }

            if (patch is IDictionary<string, object?> patchDict)
            {
                var result = AsDictionary(original);

                foreach (var kvp in patchDict)
                {
                    var key = kvp.Key;
                    var patchValue = kvp.Value;

                    if (patchValue is null)
                    {
                        // Removal
                        result.Remove(key);
                        continue;
                    }

                    result.TryGetValue(key, out var existingValue);

                    var applied = ApplyPatchInternal(existingValue, patchValue);

                    result[key] = applied;
                }

                return result;
            }

            // Any non-object patch replaces the value wholesale.
            return patch;
        }

        /// <summary>
        /// Converts a value into a new dictionary instance, cloning the contents
        /// when the value is already a dictionary, or creating an empty dictionary
        /// otherwise.
        /// </summary>
        private static Dictionary<string, object?> AsDictionary(object? value)
        {
            if (value is IDictionary<string, object?> dict)
            {
                return new Dictionary<string, object?>(
                    dict,
                    StringComparer.OrdinalIgnoreCase);
            }

            return new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Performs a deep equality comparison between two raw values, taking
        /// dictionaries and lists into account.
        /// </summary>
        private static bool AreEqual(object? a, object? b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            if (a is IDictionary<string, object?> ad &&
                b is IDictionary<string, object?> bd)
            {
                if (ad.Count != bd.Count)
                {
                    return false;
                }

                foreach (var kvp in ad)
                {
                    if (!bd.TryGetValue(kvp.Key, out var otherValue))
                    {
                        return false;
                    }

                    if (!AreEqual(kvp.Value, otherValue))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (a is IList al && b is IList bl)
            {
                if (al.Count != bl.Count)
                {
                    return false;
                }

                for (int i = 0; i < al.Count; i++)
                {
                    if (!AreEqual(al[i], bl[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return Equals(a, b);
        }
    }
}