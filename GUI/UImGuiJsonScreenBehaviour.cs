#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using ImGuiNET;
using UnityEngine;

namespace DingoJsonUI.GUI
{
    public sealed class UImGuiJsonScreenBehaviour : MonoBehaviour
    {
        private const string DefaultJson = "{\n  \"title\": \"Dingo\",\n  \"enabled\": true\n}";
        private const string DefaultSchema = "{\n  \"title\": \"Dingo Fast UI\",\n  \"root\": {\n    \"type\": \"section\",\n    \"children\": [\n      { \"type\": \"inputText\", \"label\": \"Title\", \"path\": \"$.title\" },\n      { \"type\": \"toggle\", \"label\": \"Enabled\", \"path\": \"$.enabled\" }\n    ]\n  }\n}";

        [SerializeField]
        private TextAsset _jsonAsset;

        [SerializeField]
        [TextArea(6, 20)]
        private string _inlineJson = DefaultJson;

        [SerializeField]
        private TextAsset _schemaAsset;

        [SerializeField]
        [TextArea(10, 32)]
        private string _inlineSchema = DefaultSchema;

        [SerializeField]
        private string _windowTitle = "Dingo Fast UI";

        [SerializeField]
        private bool _reloadSourcesOnEnable = true;

        [SerializeField]
        private bool _showDiagnostics = true;

        [SerializeField]
        private bool _showDiagnosticsWhenValid;

        [SerializeField]
        private bool _registerDefaultPayloadCommands = true;

        [SerializeField]
        [Min(0f)]
        private float _scrollWheelPixelsPerStep = 280f;

        [SerializeField]
        private bool _applyInitialWindowPlacement;

        [SerializeField]
        private Vector2 _initialWindowPosition = new(60f, 60f);

        [SerializeField]
        private Vector2 _initialWindowSize = new(430f, 260f);

        private JsonUiSession _session;
        private UImGuiJsonScreen _screen;
        private UImGuiJsonSchemaDiagnosticsWindow _diagnosticsWindow;
        private bool _isQuitting;

        public JsonDocumentModel Document
        {
            get
            {
                EnsureCreated();
                return _session.Document;
            }
        }

        public JsonUiSchema Schema
        {
            get
            {
                EnsureCreated();
                return _session.Schema;
            }
        }

        public JsonUiCommandRegistry Commands
        {
            get
            {
                EnsureCreated();
                return _session.Commands;
            }
        }

        public JsonUiSession Session
        {
            get
            {
                EnsureCreated();
                return _session;
            }
        }

        public IReadOnlyList<JsonUiSchemaDiagnostic> Diagnostics => Session.Diagnostics;

        public bool ShowDiagnostics
        {
            get => _showDiagnostics;
            set => _showDiagnostics = value;
        }

        public bool ShowDiagnosticsWhenValid
        {
            get => _showDiagnosticsWhenValid;
            set => _showDiagnosticsWhenValid = value;
        }

        public bool RegisterDefaultPayloadCommandsOnCreate
        {
            get => _registerDefaultPayloadCommands;
            set => _registerDefaultPayloadCommands = value;
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => _windowTitle = string.IsNullOrWhiteSpace(value) ? "Dingo Fast UI" : value;
        }

        public float ScrollWheelPixelsPerStep
        {
            get => _scrollWheelPixelsPerStep;
            set => _scrollWheelPixelsPerStep = Math.Max(0f, value);
        }

        public bool ApplyInitialWindowPlacement
        {
            get => _applyInitialWindowPlacement;
            set => _applyInitialWindowPlacement = value;
        }

        public Vector2 InitialWindowPosition
        {
            get => _initialWindowPosition;
            set => _initialWindowPosition = value;
        }

        public Vector2 InitialWindowSize
        {
            get => _initialWindowSize;
            set => _initialWindowSize = value;
        }

        public UImGuiJsonScreen Screen
        {
            get
            {
                EnsureCreated();
                return _screen;
            }
        }

        private void Awake()
        {
            EnsureCreated();
        }

        private void OnEnable()
        {
            EnsureCreated();

            if (_reloadSourcesOnEnable)
                ReloadAll();

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

        [ContextMenu("Reload All")]
        public void ReloadAll()
        {
            ReloadJson();
            ReloadSchema();
        }

        [ContextMenu("Reload JSON")]
        public bool ReloadJson()
        {
            EnsureCreated();
            return LoadJson(GetJsonSource());
        }

        public bool LoadJson(string json)
        {
            EnsureCreated();
            return _session.LoadJson(json);
        }

        [ContextMenu("Reload Schema")]
        public bool ReloadSchema()
        {
            EnsureCreated();
            return LoadSchemaJson(GetSchemaSource());
        }

        public bool LoadSchemaJson(string json)
        {
            EnsureCreated();
            var previousSchema = _session.Schema;
            var result = _session.LoadSchemaJson(json);
            if (!ReferenceEquals(previousSchema, _session.Schema))
                RebuildScreen();

            return result;
        }

        public bool LoadSchema(JsonUiSchema schema)
        {
            EnsureCreated();
            var result = _session.LoadSchema(schema);
            if (schema != null)
                RebuildScreen();

            return result;
        }

        public JsonUiCommand RegisterCommand(string id, Action<JsonUiCommandContext> callback)
        {
            EnsureCreated();
            return _session.RegisterCommand(id, callback);
        }

        public void RegisterDefaultPayloadCommands(bool replaceExisting = true)
        {
            EnsureCreated();
            _session.RegisterDefaultPayloadCommands(replaceExisting);
        }

        public void ValidateCurrentSchema()
        {
            EnsureCreated();
            _session.ValidateCurrentSchema();
        }

        private void OnLayout(UImGui.UImGui uImGui)
        {
            if (_isQuitting)
                return;

            EnsureCreated();
            _screen.WindowTitle = _windowTitle;
            SyncOptions();
            UImGuiJsonUi.ApplyOptions(_screen, _session.Options);

            if (_applyInitialWindowPlacement)
            {
                ImGui.SetNextWindowPos(ToImGuiVector(_initialWindowPosition), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(ToImGuiVector(_initialWindowSize), ImGuiCond.FirstUseEver);
            }

            _screen.Draw();

            if (_showDiagnostics)
                DrawDiagnosticsWindow();
        }

        private void EnsureCreated()
        {
            if (_session == null)
            {
                _session = JsonUi.Session(schemaJson: DefaultSchema, options: CreateOptions());
            }

            _diagnosticsWindow ??= new UImGuiJsonSchemaDiagnosticsWindow();

            if (_screen == null)
                RebuildScreen();
        }

        private void RebuildScreen()
        {
            SyncOptions();
            _screen = UImGuiJsonUi.Screen(_session, _windowTitle);
        }

        private JsonUiOptions CreateOptions()
        {
            return new JsonUiOptions
            {
                WindowTitle = _windowTitle,
                RegisterDefaultPayloadCommands = _registerDefaultPayloadCommands,
                ScrollWheelPixelsPerStep = _scrollWheelPixelsPerStep,
            };
        }

        private void SyncOptions()
        {
            if (_session == null)
                return;

            _session.Options.WindowTitle = _windowTitle;
            _session.Options.RegisterDefaultPayloadCommands = _registerDefaultPayloadCommands;
            _session.Options.ScrollWheelPixelsPerStep = _scrollWheelPixelsPerStep;
            _session.Options.Sanitize();
        }

        private string GetJsonSource()
        {
            return _jsonAsset != null ? _jsonAsset.text : string.IsNullOrWhiteSpace(_inlineJson) ? DefaultJson : _inlineJson;
        }

        private string GetSchemaSource()
        {
            return _schemaAsset != null ? _schemaAsset.text : string.IsNullOrWhiteSpace(_inlineSchema) ? DefaultSchema : _inlineSchema;
        }

        private void DrawDiagnosticsWindow()
        {
            _diagnosticsWindow.WindowTitle = $"{_windowTitle} Validator";
            _diagnosticsWindow.Diagnostics = _session.Diagnostics;
            _diagnosticsWindow.DrawWhenValid = _showDiagnosticsWhenValid;
            _diagnosticsWindow.OnValidate = ValidateCurrentSchema;
            _diagnosticsWindow.OnReloadAll = ReloadAll;
            _diagnosticsWindow.Draw();
        }

        private static System.Numerics.Vector2 ToImGuiVector(Vector2 value)
        {
            return new System.Numerics.Vector2(value.x, value.y);
        }

    }
}
#endif
