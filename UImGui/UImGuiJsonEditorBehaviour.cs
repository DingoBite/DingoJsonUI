#if NEWTONSOFT_EXISTS
using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DingoJsonUI.UImGui
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

        private JsonDocumentModel _document;
        private UImGuiJsonEditor _editor;
        private bool _isQuitting;

        public JsonDocumentModel Document => _document;

        private void Awake()
        {
            EnsureEditorCreated();
        }

        private void OnEnable()
        {
            EnsureEditorCreated();

            if (_reloadSourceOnEnable)
                ReloadSerializedSource();

            global::UImGui.UImGuiUtility.Layout += OnLayout;
        }

        private void OnDisable()
        {
            global::UImGui.UImGuiUtility.Layout -= OnLayout;
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

        private void EnsureEditorCreated()
        {
            _document ??= new JsonDocumentModel();
            _editor ??= new UImGuiJsonEditor(_document, _windowTitle);
            _editor.WindowTitle = _windowTitle;
        }

        private void OnLayout(global::UImGui.UImGui uImGui)
        {
            if (_isQuitting)
                return;

            EnsureEditorCreated();
            _editor.Draw();
        }
    }
}
#endif
