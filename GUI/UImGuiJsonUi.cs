#if NEWTONSOFT_EXISTS
using System;

namespace DingoJsonUI.GUI
{
    public static class UImGuiJsonUi
    {
        public static UImGuiJsonScreen Screen(JsonUiSession session, string windowTitle = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var screen = new UImGuiJsonScreen(session.Document, session.Schema, session.Commands, windowTitle ?? session.Options.WindowTitle);
            ApplyOptions(screen, session.Options);
            return screen;
        }

        public static UImGuiJsonEditor Editor(JsonDocumentModel document, string windowTitle = null, JsonUiOptions options = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var safeOptions = options?.Clone() ?? new JsonUiOptions();
            safeOptions.Sanitize();

            var editor = new UImGuiJsonEditor(document, windowTitle ?? safeOptions.WindowTitle);
            ApplyOptions(editor, safeOptions);
            return editor;
        }

        public static void ApplyOptions(UImGuiJsonScreen screen, JsonUiOptions options)
        {
            if (screen == null || options == null)
                return;

            options.Sanitize();
            screen.ScrollWheelPixelsPerStep = options.ScrollWheelPixelsPerStep;
        }

        public static void ApplyOptions(UImGuiJsonEditor editor, JsonUiOptions options)
        {
            if (editor == null || options == null)
                return;

            options.Sanitize();
            editor.ScrollWheelPixelsPerStep = options.ScrollWheelPixelsPerStep;
            editor.EnableLargeDataPaging = options.EnableLargeDataPaging;
            editor.MaxVisibleChildrenPerNode = options.MaxVisibleChildrenPerNode;
            editor.MaxRenderDepth = options.MaxRenderDepth;
        }
    }
}
#endif
