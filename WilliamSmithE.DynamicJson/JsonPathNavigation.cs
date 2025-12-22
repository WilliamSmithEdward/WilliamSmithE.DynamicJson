using System.Collections;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides utilities for resolving <see cref="JsonPath"/> instances against
    /// dynamic JSON values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class enables path-based navigation over values produced by the
    /// DynamicJson model. Paths are resolved structurally against raw CLR
    /// representations (dictionaries, lists, and primitives).
    /// </para>
    /// <para>
    /// Resolution is read-only and observational. No path information is stored on
    /// the JSON values themselves.
    /// </para>
    /// </remarks>
    public static class JsonPathNavigation
    {
        /// <summary>
        /// Attempts to resolve the specified <see cref="JsonPath"/> against a dynamic
        /// JSON value.
        /// </summary>
        /// <param name="root">
        /// The dynamic JSON value to resolve the path against.
        /// </param>
        /// <param name="path">
        /// The parsed <see cref="JsonPath"/> to resolve.
        /// </param>
        /// <param name="value">
        /// When this method returns, contains the value at the specified path if
        /// resolution succeeded; otherwise, <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the path resolves to a value within <paramref name="root"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method performs structural navigation only. It does not coerce
        /// values or validate semantics beyond path resolution.
        /// </remarks>
        public static bool TryGetAtPath(object? root, JsonPath path, out object? value)
        {
            var current = Raw.ToRawObject(root);

            foreach (var seg in path)
            {
                if (seg.Kind == JsonPath.SegmentKind.Property)
                {
                    if (current is not IDictionary<string, object?> dict)
                    {
                        value = null;
                        return false;
                    }

                    if (!dict.TryGetValue(seg.PropertyName, out current))
                    {
                        value = null;
                        return false;
                    }

                    continue;
                }

                if (current is not IList list)
                {
                    value = null;
                    return false;
                }

                var idx = seg.ArrayIndex;

                if (idx < 0 || idx >= list.Count)
                {
                    value = null;
                    return false;
                }

                current = list[idx];
            }

            value = current;
            return true;
        }

        /// <summary>
        /// Attempts to resolve a path string against a dynamic JSON value.
        /// </summary>
        /// <param name="root">
        /// The dynamic JSON value to resolve the path against.
        /// </param>
        /// <param name="path">
        /// The canonical path string to parse and resolve.
        /// </param>
        /// <param name="value">
        /// When this method returns, contains the value at the specified path if
        /// resolution succeeded; otherwise, <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the path string is syntactically valid and resolves to a value
        /// within <paramref name="root"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method combines string parsing and path resolution into a single
        /// non-throwing operation. It returns <c>false</c> if the path string is not a
        /// valid <see cref="JsonPath"/> or if the parsed path does not resolve against
        /// the provided JSON value.
        /// </remarks>
        public static bool TryGetAtPath(object? root, string path, out object? value)
        {
            if (!JsonPath.TryParse(path, out var jsonPath))
            {
                value = null;
                return false;
            }

            return TryGetAtPath(root, jsonPath, out value);
        }

        /// <summary>
        /// Resolves the specified <see cref="JsonPath"/> against a dynamic JSON value.
        /// </summary>
        /// <param name="root">
        /// The dynamic JSON value to resolve the path against.
        /// </param>
        /// <param name="path">
        /// The parsed <see cref="JsonPath"/> to resolve.
        /// </param>
        /// <returns>
        /// The value at the specified path.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the path does not resolve against <paramref name="root"/>.
        /// </exception>
        public static object? GetAtPath(object? root, JsonPath path)
        {
            if (!TryGetAtPath(root, path, out var value))
                throw new KeyNotFoundException($"Path not found: {path}");

            return value;
        }

        /// <summary>
        /// Resolves a path string against a dynamic JSON value.
        /// </summary>
        /// <param name="root">
        /// The dynamic JSON value to resolve the path against.
        /// </param>
        /// <param name="path">
        /// The canonical path string to parse and resolve.
        /// </param>
        /// <returns>
        /// The value at the specified path.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the path string is not a valid <see cref="JsonPath"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the parsed path does not resolve against
        /// <paramref name="root"/>.
        /// </exception>
        /// <remarks>
        /// This overload is intended for convenience when working with external
        /// path strings. It parses the path before attempting resolution.
        /// </remarks>
        public static object? GetAtPath(object? root, string path)
        {
            var jsonPath = JsonPath.Parse(path);

            if (!TryGetAtPath(root, jsonPath, out var value))
                throw new KeyNotFoundException($"Path not found: {path}");

            return value;
        }
    }
}