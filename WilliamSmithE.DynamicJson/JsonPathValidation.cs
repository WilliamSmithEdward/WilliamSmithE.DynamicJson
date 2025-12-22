namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides helpers for validating whether JSON paths are applicable to
    /// dynamic JSON values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class bridges path syntax and path resolution by offering convenience
    /// methods that determine whether a path can be safely used against a specific
    /// dynamic JSON value.
    /// </para>
    /// <para>
    /// Validation is performed without mutating the JSON or storing path state.
    /// String-based paths are parsed explicitly, while <see cref="JsonPath"/>
    /// instances are resolved directly.
    /// </para>
    /// <para>
    /// This separation preserves the distinction between path syntax, path
    /// resolution, and path semantics while providing a practical API for callers
    /// working with external input or diagnostic data.
    /// </para>
    /// </remarks>
    public static class JsonPathValidation
    {
        /// <summary>
        /// Determines whether a path string is well-formed and resolves against the
        /// specified dynamic JSON value.
        /// </summary>
        /// <param name="dynamicJson">
        /// The dynamic JSON value to validate the path against.
        /// </param>
        /// <param name="path">
        /// The path string to validate.
        /// </param>
        /// <returns>
        /// <c>true</c> if the path is syntactically valid and resolves to a value within
        /// <paramref name="dynamicJson"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This overload is intended for paths originating from external sources such
        /// as user input, configuration, or logs. It combines parsing and resolution
        /// into a single convenience check.
        /// </remarks>
        public static bool IsValidFor(object? dynamicJson, string path)
        {
            return JsonPath.TryParse(path, out var parsed) &&
                   JsonPathNavigation.TryGetAtPath(dynamicJson, parsed, out _);
        }

        /// <summary>
        /// Determines whether a <see cref="JsonPath"/> resolves against the specified
        /// dynamic JSON value.
        /// </summary>
        /// <param name="dynamicJson">
        /// The dynamic JSON value to validate the path against.
        /// </param>
        /// <param name="path">
        /// The parsed <see cref="JsonPath"/> to validate.
        /// </param>
        /// <returns>
        /// <c>true</c> if the path resolves to a value within
        /// <paramref name="dynamicJson"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This overload is intended for paths produced internally by the library or
        /// built fluently in code. It performs resolution only and does not involve
        /// string parsing.
        /// </remarks>
        public static bool IsValidFor(object? dynamicJson, JsonPath path)
        {
            return JsonPathNavigation.TryGetAtPath(dynamicJson, path, out _);
        }
    }
}