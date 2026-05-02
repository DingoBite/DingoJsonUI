#if NEWTONSOFT_EXISTS
using System;

namespace DingoJsonUI
{
    public sealed class JsonUiOptions
    {
        public string WindowTitle { get; set; } = JsonUiDefaults.WindowTitle;
        public bool RegisterDefaultPayloadCommands { get; set; } = true;
        public float ScrollWheelPixelsPerStep { get; set; } = JsonUiDefaults.ScrollWheelPixelsPerStep;
        public bool EnableLargeDataPaging { get; set; } = true;
        public int MaxVisibleChildrenPerNode { get; set; } = JsonUiLargeData.DefaultMaxVisibleChildrenPerNode;
        public int MaxRenderDepth { get; set; } = JsonUiLargeData.DefaultMaxRenderDepth;

        public JsonUiOptions Clone()
        {
            return new JsonUiOptions
            {
                WindowTitle = WindowTitle,
                RegisterDefaultPayloadCommands = RegisterDefaultPayloadCommands,
                ScrollWheelPixelsPerStep = ScrollWheelPixelsPerStep,
                EnableLargeDataPaging = EnableLargeDataPaging,
                MaxVisibleChildrenPerNode = MaxVisibleChildrenPerNode,
                MaxRenderDepth = MaxRenderDepth,
            };
        }

        public void Sanitize()
        {
            if (string.IsNullOrWhiteSpace(WindowTitle))
                WindowTitle = JsonUiDefaults.WindowTitle;

            ScrollWheelPixelsPerStep = Math.Max(0f, ScrollWheelPixelsPerStep);
            MaxVisibleChildrenPerNode = Math.Max(1, MaxVisibleChildrenPerNode);
            MaxRenderDepth = Math.Max(0, MaxRenderDepth);
        }
    }

    public static class JsonUiDefaults
    {
        public const string WindowTitle = "Dingo Fast UI";
        public const float ScrollWheelPixelsPerStep = 280f;
    }
}
#endif
