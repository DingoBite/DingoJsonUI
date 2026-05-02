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
        private readonly HashSet<string> _dirtyTextBuffers = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _pageOffsets = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ObjectPropertyCache> _objectPropertyCaches = new(StringComparer.Ordinal);
        private readonly List<JsonUiAction> _actionBuffer = new();

        public JsonUiActionCollection Actions { get; } = new();

        public string WindowTitle { get; set; }
        public bool ShowTypeHints { get; set; } = true;
        public float ScrollWheelPixelsPerStep { get; set; } = DefaultScrollWheelPixelsPerStep;
        public bool EnableLargeDataPaging { get; set; } = true;
        public int MaxVisibleChildrenPerNode { get; set; } = JsonUiLargeData.DefaultMaxVisibleChildrenPerNode;
        public int MaxRenderDepth { get; set; } = JsonUiLargeData.DefaultMaxRenderDepth;

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
            DrawToken(JsonPath.Root, "root", _document.RootToken, true, 0);

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

        private void DrawToken(string path, string label, JToken token, bool defaultOpen = false, int depth = 0)
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
                    DrawObject(path, label, (JObject)token, defaultOpen, depth);
                    break;
                case JTokenType.Array:
                    DrawArray(path, label, (JArray)token, defaultOpen, depth);
                    break;
                default:
                    DrawValue(path, label, (JValue)token);
                    break;
            }
        }

        private void DrawObject(string path, string label, JObject token, bool defaultOpen, int depth)
        {
            var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

            var isOpen = ImGui.TreeNodeEx($"{label}##{path}", flags, $"{label}  {{{token.Count}}}");
            DrawActions(path, token, JsonUiActionPlacement.Inline, sameLine: true);

            if (!isOpen)
                return;

            if (IsDepthLimitReached(depth))
            {
                DrawDepthLimitMessage();
                ImGui.TreePop();
                return;
            }

            var range = GetVisibleRange(path, token.Count);
            DrawPagingControls(path, range, "properties", "top");

            var properties = GetObjectPropertyNames(path, token);
            for (var i = range.Offset; i < range.EndExclusive && i < properties.Length; i++)
            {
                var propertyName = properties[i];
                DrawToken(JsonPath.BuildPropertyPath(path, propertyName), propertyName, token[propertyName], false, depth + 1);
            }

            DrawPagingControls(path, range, "properties", "bottom");

            ImGui.TreePop();
        }

        private void DrawArray(string path, string label, JArray token, bool defaultOpen, int depth)
        {
            var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

            var isOpen = ImGui.TreeNodeEx($"{label}##{path}", flags, $"{label}  [{token.Count}]");
            DrawActions(path, token, JsonUiActionPlacement.Inline, sameLine: true);

            if (!isOpen)
                return;

            if (IsDepthLimitReached(depth))
            {
                DrawDepthLimitMessage();
                ImGui.TreePop();
                return;
            }

            var range = GetVisibleRange(path, token.Count);
            DrawPagingControls(path, range, "items", "top");

            for (var i = range.Offset; i < range.EndExclusive; i++)
                DrawToken(JsonPath.BuildIndexPath(path, i), $"[{i}]", token[i], false, depth + 1);

            DrawPagingControls(path, range, "items", "bottom");

            ImGui.TreePop();
        }

        private JsonUiVisibleRange GetVisibleRange(string path, int totalCount)
        {
            if (!EnableLargeDataPaging)
                return JsonUiLargeData.CalculateVisibleRange(totalCount, 0, 0);

            _pageOffsets.TryGetValue(path, out var offset);
            var range = JsonUiLargeData.CalculateVisibleRange(totalCount, offset, MaxVisibleChildrenPerNode);
            if (range.Offset != offset)
                _pageOffsets[path] = range.Offset;

            return range;
        }

        private string[] GetObjectPropertyNames(string path, JObject token)
        {
            var version = _document?.Version ?? 0;
            if (_objectPropertyCaches.TryGetValue(path, out var cache)
                && cache.DocumentVersion == version
                && cache.TotalCount == token.Count)
            {
                return cache.PropertyNames;
            }

            var propertyNames = new string[token.Count];
            var index = 0;
            foreach (var property in token.Properties())
                propertyNames[index++] = property.Name;

            _objectPropertyCaches[path] = new ObjectPropertyCache(version, token.Count, propertyNames);
            return propertyNames;
        }

        private void DrawPagingControls(string path, JsonUiVisibleRange range, string itemLabel, string placement)
        {
            if (!range.IsPaged)
                return;

            ImGui.PushID($"paging:{path}:{placement}");
            ImGui.TextDisabled($"{range.Offset + 1}-{range.EndExclusive} / {range.TotalCount} {itemLabel}");
            ImGui.SameLine();

            DrawPageButton("<<", range.HasPrevious, () => _pageOffsets[path] = 0);
            ImGui.SameLine();
            DrawPageButton("<", range.HasPrevious, () => _pageOffsets[path] = JsonUiLargeData.GetPreviousPageOffset(range, MaxVisibleChildrenPerNode));
            ImGui.SameLine();
            DrawPageButton(">", range.HasNext, () => _pageOffsets[path] = JsonUiLargeData.GetNextPageOffset(range, MaxVisibleChildrenPerNode));
            ImGui.SameLine();
            DrawPageButton(">>", range.HasNext, () => _pageOffsets[path] = JsonUiLargeData.GetLastPageOffset(range.TotalCount, MaxVisibleChildrenPerNode));
            ImGui.PopID();
        }

        private static void DrawPageButton(string label, bool enabled, Action onClick)
        {
            if (!enabled)
                ImGui.BeginDisabled();

            if (ImGui.SmallButton(label) && enabled)
                onClick?.Invoke();

            if (!enabled)
                ImGui.EndDisabled();
        }

        private bool IsDepthLimitReached(int depth)
        {
            return MaxRenderDepth >= 0 && depth >= MaxRenderDepth;
        }

        private void DrawDepthLimitMessage()
        {
            ImGui.TextDisabled($"max render depth reached ({MaxRenderDepth})");
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
                StoreEditedBuffer(path, buffer);
                _document.SetValue(path, new JValue(buffer));
                currentValue = buffer;
            }

            UpdateTextBufferState(path, buffer, currentValue);
        }

        private void DrawIntegerField(string path, JValue value)
        {
            var currentValue = value.Value<long>().ToString(CultureInfo.InvariantCulture);
            var buffer = GetBuffer(path, currentValue);

            if (ImGui.InputText("##value", ref buffer, 64))
            {
                StoreEditedBuffer(path, buffer);
                if (long.TryParse(buffer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    _document.SetValue(path, new JValue(parsed));
                    currentValue = parsed.ToString(CultureInfo.InvariantCulture);
                }
            }

            UpdateTextBufferState(path, buffer, currentValue);
        }

        private void DrawFloatField(string path, JValue value)
        {
            var currentValue = value.Value<double>().ToString("G", CultureInfo.InvariantCulture);
            var buffer = GetBuffer(path, currentValue);

            if (ImGui.InputText("##value", ref buffer, 64))
            {
                StoreEditedBuffer(path, buffer);
                if (double.TryParse(buffer, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
                {
                    _document.SetValue(path, new JValue(parsed));
                    currentValue = parsed.ToString("G", CultureInfo.InvariantCulture);
                }
            }

            UpdateTextBufferState(path, buffer, currentValue);
        }

        private string GetBuffer(string path, string currentValue)
        {
            currentValue ??= string.Empty;

            if (_dirtyTextBuffers.Contains(path) && _textBuffers.TryGetValue(path, out var dirtyBuffer))
                return dirtyBuffer ?? string.Empty;

            if (!_textBuffers.TryGetValue(path, out var buffer) || buffer != currentValue)
            {
                _textBuffers[path] = currentValue;
                return currentValue;
            }

            return buffer;
        }

        private void StoreEditedBuffer(string path, string buffer)
        {
            _textBuffers[path] = buffer ?? string.Empty;
            _dirtyTextBuffers.Add(path);
        }

        private void UpdateTextBufferState(string path, string buffer, string currentValue)
        {
            currentValue ??= string.Empty;

            if (ImGui.IsItemActive())
            {
                StoreEditedBuffer(path, buffer);
                return;
            }

            if (_dirtyTextBuffers.Remove(path))
                _textBuffers[path] = currentValue;
            else
                _textBuffers[path] = currentValue;
        }

        private readonly struct ObjectPropertyCache
        {
            public ObjectPropertyCache(int documentVersion, int totalCount, string[] propertyNames)
            {
                DocumentVersion = documentVersion;
                TotalCount = totalCount;
                PropertyNames = propertyNames ?? Array.Empty<string>();
            }

            public int DocumentVersion { get; }
            public int TotalCount { get; }
            public string[] PropertyNames { get; }
        }
    }
}
#endif
