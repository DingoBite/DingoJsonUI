#if NEWTONSOFT_EXISTS
using System;

namespace DingoJsonUI
{
    public readonly struct JsonUiVisibleRange
    {
        public JsonUiVisibleRange(int totalCount, int offset, int count, bool isPaged)
        {
            TotalCount = totalCount;
            Offset = offset;
            Count = count;
            IsPaged = isPaged;
        }

        public int TotalCount { get; }
        public int Offset { get; }
        public int Count { get; }
        public int EndExclusive => Offset + Count;
        public bool IsPaged { get; }
        public bool HasPrevious => IsPaged && Offset > 0;
        public bool HasNext => IsPaged && EndExclusive < TotalCount;
    }

    public static class JsonUiLargeData
    {
        public const int DefaultMaxVisibleChildrenPerNode = 128;
        public const int DefaultMaxRenderDepth = 64;

        public static JsonUiVisibleRange CalculateVisibleRange(int totalCount, int requestedOffset, int maxVisibleChildren)
        {
            if (totalCount <= 0)
                return new JsonUiVisibleRange(0, 0, 0, false);

            if (maxVisibleChildren <= 0 || totalCount <= maxVisibleChildren)
                return new JsonUiVisibleRange(totalCount, 0, totalCount, false);

            var pageSize = Math.Max(1, maxVisibleChildren);
            var offset = Math.Max(0, Math.Min(requestedOffset, totalCount - 1));
            offset = offset / pageSize * pageSize;

            return new JsonUiVisibleRange(
                totalCount,
                offset,
                Math.Min(pageSize, totalCount - offset),
                true);
        }

        public static int GetPreviousPageOffset(JsonUiVisibleRange range, int maxVisibleChildren)
        {
            var pageSize = Math.Max(1, maxVisibleChildren);
            return Math.Max(0, range.Offset - pageSize);
        }

        public static int GetNextPageOffset(JsonUiVisibleRange range, int maxVisibleChildren)
        {
            var pageSize = Math.Max(1, maxVisibleChildren);
            return Math.Min(Math.Max(0, range.TotalCount - 1), range.Offset + pageSize);
        }

        public static int GetLastPageOffset(int totalCount, int maxVisibleChildren)
        {
            if (totalCount <= 0)
                return 0;

            var pageSize = Math.Max(1, maxVisibleChildren);
            return (totalCount - 1) / pageSize * pageSize;
        }
    }
}
#endif
