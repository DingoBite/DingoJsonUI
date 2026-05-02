#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using System.Globalization;
using ImGuiNET;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI.GUI
{
    public sealed class UImGuiJsonScreen
    {
        private const float DefaultScrollWheelPixelsPerStep = 280f;
        private const float LabelColumnWidth = 150f;
        private const float MinimumFieldWidth = 80f;

        private readonly JsonDocumentModel _document;
        private readonly Dictionary<string, string> _textBuffers = new();

        public JsonUiSchema Schema { get; set; }
        public JsonUiCommandRegistry Commands { get; }
        public string WindowTitle { get; set; }
        public float ScrollWheelPixelsPerStep { get; set; } = DefaultScrollWheelPixelsPerStep;

        public UImGuiJsonScreen(JsonDocumentModel document, JsonUiSchema schema, JsonUiCommandRegistry commands = null, string windowTitle = null)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            Commands = commands ?? new JsonUiCommandRegistry();
            WindowTitle = windowTitle ?? schema.Title ?? "Dingo Fast UI";
        }

        public void Draw()
        {
            if (!ImGui.Begin(WindowTitle))
            {
                ImGui.End();
                return;
            }

            ApplyScrollWheelAcceleration();
            DrawNode(Schema.Root ?? new JsonUiNode());
            ImGui.End();
        }

        private void DrawNode(JsonUiNode node)
        {
            if (node == null || !IsNodeVisible(node))
                return;

            if (node.SameLine)
                ImGui.SameLine();

            ImGui.PushID(GetNodeId(node));
            switch ((node.Type ?? JsonUiNodeType.Section).Trim())
            {
                case JsonUiNodeType.Row:
                    DrawRow(node);
                    break;
                case JsonUiNodeType.Columns:
                    DrawColumns(node);
                    break;
                case JsonUiNodeType.Tabs:
                    DrawTabs(node);
                    break;
                case JsonUiNodeType.Foldout:
                    DrawFoldout(node);
                    break;
                case JsonUiNodeType.Text:
                    DrawText(node);
                    break;
                case JsonUiNodeType.Button:
                    DrawButton(node);
                    break;
                case JsonUiNodeType.Toggle:
                    DrawToggle(node);
                    break;
                case JsonUiNodeType.InputText:
                    DrawStringField(node);
                    break;
                case JsonUiNodeType.Integer:
                    DrawIntegerField(node);
                    break;
                case JsonUiNodeType.Float:
                    DrawFloatField(node);
                    break;
                case JsonUiNodeType.SliderInt:
                    DrawIntSlider(node);
                    break;
                case JsonUiNodeType.SliderFloat:
                    DrawFloatSlider(node);
                    break;
                case JsonUiNodeType.Select:
                    DrawSelect(node);
                    break;
                case JsonUiNodeType.Progress:
                    DrawProgress(node);
                    break;
                case JsonUiNodeType.Separator:
                    ImGui.Separator();
                    break;
                case JsonUiNodeType.Space:
                    ImGui.Spacing();
                    break;
                case JsonUiNodeType.Field:
                    DrawAutoField(node);
                    break;
                default:
                    DrawSection(node);
                    break;
            }

            DrawTooltip(node);
            ImGui.PopID();
        }

        private void DrawSection(JsonUiNode node)
        {
            if (!string.IsNullOrWhiteSpace(node.Label))
            {
                ImGui.TextUnformatted(node.Label);
                ImGui.Separator();
            }

            DrawChildren(node);
        }

        private void DrawFoldout(JsonUiNode node)
        {
            var flags = node.DefaultOpen == true ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

            if (!ImGui.TreeNodeEx(node.Label ?? node.Path ?? "Foldout", flags))
                return;

            DrawChildren(node);
            ImGui.TreePop();
        }

        private void DrawRow(JsonUiNode node)
        {
            var children = node.SafeChildren;
            for (var i = 0; i < children.Count; i++)
            {
                if (i > 0)
                    ImGui.SameLine();

                DrawNode(children[i]);
            }
        }

        private void DrawColumns(JsonUiNode node)
        {
            var children = node.SafeChildren;
            var count = Math.Max(1, node.Columns ?? children.Count);
            ImGui.Columns(count, $"columns##{GetNodeId(node)}", false);

            for (var i = 0; i < children.Count; i++)
            {
                DrawNode(children[i]);
                ImGui.NextColumn();
            }

            ImGui.Columns(1);
        }

        private void DrawTabs(JsonUiNode node)
        {
            if (!ImGui.BeginTabBar($"tabs##{GetNodeId(node)}"))
                return;

            var children = node.SafeChildren;
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null || !IsNodeVisible(child))
                    continue;

                var label = child.Label ?? child.Id ?? $"Tab {i + 1}";
                if (!ImGui.BeginTabItem($"{label}##{i}"))
                    continue;

                ImGui.PushID(i);
                DrawChildren(child);
                ImGui.PopID();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        private void DrawText(JsonUiNode node)
        {
            var text = node.Text;
            if (string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(node.Path))
                text = FormatToken(GetToken(node.Path));

            if (!string.IsNullOrWhiteSpace(node.Label) && !string.IsNullOrWhiteSpace(node.Path))
            {
                DrawLabel(node);
                ImGui.SameLine(LabelColumnWidth);
                ImGui.TextUnformatted(text ?? string.Empty);
                return;
            }

            ImGui.TextUnformatted(text ?? node.Label ?? string.Empty);
        }

        private void DrawButton(JsonUiNode node)
        {
            var context = CreateCommandContext(node);
            var commandVisible = TryGetCommand(node.Action, out var command) && command.CanDraw(context);
            if (!commandVisible)
                return;

            var enabled = IsNodeEnabled(node) && command.CanExecute(context);
            if (!enabled)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);

            var clicked = ImGui.Button(node.Label ?? node.Action ?? "Button");

            if (!enabled)
                ImGui.PopStyleVar();

            if (clicked && enabled)
                command.Execute(context);
        }

        private void DrawToggle(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            DrawLabel(node);
            ImGui.SameLine(LabelColumnWidth);
            var value = _document.GetValue(path, false);
            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.Checkbox("##value", ref value))
                _document.SetValue(path, new JValue(value));

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawStringField(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            DrawLabel(node);
            ImGui.SameLine(LabelColumnWidth);
            SetNextItemWidth(node);

            var current = _document.GetValue<string>(path) ?? string.Empty;
            var buffer = GetBuffer(path, current);

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.InputText("##value", ref buffer, 2048))
            {
                _textBuffers[path] = buffer;
                _document.SetValue(path, new JValue(buffer));
            }

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();

            _textBuffers[path] = buffer;
        }

        private void DrawIntegerField(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            DrawLabel(node);
            ImGui.SameLine(LabelColumnWidth);
            SetNextItemWidth(node);

            var current = _document.GetValue(path, 0).ToString(CultureInfo.InvariantCulture);
            var buffer = GetBuffer(path, current);

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.InputText("##value", ref buffer, 64))
            {
                _textBuffers[path] = buffer;
                if (int.TryParse(buffer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    _document.SetValue(path, new JValue(parsed));
            }

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();

            _textBuffers[path] = buffer;
        }

        private void DrawFloatField(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            DrawLabel(node);
            ImGui.SameLine(LabelColumnWidth);
            SetNextItemWidth(node);

            var current = _document.GetValue(path, 0f).ToString("G", CultureInfo.InvariantCulture);
            var buffer = GetBuffer(path, current);

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.InputText("##value", ref buffer, 64))
            {
                _textBuffers[path] = buffer;
                if (float.TryParse(buffer, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
                    _document.SetValue(path, new JValue(parsed));
            }

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();

            _textBuffers[path] = buffer;
        }

        private void DrawIntSlider(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            DrawLabel(node);
            ImGui.SameLine(LabelColumnWidth);
            SetNextItemWidth(node);

            var value = _document.GetValue(path, 0);
            var min = (int)(node.Min ?? 0f);
            var max = (int)(node.Max ?? 100f);

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.SliderInt("##value", ref value, min, max))
                _document.SetValue(path, new JValue(value));

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawFloatSlider(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            DrawLabel(node);
            ImGui.SameLine(LabelColumnWidth);
            SetNextItemWidth(node);

            var value = _document.GetValue(path, 0f);
            var min = node.Min ?? 0f;
            var max = node.Max ?? 1f;

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.SliderFloat("##value", ref value, min, max))
                _document.SetValue(path, new JValue(value));

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawSelect(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            DrawLabel(node);
            ImGui.SameLine(LabelColumnWidth);
            SetNextItemWidth(node);

            var current = GetToken(path);
            var preview = FindOptionLabel(node, current) ?? FormatToken(current);

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.BeginCombo("##value", preview))
            {
                var options = node.SafeOptions;
                for (var i = 0; i < options.Count; i++)
                {
                    var option = options[i];
                    var value = option.Value ?? JValue.CreateNull();
                    var selected = JToken.DeepEquals(current, value);
                    if (ImGui.Selectable(option.Label ?? FormatToken(value), selected))
                        _document.SetValue(path, value);

                    if (selected)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawProgress(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            DrawLabel(node);
            ImGui.SameLine(LabelColumnWidth);
            var value = _document.GetValue(path, 0f);
            var min = node.Min ?? 0f;
            var max = node.Max ?? 1f;
            var range = Math.Max(float.Epsilon, max - min);
            var normalized = Math.Max(0f, Math.Min(1f, (value - min) / range));
            ImGui.ProgressBar(normalized, new System.Numerics.Vector2(Math.Max(MinimumFieldWidth, ImGui.GetContentRegionAvail().X), 0f));
        }

        private void DrawAutoField(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            var token = GetToken(path);
            if (token == null || token.Type == JTokenType.Null)
            {
                DrawLabel(node);
                ImGui.SameLine(LabelColumnWidth);
                ImGui.TextDisabled("null");
                return;
            }

            switch (token.Type)
            {
                case JTokenType.Boolean:
                    DrawToggle(node);
                    break;
                case JTokenType.Integer:
                    DrawIntegerField(node);
                    break;
                case JTokenType.Float:
                    DrawFloatField(node);
                    break;
                default:
                    DrawStringField(node);
                    break;
            }
        }

        private void DrawChildren(JsonUiNode node)
        {
            var children = node.SafeChildren;
            for (var i = 0; i < children.Count; i++)
                DrawNode(children[i]);
        }

        private bool IsNodeVisible(JsonUiNode node)
        {
            if (node.Visible == false)
                return false;

            return node.VisibleWhen == null || node.VisibleWhen.Evaluate(_document);
        }

        private bool IsNodeEnabled(JsonUiNode node)
        {
            if (node.Enabled == false)
                return false;

            return node.EnabledWhen == null || node.EnabledWhen.Evaluate(_document);
        }

        private JsonUiCommandContext CreateCommandContext(JsonUiNode node)
        {
            return new JsonUiCommandContext(_document, node, node.Action, TryGetPath(node, out var path) ? GetToken(path) : null, node.Payload);
        }

        private bool TryGetCommand(string action, out JsonUiCommand command)
        {
            return Commands.TryGet(action, out command);
        }

        private void DrawLabel(JsonUiNode node)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(node.Label ?? node.Path ?? string.Empty);
        }

        private void DrawTooltip(JsonUiNode node)
        {
            if (!string.IsNullOrEmpty(node.Tooltip) && ImGui.IsItemHovered())
                ImGui.SetTooltip(node.Tooltip);
        }

        private void SetNextItemWidth(JsonUiNode node)
        {
            var width = node.Width ?? ImGui.GetContentRegionAvail().X;
            ImGui.SetNextItemWidth(Math.Max(MinimumFieldWidth, width));
        }

        private JToken GetToken(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : _document.GetToken(path);
        }

        private static bool TryGetPath(JsonUiNode node, out string path)
        {
            path = string.IsNullOrWhiteSpace(node.Path) ? null : JsonPath.Normalize(node.Path);
            return path != null;
        }

        private static string FindOptionLabel(JsonUiNode node, JToken current)
        {
            var options = node.SafeOptions;
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (JToken.DeepEquals(current, option.Value ?? JValue.CreateNull()))
                    return option.Label;
            }

            return null;
        }

        private static string FormatToken(JToken token)
        {
            if (token == null)
                return string.Empty;

            if (token.Type == JTokenType.Null)
                return "null";

            return token is JValue value
                ? Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty
                : token.ToString(Newtonsoft.Json.Formatting.None);
        }

        private string GetBuffer(string path, string currentValue)
        {
            currentValue ??= string.Empty;

            if (!_textBuffers.TryGetValue(path, out var buffer) || buffer != currentValue)
            {
                _textBuffers[path] = currentValue;
                return currentValue;
            }

            return buffer ?? string.Empty;
        }

        private string GetNodeId(JsonUiNode node)
        {
            return node.Id ?? node.Path ?? node.Action ?? node.Label ?? node.Type ?? "node";
        }

        private void ApplyScrollWheelAcceleration()
        {
            if (ScrollWheelPixelsPerStep <= 0f || ImGui.GetScrollMaxY() <= 0f || !ImGui.IsWindowHovered())
                return;

            var wheel = ImGui.GetIO().MouseWheel;
            if (Math.Abs(wheel) <= float.Epsilon)
                return;

            var nextScrollY = ImGui.GetScrollY() - wheel * ScrollWheelPixelsPerStep;
            nextScrollY = Math.Max(0f, Math.Min(ImGui.GetScrollMaxY(), nextScrollY));
            ImGui.SetScrollY(nextScrollY);
        }
    }
}
#endif
