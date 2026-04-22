#if NEWTONSOFT_EXISTS
using System.Collections.Generic;

namespace DingoJsonUI
{
    internal sealed class JsonPathPattern
    {
        private readonly IReadOnlyList<JsonPathSegment> _segments;

        public string Pattern { get; }

        public JsonPathPattern(string pattern)
        {
            Pattern = JsonPath.Normalize(pattern);
            _segments = JsonPath.ParseSegments(Pattern);
        }

        public bool IsMatch(string candidatePath)
        {
            var candidateSegments = JsonPath.ParseSegments(JsonPath.Normalize(candidatePath));
            if (_segments.Count != candidateSegments.Count)
                return false;

            for (var i = 0; i < _segments.Count; i++)
            {
                var expected = _segments[i];
                var actual = candidateSegments[i];

                if (expected.Kind == JsonPathSegmentKind.AnyProperty && actual.Kind == JsonPathSegmentKind.Property)
                    continue;

                if (expected.Kind == JsonPathSegmentKind.AnyIndex && actual.Kind == JsonPathSegmentKind.Index)
                    continue;

                if (!expected.Equals(actual))
                    return false;
            }

            return true;
        }
    }
}
#endif
