#if NEWTONSOFT_EXISTS
using System.Collections.Generic;
using System.Globalization;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI.UImGui
{
    public sealed class UImGuiJsonEditor
    {
        private readonly JsonDocumentModel _document;
        private readonly Dictionary<string, string> _textBuffers = new();

        public string WindowTitle { get; set; }
        public bool ShowTypeHints { get; set; } = true;

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

            DrawToolbar();
            ImGui.Separator();
            DrawToken(JsonPath.Root, "root", _document.RootToken, true);

            ImGui.End();
        }

        private void DrawToolbar()
        {
            if (ImGui.SmallButton("Copy JSON"))
                ImGui.SetClipboardText(_document.ToJson(Formatting.Indented));

            ImGui.SameLine();
            var rootToken = _document.RootToken;
            var typeName = rootToken?.Type.ToString() ?? "Null";
            ImGui.TextDisabled($"root: {typeName}");
        }

        private void DrawToken(string path, string label, JToken token, bool defaultOpen = false)
        {
            if (token == null)
            {
                DrawLeafLabel(label, JTokenType.Null);
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

            if (!ImGui.TreeNodeEx($"{label}##{path}", flags, $"{label}  {{{token.Count}}}"))
                return;

            foreach (var property in token.Properties())
                DrawToken(JsonPath.BuildPropertyPath(path, property.Name), property.Name, property.Value);

            ImGui.TreePop();
        }

        private void DrawArray(string path, string label, JArray token, bool defaultOpen)
        {
            var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

            if (!ImGui.TreeNodeEx($"{label}##{path}", flags, $"{label}  [{token.Count}]"))
                return;

            for (var i = 0; i < token.Count; i++)
                DrawToken(JsonPath.BuildIndexPath(path, i), $"[{i}]", token[i]);

            ImGui.TreePop();
        }

        private void DrawValue(string path, string label, JValue value)
        {
            DrawLeafLabel(label, value.Type);
            ImGui.SameLine(260f);
            ImGui.PushID(path);

            switch (value.Type)
            {
                case JTokenType.Boolean:
                    var boolValue = value.Value<bool>();
                    if (ImGui.Checkbox("##value", ref boolValue))
                        _document.SetValue(path, new JValue(boolValue));
                    break;

                case JTokenType.Integer:
                    DrawIntegerField(path, value);
                    break;

                case JTokenType.Float:
                    DrawFloatField(path, value);
                    break;

                case JTokenType.Null:
                    ImGui.TextDisabled("null");
                    break;

                default:
                    DrawStringField(path, value.Value<string>() ?? string.Empty);
                    break;
            }

            ImGui.PopID();
        }

        private void DrawLeafLabel(string label, JTokenType type)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);

            if (!ShowTypeHints)
                return;

            ImGui.SameLine(180f);
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
