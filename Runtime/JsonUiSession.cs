#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI
{
    public sealed class JsonUiSession
    {
        public const string JsonSourceDiagnosticPath = "$.json";
        public const string SchemaSourceDiagnosticPath = "$.schema";

        private readonly List<JsonUiSchemaDiagnostic> _diagnostics = new();

        public JsonUiSession(JsonDocumentModel document = null, JsonUiSchema schema = null, JsonUiCommandRegistry commands = null, JsonUiOptions options = null)
        {
            Options = options?.Clone() ?? new JsonUiOptions();
            Options.Sanitize();

            Document = document ?? new JsonDocumentModel();
            Commands = commands ?? new JsonUiCommandRegistry();
            Schema = schema ?? new JsonUiSchema();

            if (Options.RegisterDefaultPayloadCommands)
                RegisterDefaultPayloadCommands();

            ValidateCurrentSchema();
        }

        public JsonDocumentModel Document { get; }
        public JsonUiSchema Schema { get; private set; }
        public JsonUiCommandRegistry Commands { get; }
        public JsonUiOptions Options { get; }
        public IReadOnlyList<JsonUiSchemaDiagnostic> Diagnostics => _diagnostics;

        public bool HasErrors => HasErrorDiagnostics(_diagnostics);

        public bool LoadJson(string json, bool notifyRoot = true)
        {
            try
            {
                Document.LoadJson(json, notifyRoot);
                RemoveDiagnostics(JsonSourceDiagnosticPath);
                return true;
            }
            catch (Exception e)
            {
                ReplaceJsonDiagnostic(new JsonUiSchemaDiagnostic(JsonUiSchemaDiagnosticSeverity.Error, JsonSourceDiagnosticPath, $"JSON reload failed: {e.Message}"));
                return false;
            }
        }

        public void LoadToken(JToken token, bool notifyRoot = true)
        {
            Document.LoadToken(token, notifyRoot);
            RemoveDiagnostics(JsonSourceDiagnosticPath);
        }

        public bool LoadSchemaJson(string json)
        {
            JsonUiSchema nextSchema;
            try
            {
                nextSchema = JsonUiSchema.FromJson(json);
            }
            catch (Exception e)
            {
                ReplaceSchemaDiagnostics(new[]
                {
                    new JsonUiSchemaDiagnostic(JsonUiSchemaDiagnosticSeverity.Error, SchemaSourceDiagnosticPath, $"Schema parse failed: {e.Message}"),
                });
                return false;
            }

            return LoadSchema(nextSchema);
        }

        public bool LoadSchemaToken(JToken token)
        {
            return LoadSchemaJson(token == null || token.Type == JTokenType.Null
                ? null
                : token.ToString(Newtonsoft.Json.Formatting.None));
        }

        public bool LoadSchema(JsonUiSchema schema)
        {
            if (schema == null)
            {
                ReplaceSchemaDiagnostics(new[]
                {
                    new JsonUiSchemaDiagnostic(JsonUiSchemaDiagnosticSeverity.Error, SchemaSourceDiagnosticPath, "Schema is null."),
                });
                return false;
            }

            Schema = schema;
            return ValidateCurrentSchema();
        }

        public bool ValidateCurrentSchema()
        {
            ReplaceSchemaDiagnostics(new JsonUiSchemaValidator().Validate(Schema, Commands));
            return !HasErrors;
        }

        public JsonUiCommand RegisterCommand(string id, Action<JsonUiCommandContext> callback)
        {
            var command = Commands.Register(id, callback);
            ValidateCurrentSchema();
            return command;
        }

        public void ConfigureCommands(Action<JsonUiCommandRegistry> configure)
        {
            configure?.Invoke(Commands);
            ValidateCurrentSchema();
        }

        public void RegisterDefaultPayloadCommands(bool replaceExisting = true)
        {
            JsonUiPayloadCommands.RegisterDefaults(Commands, replaceExisting);
            ValidateCurrentSchema();
        }

        public bool SetValue<T>(string path, T value)
        {
            return Document.SetValue(path, value);
        }

        public T GetValue<T>(string path, T defaultValue = default)
        {
            return Document.GetValue(path, defaultValue);
        }

        public JsonPathSubscription Subscribe(string path, Action<JsonChange> callback, bool fireImmediately = false)
        {
            return Document.Subscribe(path, callback, fireImmediately);
        }

        private void ReplaceSchemaDiagnostics(IEnumerable<JsonUiSchemaDiagnostic> diagnostics)
        {
            for (var i = _diagnostics.Count - 1; i >= 0; i--)
            {
                if (!IsJsonDiagnostic(_diagnostics[i]))
                    _diagnostics.RemoveAt(i);
            }

            if (diagnostics == null)
                return;

            foreach (var diagnostic in diagnostics)
                _diagnostics.Add(diagnostic);
        }

        private void ReplaceJsonDiagnostic(JsonUiSchemaDiagnostic diagnostic)
        {
            RemoveDiagnostics(JsonSourceDiagnosticPath);
            _diagnostics.Add(diagnostic);
        }

        private void RemoveDiagnostics(string schemaPath)
        {
            for (var i = _diagnostics.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_diagnostics[i].SchemaPath, schemaPath, StringComparison.Ordinal))
                    _diagnostics.RemoveAt(i);
            }
        }

        private static bool HasErrorDiagnostics(IReadOnlyList<JsonUiSchemaDiagnostic> diagnostics)
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

        private static bool IsJsonDiagnostic(JsonUiSchemaDiagnostic diagnostic)
        {
            return string.Equals(diagnostic.SchemaPath, JsonSourceDiagnosticPath, StringComparison.Ordinal);
        }
    }
}
#endif
