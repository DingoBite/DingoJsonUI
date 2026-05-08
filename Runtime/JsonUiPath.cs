#if NEWTONSOFT_EXISTS
using System;
using System.Globalization;

namespace DingoJsonUI
{
    public readonly struct JsonUiPath : IEquatable<JsonUiPath>
    {
        private readonly string _path;

        private JsonUiPath(string path, bool isRelative, bool normalize)
        {
            IsRelative = isRelative;
            _path = normalize
                ? isRelative
                    ? NormalizeRelative(path)
                    : JsonPath.Normalize(path)
                : path ?? string.Empty;
        }

        public bool IsRelative { get; }

        public static JsonUiPath Root => new(JsonPath.Root, false, false);
        public static JsonUiPath RelativeRoot => new(string.Empty, true, false);

        public static JsonUiPath From(string path)
        {
            return new JsonUiPath(path, false, true);
        }

        public static JsonUiPath Relative(string path)
        {
            return new JsonUiPath(path, true, true);
        }

        public JsonUiPath this[string propertyName] => Property(propertyName);
        public JsonUiPath this[int index] => Index(index);

        public JsonUiPath Property(string propertyName)
        {
            if (!IsRelative)
                return new JsonUiPath(JsonPath.BuildPropertyPath(ToString(), propertyName), false, false);

            return AppendSegment(FormatPropertySegment(propertyName));
        }

        public JsonUiPath Index(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative.");

            var segment = $"[{index.ToString(CultureInfo.InvariantCulture)}]";
            return !IsRelative
                ? new JsonUiPath(JsonPath.BuildIndexPath(ToString(), index), false, false)
                : AppendSegment(segment);
        }

        public JsonUiPath AnyProperty()
        {
            return AppendSegment(".*");
        }

        public JsonUiPath AnyIndex()
        {
            return AppendSegment("[*]");
        }

        public JsonUiPath Child(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return this;

            var relative = NormalizeRelative(relativePath);
            if (string.IsNullOrEmpty(relative))
                return this;

            return AppendRelativePath(relative);
        }

        public bool Equals(JsonUiPath other)
        {
            return IsRelative == other.IsRelative && string.Equals(ToString(), other.ToString(), StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is JsonUiPath other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((IsRelative ? 1 : 0) * 397) ^ StringComparer.Ordinal.GetHashCode(ToString());
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_path))
                return IsRelative ? string.Empty : JsonPath.Root;

            return _path;
        }

        public static bool operator ==(JsonUiPath left, JsonUiPath right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(JsonUiPath left, JsonUiPath right)
        {
            return !left.Equals(right);
        }

        public static implicit operator string(JsonUiPath path)
        {
            return path.ToString();
        }

        private JsonUiPath AppendSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment))
                return this;

            if (!IsRelative)
                return new JsonUiPath(ToString() + segment, false, false);

            if (string.IsNullOrEmpty(_path))
                return new JsonUiPath(RemoveLeadingDot(segment), true, false);

            return new JsonUiPath(_path + segment, true, false);
        }

        private JsonUiPath AppendRelativePath(string relativePath)
        {
            if (!IsRelative)
            {
                var separator = relativePath[0] == '[' ? string.Empty : ".";
                return From(ToString() + separator + relativePath);
            }

            if (string.IsNullOrEmpty(_path))
                return new JsonUiPath(relativePath, true, false);

            var relativeSeparator = relativePath[0] == '[' ? string.Empty : ".";
            return new JsonUiPath(_path + relativeSeparator + relativePath, true, false);
        }

        private static string NormalizeRelative(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var normalized = JsonPath.Normalize(path);
            if (normalized == JsonPath.Root)
                return string.Empty;

            return normalized.StartsWith(JsonPath.Root + ".", StringComparison.Ordinal)
                ? normalized.Substring(2)
                : normalized.Substring(1);
        }

        private static string RemoveLeadingDot(string segment)
        {
            return segment.Length > 0 && segment[0] == '.' ? segment.Substring(1) : segment;
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
