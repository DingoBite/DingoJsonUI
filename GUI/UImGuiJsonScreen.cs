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
        private const float DefaultLabelColumnWidth = 150f;
        private const float MinimumLabelColumnWidth = 72f;
        private const float MinimumFieldWidth = 80f;

        private readonly JsonDocumentModel _document;
        private readonly Dictionary<string, string> _textBuffers = new();
        private readonly HashSet<string> _dirtyTextBuffers = new(StringComparer.Ordinal);
        private readonly Stack<JsonUiLayoutScope> _layoutScopes = new();
        private readonly Stack<string> _pathScopes = new();
        private readonly HashSet<string> _templateStack = new(StringComparer.Ordinal);

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

            if (JsonUiSchema.IsTemplateReference(node))
            {
                DrawTemplateReference(node);
                return;
            }

            ImGui.PushID(GetNodeId(node));
            PushLayoutScope(node);
            try
            {
                switch (NormalizeNodeType(node.Type))
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
                    case JsonUiNodeType.List:
                        DrawList(node);
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
                    case JsonUiNodeType.InputTextMultiline:
                        DrawMultilineStringField(node);
                        break;
                    case JsonUiNodeType.Integer:
                        DrawIntegerField(node);
                        break;
                    case JsonUiNodeType.Float:
                        DrawFloatField(node);
                        break;
                    case JsonUiNodeType.DragInt:
                        DrawDragInt(node);
                        break;
                    case JsonUiNodeType.DragFloat:
                        DrawDragFloat(node);
                        break;
                    case JsonUiNodeType.SliderInt:
                        DrawIntSlider(node);
                        break;
                    case JsonUiNodeType.SliderFloat:
                        DrawFloatSlider(node);
                        break;
                    case JsonUiNodeType.Vector2:
                        DrawVector2(node);
                        break;
                    case JsonUiNodeType.Vector3:
                        DrawVector3(node);
                        break;
                    case JsonUiNodeType.Color:
                        DrawColor(node);
                        break;
                    case JsonUiNodeType.Select:
                        DrawSelect(node);
                        break;
                    case JsonUiNodeType.Radio:
                        DrawRadio(node);
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
            }
            finally
            {
                PopLayoutScope();
                ImGui.PopID();
            }
        }

        private void DrawTemplateReference(JsonUiNode node)
        {
            var templateName = JsonUiSchema.GetTemplateName(node);
            if (string.IsNullOrWhiteSpace(templateName))
            {
                DrawTemplateError("missing template name");
                return;
            }

            if (!_templateStack.Add(templateName))
            {
                DrawTemplateError($"recursive template: {templateName}");
                return;
            }

            try
            {
                if (Schema == null || !Schema.TryCreateTemplateInstance(node, out var templateInstance))
                {
                    DrawTemplateError($"missing template: {templateName}");
                    return;
                }

                DrawNode(templateInstance);
            }
            finally
            {
                _templateStack.Remove(templateName);
            }
        }

        private static void DrawTemplateError(string message)
        {
            ImGui.TextDisabled($"[template {message}]");
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
            var wrap = node.Wrap != false;
            var spacing = CurrentLayout.Spacing ?? ImGui.GetStyle().ItemSpacing.X;
            for (var i = 0; i < children.Count; i++)
            {
                if (i > 0)
                {
                    var nextWidth = EstimateNodeWidth(children[i]);
                    if (!wrap || CanFitSameLine(nextWidth, spacing))
                        ImGui.SameLine(0f, spacing);
                }

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

        private void DrawList(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            var token = GetToken(path);
            var array = token as JArray;
            var label = GetLabelText(node);
            if (!string.IsNullOrWhiteSpace(label))
            {
                var countText = array != null ? array.Count.ToString(CultureInfo.InvariantCulture) : "not array";
                ImGui.TextUnformatted($"{label} [{countText}]");
            }

            var enabled = IsNodeEnabled(node);
            if (!enabled)
                ImGui.BeginDisabled();

            try
            {
                if (array == null)
                {
                    DrawMissingList(path, token);
                    return;
                }

                DrawListToolbar(node, path, array);

                if (array.Count == 0)
                {
                    ImGui.TextDisabled(string.IsNullOrWhiteSpace(node.EmptyText) ? "empty" : node.EmptyText);
                    return;
                }

                var flags = node.DefaultOpen == true ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
                flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

                for (var i = 0; i < array.Count; i++)
                {
                    if (DrawListItem(node, path, array, i, flags))
                        return;
                }
            }
            finally
            {
                if (!enabled)
                    ImGui.EndDisabled();
            }
        }

        private void DrawMissingList(string path, JToken token)
        {
            ImGui.TextDisabled(token == null ? "missing array" : $"expected array, got {token.Type}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Create Array"))
                _document.SetValue(path, new JArray());
        }

        private void DrawListToolbar(JsonUiNode node, string path, JArray array)
        {
            if (ImGui.SmallButton(string.IsNullOrWhiteSpace(node.AddLabel) ? "Add" : node.AddLabel))
                AddListItem(path, node);

            if (array.Count <= 0)
                return;

            ImGui.SameLine();
            if (ImGui.SmallButton("Clear"))
                _document.SetValue(path, new JArray());
        }

        private bool DrawListItem(JsonUiNode node, string path, JArray array, int index, ImGuiTreeNodeFlags flags)
        {
            ImGui.PushID(index);
            var open = false;
            var changed = false;
            try
            {
                var title = FitTextToWidth(GetListItemLabel(node, array[index], index), Math.Max(80f, ImGui.GetContentRegionAvail().X - 260f));
                open = ImGui.TreeNodeEx($"{title}##item", flags);
                ImGui.SameLine();

                if (index <= 0)
                    ImGui.BeginDisabled();
                if (ImGui.SmallButton("Up"))
                    changed = MoveListItem(path, array, index, index - 1);
                if (index <= 0)
                    ImGui.EndDisabled();

                ImGui.SameLine();
                if (index >= array.Count - 1)
                    ImGui.BeginDisabled();
                if (ImGui.SmallButton("Down"))
                    changed = MoveListItem(path, array, index, index + 1);
                if (index >= array.Count - 1)
                    ImGui.EndDisabled();

                ImGui.SameLine();
                if (ImGui.SmallButton("Duplicate"))
                    changed = DuplicateListItem(path, array, index);

                ImGui.SameLine();
                if (ImGui.SmallButton("Remove"))
                    changed = RemoveListItem(path, array, index);

                if (open)
                {
                    if (!changed)
                        DrawListItemContent(node, JsonPath.BuildIndexPath(path, index), array[index]);

                    ImGui.TreePop();
                }
            }
            finally
            {
                ImGui.PopID();
            }

            return changed;
        }

        private void DrawListItemContent(JsonUiNode node, string itemPath, JToken item)
        {
            var children = node.SafeChildren;
            if (children.Count == 0)
            {
                ImGui.TextWrapped(FormatToken(item));
                return;
            }

            _pathScopes.Push(itemPath);
            try
            {
                for (var i = 0; i < children.Count; i++)
                    DrawNode(children[i]);
            }
            finally
            {
                _pathScopes.Pop();
            }
        }

        private void AddListItem(string path, JsonUiNode node)
        {
            var array = GetToken(path) as JArray ?? new JArray();
            array.Add(CreateListItem(node));
            _document.SetValue(path, array);
        }

        private bool MoveListItem(string path, JArray array, int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= array.Count || toIndex < 0 || toIndex >= array.Count || fromIndex == toIndex)
                return false;

            var item = array[fromIndex];
            array.RemoveAt(fromIndex);
            array.Insert(toIndex, item);
            _document.SetValue(path, array);
            return true;
        }

        private bool DuplicateListItem(string path, JArray array, int index)
        {
            if (index < 0 || index >= array.Count)
                return false;

            array.Insert(index + 1, array[index]?.DeepClone() ?? JValue.CreateNull());
            _document.SetValue(path, array);
            return true;
        }

        private bool RemoveListItem(string path, JArray array, int index)
        {
            if (index < 0 || index >= array.Count)
                return false;

            array.RemoveAt(index);
            _document.SetValue(path, array);
            return true;
        }

        private JToken CreateListItem(JsonUiNode node)
        {
            if (node.ItemTemplate != null)
                return node.ItemTemplate.DeepClone();

            var model = new JsonDocumentModel(new JObject());
            var wroteDefault = false;
            AddDefaultListValues(model, node.SafeChildren, ref wroteDefault);
            return wroteDefault ? model.RootToken.DeepClone() : new JObject();
        }

        private void AddDefaultListValues(JsonDocumentModel model, IReadOnlyList<JsonUiNode> children, ref bool wroteDefault)
        {
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null || JsonUiSchema.IsTemplateReference(child))
                    continue;

                var type = NormalizeNodeType(child.Type);
                if (IsRelativeListPath(child.Path) && TryCreateDefaultValue(child, type, out var defaultValue))
                {
                    model.SetValue(child.Path, defaultValue);
                    wroteDefault = true;
                }

                if (type != JsonUiNodeType.List)
                    AddDefaultListValues(model, child.SafeChildren, ref wroteDefault);
            }
        }

        private static bool TryCreateDefaultValue(JsonUiNode node, string type, out JToken value)
        {
            switch (type)
            {
                case JsonUiNodeType.InputText:
                case JsonUiNodeType.InputTextMultiline:
                    value = new JValue(string.Empty);
                    return true;
                case JsonUiNodeType.Integer:
                case JsonUiNodeType.DragInt:
                case JsonUiNodeType.SliderInt:
                    value = new JValue((int)(node.Min ?? 0f));
                    return true;
                case JsonUiNodeType.Float:
                case JsonUiNodeType.DragFloat:
                case JsonUiNodeType.SliderFloat:
                case JsonUiNodeType.Progress:
                    value = new JValue(node.Min ?? 0f);
                    return true;
                case JsonUiNodeType.Toggle:
                    value = new JValue(false);
                    return true;
                case JsonUiNodeType.Vector2:
                    value = ToArray(0f, 0f);
                    return true;
                case JsonUiNodeType.Vector3:
                    value = ToArray(0f, 0f, 0f);
                    return true;
                case JsonUiNodeType.Color:
                    value = ToArray(1f, 1f, 1f, 1f);
                    return true;
                case JsonUiNodeType.Select:
                case JsonUiNodeType.Radio:
                    value = node.SafeOptions.Count > 0 ? node.SafeOptions[0].Value?.DeepClone() ?? JValue.CreateNull() : JValue.CreateNull();
                    return true;
                case JsonUiNodeType.List:
                    value = new JArray();
                    return true;
                case JsonUiNodeType.Field:
                    value = JValue.CreateNull();
                    return true;
                default:
                    value = null;
                    return false;
            }
        }

        private static bool IsRelativeListPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            path = path.Trim();
            return !path.StartsWith(JsonPath.Root, StringComparison.Ordinal) && path[0] != '[';
        }

        private string GetListItemLabel(JsonUiNode node, JToken item, int index)
        {
            var itemLabel = TryGetListItemLabelFromPath(item, node.ItemLabelPath)
                            ?? TryGetObjectLabel(item, "label")
                            ?? TryGetObjectLabel(item, "name")
                            ?? TryGetObjectLabel(item, "id");

            return string.IsNullOrWhiteSpace(itemLabel)
                ? $"Item {index + 1}"
                : $"{index + 1}: {itemLabel}";
        }

        private static string TryGetListItemLabelFromPath(JToken item, string itemLabelPath)
        {
            if (item == null || string.IsNullOrWhiteSpace(itemLabelPath))
                return null;

            try
            {
                var token = item.SelectToken(JsonPath.Normalize(itemLabelPath), false);
                var label = FormatToken(token);
                return string.IsNullOrWhiteSpace(label) ? null : label;
            }
            catch
            {
                return null;
            }
        }

        private static string TryGetObjectLabel(JToken item, string propertyName)
        {
            if (item is not JObject obj || !obj.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out var value))
                return null;

            var label = FormatToken(value);
            return string.IsNullOrWhiteSpace(label) ? null : label;
        }

        private void DrawText(JsonUiNode node)
        {
            var text = node.Text;
            var hasPath = TryGetPath(node, out var path);
            if (string.IsNullOrEmpty(text) && hasPath)
                text = FormatToken(GetToken(path));

            if (!string.IsNullOrWhiteSpace(node.Label) && hasPath)
            {
                var width = BeginValueField(node, MinimumFieldWidth, false);
                ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + width);
                ImGui.TextUnformatted(text ?? string.Empty);
                ImGui.PopTextWrapPos();
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

            var size = GetButtonSize(node);
            var clicked = size.X > 0f || size.Y > 0f
                ? ImGui.Button(node.Label ?? node.Action ?? "Button", size)
                : ImGui.Button(node.Label ?? node.Action ?? "Button");

            if (!enabled)
                ImGui.PopStyleVar();

            if (clicked && enabled)
                command.Execute(context);
        }

        private void DrawToggle(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            BeginValueField(node, MinimumFieldWidth, false);
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

            BeginValueField(node);

            var current = _document.GetValue<string>(path) ?? string.Empty;
            var buffer = GetBuffer(path, current);

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.InputText("##value", ref buffer, 2048))
            {
                StoreEditedBuffer(path, buffer);
                _document.SetValue(path, new JValue(buffer));
                current = buffer;
            }

            UpdateTextBufferState(path, buffer, current);

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawMultilineStringField(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            var width = BeginValueField(node, MinimumFieldWidth, false);
            var height = Math.Max(ImGui.GetTextLineHeightWithSpacing() * 3f, node.Height ?? ImGui.GetTextLineHeightWithSpacing() * 4f);

            var current = _document.GetValue<string>(path) ?? string.Empty;
            var buffer = GetBuffer(path, current);

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.InputTextMultiline("##value", ref buffer, 8192, new System.Numerics.Vector2(width, height)))
            {
                StoreEditedBuffer(path, buffer);
                _document.SetValue(path, new JValue(buffer));
                current = buffer;
            }

            UpdateTextBufferState(path, buffer, current);

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawIntegerField(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            BeginValueField(node);

            var current = _document.GetValue(path, 0).ToString(CultureInfo.InvariantCulture);
            var buffer = GetBuffer(path, current);

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.InputText("##value", ref buffer, 64))
            {
                StoreEditedBuffer(path, buffer);
                if (int.TryParse(buffer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    _document.SetValue(path, new JValue(parsed));
                    current = parsed.ToString(CultureInfo.InvariantCulture);
                }
            }

            UpdateTextBufferState(path, buffer, current);

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawFloatField(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            BeginValueField(node);

            var current = _document.GetValue(path, 0f).ToString("G", CultureInfo.InvariantCulture);
            var buffer = GetBuffer(path, current);

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.InputText("##value", ref buffer, 64))
            {
                StoreEditedBuffer(path, buffer);
                if (float.TryParse(buffer, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
                {
                    _document.SetValue(path, new JValue(parsed));
                    current = parsed.ToString("G", CultureInfo.InvariantCulture);
                }
            }

            UpdateTextBufferState(path, buffer, current);

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawDragInt(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            BeginValueField(node);

            var value = _document.GetValue(path, 0);
            var speed = Math.Max(float.Epsilon, node.Step ?? 1f);
            var min = (int)(node.Min ?? 0f);
            var max = (int)(node.Max ?? 0f);

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.DragInt("##value", ref value, speed, min, max))
                _document.SetValue(path, new JValue(value));

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawDragFloat(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            BeginValueField(node);

            var value = _document.GetValue(path, 0f);
            var speed = Math.Max(float.Epsilon, node.Step ?? 0.01f);
            var min = node.Min ?? 0f;
            var max = node.Max ?? 0f;

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.DragFloat("##value", ref value, speed, min, max))
                _document.SetValue(path, new JValue(value));

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawIntSlider(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            BeginValueField(node);

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

            BeginValueField(node);

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

        private void DrawVector2(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            BeginValueField(node);

            var value = ReadVector2(GetToken(path));
            var speed = Math.Max(float.Epsilon, node.Step ?? 0.01f);
            var min = node.Min ?? 0f;
            var max = node.Max ?? 0f;

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.DragFloat2("##value", ref value, speed, min, max))
                _document.SetValue(path, ToArray(value.X, value.Y));

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawVector3(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            BeginValueField(node);

            var value = ReadVector3(GetToken(path));
            var speed = Math.Max(float.Epsilon, node.Step ?? 0.01f);
            var min = node.Min ?? 0f;
            var max = node.Max ?? 0f;

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.DragFloat3("##value", ref value, speed, min, max))
                _document.SetValue(path, ToArray(value.X, value.Y, value.Z));

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawColor(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            BeginValueField(node);

            var value = ReadColor(GetToken(path));

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            if (ImGui.ColorEdit4("##value", ref value))
                _document.SetValue(path, ToArray(value.X, value.Y, value.Z, value.W));

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawSelect(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            BeginValueField(node);

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

        private void DrawRadio(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            BeginValueField(node, MinimumFieldWidth, false);

            var current = GetToken(path);
            var options = node.SafeOptions;
            var wrap = node.Wrap != false;
            var spacing = CurrentLayout.Spacing ?? ImGui.GetStyle().ItemSpacing.X;

            if (!IsNodeEnabled(node))
                ImGui.BeginDisabled();

            for (var i = 0; i < options.Count; i++)
            {
                if (i > 0)
                {
                    var nextWidth = EstimateRadioOptionWidth(options[i]);
                    if (!wrap || CanFitSameLine(nextWidth, spacing))
                        ImGui.SameLine(0f, spacing);
                }

                var option = options[i];
                var value = option.Value ?? JValue.CreateNull();
                var selected = JToken.DeepEquals(current, value);
                if (ImGui.RadioButton(option.Label ?? FormatToken(value), selected))
                    _document.SetValue(path, value);
            }

            if (!IsNodeEnabled(node))
                ImGui.EndDisabled();
        }

        private void DrawProgress(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            var width = BeginValueField(node, MinimumFieldWidth, false);
            var value = _document.GetValue(path, 0f);
            var min = node.Min ?? 0f;
            var max = node.Max ?? 1f;
            var range = Math.Max(float.Epsilon, max - min);
            var normalized = Math.Max(0f, Math.Min(1f, (value - min) / range));
            ImGui.ProgressBar(normalized, new System.Numerics.Vector2(width, 0f));
        }

        private void DrawAutoField(JsonUiNode node)
        {
            if (!TryGetPath(node, out var path))
                return;

            var token = GetToken(path);
            if (token == null || token.Type == JTokenType.Null)
            {
                BeginValueField(node, MinimumFieldWidth, false);
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

            return node.VisibleWhen == null || EvaluateCondition(node.VisibleWhen);
        }

        private bool IsNodeEnabled(JsonUiNode node)
        {
            if (node.Enabled == false)
                return false;

            return node.EnabledWhen == null || EvaluateCondition(node.EnabledWhen);
        }

        private bool EvaluateCondition(JsonUiCondition condition)
        {
            if (condition == null)
                return true;

            var path = ResolvePath(condition.Path);
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var scopedCondition = new JsonUiCondition
            {
                Path = path,
                EqualValue = condition.EqualValue,
                NotEqualValue = condition.NotEqualValue,
                Exists = condition.Exists,
                Truthy = condition.Truthy,
                GreaterThan = condition.GreaterThan,
                GreaterThanOrEqual = condition.GreaterThanOrEqual,
                LessThan = condition.LessThan,
                LessThanOrEqual = condition.LessThanOrEqual,
                Not = condition.Not,
            };

            return scopedCondition.Evaluate(_document);
        }

        private JsonUiCommandContext CreateCommandContext(JsonUiNode node)
        {
            return new JsonUiCommandContext(_document, node, node.Action, TryGetPath(node, out var path) ? GetToken(path) : null, node.Payload);
        }

        private bool TryGetCommand(string action, out JsonUiCommand command)
        {
            return Commands.TryGet(action, out command);
        }

        private JsonUiLayoutScope CurrentLayout => _layoutScopes.Count > 0
            ? _layoutScopes.Peek()
            : new JsonUiLayoutScope(DefaultLabelColumnWidth, null, 0f, false);

        private void PushLayoutScope(JsonUiNode node)
        {
            var parent = CurrentLayout;
            var labelWidth = Math.Max(MinimumLabelColumnWidth, node.LabelWidth ?? parent.LabelWidth);
            var spacing = node.Spacing ?? parent.Spacing;
            var indent = Math.Max(0f, node.Indent ?? 0f);
            var pushedSpacing = node.Spacing.HasValue;

            if (pushedSpacing)
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(Math.Max(0f, node.Spacing.Value), Math.Max(0f, node.Spacing.Value)));

            if (indent > 0f)
                ImGui.Indent(indent);

            _layoutScopes.Push(new JsonUiLayoutScope(labelWidth, spacing, indent, pushedSpacing));
        }

        private void PopLayoutScope()
        {
            var scope = _layoutScopes.Pop();

            if (scope.Indent > 0f)
                ImGui.Unindent(scope.Indent);

            if (scope.PushedSpacing)
                ImGui.PopStyleVar();
        }

        private float BeginValueField(JsonUiNode node, float minimumWidth = MinimumFieldWidth, bool setNextItemWidth = true)
        {
            var available = Math.Max(1f, ImGui.GetContentRegionAvail().X);
            var label = GetLabelText(node);
            if (string.IsNullOrWhiteSpace(label))
            {
                var fullWidth = ResolveItemWidth(node, available, minimumWidth);
                if (setNextItemWidth)
                    ImGui.SetNextItemWidth(fullWidth);

                return fullWidth;
            }

            var style = ImGui.GetStyle();
            var lineStartX = ImGui.GetCursorPosX();
            var inline = available >= MinimumLabelColumnWidth + minimumWidth + style.ItemInnerSpacing.X;
            if (inline)
            {
                var labelWidth = ResolveLabelWidth(node, available, minimumWidth);
                DrawLabelText(label, Math.Max(1f, labelWidth - style.ItemInnerSpacing.X));
                ImGui.SameLine(lineStartX + labelWidth, style.ItemInnerSpacing.X);

                var fieldWidth = ResolveItemWidth(node, available - labelWidth - style.ItemInnerSpacing.X, minimumWidth);
                if (setNextItemWidth)
                    ImGui.SetNextItemWidth(fieldWidth);

                return fieldWidth;
            }

            DrawLabelText(label, available);
            var stackedWidth = ResolveItemWidth(node, ImGui.GetContentRegionAvail().X, minimumWidth);
            if (setNextItemWidth)
                ImGui.SetNextItemWidth(stackedWidth);

            return stackedWidth;
        }

        private void DrawLabel(JsonUiNode node)
        {
            DrawLabelText(GetLabelText(node), 0f);
        }

        private static void DrawLabelText(string label, float maxWidth)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(FitTextToWidth(label ?? string.Empty, maxWidth));
        }

        private void DrawTooltip(JsonUiNode node)
        {
            if (!string.IsNullOrEmpty(node.Tooltip) && ImGui.IsItemHovered())
                ImGui.SetTooltip(node.Tooltip);
        }

        private float ResolveLabelWidth(JsonUiNode node, float available, float minimumFieldWidth)
        {
            var style = ImGui.GetStyle();
            var desired = Math.Max(MinimumLabelColumnWidth, node.LabelWidth ?? CurrentLayout.LabelWidth);
            var max = Math.Max(MinimumLabelColumnWidth, available - minimumFieldWidth - style.ItemInnerSpacing.X);
            return Math.Min(desired, max);
        }

        private static float ResolveItemWidth(JsonUiNode node, float available, float minimumWidth)
        {
            var max = Math.Max(1f, available);
            var desired = node.Width ?? max;
            return Math.Max(1f, Math.Min(Math.Max(minimumWidth, desired), max));
        }

        private System.Numerics.Vector2 GetButtonSize(JsonUiNode node)
        {
            var width = node.Width.HasValue
                ? ResolveItemWidth(node, ImGui.GetContentRegionAvail().X, 1f)
                : 0f;

            return new System.Numerics.Vector2(width, node.Height ?? 0f);
        }

        private bool CanFitSameLine(float nextWidth, float spacing)
        {
            var itemRight = ImGui.GetItemRectMax().X;
            var contentRight = ImGui.GetWindowPos().X + ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X;
            return itemRight + spacing + Math.Max(0f, nextWidth) <= contentRight;
        }

        private float EstimateNodeWidth(JsonUiNode node)
        {
            if (node == null)
                return 0f;

            if (node.Width.HasValue)
                return node.Width.Value;

            var style = ImGui.GetStyle();
            switch (NormalizeNodeType(node.Type))
            {
                case JsonUiNodeType.Button:
                    return ImGui.CalcTextSize(node.Label ?? node.Action ?? "Button").X + style.FramePadding.X * 2f;
                case JsonUiNodeType.Text:
                    return ImGui.CalcTextSize(node.Text ?? GetLabelText(node)).X;
                case JsonUiNodeType.Separator:
                case JsonUiNodeType.Space:
                    return 0f;
                default:
                    return CurrentLayout.LabelWidth + MinimumFieldWidth + style.ItemInnerSpacing.X;
            }
        }

        private static float EstimateRadioOptionWidth(JsonUiOption option)
        {
            var label = option?.Label ?? FormatToken(option?.Value);
            var style = ImGui.GetStyle();
            return ImGui.GetFrameHeight() + style.ItemInnerSpacing.X + ImGui.CalcTextSize(label).X;
        }

        private static string GetLabelText(JsonUiNode node)
        {
            return node?.Label ?? node?.Path ?? string.Empty;
        }

        private static string FitTextToWidth(string text, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0f || ImGui.CalcTextSize(text).X <= maxWidth)
                return text ?? string.Empty;

            const string suffix = "...";
            var suffixWidth = ImGui.CalcTextSize(suffix).X;
            if (suffixWidth >= maxWidth)
                return suffix;

            for (var length = text.Length - 1; length > 0; length--)
            {
                var candidate = text.Substring(0, length);
                if (ImGui.CalcTextSize(candidate).X + suffixWidth <= maxWidth)
                    return candidate + suffix;
            }

            return suffix;
        }

        private static string NormalizeNodeType(string type)
        {
            return string.IsNullOrWhiteSpace(type) ? JsonUiNodeType.Section : type.Trim();
        }

        private JToken GetToken(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : _document.GetToken(path);
        }

        private bool TryGetPath(JsonUiNode node, out string path)
        {
            path = ResolvePath(node.Path);
            return path != null;
        }

        private string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Trim();
            if (_pathScopes.Count == 0 || path.StartsWith(JsonPath.Root, StringComparison.Ordinal))
                return JsonPath.Normalize(path);

            var scope = JsonPath.Normalize(_pathScopes.Peek());
            return path[0] switch
            {
                '[' or '.' => JsonPath.Normalize(scope + path),
                _ => JsonPath.Normalize(scope + "." + path),
            };
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

        private static System.Numerics.Vector2 ReadVector2(JToken token)
        {
            return new System.Numerics.Vector2(
                ReadComponent(token, 0, "x", 0f),
                ReadComponent(token, 1, "y", 0f));
        }

        private static System.Numerics.Vector3 ReadVector3(JToken token)
        {
            return new System.Numerics.Vector3(
                ReadComponent(token, 0, "x", 0f),
                ReadComponent(token, 1, "y", 0f),
                ReadComponent(token, 2, "z", 0f));
        }

        private static System.Numerics.Vector4 ReadColor(JToken token)
        {
            return new System.Numerics.Vector4(
                ReadComponent(token, 0, "r", 1f),
                ReadComponent(token, 1, "g", 1f),
                ReadComponent(token, 2, "b", 1f),
                ReadComponent(token, 3, "a", 1f));
        }

        private static float ReadComponent(JToken token, int index, string name, float fallback)
        {
            try
            {
                if (token is JArray array && index >= 0 && index < array.Count)
                    return array[index].Value<float>();

                if (token is JObject obj && obj.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out var value))
                    return value.Value<float>();
            }
            catch
            {
                return fallback;
            }

            return fallback;
        }

        private static JArray ToArray(params float[] values)
        {
            var array = new JArray();
            for (var i = 0; i < values.Length; i++)
                array.Add(values[i]);

            return array;
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

            return buffer ?? string.Empty;
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

        private readonly struct JsonUiLayoutScope
        {
            public JsonUiLayoutScope(float labelWidth, float? spacing, float indent, bool pushedSpacing)
            {
                LabelWidth = labelWidth;
                Spacing = spacing;
                Indent = indent;
                PushedSpacing = pushedSpacing;
            }

            public float LabelWidth { get; }
            public float? Spacing { get; }
            public float Indent { get; }
            public bool PushedSpacing { get; }
        }
    }
}
#endif
