#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DingoJsonUI
{
    internal enum JsonPathSegmentKind
    {
        Property,
        Index,
        AnyProperty,
        AnyIndex,
    }

    internal readonly struct JsonPathSegment : IEquatable<JsonPathSegment>
    {
        public JsonPathSegmentKind Kind { get; }
        public string PropertyName { get; }
        public int Index { get; }

        private JsonPathSegment(JsonPathSegmentKind kind, string propertyName, int index)
        {
            Kind = kind;
            PropertyName = propertyName;
            Index = index;
        }

        public static JsonPathSegment Property(string name) => new(JsonPathSegmentKind.Property, name, -1);
        public static JsonPathSegment Indexer(int index) => new(JsonPathSegmentKind.Index, null, index);
        public static JsonPathSegment AnyProperty() => new(JsonPathSegmentKind.AnyProperty, null, -1);
        public static JsonPathSegment AnyIndex() => new(JsonPathSegmentKind.AnyIndex, null, -1);

        public bool Equals(JsonPathSegment other)
        {
            if (Kind != other.Kind)
                return false;

            return Kind switch
            {
                JsonPathSegmentKind.Property => string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal),
                JsonPathSegmentKind.Index => Index == other.Index,
                _ => true,
            };
        }
    }

    public static class JsonPath
    {
        public const string Root = "$";

        public static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Root;

            path = path.Trim();
            if (!path.StartsWith(Root, StringComparison.Ordinal))
            {
                path = path[0] switch
                {
                    '.' or '[' => Root + path,
                    _ => Root + "." + path,
                };
            }

            return BuildPath(ParseSegments(path));
        }

        public static string BuildPropertyPath(string parentPath, string propertyName)
        {
            var normalizedParent = Normalize(parentPath);
            if (normalizedParent == Root)
                return Root + FormatPropertySegment(propertyName);

            return normalizedParent + FormatPropertySegment(propertyName);
        }

        public static string BuildIndexPath(string parentPath, int index)
        {
            var normalizedParent = Normalize(parentPath);
            return $"{normalizedParent}[{index.ToString(CultureInfo.InvariantCulture)}]";
        }

        public static IReadOnlyList<string> GetAffectedPaths(string path)
        {
            var segments = ParseSegments(Normalize(path));
            var paths = new List<string>(segments.Count + 1) { Root };
            var builder = new StringBuilder(Root);

            for (var i = 0; i < segments.Count; i++)
            {
                builder.Append(FormatSegment(segments[i]));
                paths.Add(builder.ToString());
            }

            paths.Reverse();
            return paths;
        }

        internal static List<JsonPathSegment> ParseSegments(string path)
        {
            var normalized = path?.Trim();
            if (string.IsNullOrEmpty(normalized))
                return new List<JsonPathSegment>();

            if (!normalized.StartsWith(Root, StringComparison.Ordinal))
                throw new FormatException($"JSONPath must start with '{Root}': {path}");

            if (normalized.Length == 1)
                return new List<JsonPathSegment>();

            var segments = new List<JsonPathSegment>();
            var index = 1;

            while (index < normalized.Length)
            {
                switch (normalized[index])
                {
                    case '.':
                        index++;
                        ParsePropertySegment(normalized, ref index, segments);
                        break;
                    case '[':
                        index++;
                        ParseBracketSegment(normalized, ref index, segments);
                        break;
                    default:
                        throw new FormatException($"Unsupported JSONPath syntax near '{normalized[index]}' in '{path}'.");
                }
            }

            return segments;
        }

        private static void ParsePropertySegment(string path, ref int index, ICollection<JsonPathSegment> segments)
        {
            if (index >= path.Length)
                throw new FormatException($"Property name is missing in '{path}'.");

            if (path[index] == '*')
            {
                index++;
                segments.Add(JsonPathSegment.AnyProperty());
                return;
            }

            var start = index;
            while (index < path.Length && path[index] != '.' && path[index] != '[')
                index++;

            if (start == index)
                throw new FormatException($"Property name is missing in '{path}'.");

            segments.Add(JsonPathSegment.Property(path.Substring(start, index - start)));
        }

        private static void ParseBracketSegment(string path, ref int index, ICollection<JsonPathSegment> segments)
        {
            if (index >= path.Length)
                throw new FormatException($"Bracket segment is incomplete in '{path}'.");

            if (path[index] == '\'')
            {
                index++;
                var property = ParseQuotedProperty(path, ref index);
                segments.Add(JsonPathSegment.Property(property));
                return;
            }

            if (path[index] == '*')
            {
                index++;
                Expect(path, ref index, ']');
                segments.Add(JsonPathSegment.AnyIndex());
                return;
            }

            var start = index;
            while (index < path.Length && char.IsDigit(path[index]))
                index++;

            if (start == index)
                throw new FormatException($"Array index is missing in '{path}'.");

            var indexValue = int.Parse(path.Substring(start, index - start), CultureInfo.InvariantCulture);
            Expect(path, ref index, ']');
            segments.Add(JsonPathSegment.Indexer(indexValue));
        }

        private static string ParseQuotedProperty(string path, ref int index)
        {
            var builder = new StringBuilder();

            while (index < path.Length)
            {
                var current = path[index++];
                if (current == '\\')
                {
                    if (index >= path.Length)
                        throw new FormatException($"Invalid escape sequence in '{path}'.");

                    builder.Append(path[index++]);
                    continue;
                }

                if (current == '\'')
                {
                    Expect(path, ref index, ']');
                    return builder.ToString();
                }

                builder.Append(current);
            }

            throw new FormatException($"Quoted property is not closed in '{path}'.");
        }

        private static void Expect(string path, ref int index, char expected)
        {
            if (index >= path.Length || path[index] != expected)
                throw new FormatException($"Expected '{expected}' in '{path}'.");

            index++;
        }

        private static string BuildPath(IReadOnlyList<JsonPathSegment> segments)
        {
            var builder = new StringBuilder(Root);
            for (var i = 0; i < segments.Count; i++)
                builder.Append(FormatSegment(segments[i]));
            return builder.ToString();
        }

        private static string FormatSegment(JsonPathSegment segment)
        {
            return segment.Kind switch
            {
                JsonPathSegmentKind.Property => FormatPropertySegment(segment.PropertyName),
                JsonPathSegmentKind.Index => $"[{segment.Index.ToString(CultureInfo.InvariantCulture)}]",
                JsonPathSegmentKind.AnyProperty => ".*",
                JsonPathSegmentKind.AnyIndex => "[*]",
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private static string FormatPropertySegment(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return "['']";

            if (IsSimpleIdentifier(propertyName))
                return "." + propertyName;

            return "['" + propertyName.Replace("\\", "\\\\").Replace("'", "\\'") + "']";
        }

        private static bool IsSimpleIdentifier(string propertyName)
        {
            for (var i = 0; i < propertyName.Length; i++)
            {
                var ch = propertyName[i];
                var isLetter = ch is >= 'a' and <= 'z' || ch is >= 'A' and <= 'Z';
                var isDigit = ch is >= '0' and <= '9';
                if (!isLetter && !isDigit && ch != '_')
                    return false;

                if (i == 0 && isDigit)
                    return false;
            }

            return propertyName.Length > 0;
        }
    }
}
#endif
