#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI
{
    public static class JsonUiNodeType
    {
        public const string Section = "section";
        public const string Foldout = "foldout";
        public const string Row = "row";
        public const string Columns = "columns";
        public const string Tabs = "tabs";
        public const string Include = "include";
        public const string Text = "text";
        public const string Field = "field";
        public const string InputText = "inputText";
        public const string InputTextMultiline = "inputTextMultiline";
        public const string Integer = "int";
        public const string Float = "float";
        public const string DragInt = "dragInt";
        public const string DragFloat = "dragFloat";
        public const string Toggle = "toggle";
        public const string SliderInt = "sliderInt";
        public const string SliderFloat = "sliderFloat";
        public const string Vector2 = "vector2";
        public const string Vector3 = "vector3";
        public const string Color = "color";
        public const string Select = "select";
        public const string Radio = "radio";
        public const string Button = "button";
        public const string Progress = "progress";
        public const string Separator = "separator";
        public const string Space = "space";
    }

    public sealed class JsonUiSchema
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("root")]
        public JsonUiNode Root { get; set; } = new();

        [JsonProperty("templates")]
        public Dictionary<string, JsonUiNode> Templates { get; set; } = new(StringComparer.Ordinal);

        public static JsonUiSchema FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new JsonUiSchema();

            var token = JToken.Parse(json);
            if (token.Type != JTokenType.Object)
                throw new JsonSerializationException("JsonUiSchema root must be a JSON object.");

            var rootObject = (JObject)token;
            if (rootObject.Property("root") != null || rootObject.Property("title") != null)
                return rootObject.ToObject<JsonUiSchema>() ?? new JsonUiSchema();

            return new JsonUiSchema
            {
                Root = rootObject.ToObject<JsonUiNode>() ?? new JsonUiNode(),
            };
        }

        public static bool IsTemplateReference(JsonUiNode node)
        {
            if (node == null)
                return false;

            return !string.IsNullOrWhiteSpace(GetTemplateName(node))
                   || string.Equals(NormalizeType(node.Type), JsonUiNodeType.Include, StringComparison.Ordinal);
        }

        public static string GetTemplateName(JsonUiNode node)
        {
            if (node == null)
                return null;

            return !string.IsNullOrWhiteSpace(node.Template)
                ? node.Template
                : !string.IsNullOrWhiteSpace(node.Use)
                    ? node.Use
                    : null;
        }

        public bool TryCreateTemplateInstance(JsonUiNode includeNode, out JsonUiNode instance)
        {
            instance = null;
            var templateName = GetTemplateName(includeNode);
            if (string.IsNullOrWhiteSpace(templateName)
                || Templates == null
                || !Templates.TryGetValue(templateName, out var template)
                || template == null)
            {
                return false;
            }

            instance = CloneNode(template);
            ApplyTemplateOverrides(instance, includeNode);
            return true;
        }

        internal static string NormalizeType(string type)
        {
            return string.IsNullOrWhiteSpace(type) ? JsonUiNodeType.Section : type.Trim();
        }

        private static JsonUiNode CloneNode(JsonUiNode source)
        {
            if (source == null)
                return null;

            var clone = new JsonUiNode
            {
                Type = source.Type,
                Id = source.Id,
                Label = source.Label,
                Path = source.Path,
                Action = source.Action,
                Tooltip = source.Tooltip,
                Visible = source.Visible,
                Enabled = source.Enabled,
                VisibleWhen = CloneCondition(source.VisibleWhen),
                EnabledWhen = CloneCondition(source.EnabledWhen),
                Payload = source.Payload?.DeepClone(),
                SameLine = source.SameLine,
                DefaultOpen = source.DefaultOpen,
                Width = source.Width,
                Height = source.Height,
                LabelWidth = source.LabelWidth,
                Spacing = source.Spacing,
                Indent = source.Indent,
                Wrap = source.Wrap,
                Step = source.Step,
                Min = source.Min,
                Max = source.Max,
                Columns = source.Columns,
                Text = source.Text,
                Template = source.Template,
                Use = source.Use,
                Children = CloneNodes(source.Children),
                Options = CloneOptions(source.Options),
            };

            return clone;
        }

        private static void ApplyTemplateOverrides(JsonUiNode target, JsonUiNode overrides)
        {
            if (target == null || overrides == null)
                return;

            var overrideType = NormalizeType(overrides.Type);
            var overrideUsesTemplate = !string.IsNullOrWhiteSpace(overrides.Template) || !string.IsNullOrWhiteSpace(overrides.Use);
            if (!string.IsNullOrWhiteSpace(overrides.Type)
                && overrideType != JsonUiNodeType.Include
                && !(overrideUsesTemplate && overrideType == JsonUiNodeType.Section))
            {
                target.Type = overrides.Type;
            }

            if (!string.IsNullOrWhiteSpace(overrides.Id))
                target.Id = overrides.Id;

            if (overrides.Label != null)
                target.Label = overrides.Label;

            if (overrides.Path != null)
                target.Path = overrides.Path;

            if (overrides.Action != null)
                target.Action = overrides.Action;

            if (overrides.Tooltip != null)
                target.Tooltip = overrides.Tooltip;

            if (overrides.Visible.HasValue)
                target.Visible = overrides.Visible;

            if (overrides.Enabled.HasValue)
                target.Enabled = overrides.Enabled;

            if (overrides.VisibleWhen != null)
                target.VisibleWhen = CloneCondition(overrides.VisibleWhen);

            if (overrides.EnabledWhen != null)
                target.EnabledWhen = CloneCondition(overrides.EnabledWhen);

            if (overrides.Payload != null)
                target.Payload = overrides.Payload.DeepClone();

            if (overrides.SameLine)
                target.SameLine = true;

            if (overrides.DefaultOpen.HasValue)
                target.DefaultOpen = overrides.DefaultOpen;

            if (overrides.Width.HasValue)
                target.Width = overrides.Width;

            if (overrides.Height.HasValue)
                target.Height = overrides.Height;

            if (overrides.LabelWidth.HasValue)
                target.LabelWidth = overrides.LabelWidth;

            if (overrides.Spacing.HasValue)
                target.Spacing = overrides.Spacing;

            if (overrides.Indent.HasValue)
                target.Indent = overrides.Indent;

            if (overrides.Wrap.HasValue)
                target.Wrap = overrides.Wrap;

            if (overrides.Step.HasValue)
                target.Step = overrides.Step;

            if (overrides.Min.HasValue)
                target.Min = overrides.Min;

            if (overrides.Max.HasValue)
                target.Max = overrides.Max;

            if (overrides.Columns.HasValue)
                target.Columns = overrides.Columns;

            if (overrides.Text != null)
                target.Text = overrides.Text;

            if (overrides.Children != null && overrides.Children.Count > 0)
                target.Children = CloneNodes(overrides.Children);

            if (overrides.Options != null && overrides.Options.Count > 0)
                target.Options = CloneOptions(overrides.Options);
        }

        private static JsonUiCondition CloneCondition(JsonUiCondition condition)
        {
            if (condition == null)
                return null;

            return new JsonUiCondition
            {
                Path = condition.Path,
                EqualValue = condition.EqualValue?.DeepClone(),
                NotEqualValue = condition.NotEqualValue?.DeepClone(),
                Exists = condition.Exists,
                Truthy = condition.Truthy,
                GreaterThan = condition.GreaterThan,
                GreaterThanOrEqual = condition.GreaterThanOrEqual,
                LessThan = condition.LessThan,
                LessThanOrEqual = condition.LessThanOrEqual,
                Not = condition.Not,
            };
        }

        private static List<JsonUiNode> CloneNodes(IReadOnlyList<JsonUiNode> nodes)
        {
            var clone = new List<JsonUiNode>();
            if (nodes == null)
                return clone;

            for (var i = 0; i < nodes.Count; i++)
                clone.Add(CloneNode(nodes[i]));

            return clone;
        }

        private static List<JsonUiOption> CloneOptions(IReadOnlyList<JsonUiOption> options)
        {
            var clone = new List<JsonUiOption>();
            if (options == null)
                return clone;

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                clone.Add(new JsonUiOption
                {
                    Label = option?.Label,
                    Value = option?.Value?.DeepClone(),
                });
            }

            return clone;
        }
    }

    public sealed class JsonUiNode
    {
        [JsonProperty("type")]
        public string Type { get; set; } = JsonUiNodeType.Section;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("template")]
        public string Template { get; set; }

        [JsonProperty("use")]
        public string Use { get; set; }

        [JsonProperty("tooltip")]
        public string Tooltip { get; set; }

        [JsonProperty("visible")]
        public bool? Visible { get; set; }

        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        [JsonProperty("visibleWhen")]
        public JsonUiCondition VisibleWhen { get; set; }

        [JsonProperty("enabledWhen")]
        public JsonUiCondition EnabledWhen { get; set; }

        [JsonProperty("payload")]
        public JToken Payload { get; set; }

        [JsonProperty("sameLine")]
        public bool SameLine { get; set; }

        [JsonProperty("defaultOpen")]
        public bool? DefaultOpen { get; set; }

        [JsonProperty("width")]
        public float? Width { get; set; }

        [JsonProperty("height")]
        public float? Height { get; set; }

        [JsonProperty("labelWidth")]
        public float? LabelWidth { get; set; }

        [JsonProperty("spacing")]
        public float? Spacing { get; set; }

        [JsonProperty("indent")]
        public float? Indent { get; set; }

        [JsonProperty("wrap")]
        public bool? Wrap { get; set; }

        [JsonProperty("step")]
        public float? Step { get; set; }

        [JsonProperty("min")]
        public float? Min { get; set; }

        [JsonProperty("max")]
        public float? Max { get; set; }

        [JsonProperty("columns")]
        public int? Columns { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("children")]
        public List<JsonUiNode> Children { get; set; } = new();

        [JsonProperty("options")]
        public List<JsonUiOption> Options { get; set; } = new();

        public IReadOnlyList<JsonUiNode> SafeChildren => Children != null ? Children : Array.Empty<JsonUiNode>();
        public IReadOnlyList<JsonUiOption> SafeOptions => Options != null ? Options : Array.Empty<JsonUiOption>();
    }

    public sealed class JsonUiOption
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("value")]
        public JToken Value { get; set; }
    }

    [JsonConverter(typeof(JsonUiConditionConverter))]
    public sealed class JsonUiCondition
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("equals")]
        public JToken EqualValue { get; set; }

        [JsonProperty("notEquals")]
        public JToken NotEqualValue { get; set; }

        [JsonProperty("exists")]
        public bool? Exists { get; set; }

        [JsonProperty("truthy")]
        public bool? Truthy { get; set; }

        [JsonProperty("gt")]
        public double? GreaterThan { get; set; }

        [JsonProperty("gte")]
        public double? GreaterThanOrEqual { get; set; }

        [JsonProperty("lt")]
        public double? LessThan { get; set; }

        [JsonProperty("lte")]
        public double? LessThanOrEqual { get; set; }

        [JsonProperty("not")]
        public bool Not { get; set; }

        public static JsonUiCondition FromPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : new JsonUiCondition { Path = path };
        }

        public bool Evaluate(JsonDocumentModel document)
        {
            if (document == null || string.IsNullOrWhiteSpace(Path))
                return false;

            var token = document.GetToken(Path);
            var result = EvaluateToken(token);
            return Not ? !result : result;
        }

        private bool EvaluateToken(JToken token)
        {
            if (Exists.HasValue && Exists.Value != (token != null))
                return false;

            if (EqualValue != null && !JToken.DeepEquals(token ?? JValue.CreateNull(), EqualValue))
                return false;

            if (NotEqualValue != null && JToken.DeepEquals(token ?? JValue.CreateNull(), NotEqualValue))
                return false;

            if (Truthy.HasValue && Truthy.Value != IsTruthy(token))
                return false;

            if (HasNumericOperator())
            {
                if (!TryGetNumber(token, out var number))
                    return false;

                if (GreaterThan.HasValue && number <= GreaterThan.Value)
                    return false;

                if (GreaterThanOrEqual.HasValue && number < GreaterThanOrEqual.Value)
                    return false;

                if (LessThan.HasValue && number >= LessThan.Value)
                    return false;

                if (LessThanOrEqual.HasValue && number > LessThanOrEqual.Value)
                    return false;
            }

            return HasAnyOperator() || IsTruthy(token);
        }

        private bool HasAnyOperator()
        {
            return Exists.HasValue
                   || EqualValue != null
                   || NotEqualValue != null
                   || Truthy.HasValue
                   || GreaterThan.HasValue
                   || GreaterThanOrEqual.HasValue
                   || LessThan.HasValue
                   || LessThanOrEqual.HasValue;
        }

        private bool HasNumericOperator()
        {
            return GreaterThan.HasValue
                   || GreaterThanOrEqual.HasValue
                   || LessThan.HasValue
                   || LessThanOrEqual.HasValue;
        }

        internal static bool IsTruthy(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return false;

            return token.Type switch
            {
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Integer => token.Value<long>() != 0,
                JTokenType.Float => Math.Abs(token.Value<double>()) > double.Epsilon,
                JTokenType.String => !string.IsNullOrEmpty(token.Value<string>()),
                JTokenType.Array => token.HasValues,
                JTokenType.Object => token.HasValues,
                _ => true,
            };
        }

        private static bool TryGetNumber(JToken token, out double number)
        {
            if (token == null)
            {
                number = 0d;
                return false;
            }

            try
            {
                number = token.Value<double>();
                return true;
            }
            catch
            {
                number = 0d;
                return false;
            }
        }
    }

    internal sealed class JsonUiConditionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(JsonUiCondition);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType == JsonToken.String)
                return JsonUiCondition.FromPath((string)reader.Value);

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException("JsonUiCondition must be a string path or object.");

            var obj = JObject.Load(reader);
            return new JsonUiCondition
            {
                Path = obj.Value<string>("path"),
                EqualValue = obj["equals"]?.DeepClone(),
                NotEqualValue = obj["notEquals"]?.DeepClone(),
                Exists = obj["exists"]?.Value<bool?>(),
                Truthy = obj["truthy"]?.Value<bool?>(),
                GreaterThan = obj["gt"]?.Value<double?>(),
                GreaterThanOrEqual = obj["gte"]?.Value<double?>(),
                LessThan = obj["lt"]?.Value<double?>(),
                LessThanOrEqual = obj["lte"]?.Value<double?>(),
                Not = obj["not"]?.Value<bool>() ?? false,
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is not JsonUiCondition condition)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("path");
            writer.WriteValue(condition.Path);

            WriteToken(writer, "equals", condition.EqualValue);
            WriteToken(writer, "notEquals", condition.NotEqualValue);
            WriteNullable(writer, "exists", condition.Exists);
            WriteNullable(writer, "truthy", condition.Truthy);
            WriteNullable(writer, "gt", condition.GreaterThan);
            WriteNullable(writer, "gte", condition.GreaterThanOrEqual);
            WriteNullable(writer, "lt", condition.LessThan);
            WriteNullable(writer, "lte", condition.LessThanOrEqual);

            if (condition.Not)
            {
                writer.WritePropertyName("not");
                writer.WriteValue(true);
            }

            writer.WriteEndObject();
        }

        private static void WriteToken(JsonWriter writer, string name, JToken token)
        {
            if (token == null)
                return;

            writer.WritePropertyName(name);
            token.WriteTo(writer);
        }

        private static void WriteNullable(JsonWriter writer, string name, bool? value)
        {
            if (!value.HasValue)
                return;

            writer.WritePropertyName(name);
            writer.WriteValue(value.Value);
        }

        private static void WriteNullable(JsonWriter writer, string name, double? value)
        {
            if (!value.HasValue)
                return;

            writer.WritePropertyName(name);
            writer.WriteValue(value.Value);
        }
    }
}
#endif
