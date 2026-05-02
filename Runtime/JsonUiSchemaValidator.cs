#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;

namespace DingoJsonUI
{
    public enum JsonUiSchemaDiagnosticSeverity
    {
        Info,
        Warning,
        Error,
    }

    public readonly struct JsonUiSchemaDiagnostic
    {
        public JsonUiSchemaDiagnosticSeverity Severity { get; }
        public string SchemaPath { get; }
        public string Message { get; }

        public JsonUiSchemaDiagnostic(JsonUiSchemaDiagnosticSeverity severity, string schemaPath, string message)
        {
            Severity = severity;
            SchemaPath = schemaPath ?? "$";
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            return $"{Severity} {SchemaPath}: {Message}";
        }
    }

    public sealed class JsonUiSchemaValidator
    {
        private static readonly HashSet<string> KnownTypes = new(StringComparer.Ordinal)
        {
            JsonUiNodeType.Section,
            JsonUiNodeType.Foldout,
            JsonUiNodeType.Row,
            JsonUiNodeType.Columns,
            JsonUiNodeType.Tabs,
            JsonUiNodeType.Text,
            JsonUiNodeType.Field,
            JsonUiNodeType.InputText,
            JsonUiNodeType.Integer,
            JsonUiNodeType.Float,
            JsonUiNodeType.Toggle,
            JsonUiNodeType.SliderInt,
            JsonUiNodeType.SliderFloat,
            JsonUiNodeType.Select,
            JsonUiNodeType.Button,
            JsonUiNodeType.Progress,
            JsonUiNodeType.Separator,
            JsonUiNodeType.Space,
        };

        private static readonly HashSet<string> PathRequiredTypes = new(StringComparer.Ordinal)
        {
            JsonUiNodeType.Field,
            JsonUiNodeType.InputText,
            JsonUiNodeType.Integer,
            JsonUiNodeType.Float,
            JsonUiNodeType.Toggle,
            JsonUiNodeType.SliderInt,
            JsonUiNodeType.SliderFloat,
            JsonUiNodeType.Select,
            JsonUiNodeType.Progress,
        };

        public IReadOnlyList<JsonUiSchemaDiagnostic> Validate(JsonUiSchema schema, JsonUiCommandRegistry commands = null)
        {
            var diagnostics = new List<JsonUiSchemaDiagnostic>();
            if (schema == null)
            {
                diagnostics.Add(Error("$", "Schema is null."));
                return diagnostics;
            }

            if (schema.Root == null)
            {
                diagnostics.Add(Error("$.root", "Schema root is null."));
                return diagnostics;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            ValidateNode(schema.Root, "$.root", commands, diagnostics, ids);
            return diagnostics;
        }

        public bool IsValid(JsonUiSchema schema, JsonUiCommandRegistry commands = null)
        {
            var diagnostics = Validate(schema, commands);
            for (var i = 0; i < diagnostics.Count; i++)
            {
                if (diagnostics[i].Severity == JsonUiSchemaDiagnosticSeverity.Error)
                    return false;
            }

            return true;
        }

        private static void ValidateNode(JsonUiNode node, string schemaPath, JsonUiCommandRegistry commands, ICollection<JsonUiSchemaDiagnostic> diagnostics, ISet<string> ids)
        {
            if (node == null)
            {
                diagnostics.Add(Error(schemaPath, "Node is null."));
                return;
            }

            var type = NormalizeType(node.Type);
            if (!KnownTypes.Contains(type))
                diagnostics.Add(Error(schemaPath, $"Unknown widget type '{node.Type}'."));

            if (!string.IsNullOrWhiteSpace(node.Id) && !ids.Add(node.Id))
                diagnostics.Add(Warning(schemaPath, $"Duplicate node id '{node.Id}'."));

            ValidateNodePath(node, type, schemaPath, diagnostics);
            ValidateConditions(node, schemaPath, diagnostics);
            ValidateTypeSpecificRules(node, type, schemaPath, commands, diagnostics);

            var children = node.SafeChildren;
            for (var i = 0; i < children.Count; i++)
                ValidateNode(children[i], $"{schemaPath}.children[{i}]", commands, diagnostics, ids);
        }

        private static void ValidateNodePath(JsonUiNode node, string type, string schemaPath, ICollection<JsonUiSchemaDiagnostic> diagnostics)
        {
            if (PathRequiredTypes.Contains(type) && string.IsNullOrWhiteSpace(node.Path))
            {
                diagnostics.Add(Error(schemaPath, $"Widget type '{type}' requires 'path'."));
                return;
            }

            if (!string.IsNullOrWhiteSpace(node.Path))
                ValidateJsonPath(node.Path, schemaPath, "path", diagnostics);
        }

        private static void ValidateConditions(JsonUiNode node, string schemaPath, ICollection<JsonUiSchemaDiagnostic> diagnostics)
        {
            ValidateCondition(node.VisibleWhen, $"{schemaPath}.visibleWhen", diagnostics);
            ValidateCondition(node.EnabledWhen, $"{schemaPath}.enabledWhen", diagnostics);
        }

        private static void ValidateCondition(JsonUiCondition condition, string schemaPath, ICollection<JsonUiSchemaDiagnostic> diagnostics)
        {
            if (condition == null)
                return;

            if (string.IsNullOrWhiteSpace(condition.Path))
            {
                diagnostics.Add(Error(schemaPath, "Condition requires 'path'."));
                return;
            }

            ValidateJsonPath(condition.Path, schemaPath, "path", diagnostics);
        }

        private static void ValidateTypeSpecificRules(JsonUiNode node, string type, string schemaPath, JsonUiCommandRegistry commands, ICollection<JsonUiSchemaDiagnostic> diagnostics)
        {
            if (type == JsonUiNodeType.Button)
            {
                if (string.IsNullOrWhiteSpace(node.Action))
                    diagnostics.Add(Error(schemaPath, "Button requires 'action'."));
                else if (commands != null && !commands.TryGet(node.Action, out _))
                    diagnostics.Add(Error(schemaPath, $"Unknown action id '{node.Action}'."));
            }

            if (type == JsonUiNodeType.Select && node.SafeOptions.Count == 0)
                diagnostics.Add(Warning(schemaPath, "Select has no options."));

            if ((type == JsonUiNodeType.SliderInt || type == JsonUiNodeType.SliderFloat || type == JsonUiNodeType.Progress)
                && node.Min.HasValue
                && node.Max.HasValue
                && node.Min.Value > node.Max.Value)
            {
                diagnostics.Add(Error(schemaPath, "'min' cannot be greater than 'max'."));
            }

            if (type == JsonUiNodeType.Columns && node.Columns.HasValue && node.Columns.Value <= 0)
                diagnostics.Add(Error(schemaPath, "'columns' must be greater than zero."));
        }

        private static void ValidateJsonPath(string path, string schemaPath, string propertyName, ICollection<JsonUiSchemaDiagnostic> diagnostics)
        {
            try
            {
                JsonPath.Normalize(path);
            }
            catch (Exception e)
            {
                diagnostics.Add(Error(schemaPath, $"Invalid '{propertyName}' JSONPath '{path}': {e.Message}"));
            }
        }

        private static string NormalizeType(string type)
        {
            return string.IsNullOrWhiteSpace(type) ? JsonUiNodeType.Section : type.Trim();
        }

        private static JsonUiSchemaDiagnostic Error(string schemaPath, string message)
        {
            return new JsonUiSchemaDiagnostic(JsonUiSchemaDiagnosticSeverity.Error, schemaPath, message);
        }

        private static JsonUiSchemaDiagnostic Warning(string schemaPath, string message)
        {
            return new JsonUiSchemaDiagnostic(JsonUiSchemaDiagnosticSeverity.Warning, schemaPath, message);
        }
    }
}
#endif
