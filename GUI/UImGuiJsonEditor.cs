#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using System.Globalization;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI.GUI
{
    public sealed class UImGuiJsonEditor
    {
        private readonly struct RowLayout
        {
            public readonly float TypeColumnX;
            public readonly float ValueColumnX;

            public RowLayout(float typeColumnX, float valueColumnX)
            {
                TypeColumnX = typeColumnX;
                ValueColumnX = valueColumnX;
            }
        }

        private const float TypeColumnOffset = 140f;
        private const float ValueColumnOffset = 230f;
        private const float MinimumValueWidth = 80f;
        private const float DefaultScrollWheelPixelsPerStep = 280f;

        private readonly JsonDocumentModel _document;
        private readonly Dictionary<string, string> _textBuffers = new();
        private readonly List<JsonUiAction> _actionBuffer = new();

        public JsonUiActionCollection Actions { get; } = new();

        public string WindowTitle { get; set; }
        public bool ShowTypeHints { get; set; } = true;
        public float ScrollWheelPixelsPerStep { get; set; } = DefaultScrollWheelPixelsPerStep;

        public UImGuiJsonEditor(JsonDocumentModel document, string windowTitle = "Dingo JSON UI")
        {
            _document = document;
            WindowTitle = windowTitle;
        }

        public void Draw()
        {
            if (_document == null)
                return;

            if (!ImGui.Begin(WindowTitle))
            {
                ImGui.End();
                return;
            }

            ApplyScrollWheelAcceleration();
            DrawToolbar();
            ImGui.Separator();
            DrawToken(JsonPath.Root, "root", _document.RootToken, true);

            ImGui.End();
        }

        private void DrawToolbar()
        {
            if (ImGui.SmallButton("Copy JSON"))
                ImGui.SetClipboardText(_document.ToJson(Formatting.Indented));

            DrawActions(JsonPath.Root, _document.RootToken, JsonUiActionPlacement.Toolbar, sameLine: true);

            ImGui.SameLine();
            var rootToken = _document.RootToken;
            var typeName = rootToken?.Type.ToString() ?? "Null";
            ImGui.TextDisabled($"root: {typeName}");
        }

        private void DrawToken(string path, string label, JToken token, bool defaultOpen = false)
        {
            if (token == null)
            {
                var layout = CalculateRowLayout();
                DrawLeafLabel(label, JTokenType.Null, layout);
                ImGui.SameLine();
                ImGui.TextDisabled("null");
                return;
            }

            switch (token.Type)
            {
                case JTokenType.Object:
                    DrawObject(path, label, (JObject)token, defaultOpen);
                    break;
                case JTokenType.Array:
                    DrawArray(path, label, (JArray)token, defaultOpen);
                    break;
                default:
                    DrawValue(path, label, (JValue)token);
                    break;
            }
        }

        private void DrawObject(string path, string label, JObject token, bool defaultOpen)
        {
            var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

            var isOpen = ImGui.TreeNodeEx($"{label}##{path}", flags, $"{label}  {{{token.Count}}}");
            DrawActions(path, token, JsonUiActionPlacement.Inline, sameLine: true);

            if (!isOpen)
                return;

            foreach (var property in token.Properties())
                DrawToken(JsonPath.BuildPropertyPath(path, property.Name), property.Name, property.Value);

            ImGui.TreePop();
        }

        private void DrawArray(string path, string label, JArray token, bool defaultOpen)
        {
            var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

            var isOpen = ImGui.TreeNodeEx($"{label}##{path}", flags, $"{label}  [{token.Count}]");
            DrawActions(path, token, JsonUiActionPlacement.Inline, sameLine: true);

            if (!isOpen)
                return;

            for (var i = 0; i < token.Count; i++)
                DrawToken(JsonPath.BuildIndexPath(path, i), $"[{i}]", token[i]);

            ImGui.TreePop();
        }

        private void DrawValue(string path, string label, JValue value)
        {
            var layout = CalculateRowLayout();
            DrawLeafLabel(label, value.Type, layout);
            ImGui.SameLine(layout.ValueColumnX);
            ImGui.PushID(path);

            var hasInlineActions = PrepareActions(path, value, JsonUiActionPlacement.Inline);
            var actionWidth = hasInlineActions ? CalculatePreparedActionsWidth() : 0f;

            switch (value.Type)
            {
                case JTokenType.Boolean:
                    var boolValue = value.Value<bool>();
                    if (ImGui.Checkbox("##value", ref boolValue))
                        _document.SetValue(path, new JValue(boolValue));
                    break;

                case JTokenType.Integer:
                    SetNextValueWidth(actionWidth);
                    DrawIntegerField(path, value);
                    break;

                case JTokenType.Float:
                    SetNextValueWidth(actionWidth);
                    DrawFloatField(path, value);
                    break;

                case JTokenType.Null:
                    ImGui.TextDisabled("null");
                    break;

                default:
                    SetNextValueWidth(actionWidth);
                    DrawStringField(path, value.Value<string>() ?? string.Empty);
                    break;
            }

            DrawPreparedActions(path, value, JsonUiActionPlacement.Inline, sameLine: true);
            ImGui.PopID();
        }

        private bool DrawActions(string path, JToken token, JsonUiActionPlacement placement, bool sameLine)
        {
            PrepareActions(path, token, placement);
            return DrawPreparedActions(path, token, placement, sameLine);
        }

        private bool PrepareActions(string path, JToken token, JsonUiActionPlacement placement)
        {
            _actionBuffer.Clear();
            Actions.Fill(path, placement, _actionBuffer, _document, token);
            return _actionBuffer.Count > 0;
        }

        private bool DrawPreparedActions(string path, JToken token, JsonUiActionPlacement placement, bool sameLine)
        {
            if (_actionBuffer.Count == 0)
                return false;

            if (sameLine)
                ImGui.SameLine();

            ImGui.PushID($"actions:{path}:{placement}");
            for (var i = 0; i < _actionBuffer.Count; i++)
            {
                var action = _actionBuffer[i];
                var context = new JsonUiActionContext(_document, path, token, action);
                DrawActionButton(action, context, i);
            }

            ImGui.PopID();
            return true;
        }

        private float CalculatePreparedActionsWidth()
        {
            if (_actionBuffer.Count == 0)
                return 0f;

            var style = ImGui.GetStyle();
            var width = style.ItemSpacing.X * _actionBuffer.Count;

            for (var i = 0; i < _actionBuffer.Count; i++)
            {
                var labelSize = ImGui.CalcTextSize(_actionBuffer[i].Label);
                width += labelSize.X + style.FramePadding.X * 2f;
            }

            return width;
        }

        private void SetNextValueWidth(float reservedActionWidth)
        {
            var availableWidth = ImGui.GetContentRegionAvail().X - reservedActionWidth;
            ImGui.SetNextItemWidth(Math.Max(MinimumValueWidth, availableWidth));
        }

        private void DrawActionButton(JsonUiAction action, JsonUiActionContext context, int index)
        {
            if (index > 0)
                ImGui.SameLine();

            var enabled = action.CanExecute(context);
            if (!enabled)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);

            var clicked = ImGui.SmallButton($"{action.Label}##{index}");

            if (!enabled)
                ImGui.PopStyleVar();

            if (!string.IsNullOrEmpty(action.Tooltip) && ImGui.IsItemHovered())
                ImGui.SetTooltip(action.Tooltip);

            if (clicked && enabled)
                action.Execute(context);
        }

        private RowLayout CalculateRowLayout()
        {
            var rowStart = ImGui.GetCursorPosX();
            var rowWidth = ImGui.GetContentRegionAvail().X;
            var valueOffset = Math.Min(ValueColumnOffset, Math.Max(150f, rowWidth * 0.32f));
            var typeOffset = Math.Min(TypeColumnOffset, Math.Max(90f, valueOffset - 80f));

            return new RowLayout(rowStart + typeOffset, rowStart + valueOffset);
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

        private void DrawLeafLabel(string label, JTokenType type, RowLayout layout)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);

            if (!ShowTypeHints)
                return;

            ImGui.SameLine(layout.TypeColumnX);
            ImGui.TextDisabled(type.ToString());
        }

        private void DrawStringField(string path, string currentValue)
        {
            var buffer = GetBuffer(path, currentValue);
            if (ImGui.InputText("##value", ref buffer, 2048))
            {
                _textBuffers[path] = buffer;
                _document.SetValue(path, new JValue(buffer));
                return;
            }

            _textBuffers[path] = buffer;
        }

        private void DrawIntegerField(string path, JValue value)
        {
            var currentValue = value.Value<long>().ToString(CultureInfo.InvariantCulture);
            var buffer = GetBuffer(path, currentValue);

            if (ImGui.InputText("##value", ref buffer, 64))
            {
                _textBuffers[path] = buffer;
                if (long.TryParse(buffer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    _document.SetValue(path, new JValue(parsed));
                return;
            }

            _textBuffers[path] = buffer;
        }

        private void DrawFloatField(string path, JValue value)
        {
            var currentValue = value.Value<double>().ToString("G", CultureInfo.InvariantCulture);
            var buffer = GetBuffer(path, currentValue);

            if (ImGui.InputText("##value", ref buffer, 64))
            {
                _textBuffers[path] = buffer;
                if (double.TryParse(buffer, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
                    _document.SetValue(path, new JValue(parsed));
                return;
            }

            _textBuffers[path] = buffer;
        }

        private string GetBuffer(string path, string currentValue)
        {
            if (!_textBuffers.TryGetValue(path, out var buffer) || buffer != currentValue)
            {
                _textBuffers[path] = currentValue;
                return currentValue;
            }

            return buffer;
        }
    }
}
#endif
