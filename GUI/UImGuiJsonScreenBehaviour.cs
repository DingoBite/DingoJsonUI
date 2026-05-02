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
        [Min(0f)]
        private float _scrollWheelPixelsPerStep = 280f;

        private readonly List<JsonUiSchemaDiagnostic> _diagnostics = new();
        private JsonDocumentModel _document;
        private JsonUiSchema _schema;
        private JsonUiCommandRegistry _commands;
        private UImGuiJsonScreen _screen;
        private bool _isQuitting;

        public JsonDocumentModel Document
        {
            get
            {
                EnsureCreated();
                return _document;
            }
        }

        public JsonUiSchema Schema
        {
            get
            {
                EnsureCreated();
                return _schema;
            }
        }

        public JsonUiCommandRegistry Commands
        {
            get
            {
                EnsureCreated();
                return _commands;
            }
        }

        public IReadOnlyList<JsonUiSchemaDiagnostic> Diagnostics => _diagnostics;
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

            try
            {
                _document.LoadJson(GetJsonSource());
                return true;
            }
            catch (Exception e)
            {
                AddDiagnostic(JsonUiSchemaDiagnosticSeverity.Error, "$.json", $"JSON reload failed: {e.Message}");
                return false;
            }
        }

        [ContextMenu("Reload Schema")]
        public bool ReloadSchema()
        {
            EnsureCreated();

            JsonUiSchema nextSchema;
            try
            {
                nextSchema = JsonUiSchema.FromJson(GetSchemaSource());
            }
            catch (Exception e)
            {
                ReplaceDiagnostics(new[]
                {
                    new JsonUiSchemaDiagnostic(JsonUiSchemaDiagnosticSeverity.Error, "$.schema", $"Schema parse failed: {e.Message}"),
                });
                return false;
            }

            var diagnostics = new JsonUiSchemaValidator().Validate(nextSchema, _commands);
            ReplaceDiagnostics(diagnostics);

            if (HasErrors(diagnostics))
                return false;

            _schema = nextSchema;
            RebuildScreen();
            return true;
        }

        public JsonUiCommand RegisterCommand(string id, Action<JsonUiCommandContext> callback)
        {
            EnsureCreated();
            var command = _commands.Register(id, callback);
            ValidateCurrentSchema();
            return command;
        }

        public void ValidateCurrentSchema()
        {
            EnsureCreated();
            ReplaceDiagnostics(new JsonUiSchemaValidator().Validate(_schema, _commands));
        }

        private void OnLayout(UImGui.UImGui uImGui)
        {
            if (_isQuitting)
                return;

            EnsureCreated();
            _screen.WindowTitle = _windowTitle;
            _screen.ScrollWheelPixelsPerStep = _scrollWheelPixelsPerStep;
            _screen.Draw();

            if (_showDiagnostics)
                DrawDiagnosticsWindow();
        }

        private void EnsureCreated()
        {
            _document ??= new JsonDocumentModel();
            _commands ??= new JsonUiCommandRegistry();
            _schema ??= JsonUiSchema.FromJson(DefaultSchema);

            if (_screen == null)
                RebuildScreen();
        }

        private void RebuildScreen()
        {
            _screen = new UImGuiJsonScreen(_document, _schema, _commands, _windowTitle)
            {
                ScrollWheelPixelsPerStep = _scrollWheelPixelsPerStep,
            };
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
            if (_diagnostics.Count == 0)
                return;

            if (!ImGui.Begin($"{_windowTitle} Diagnostics"))
            {
                ImGui.End();
                return;
            }

            for (var i = 0; i < _diagnostics.Count; i++)
            {
                var diagnostic = _diagnostics[i];
                ImGui.TextColored(GetColor(diagnostic.Severity), diagnostic.ToString());
            }

            ImGui.End();
        }

        private void ReplaceDiagnostics(IEnumerable<JsonUiSchemaDiagnostic> diagnostics)
        {
            _diagnostics.Clear();
            if (diagnostics == null)
                return;

            foreach (var diagnostic in diagnostics)
                _diagnostics.Add(diagnostic);
        }

        private void AddDiagnostic(JsonUiSchemaDiagnosticSeverity severity, string schemaPath, string message)
        {
            _diagnostics.Add(new JsonUiSchemaDiagnostic(severity, schemaPath, message));
        }

        private static bool HasErrors(IReadOnlyList<JsonUiSchemaDiagnostic> diagnostics)
        {
            if (diagnostics == null)
                return false;

            for (var i = 0; i < diagnostics.Count; i++)
            {
                if (diagnostics[i].Severity == JsonUiSchemaDiagnosticSeverity.Error)
                    return true;
            }

            return false;
        }

        private static System.Numerics.Vector4 GetColor(JsonUiSchemaDiagnosticSeverity severity)
        {
            return severity switch
            {
                JsonUiSchemaDiagnosticSeverity.Error => new System.Numerics.Vector4(1f, 0.35f, 0.25f, 1f),
                JsonUiSchemaDiagnosticSeverity.Warning => new System.Numerics.Vector4(1f, 0.75f, 0.25f, 1f),
                _ => new System.Numerics.Vector4(0.65f, 0.8f, 1f, 1f),
            };
        }
    }
}
#endif
