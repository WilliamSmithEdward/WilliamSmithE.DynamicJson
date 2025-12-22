namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Specifies the type of structural change detected during a path-aware diff.
    /// </summary>
    public enum DiffKind
    {
        /// <summary>
        /// Indicates that a value was added at the specified path.
        /// </summary>
        Added,

        /// <summary>
        /// Indicates that a value was removed from the specified path.
        /// </summary>
        Removed,

        /// <summary>
        /// Indicates that an existing value at the specified path was modified.
        /// </summary>
        Modified
    }

    /// <summary>
    /// Represents a single path-aware difference between two JSON-like values.
    /// </summary>
    /// <param name="Path">
    /// The <see cref="JsonPath"/> identifying the location of the change.
    /// </param>
    /// <param name="OldValue">
    /// The value before the change occurred, or <c>null</c> if the value was added.
    /// </param>
    /// <param name="NewValue">
    /// The value after the change occurred, or <c>null</c> if the value was removed.
    /// </param>
    /// <param name="Kind">
    /// The type of change represented by this entry.
    /// </param>
    /// <remarks>
    /// Instances of this type are produced by path-aware diff operations and are
    /// intended for diagnostics, auditing, logging, and explainability.
    /// </remarks>
    public sealed record DiffEntry(
        JsonPath Path,
        object? OldValue,
        object? NewValue,
        DiffKind Kind
    );
}