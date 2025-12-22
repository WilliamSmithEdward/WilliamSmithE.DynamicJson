using System.Text.Json;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Provides high-level utilities for parsing JSON strings into dynamic
    /// representations and serializing dynamic values back to JSON.
    /// </summary>
    /// <remarks>
    /// This class serves as the entry point for converting raw JSON into
    /// <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/> instances,
    /// as well as producing JSON output from dynamic structures.
    /// </remarks>
    public static class DynamicJson
    {
        /// <summary>
        /// Parses a JSON string and converts the root element into a dynamic representation,
        /// optionally applying a custom key sanitization filter.
        /// </summary>
        /// <param name="json">
        /// The JSON string to parse. Must not be <c>null</c>, empty, or whitespace.
        /// </param>
        /// <param name="sanitizationFilter">
        /// An optional predicate that determines which characters are retained when sanitizing
        /// JSON object property names. If <c>null</c>, the default alphanumeric sanitizer is used.
        /// The filter is applied consistently throughout all nested objects.
        /// </param>
        /// <returns>
        /// A dynamic representation of the root JSON structure. This will be a
        /// <see cref="DynamicJsonObject"/> for JSON objects or a <see cref="DynamicJsonList"/>
        /// for JSON arrays.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="json"/> is <c>null</c>, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the root JSON element is not an object or an array.
        /// </exception>
        /// <remarks>
        /// This method provides the primary entry point for converting raw JSON text into
        /// the dynamic JSON system. The optional sanitization filter allows callers to control
        /// how JSON object keys are normalized during conversion.
        /// </remarks>
        public static dynamic FromJson(string json, Func<char, bool>? sanitizationFilter = null)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            using var doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            return root.ValueKind switch
            {
                JsonValueKind.Object => root.AsDynamic(sanitizationFilter),
                JsonValueKind.Array => root.AsDynamic(sanitizationFilter),
                _ => throw new InvalidOperationException(
                        $"Unsupported JSON root type: {root.ValueKind}")
            };
        }

        /// <summary>
        /// Serializes the specified value to a JSON string after converting any dynamic
        /// wrapper types into their raw CLR representations.
        /// </summary>
        /// <param name="value">
        /// The value to serialize. May be a primitive, a <see cref="DynamicJsonObject"/>,
        /// a <see cref="DynamicJsonList"/>, or any other JSON-compatible structure.
        /// </param>
        /// <returns>
        /// A JSON string representing the provided value.
        /// </returns>
        /// <remarks>
        /// This method first normalizes the input by converting dynamic wrappers into
        /// dictionaries or lists using <see cref="Raw.ToRawObject(object?)"/>, then
        /// serializes the result with <see cref="System.Text.Json.JsonSerializer"/>.
        /// </remarks>
        public static string ToJson(object? value)
        {
            var raw = Raw.ToRawObject(value);
            return JsonSerializer.Serialize(raw);
        }

        /// <summary>
        /// Computes a minimal structural diff between two JSON-like values.
        /// </summary>
        /// <param name="original">
        /// The original value. May be a <see cref="DynamicJsonObject"/>,
        /// a <see cref="DynamicJsonList"/>, a raw CLR value, or <c>null</c>.
        /// </param>
        /// <param name="updated">
        /// The updated value to compare against. May be any JSON-compatible value.
        /// </param>
        /// <returns>
        /// A raw CLR structure (dictionary, list, primitive, or <c>null</c>) representing
        /// the minimal set of changes required to transform <paramref name="original"/>
        /// into <paramref name="updated"/>.
        /// 
        /// Returns <c>null</c> when both values are equivalent.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method produces a JSON-merge-style patch object, where:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Changed values are included in the diff.</description></item>
        /// <item><description>Missing keys are represented as <c>null</c> entries.</description></item>
        /// <item><description>Unchanged fields are omitted entirely.</description></item>
        /// </list>
        /// <para>
        /// The resulting diff can be applied using <see cref="ApplyPatch(object?, object?)"/>.
        /// </para>
        /// </remarks>
        public static object? Diff(object? original, object? updated)
        {
            return DynamicJsonDiff.Diff(original, updated);
        }

        /// <summary>
        /// Applies a structural diff (patch) to the specified JSON-like value.
        /// </summary>
        /// <param name="original">
        /// The original value to patch. May be a dynamic JSON wrapper, a raw CLR value,
        /// or <c>null</c>.
        /// </param>
        /// <param name="patch">
        /// A patch object produced by <see cref="Diff(object?, object?)"/>, expressed as
        /// raw CLR values (dictionary, list, primitive, or <c>null</c>).
        /// </param>
        /// <returns>
        /// A new raw CLR structure containing the patched result. This value is suitable
        /// for further conversion, serialization, or transformation.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Patch operations follow JSON merge semantics:
        /// </para>
        /// <list type="bullet">
        /// <item><description>A <c>null</c> value removes the corresponding key.</description></item>
        /// <item><description>Object patches are applied recursively.</description></item>
        /// <item><description>Arrays and primitives replace the original value wholesale.</description></item>
        /// </list>
        /// <para>
        /// To receive the patched result as a dynamic JSON wrapper, use
        /// <see cref="ApplyPatchDynamic(object?, object?, Func{char, bool}?)"/>.
        /// </para>
        /// </remarks>
        public static object? ApplyPatch(object? original, object? patch)
        {
            return DynamicJsonDiff.ApplyPatch(original, patch);
        }

        /// <summary>
        /// Computes a structural diff between two JSON-like values and returns the
        /// result as a dynamic JSON structure.
        /// </summary>
        /// <param name="original">
        /// The original JSON-like value. May be dynamic or a raw CLR representation.
        /// </param>
        /// <param name="updated">
        /// The updated JSON-like value to compare against.
        /// </param>
        /// <param name="sanitizationFilter">
        /// Optional predicate controlling how JSON object keys are sanitized in the
        /// resulting dynamic structure. When <c>null</c>, the default alphanumeric
        /// sanitization rules are used.
        /// </param>
        /// <returns>
        /// A <see cref="DynamicJsonObject"/> or <see cref="DynamicJsonList"/> representing
        /// the diff, or <c>null</c> when no differences exist.
        /// </returns>
        /// <remarks>
        /// This method behaves like <see cref="Diff(object?, object?)"/> but wraps the
        /// resulting patch in the library's dynamic JSON model, allowing convenient
        /// dynamic navigation of the diff.
        /// </remarks>
        public static dynamic? DiffDynamic(
            object? original,
            object? updated,
            Func<char, bool>? sanitizationFilter = null)
        {
            return DynamicJsonDiff.DiffDynamic(original, updated, sanitizationFilter);
        }

        /// <summary>
        /// Applies a structural patch to the specified value and returns the result as
        /// a dynamic JSON structure.
        /// </summary>
        /// <param name="original">
        /// The original value to patch. May be a dynamic wrapper, a raw CLR object,
        /// or <c>null</c>.
        /// </param>
        /// <param name="patch">
        /// A patch object produced by <see cref="Diff(object?, object?)"/> or
        /// <see cref="DiffDynamic(object?, object?, Func{char, bool}?)"/>.
        /// </param>
        /// <param name="sanitizationFilter">
        /// Optional predicate controlling key sanitization within the resulting dynamic
        /// JSON wrapper. When <c>null</c>, the default alphanumeric sanitizer is applied.
        /// </param>
        /// <returns>
        /// A dynamic JSON value (<see cref="DynamicJsonObject"/> or
        /// <see cref="DynamicJsonList"/>) representing the patched result,
        /// or <c>null</c> if the resulting value is <c>null</c>.
        /// </returns>
        /// <remarks>
        /// Use this method when you want to immediately continue working with the patched
        /// object using dynamic member access, indexing, or further transformations.
        /// </remarks>
        public static dynamic? ApplyPatchDynamic(
            object? original,
            object? patch,
            Func<char, bool>? sanitizationFilter = null)
        {
            return DynamicJsonDiff.ApplyPatchDynamic(original, patch, sanitizationFilter);
        }

        /// <summary>
        /// Merges two JSON-like values by combining their fields into a new
        /// JSON structure.
        /// </summary>
        /// <param name="left">
        /// The base value. Its fields are used as the starting point.
        /// </param>
        /// <param name="right">
        /// The overlay value. Its fields override or extend those from
        /// <paramref name="left"/>.
        /// </param>
        /// <returns>
        /// A new raw CLR structure representing the merged result.
        /// </returns>
        public static object? Merge(object? left, object? right)
        {
            return DynamicJsonMerge.Merge(left, right);
        }

        /// <summary>
        /// Merges two JSON-like values and returns the result as a dynamic
        /// JSON structure.
        /// </summary>
        /// <param name="left">
        /// The base value. Its fields are used as the starting point.
        /// </param>
        /// <param name="right">
        /// The overlay value. Its fields override or extend those from
        /// <paramref name="left"/>.
        /// </param>
        /// <param name="concatArrays">
        /// When <c>true</c>, arrays present in both values are concatenated
        /// (left elements followed by right elements). When <c>false</c>,
        /// arrays from <paramref name="right"/> replace arrays from
        /// <paramref name="left"/>.
        /// </param>
        /// <param name="sanitizationFilter">
        /// Optional predicate controlling key sanitization in the resulting
        /// dynamic JSON wrapper. When <c>null</c>, the default alphanumeric
        /// sanitizer is used.
        /// </param>
        /// <returns>
        /// A dynamic JSON representation of the merged result, or <c>null</c>
        /// when the merged value is <c>null</c>.
        /// </returns>
        public static dynamic? MergeDynamic(
            object? left,
            object? right,
            bool concatArrays = false,
            Func<char, bool>? sanitizationFilter = null)
        {
            return DynamicJsonMerge.MergeDynamic(left, right, concatArrays, sanitizationFilter);
        }

        /// <summary>
        /// Computes a path-aware structural diff between two JSON-like values.
        /// </summary>
        /// <param name="original">
        /// The original value to compare. May be a dynamic JSON wrapper or a raw
        /// CLR value produced by the dynamic JSON pipeline.
        /// </param>
        /// <param name="updated">
        /// The updated value to compare against. May be a dynamic JSON wrapper or a
        /// raw CLR value produced by the dynamic JSON pipeline.
        /// </param>
        /// <returns>
        /// A read-only list of <see cref="DiffEntry"/> instances describing each
        /// detected change, including its associated <see cref="JsonPath"/>.
        /// The list is empty when no differences are found.
        /// </returns>
        /// <remarks>
        /// This method delegates to <see cref="DynamicJsonPathDiff.DiffWithPaths(object?, object?)"/>
        /// and preserves the same diff semantics as the core dynamic diff logic.
        /// </remarks>
        public static IReadOnlyList<DiffEntry> DiffWithPaths(object? original, object? updated)
        {
            return DynamicJsonPathDiff.DiffWithPaths(original, updated);
        }
    }
}