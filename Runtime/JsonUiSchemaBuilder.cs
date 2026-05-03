#if NEWTONSOFT_EXISTS
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI
{
    public sealed class JsonUiSchemaBuilder
    {
        private readonly JsonUiSchema _schema = new();

        private JsonUiSchemaBuilder(string title)
        {
            _schema.Title = title;
        }

        public static JsonUiSchemaBuilder Create(string title = null)
        {
            return new JsonUiSchemaBuilder(title);
        }

        public JsonUiSchemaBuilder Title(string title)
        {
            _schema.Title = title;
            return this;
        }

        public JsonUiSchemaBuilder Root(JsonUiNode root)
        {
            _schema.Root = root ?? new JsonUiNode();
            return this;
        }

        public JsonUiSchemaBuilder Root(Func<JsonUiRootBuilder, JsonUiNode> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));

            return Root(build.Invoke(new JsonUiRootBuilder()));
        }

        public JsonUiSchemaBuilder Template(string name, JsonUiNode node)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Template name cannot be null or empty.", nameof(name));

            _schema.Templates[name] = node ?? new JsonUiNode();
            return this;
        }

        public JsonUiSchema Build()
        {
            return _schema;
        }

        public string ToJson(Formatting formatting = Formatting.None)
        {
            return JObject.FromObject(_schema).ToString(formatting);
        }
    }

    public sealed class JsonUiRootBuilder
    {
        public JsonUiContainerBuilder Section(string label = null)
        {
            return new JsonUiContainerBuilder(Ui.Section(label));
        }

        public JsonUiContainerBuilder Foldout(string label)
        {
            return new JsonUiContainerBuilder(Ui.Foldout(label));
        }

        public JsonUiContainerBuilder Row()
        {
            return new JsonUiContainerBuilder(Ui.Row());
        }

        public JsonUiContainerBuilder Columns(int columns)
        {
            return new JsonUiContainerBuilder(Ui.Columns(columns));
        }

        public JsonUiContainerBuilder Tabs()
        {
            return new JsonUiContainerBuilder(Ui.Tabs());
        }
    }

    public sealed class JsonUiContainerBuilder
    {
        public JsonUiContainerBuilder(JsonUiNode node)
        {
            Node = node ?? new JsonUiNode();
        }

        public JsonUiNode Node { get; }

        public JsonUiContainerBuilder Add(JsonUiNode child)
        {
            if (child != null)
                Node.Children.Add(child);

            return this;
        }

        public JsonUiContainerBuilder Add(params JsonUiNode[] children)
        {
            if (children == null)
                return this;

            for (var i = 0; i < children.Length; i++)
                Add(children[i]);

            return this;
        }

        public JsonUiContainerBuilder Section(string label, Action<JsonUiContainerBuilder> configure)
        {
            var child = new JsonUiContainerBuilder(Ui.Section(label));
            configure?.Invoke(child);
            return Add(child.Node);
        }

        public JsonUiContainerBuilder Foldout(string label, Action<JsonUiContainerBuilder> configure)
        {
            var child = new JsonUiContainerBuilder(Ui.Foldout(label));
            configure?.Invoke(child);
            return Add(child.Node);
        }

        public JsonUiContainerBuilder Row(Action<JsonUiContainerBuilder> configure)
        {
            var child = new JsonUiContainerBuilder(Ui.Row());
            configure?.Invoke(child);
            return Add(child.Node);
        }

        public JsonUiContainerBuilder Columns(int columns, Action<JsonUiContainerBuilder> configure)
        {
            var child = new JsonUiContainerBuilder(Ui.Columns(columns));
            configure?.Invoke(child);
            return Add(child.Node);
        }

        public JsonUiContainerBuilder Tabs(Action<JsonUiContainerBuilder> configure)
        {
            var child = new JsonUiContainerBuilder(Ui.Tabs());
            configure?.Invoke(child);
            return Add(child.Node);
        }

        public JsonUiContainerBuilder Tab(string label, Action<JsonUiContainerBuilder> configure)
        {
            var child = new JsonUiContainerBuilder(Ui.Tab(label));
            configure?.Invoke(child);
            return Add(child.Node);
        }

        public JsonUiContainerBuilder Include(string template)
        {
            return Add(Ui.Include(template));
        }

        public JsonUiContainerBuilder Separator()
        {
            return Add(Ui.Separator());
        }

        public JsonUiContainerBuilder Space(float height = 0f)
        {
            return Add(Ui.Space(height));
        }

        public JsonUiContainerBuilder Text(string text)
        {
            return Add(Ui.Text(text));
        }

        public JsonUiContainerBuilder Text(string label, string path)
        {
            return Add(Ui.Text(label, path));
        }

        public JsonUiContainerBuilder Field(string label, string path)
        {
            return Add(Ui.Field(label, path));
        }

        public JsonUiContainerBuilder InputText(string label, string path)
        {
            return Add(Ui.InputText(label, path));
        }

        public JsonUiContainerBuilder InputTextMultiline(string label, string path, float height = 64f)
        {
            return Add(Ui.InputTextMultiline(label, path, height));
        }

        public JsonUiContainerBuilder Int(string label, string path)
        {
            return Add(Ui.Int(label, path));
        }

        public JsonUiContainerBuilder Float(string label, string path)
        {
            return Add(Ui.Float(label, path));
        }

        public JsonUiContainerBuilder DragInt(string label, string path, float step = 1f, float? min = null, float? max = null)
        {
            return Add(Ui.DragInt(label, path, step, min, max));
        }

        public JsonUiContainerBuilder DragFloat(string label, string path, float step = 0.1f, float? min = null, float? max = null)
        {
            return Add(Ui.DragFloat(label, path, step, min, max));
        }

        public JsonUiContainerBuilder Toggle(string label, string path)
        {
            return Add(Ui.Toggle(label, path));
        }

        public JsonUiContainerBuilder SliderInt(string label, string path, float min, float max)
        {
            return Add(Ui.SliderInt(label, path, min, max));
        }

        public JsonUiContainerBuilder SliderFloat(string label, string path, float min, float max)
        {
            return Add(Ui.SliderFloat(label, path, min, max));
        }

        public JsonUiContainerBuilder Vector2(string label, string path, float step = 0.1f)
        {
            return Add(Ui.Vector2(label, path, step));
        }

        public JsonUiContainerBuilder Vector3(string label, string path, float step = 0.1f)
        {
            return Add(Ui.Vector3(label, path, step));
        }

        public JsonUiContainerBuilder Color(string label, string path)
        {
            return Add(Ui.Color(label, path));
        }

        public JsonUiContainerBuilder Select(string label, string path, params JsonUiOption[] options)
        {
            return Add(Ui.Select(label, path, options));
        }

        public JsonUiContainerBuilder Radio(string label, string path, params JsonUiOption[] options)
        {
            return Add(Ui.Radio(label, path, options));
        }

        public JsonUiContainerBuilder Progress(string label, string path, float min = 0f, float max = 1f)
        {
            return Add(Ui.Progress(label, path, min, max));
        }

        public JsonUiContainerBuilder Button(string label, string action, JToken payload = null)
        {
            return Add(Ui.Button(label, action, payload));
        }

        public static implicit operator JsonUiNode(JsonUiContainerBuilder builder)
        {
            return builder?.Node;
        }
    }

    public static class Ui
    {
        public static JsonUiSchema Schema(string title, JsonUiNode root)
        {
            return new JsonUiSchema
            {
                Title = title,
                Root = root ?? new JsonUiNode(),
            };
        }

        public static JsonUiNode Node(string type, string label = null, string path = null, params JsonUiNode[] children)
        {
            return new JsonUiNode
            {
                Type = type,
                Label = label,
                Path = path,
                Children = CreateChildren(children),
            };
        }

        public static JsonUiNode Section(params JsonUiNode[] children)
        {
            return Node(JsonUiNodeType.Section, children: children);
        }

        public static JsonUiNode Section(string label, params JsonUiNode[] children)
        {
            return Node(JsonUiNodeType.Section, label, children: children);
        }

        public static JsonUiNode Foldout(string label, params JsonUiNode[] children)
        {
            return Node(JsonUiNodeType.Foldout, label, children: children);
        }

        public static JsonUiNode Row(params JsonUiNode[] children)
        {
            return Node(JsonUiNodeType.Row, children: children);
        }

        public static JsonUiNode Columns(int columns, params JsonUiNode[] children)
        {
            return Node(JsonUiNodeType.Columns, children: children).Columns(columns);
        }

        public static JsonUiNode Tabs(params JsonUiNode[] tabs)
        {
            return Node(JsonUiNodeType.Tabs, children: tabs);
        }

        public static JsonUiNode Tab(string label, params JsonUiNode[] children)
        {
            return Node(JsonUiNodeType.Section, label, children: children);
        }

        public static JsonUiNode Include(string template)
        {
            return Node(JsonUiNodeType.Include).Template(template);
        }

        public static JsonUiNode Separator()
        {
            return Node(JsonUiNodeType.Separator);
        }

        public static JsonUiNode Space(float height = 0f)
        {
            return Node(JsonUiNodeType.Space).Height(height);
        }

        public static JsonUiNode Text(string text)
        {
            return Node(JsonUiNodeType.Text).TextValue(text);
        }

        public static JsonUiNode Text(string label, string path)
        {
            return Node(JsonUiNodeType.Text, label, path);
        }

        public static JsonUiNode Field(string label, string path)
        {
            return Node(JsonUiNodeType.Field, label, path);
        }

        public static JsonUiNode InputText(string label, string path)
        {
            return Node(JsonUiNodeType.InputText, label, path);
        }

        public static JsonUiNode InputTextMultiline(string label, string path, float height = 64f)
        {
            return Node(JsonUiNodeType.InputTextMultiline, label, path).Height(height);
        }

        public static JsonUiNode Int(string label, string path)
        {
            return Node(JsonUiNodeType.Integer, label, path);
        }

        public static JsonUiNode Float(string label, string path)
        {
            return Node(JsonUiNodeType.Float, label, path);
        }

        public static JsonUiNode DragInt(string label, string path, float step = 1f, float? min = null, float? max = null)
        {
            return Numeric(JsonUiNodeType.DragInt, label, path, min, max).Step(step);
        }

        public static JsonUiNode DragFloat(string label, string path, float step = 0.1f, float? min = null, float? max = null)
        {
            return Numeric(JsonUiNodeType.DragFloat, label, path, min, max).Step(step);
        }

        public static JsonUiNode Toggle(string label, string path)
        {
            return Node(JsonUiNodeType.Toggle, label, path);
        }

        public static JsonUiNode SliderInt(string label, string path, float min, float max)
        {
            return Numeric(JsonUiNodeType.SliderInt, label, path, min, max);
        }

        public static JsonUiNode SliderFloat(string label, string path, float min, float max)
        {
            return Numeric(JsonUiNodeType.SliderFloat, label, path, min, max);
        }

        public static JsonUiNode Vector2(string label, string path, float step = 0.1f)
        {
            return Node(JsonUiNodeType.Vector2, label, path).Step(step);
        }

        public static JsonUiNode Vector3(string label, string path, float step = 0.1f)
        {
            return Node(JsonUiNodeType.Vector3, label, path).Step(step);
        }

        public static JsonUiNode Color(string label, string path)
        {
            return Node(JsonUiNodeType.Color, label, path);
        }

        public static JsonUiNode Select(string label, string path, params JsonUiOption[] options)
        {
            return Node(JsonUiNodeType.Select, label, path).Options(options);
        }

        public static JsonUiNode Radio(string label, string path, params JsonUiOption[] options)
        {
            return Node(JsonUiNodeType.Radio, label, path).Options(options);
        }

        public static JsonUiNode Button(string label, string action, JToken payload = null)
        {
            return Node(JsonUiNodeType.Button, label).Action(action).Payload(payload);
        }

        public static JsonUiNode Progress(string label, string path, float min = 0f, float max = 1f)
        {
            return Numeric(JsonUiNodeType.Progress, label, path, min, max);
        }

        public static JsonUiOption Option(string label, object value)
        {
            return new JsonUiOption
            {
                Label = label,
                Value = ToToken(value),
            };
        }

        public static JsonUiCondition Condition(string path)
        {
            return new JsonUiCondition { Path = path };
        }

        public static JsonUiCondition Eq(string path, object value)
        {
            return Condition(path).Eq(value);
        }

        public static JsonUiCondition NotEq(string path, object value)
        {
            return Condition(path).NotEq(value);
        }

        public static JsonUiCondition Exists(string path, bool value = true)
        {
            return Condition(path).Exists(value);
        }

        public static JsonUiCondition Truthy(string path, bool value = true)
        {
            return Condition(path).Truthy(value);
        }

        public static JsonUiCondition Gt(string path, double value)
        {
            return Condition(path).Gt(value);
        }

        public static JsonUiCondition Gte(string path, double value)
        {
            return Condition(path).Gte(value);
        }

        public static JsonUiCondition Lt(string path, double value)
        {
            return Condition(path).Lt(value);
        }

        public static JsonUiCondition Lte(string path, double value)
        {
            return Condition(path).Lte(value);
        }

        internal static JToken ToToken(object value)
        {
            if (value == null)
                return JValue.CreateNull();

            return value is JToken token ? token.DeepClone() : JToken.FromObject(value);
        }

        private static JsonUiNode Numeric(string type, string label, string path, float? min, float? max)
        {
            return Node(type, label, path).Min(min).Max(max);
        }

        private static System.Collections.Generic.List<JsonUiNode> CreateChildren(JsonUiNode[] children)
        {
            var result = new System.Collections.Generic.List<JsonUiNode>();
            if (children == null)
                return result;

            for (var i = 0; i < children.Length; i++)
            {
                if (children[i] != null)
                    result.Add(children[i]);
            }

            return result;
        }
    }

    public static class JsonUiNodeFluentExtensions
    {
        public static JsonUiNode Id(this JsonUiNode node, string id)
        {
            node.Id = id;
            return node;
        }

        public static JsonUiNode Label(this JsonUiNode node, string label)
        {
            node.Label = label;
            return node;
        }

        public static JsonUiNode Path(this JsonUiNode node, string path)
        {
            node.Path = path;
            return node;
        }

        public static JsonUiNode Action(this JsonUiNode node, string action)
        {
            node.Action = action;
            return node;
        }

        public static JsonUiNode Template(this JsonUiNode node, string template)
        {
            node.Template = template;
            return node;
        }

        public static JsonUiNode Use(this JsonUiNode node, string template)
        {
            node.Use = template;
            return node;
        }

        public static JsonUiNode Tooltip(this JsonUiNode node, string tooltip)
        {
            node.Tooltip = tooltip;
            return node;
        }

        public static JsonUiNode Visible(this JsonUiNode node, bool visible)
        {
            node.Visible = visible;
            return node;
        }

        public static JsonUiNode Enabled(this JsonUiNode node, bool enabled)
        {
            node.Enabled = enabled;
            return node;
        }

        public static JsonUiNode VisibleWhen(this JsonUiNode node, JsonUiCondition condition)
        {
            node.VisibleWhen = condition;
            return node;
        }

        public static JsonUiNode EnabledWhen(this JsonUiNode node, JsonUiCondition condition)
        {
            node.EnabledWhen = condition;
            return node;
        }

        public static JsonUiNode Payload(this JsonUiNode node, JToken payload)
        {
            node.Payload = payload?.DeepClone();
            return node;
        }

        public static JsonUiNode Payload(this JsonUiNode node, string key, object value)
        {
            var payload = node.Payload as JObject;
            if (payload == null)
            {
                payload = new JObject();
                node.Payload = payload;
            }

            payload[key] = Ui.ToToken(value);
            return node;
        }

        public static JsonUiNode SameLine(this JsonUiNode node, bool sameLine = true)
        {
            node.SameLine = sameLine;
            return node;
        }

        public static JsonUiNode DefaultOpen(this JsonUiNode node, bool defaultOpen = true)
        {
            node.DefaultOpen = defaultOpen;
            return node;
        }

        public static JsonUiNode Width(this JsonUiNode node, float? width)
        {
            node.Width = width;
            return node;
        }

        public static JsonUiNode Height(this JsonUiNode node, float? height)
        {
            node.Height = height;
            return node;
        }

        public static JsonUiNode LabelWidth(this JsonUiNode node, float? labelWidth)
        {
            node.LabelWidth = labelWidth;
            return node;
        }

        public static JsonUiNode Spacing(this JsonUiNode node, float? spacing)
        {
            node.Spacing = spacing;
            return node;
        }

        public static JsonUiNode Indent(this JsonUiNode node, float? indent)
        {
            node.Indent = indent;
            return node;
        }

        public static JsonUiNode Wrap(this JsonUiNode node, bool? wrap = true)
        {
            node.Wrap = wrap;
            return node;
        }

        public static JsonUiNode Step(this JsonUiNode node, float? step)
        {
            node.Step = step;
            return node;
        }

        public static JsonUiNode Min(this JsonUiNode node, float? min)
        {
            node.Min = min;
            return node;
        }

        public static JsonUiNode Max(this JsonUiNode node, float? max)
        {
            node.Max = max;
            return node;
        }

        public static JsonUiNode Columns(this JsonUiNode node, int? columns)
        {
            node.Columns = columns;
            return node;
        }

        public static JsonUiNode TextValue(this JsonUiNode node, string text)
        {
            node.Text = text;
            return node;
        }

        public static JsonUiNode Children(this JsonUiNode node, params JsonUiNode[] children)
        {
            node.Children.Clear();
            return node.AddChildren(children);
        }

        public static JsonUiNode AddChildren(this JsonUiNode node, params JsonUiNode[] children)
        {
            if (children == null)
                return node;

            for (var i = 0; i < children.Length; i++)
            {
                if (children[i] != null)
                    node.Children.Add(children[i]);
            }

            return node;
        }

        public static JsonUiNode Options(this JsonUiNode node, params JsonUiOption[] options)
        {
            node.Options.Clear();
            if (options == null)
                return node;

            for (var i = 0; i < options.Length; i++)
            {
                if (options[i] != null)
                    node.Options.Add(options[i]);
            }

            return node;
        }
    }

    public static class JsonUiConditionFluentExtensions
    {
        public static JsonUiCondition Eq(this JsonUiCondition condition, object value)
        {
            condition.EqualValue = Ui.ToToken(value);
            return condition;
        }

        public static JsonUiCondition NotEq(this JsonUiCondition condition, object value)
        {
            condition.NotEqualValue = Ui.ToToken(value);
            return condition;
        }

        public static JsonUiCondition Exists(this JsonUiCondition condition, bool value = true)
        {
            condition.Exists = value;
            return condition;
        }

        public static JsonUiCondition Truthy(this JsonUiCondition condition, bool value = true)
        {
            condition.Truthy = value;
            return condition;
        }

        public static JsonUiCondition Gt(this JsonUiCondition condition, double value)
        {
            condition.GreaterThan = value;
            return condition;
        }

        public static JsonUiCondition Gte(this JsonUiCondition condition, double value)
        {
            condition.GreaterThanOrEqual = value;
            return condition;
        }

        public static JsonUiCondition Lt(this JsonUiCondition condition, double value)
        {
            condition.LessThan = value;
            return condition;
        }

        public static JsonUiCondition Lte(this JsonUiCondition condition, double value)
        {
            condition.LessThanOrEqual = value;
            return condition;
        }

        public static JsonUiCondition Not(this JsonUiCondition condition, bool value = true)
        {
            condition.Not = value;
            return condition;
        }
    }
}
#endif
