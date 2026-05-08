#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI
{
    public sealed class JsonUiSchemaReport
    {
        private JsonUiSchemaReport(JsonUiSchema schema, JsonUiSchemaPreview preview, IReadOnlyList<JsonUiSchemaDiagnostic> diagnostics, bool parsed)
        {
            Schema = schema;
            Preview = preview ?? JsonUiSchemaPreview.Empty;
            Diagnostics = diagnostics == null
                ? Array.Empty<JsonUiSchemaDiagnostic>()
                : new List<JsonUiSchemaDiagnostic>(diagnostics).AsReadOnly();
            Parsed = parsed;

            CountDiagnostics(Diagnostics, out var errors, out var warnings, out var info);
            ErrorCount = errors;
            WarningCount = warnings;
            InfoCount = info;
        }

        public JsonUiSchema Schema { get; }
        public JsonUiSchemaPreview Preview { get; }
        public IReadOnlyList<JsonUiSchemaDiagnostic> Diagnostics { get; }
        public bool Parsed { get; }
        public bool IsValid => ErrorCount == 0;
        public bool HasErrors => ErrorCount > 0;
        public int ErrorCount { get; }
        public int WarningCount { get; }
        public int InfoCount { get; }

        public static JsonUiSchemaReport Create(JsonUiSchema schema, JsonUiCommandRegistry commands = null, IReadOnlyList<JsonUiSchemaDiagnostic> diagnostics = null)
        {
            var resolvedDiagnostics = diagnostics ?? new JsonUiSchemaValidator().Validate(schema, commands);
            return new JsonUiSchemaReport(schema, JsonUiSchemaPreview.Create(schema), resolvedDiagnostics, parsed: schema != null);
        }

        public static JsonUiSchemaReport FromJson(string schemaJson, JsonUiCommandRegistry commands = null)
        {
            try
            {
                return Create(JsonUiSchema.FromJson(schemaJson), commands);
            }
            catch (Exception e)
            {
                return FromParseError($"Schema parse failed: {e.Message}");
            }
        }

        public static JsonUiSchemaReport FromToken(JToken schemaToken, JsonUiCommandRegistry commands = null)
        {
            try
            {
                return FromJson(schemaToken == null || schemaToken.Type == JTokenType.Null
                    ? null
                    : schemaToken.ToString(Formatting.None), commands);
            }
            catch (Exception e)
            {
                return FromParseError($"Schema token parse failed: {e.Message}");
            }
        }

        public string ToText()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Schema: {Preview.Title ?? "<untitled>"}");
            builder.AppendLine($"Parsed: {Parsed}  Valid: {IsValid}  Errors: {ErrorCount}  Warnings: {WarningCount}  Info: {InfoCount}");
            builder.Append(Preview.ToText());

            if (Diagnostics.Count == 0)
                return builder.ToString();

            builder.AppendLine();
            builder.AppendLine("Diagnostics:");
            for (var i = 0; i < Diagnostics.Count; i++)
                builder.AppendLine(Diagnostics[i].ToString());

            return builder.ToString();
        }

        private static JsonUiSchemaReport FromParseError(string message)
        {
            var diagnostics = new[]
            {
                new JsonUiSchemaDiagnostic(JsonUiSchemaDiagnosticSeverity.Error, JsonUiSession.SchemaSourceDiagnosticPath, message),
            };

            return new JsonUiSchemaReport(null, JsonUiSchemaPreview.Empty, diagnostics, parsed: false);
        }

        private static void CountDiagnostics(IReadOnlyList<JsonUiSchemaDiagnostic> diagnostics, out int errors, out int warnings, out int info)
        {
            errors = 0;
            warnings = 0;
            info = 0;

            if (diagnostics == null)
                return;

            for (var i = 0; i < diagnostics.Count; i++)
            {
                switch (diagnostics[i].Severity)
                {
                    case JsonUiSchemaDiagnosticSeverity.Error:
                        errors++;
                        break;
                    case JsonUiSchemaDiagnosticSeverity.Warning:
                        warnings++;
                        break;
                    default:
                        info++;
                        break;
                }
            }
        }
    }

    public sealed class JsonUiSchemaPreview
    {
        private JsonUiSchemaPreview(
            string title,
            IReadOnlyList<JsonUiSchemaPreviewNode> nodes,
            IReadOnlyList<string> dataPaths,
            IReadOnlyList<string> actions,
            IReadOnlyList<string> templates,
            IReadOnlyDictionary<string, int> nodeTypeCounts,
            int maxDepth)
        {
            Title = title;
            Nodes = nodes ?? Array.Empty<JsonUiSchemaPreviewNode>();
            DataPaths = dataPaths ?? Array.Empty<string>();
            Actions = actions ?? Array.Empty<string>();
            Templates = templates ?? Array.Empty<string>();
            NodeTypeCounts = nodeTypeCounts ?? new Dictionary<string, int>(StringComparer.Ordinal);
            MaxDepth = maxDepth;
        }

        public static JsonUiSchemaPreview Empty { get; } = new(
            null,
            Array.Empty<JsonUiSchemaPreviewNode>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            new Dictionary<string, int>(StringComparer.Ordinal),
            0);

        public string Title { get; }
        public IReadOnlyList<JsonUiSchemaPreviewNode> Nodes { get; }
        public IReadOnlyList<string> DataPaths { get; }
        public IReadOnlyList<string> Actions { get; }
        public IReadOnlyList<string> Templates { get; }
        public IReadOnlyDictionary<string, int> NodeTypeCounts { get; }
        public int NodeCount => Nodes.Count;
        public int DataPathCount => DataPaths.Count;
        public int ActionCount => Actions.Count;
        public int TemplateCount => Templates.Count;
        public int MaxDepth { get; }

        public static JsonUiSchemaPreview Create(JsonUiSchema schema)
        {
            if (schema == null)
                return Empty;

            var builder = new Builder(schema);
            return builder.Build();
        }

        public string ToText()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Nodes: {NodeCount}  MaxDepth: {MaxDepth}  Paths: {DataPathCount}  Actions: {ActionCount}  Templates: {TemplateCount}");
            AppendList(builder, "Types", FormatCounts(NodeTypeCounts));
            AppendList(builder, "Paths", DataPaths);
            AppendList(builder, "Actions", Actions);
            AppendList(builder, "Templates", Templates);
            return builder.ToString();
        }

        private static void AppendList(StringBuilder builder, string label, IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
                return;

            builder.Append(label);
            builder.Append(": ");
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(values[i]);
            }

            builder.AppendLine();
        }

        private static IReadOnlyList<string> FormatCounts(IReadOnlyDictionary<string, int> counts)
        {
            var result = new List<string>();
            if (counts == null)
                return result;

            foreach (var pair in counts)
                result.Add($"{pair.Key}={pair.Value}");

            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private sealed class Builder
        {
            private readonly JsonUiSchema _schema;
            private readonly List<JsonUiSchemaPreviewNode> _nodes = new();
            private readonly HashSet<string> _dataPaths = new(StringComparer.Ordinal);
            private readonly HashSet<string> _actions = new(StringComparer.Ordinal);
            private readonly HashSet<string> _templates = new(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _nodeTypeCounts = new(StringComparer.Ordinal);
            private readonly HashSet<string> _templateStack = new(StringComparer.Ordinal);
            private int _maxDepth;

            public Builder(JsonUiSchema schema)
            {
                _schema = schema;
            }

            public JsonUiSchemaPreview Build()
            {
                if (_schema.Templates != null)
                {
                    foreach (var template in _schema.Templates)
                        AddValue(_templates, template.Key);
                }

                Visit(_schema.Root, "$.root", 0);

                return new JsonUiSchemaPreview(
                    _schema.Title,
                    _nodes.AsReadOnly(),
                    ToSortedList(_dataPaths),
                    ToSortedList(_actions),
                    ToSortedList(_templates),
                    new Dictionary<string, int>(_nodeTypeCounts, StringComparer.Ordinal),
                    _maxDepth);
            }

            private void Visit(JsonUiNode node, string schemaPath, int depth)
            {
                if (node == null)
                    return;

                var templateName = JsonUiSchema.GetTemplateName(node);
                var type = GetPreviewType(node);
                _maxDepth = Math.Max(_maxDepth, depth);
                Increment(type);
                CollectNodeReferences(node);

                _nodes.Add(new JsonUiSchemaPreviewNode(
                    schemaPath,
                    type,
                    node.Label,
                    node.Path,
                    node.Action,
                    templateName,
                    depth,
                    node.SafeChildren.Count,
                    JsonUiSchema.IsTemplateReference(node)));

                if (JsonUiSchema.IsTemplateReference(node))
                {
                    VisitTemplate(node, templateName, schemaPath, depth);
                    return;
                }

                var children = node.SafeChildren;
                for (var i = 0; i < children.Count; i++)
                    Visit(children[i], $"{schemaPath}.children[{i}]", depth + 1);
            }

            private void VisitTemplate(JsonUiNode node, string templateName, string schemaPath, int depth)
            {
                AddValue(_templates, templateName);
                if (string.IsNullOrWhiteSpace(templateName) || !_templateStack.Add(templateName))
                    return;

                try
                {
                    if (_schema.TryCreateTemplateInstance(node, out var instance))
                        Visit(instance, $"{schemaPath}<template:{templateName}>", depth + 1);
                }
                finally
                {
                    _templateStack.Remove(templateName);
                }
            }

            private void CollectNodeReferences(JsonUiNode node)
            {
                AddValue(_dataPaths, node.Path);
                AddValue(_dataPaths, node.ItemLabelPath);
                AddCondition(node.VisibleWhen);
                AddCondition(node.EnabledWhen);

                if (!string.IsNullOrWhiteSpace(node.Action))
                    AddValue(_actions, node.Action);

                if (node.Payload is JObject payload)
                {
                    AddPayloadPath(payload, "path");
                    AddPayloadPath(payload, "from");
                    AddPayloadPath(payload, "to");
                }
            }

            private void AddCondition(JsonUiCondition condition)
            {
                if (condition == null)
                    return;

                AddValue(_dataPaths, condition.Path);
            }

            private void AddPayloadPath(JObject payload, string property)
            {
                if (payload.TryGetValue(property, out var token) && token.Type == JTokenType.String)
                    AddValue(_dataPaths, token.Value<string>());
            }

            private void Increment(string type)
            {
                if (string.IsNullOrWhiteSpace(type))
                    type = JsonUiNodeType.Section;

                _nodeTypeCounts.TryGetValue(type, out var count);
                _nodeTypeCounts[type] = count + 1;
            }

            private static string GetPreviewType(JsonUiNode node)
            {
                var normalized = JsonUiSchema.NormalizeType(node?.Type);
                if (node == null || !JsonUiSchema.IsTemplateReference(node))
                    return normalized;

                return normalized == JsonUiNodeType.Section ? JsonUiNodeType.Include : normalized;
            }

            private static void AddValue(ISet<string> values, string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    values.Add(value);
            }

            private static List<string> ToSortedList(HashSet<string> values)
            {
                var result = new List<string>(values);
                result.Sort(StringComparer.Ordinal);
                return result;
            }
        }
    }

    public sealed class JsonUiSchemaPreviewNode
    {
        public JsonUiSchemaPreviewNode(
            string schemaPath,
            string type,
            string label,
            string path,
            string action,
            string template,
            int depth,
            int childCount,
            bool isTemplateReference)
        {
            SchemaPath = schemaPath ?? "$";
            Type = type ?? JsonUiNodeType.Section;
            Label = label;
            Path = path;
            Action = action;
            Template = template;
            Depth = depth;
            ChildCount = childCount;
            IsTemplateReference = isTemplateReference;
        }

        public string SchemaPath { get; }
        public string Type { get; }
        public string Label { get; }
        public string Path { get; }
        public string Action { get; }
        public string Template { get; }
        public int Depth { get; }
        public int ChildCount { get; }
        public bool IsTemplateReference { get; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(SchemaPath);
            builder.Append(" ");
            builder.Append(Type);

            if (!string.IsNullOrWhiteSpace(Label))
                builder.Append($" \"{Label}\"");

            if (!string.IsNullOrWhiteSpace(Path))
                builder.Append($" path={Path}");

            if (!string.IsNullOrWhiteSpace(Action))
                builder.Append($" action={Action}");

            if (!string.IsNullOrWhiteSpace(Template))
                builder.Append($" template={Template}");

            return builder.ToString();
        }
    }
}
#endif
