#if NEWTONSOFT_EXISTS
using System;
using ImGuiNET;
using UnityEngine;

namespace DingoJsonUI.GUI
{
    public sealed class UImGuiJsonSerializedObjectInspector<T> : IDisposable where T : class
    {
        private readonly JsonSerializedObject<T> _serializedObject;
        private readonly UImGuiJsonEditor _editor;
        private readonly JsonPathSubscription _dirtySubscription;
        private bool _dirty;

        public JsonSerializedObject<T> SerializedObject => _serializedObject;
        public JsonDocumentModel Document => _serializedObject.Document;
        public JsonUiActionCollection Actions => _editor.Actions;
        public string WindowTitle
        {
            get => _editor.WindowTitle;
            set => _editor.WindowTitle = value;
        }

        public bool AutoApplyOnChange { get; set; } = true;
        public Exception LastApplyException { get; private set; }
        public float ScrollWheelPixelsPerStep
        {
            get => _editor.ScrollWheelPixelsPerStep;
            set => _editor.ScrollWheelPixelsPerStep = value;
        }

        public UImGuiJsonSerializedObjectInspector(JsonSerializedObject<T> serializedObject, string windowTitle = null)
        {
            _serializedObject = serializedObject ?? throw new ArgumentNullException(nameof(serializedObject));
            _editor = new UImGuiJsonEditor(_serializedObject.Document, windowTitle ?? typeof(T).Name);
            _dirtySubscription = _serializedObject.Document.Subscribe(JsonPath.Root, _ => _dirty = true);

            _editor.Actions.AddButton(JsonPath.Root, "Reload", _ => ReloadFromTarget(), JsonUiActionPlacement.Toolbar);
            _editor.Actions.AddButton(JsonPath.Root, "Apply", _ => ApplyDocumentToTarget(), JsonUiActionPlacement.Toolbar);
        }

        public void Draw()
        {
            _editor.Draw();

            if (AutoApplyOnChange && _dirty)
                ApplyDocumentToTarget();
        }

        public void DrawInside()
        {
            _editor.DrawInside();

            if (AutoApplyOnChange && _dirty)
                ApplyDocumentToTarget();
        }

        public void ReloadFromTarget(bool notifyRoot = true)
        {
            _dirty = false;
            _serializedObject.ReloadFromTarget(notifyRoot);
        }

        public bool ApplyDocumentToTarget()
        {
            _dirty = false;
            var applied = _serializedObject.TryApplyDocumentToTarget(out var exception);
            LastApplyException = exception;

            if (!applied)
                Debug.LogException(exception);

            return applied;
        }

        public void Dispose()
        {
            _dirtySubscription?.Dispose();
        }
    }
}
#endif
