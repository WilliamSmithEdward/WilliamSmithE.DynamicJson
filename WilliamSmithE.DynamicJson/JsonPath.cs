using System;
using System.Collections;
using System.Collections.Generic;

namespace WilliamSmithE.DynamicJson
{
    /// <summary>
    /// Represents an immutable, structural identifier for a location within a
    /// JSON-like value graph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="JsonPath"/> describes <em>where</em> a value exists inside a
    /// JSON structure, independent of any specific JSON instance. It is composed
    /// of an ordered sequence of property and array index segments.
    /// </para>
    /// <para>
    /// Paths are built fluently using <see cref="Property(string)"/> and
    /// <see cref="Index(int)"/> and support structural equality and stable hashing,
    /// making them safe for use as dictionary keys and set members.
    /// </para>
    /// <para>
    /// This type is intentionally observational:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// It does not store or reference JSON values.
    /// </description></item>
    /// <item><description>
    /// It does not track parent relationships.
    /// </description></item>
    /// <item><description>
    /// It is not embedded in dynamic JSON objects.
    /// </description></item>
    /// </list>
    /// <para>
    /// <see cref="JsonPath"/> is primarily used for diagnostics, diff reporting,
    /// auditing, and explainability, rather than for navigation or mutation.
    /// </para>
    /// </remarks>
    public readonly struct JsonPath : IEquatable<JsonPath>, IEnumerable<JsonPath.Segment>
    {
        private readonly Segment[] segments;

        private JsonPath(Segment[] segments)
        {
            this.segments = segments ?? [];
        }

        /// <summary>
        /// Gets the root path, representing the top-level of a JSON structure.
        /// </summary>
        public static JsonPath Root => new([]);

        /// <summary>
        /// Gets a value indicating whether this path represents the root location.
        /// </summary>
        public bool IsRoot => segments.Length == 0;

        /// <summary>
        /// Gets the number of segments that make up this path.
        /// </summary>
        public int Length => segments.Length;

        /// <summary>
        /// Gets the path segment at the specified zero-based index.
        /// </summary>
        /// <param name="i">The index of the segment to retrieve.</param>
        /// <returns>The <see cref="Segment"/> at the specified index.</returns>
        public Segment this[int i] => segments[i];

        /// <summary>
        /// Returns a new <see cref="JsonPath"/> with an appended object property segment.
        /// </summary>
        /// <param name="name">The JSON object property name.</param>
        /// <returns>
        /// A new <see cref="JsonPath"/> representing the current path followed by the
        /// specified property.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="name"/> is <c>null</c> or empty.
        /// </exception>
        public JsonPath Property(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Property name cannot be null or empty.", nameof(name));

            return Append(Segment.ForProperty(name));
        }

        /// <summary>
        /// Returns a new <see cref="JsonPath"/> with an appended array index segment.
        /// </summary>
        /// <param name="index">The zero-based array index.</param>
        /// <returns>
        /// A new <see cref="JsonPath"/> representing the current path followed by the
        /// specified array index.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is negative.
        /// </exception>
        public JsonPath Index(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative.");

            return Append(Segment.ForIndex(index));
        }

        private JsonPath Append(Segment seg)
        {
            var next = new Segment[segments.Length + 1];
            Array.Copy(segments, next, segments.Length);
            next[^1] = seg;
            return new JsonPath(next);
        }

        /// <summary>
        /// Returns the canonical string representation of this path.
        /// </summary>
        /// <returns>
        /// A slash-delimited string representation of the path, where object properties
        /// are prefixed with <c>/</c> and array indices are enclosed in square brackets.
        /// The root path is represented as <c>/</c>.
        /// </returns>
        public override string ToString()
        {
            if (IsRoot)
                return "/";

            var s = "";

            foreach (var seg in segments)
            {
                if (seg.Kind == SegmentKind.Property)
                {
                    s += "/" + seg.PropertyName;
                }
                else
                {
                    s += "[" + seg.ArrayIndex + "]";
                }
            }

            return s;
        }

        /// <summary>
        /// Determines whether this instance is structurally equal to another
        /// <see cref="JsonPath"/>.
        /// </summary>
        /// <param name="other">The path to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if both paths contain the same sequence of segments; otherwise,
        /// <c>false</c>.
        /// </returns>
        public bool Equals(JsonPath other)
        {
            if (segments.Length != other.segments.Length)
                return false;

            for (int i = 0; i < segments.Length; i++)
            {
                if (!segments[i].Equals(other.segments[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether this instance is equal to the specified object.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is a <see cref="JsonPath"/> and is
        /// structurally equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj) => obj is JsonPath other && Equals(other);

        /// <summary>
        /// Returns a hash code based on the structural contents of this path.
        /// </summary>
        /// <returns>
        /// A hash code computed from the ordered sequence of path segments.
        /// </returns>
        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var seg in segments)
                hash.Add(seg);

            return hash.ToHashCode();
        }

        /// <summary>
        /// Determines whether two <see cref="JsonPath"/> instances are equal.
        /// </summary>
        /// <param name="left">The first path to compare.</param>
        /// <param name="right">The second path to compare.</param>
        /// <returns>
        /// <c>true</c> if the paths are structurally equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(JsonPath left, JsonPath right) => left.Equals(right);

        /// <summary>
        /// Determines whether two <see cref="JsonPath"/> instances are not equal.
        /// </summary>
        /// <param name="left">The first path to compare.</param>
        /// <param name="right">The second path to compare.</param>
        /// <returns>
        /// <c>true</c> if the paths are not structurally equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(JsonPath left, JsonPath right) => !left.Equals(right);

        /// <summary>
        /// Returns an enumerator that iterates through the segments of this path
        /// in order.
        /// </summary>
        /// <returns>
        /// An enumerator over the <see cref="Segment"/> values that make up this path.
        /// </returns>
        public IEnumerator<Segment> GetEnumerator()
        {
            for (int i = 0; i < segments.Length; i++)
                yield return segments[i];
        }

        /// <summary>
        /// Returns a non-generic enumerator that iterates through the segments of this path.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> that can be used to iterate through the
        /// <see cref="Segment"/> values that make up this path.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Specifies the kind of segment represented in a <see cref="JsonPath"/>.
        /// </summary>
        public enum SegmentKind
        {
            /// <summary>
            /// Represents an object property access.
            /// </summary>
            Property,

            /// <summary>
            /// Represents an array index access.
            /// </summary>
            Index
        }

        /// <summary>
        /// Represents a single segment within a <see cref="JsonPath"/>,
        /// identifying either an object property or an array index.
        /// </summary>
        /// <remarks>
        /// A segment is immutable and participates in structural equality
        /// comparisons for path matching and hashing.
        /// </remarks>
        public readonly record struct Segment
        {
            /// <summary>
            /// Gets the kind of this path segment.
            /// </summary>
            public SegmentKind Kind { get; }

            /// <summary>
            /// Gets the property name for a <see cref="SegmentKind.Property"/> segment.
            /// </summary>
            /// <remarks>
            /// This value is only meaningful when <see cref="Kind"/> is
            /// <see cref="SegmentKind.Property"/>.
            /// </remarks>
            public string PropertyName { get; }

            /// <summary>
            /// Gets the array index for a <see cref="SegmentKind.Index"/> segment.
            /// </summary>
            /// <remarks>
            /// This value is only meaningful when <see cref="Kind"/> is
            /// <see cref="SegmentKind.Index"/>.
            /// </remarks>
            public int ArrayIndex { get; }

            private Segment(SegmentKind kind, string propertyName, int arrayIndex)
            {
                Kind = kind;
                PropertyName = propertyName;
                ArrayIndex = arrayIndex;
            }

            /// <summary>
            /// Creates a property-name segment.
            /// </summary>
            /// <param name="name">The JSON object property name.</param>
            /// <returns>
            /// A <see cref="Segment"/> representing access to the specified property.
            /// </returns>
            public static Segment ForProperty(string name)
            {
                return new Segment(SegmentKind.Property, name, -1);
            }

            /// <summary>
            /// Creates an array-index segment.
            /// </summary>
            /// <param name="index">The zero-based array index.</param>
            /// <returns>
            /// A <see cref="Segment"/> representing access to the specified array index.
            /// </returns>
            public static Segment ForIndex(int index)
            {
                return new Segment(SegmentKind.Index, string.Empty, index);
            }
        }
    }
}