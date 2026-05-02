#if NEWTONSOFT_EXISTS
using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DingoJsonUI.GUI
{
    public sealed class UImGuiJsonEditorBehaviour : MonoBehaviour
    {
        [SerializeField]
        private TextAsset _jsonAsset;

        [SerializeField]
        [TextArea(8, 24)]
        private string _inlineJson = "{\n  \"player\": {\n    \"name\": \"Dingo\",\n    \"hp\": 100,\n    \"speed\": 6.5,\n    \"alive\": true\n  },\n  \"inventory\": [\n    \"key\",\n    \"map\"\n  ]\n}";

        [SerializeField]
        private string _windowTitle = "Dingo JSON UI";

        [SerializeField]
        private bool _reloadSourceOnEnable = true;

        [SerializeField]
        [Min(0f)]
        private float _scrollWheelPixelsPerStep = 280f;

        [SerializeField]
        private bool _enableLargeDataPaging = true;

        [SerializeField]
        [Min(1)]
        private int _maxVisibleChildrenPerNode = JsonUiLargeData.DefaultMaxVisibleChildrenPerNode;

        [SerializeField]
        [Min(0)]
        private int _maxRenderDepth = JsonUiLargeData.DefaultMaxRenderDepth;

        private JsonDocumentModel _document;
        private UImGuiJsonEditor _editor;
        private bool _isQuitting;

        public JsonDocumentModel Document => _document;
        public UImGuiJsonEditor Editor
        {
            get
            {
                EnsureEditorCreated();
                return _editor;
            }
        }

        public JsonUiActionCollection Actions
        {
            get
            {
                EnsureEditorCreated();
                return _editor.Actions;
            }
        }

        private void Awake()
        {
            EnsureEditorCreated();
        }

        private void OnEnable()
        {
            EnsureEditorCreated();

            if (_reloadSourceOnEnable)
                ReloadSerializedSource();

            UImGui.UImGuiUtility.Layout += OnLayout;
        }

        private void OnDisable()
        {
            UImGui.UImGuiUtility.Layout -= OnLayout;
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        [ContextMenu("Reload Serialized Source")]
        public void ReloadSerializedSource()
        {
            EnsureEditorCreated();

            var source = _jsonAsset != null ? _jsonAsset.text : _inlineJson;
            try
            {
                _document.LoadJson(source, notifyRoot: false);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
                _document.LoadToken(new JObject(), notifyRoot: false);
            }
        }

        public void LoadJson(string json)
        {
            EnsureEditorCreated();
            _document.LoadJson(json);
        }

        public void LoadToken(JToken token)
        {
            EnsureEditorCreated();
            _document.LoadToken(token);
        }

        public bool SetValue<T>(string path, T value)
        {
            EnsureEditorCreated();
            return _document.SetValue(path, value);
        }

        public JsonPathSubscription Subscribe(string path, Action<JsonChange> callback, bool fireImmediately = false)
        {
            EnsureEditorCreated();
            return _document.Subscribe(path, callback, fireImmediately);
        }

        public JsonUiAction AddButton(string path, string label, Action<JsonUiActionContext> callback, JsonUiActionPlacement placement = JsonUiActionPlacement.Inline)
        {
            EnsureEditorCreated();
            return _editor.Actions.AddButton(path, label, callback, placement);
        }

        private void EnsureEditorCreated()
        {
            _document ??= new JsonDocumentModel();
            _editor ??= UImGuiJsonUi.Editor(_document, _windowTitle, CreateOptions());
            _editor.WindowTitle = _windowTitle;
            UImGuiJsonUi.ApplyOptions(_editor, CreateOptions());
        }

        private JsonUiOptions CreateOptions()
        {
            return new JsonUiOptions
            {
                WindowTitle = _windowTitle,
                ScrollWheelPixelsPerStep = _scrollWheelPixelsPerStep,
                EnableLargeDataPaging = _enableLargeDataPaging,
                MaxVisibleChildrenPerNode = Math.Max(1, _maxVisibleChildrenPerNode),
                MaxRenderDepth = Math.Max(0, _maxRenderDepth),
            };
        }

        private void OnLayout(UImGui.UImGui uImGui)
        {
            if (_isQuitting)
                return;

            EnsureEditorCreated();
            _editor.Draw();
        }
    }
}
#endif
