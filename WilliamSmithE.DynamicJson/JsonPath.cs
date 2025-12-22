using System;
using System.Collections;
using System.Collections.Generic;

namespace WilliamSmithE.DynamicJson
{
    public readonly struct JsonPath : IEquatable<JsonPath>, IEnumerable<JsonPath.Segment>
    {
        private readonly Segment[] segments;

        private JsonPath(Segment[] segments)
        {
            this.segments = segments ?? [];
        }

        public static JsonPath Root => new([]);

        public bool IsRoot => segments.Length == 0;

        public int Length => segments.Length;

        public Segment this[int i] => segments[i];

        public JsonPath Property(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Property name cannot be null or empty.", nameof(name));

            return Append(Segment.ForProperty(name));
        }

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

        public override bool Equals(object? obj) => obj is JsonPath other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var seg in segments)
                hash.Add(seg);

            return hash.ToHashCode();
        }

        public static bool operator ==(JsonPath left, JsonPath right) => left.Equals(right);

        public static bool operator !=(JsonPath left, JsonPath right) => !left.Equals(right);

        public IEnumerator<Segment> GetEnumerator()
        {
            for (int i = 0; i < segments.Length; i++)
                yield return segments[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public enum SegmentKind
        {
            Property,
            Index
        }

        public readonly record struct Segment
        {
            public SegmentKind Kind { get; }

            public string PropertyName { get; }

            public int ArrayIndex { get; }

            private Segment(SegmentKind kind, string propertyName, int arrayIndex)
            {
                Kind = kind;
                PropertyName = propertyName;
                ArrayIndex = arrayIndex;
            }

            public static Segment ForProperty(string name)
            {
                return new Segment(SegmentKind.Property, name, -1);
            }

            public static Segment ForIndex(int index)
            {
                return new Segment(SegmentKind.Index, string.Empty, index);
            }
        }
    }
}