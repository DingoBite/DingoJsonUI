using System.Globalization;
using DingoJsonUI.GUI;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DingoJsonUI.Examples.Scripts
{
    public sealed class DingoJsonUISerializedObjectSample : MonoBehaviour
    {
        [SerializeField] private bool _drawInspector = true;
        [SerializeField] private bool _drawAppliedTarget = true;
        [SerializeField] private bool _autoApply = true;
        [SerializeField] private Vector2 _inspectorPosition = new(24f, 290f);
        [SerializeField] private Vector2 _inspectorSize = new(490f, 235f);
        [SerializeField] private Vector2 _targetPosition = new(530f, 290f);
        [SerializeField] private Vector2 _targetSize = new(500f, 210f);
        [SerializeField] private DingoJsonUISampleData.PlayerConfig _playerConfig = new();

        private UImGuiJsonSerializedObjectInspector<DingoJsonUISampleData.PlayerConfig> _playerInspector;
        private bool _initialized;

        private void OnEnable()
        {
            EnsureInitialized();
            UImGui.UImGuiUtility.Layout += OnLayout;
        }

        private void OnDisable()
        {
            UImGui.UImGuiUtility.Layout -= OnLayout;
            _playerInspector?.Dispose();
            _playerInspector = null;
            _initialized = false;
        }

        private void OnLayout(UImGui.UImGui uImGui)
        {
            EnsureInitialized();
            _playerInspector.AutoApplyOnChange = _autoApply;

            if (_drawInspector)
            {
                ImGui.SetNextWindowPos(ToImGuiVector(_inspectorPosition), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(ToImGuiVector(_inspectorSize), ImGuiCond.FirstUseEver);
                _playerInspector.Draw();
            }

            if (_drawAppliedTarget)
                DrawAppliedTarget();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            _playerInspector = new UImGuiJsonSerializedObjectInspector<DingoJsonUISampleData.PlayerConfig>(new JsonSerializedObject<DingoJsonUISampleData.PlayerConfig>(_playerConfig), "03 Serialized Object");
            _playerInspector.AutoApplyOnChange = _autoApply;
            RegisterButtons();
            _initialized = true;
        }

        private void RegisterButtons()
        {
            var actions = _playerInspector.Actions;

            actions.AddButton(JsonPath.Root, "Level Up", context =>
            {
                var current = context.Document.GetValue("$.level", 0);
                context.Document.SetValue("$.level", new JValue(current + 1));
            }, JsonUiActionPlacement.Toolbar).Tooltip = "Changes a [JsonProperty] C# property.";

            actions.AddButton("$.Stats.Health", "Max", context =>
            {
                var maxHealth = context.Document.GetValue("$.Stats.MaxHealth", 100);
                context.Document.SetValue(context.Path, new JValue(maxHealth));
            }).Tooltip = "Edits a nested serializable object field.";

            actions.AddButton("$.Stats.skillPoints", "+1", context =>
            {
                var current = context.Document.GetValue(context.Path, 0);
                context.Document.SetValue(context.Path, new JValue(current + 1));
            }).Tooltip = "Edits a [JsonProperty] property inside a nested object.";

            actions.AddButton("$.Loadout[0].Count", "+1", context =>
            {
                var current = context.Document.GetValue(context.Path, 0);
                context.Document.SetValue(context.Path, new JValue(current + 1));
            }).Tooltip = "Edits an array element field.";

            actions.AddButton("$.Tags[0]", "Pin", context => { context.Document.SetValue(context.Path, new JValue("featured")); }).Tooltip = "Edits a list element.";

            actions.AddButton("$.UserPreferences.NullableNote", "Note", context =>
            {
                var current = context.Token?.Type == JTokenType.Null ? string.Empty : context.Document.GetValue(context.Path, string.Empty);
                context.Document.SetValue(context.Path, new JValue(string.IsNullOrEmpty(current) ? "Applied through JsonSerializedObject" : null));
            }).Tooltip = "Toggles null/string in the serialized object inspector.";
        }

        private void DrawAppliedTarget()
        {
            ImGui.SetNextWindowPos(ToImGuiVector(_targetPosition), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(ToImGuiVector(_targetSize), ImGuiCond.FirstUseEver);

            if (!ImGui.Begin("03 Applied Target"))
            {
                ImGui.End();
                return;
            }

            ImGui.Checkbox("Auto Apply", ref _autoApply);

            if (ImGui.SmallButton("Reload From Target"))
                _playerInspector.ReloadFromTarget();

            ImGui.SameLine();
            if (ImGui.SmallButton("Apply To Target"))
                _playerInspector.ApplyDocumentToTarget();

            ImGui.Separator();
            ImGui.TextUnformatted($"displayName: {_playerConfig.DisplayName}");
            ImGui.TextUnformatted($"level: {_playerConfig.Level}");
            ImGui.TextUnformatted($"private note: {_playerConfig.PrivateInspectorNote}");
            ImGui.TextUnformatted($"runtime only: {_playerConfig.RuntimeOnly}");

            if (_playerConfig.Stats != null)
            {
                ImGui.TextUnformatted($"stats: hp {_playerConfig.Stats.Health}/{_playerConfig.Stats.MaxHealth}, speed {_playerConfig.Stats.Speed.ToString("G", CultureInfo.InvariantCulture)}, alive {_playerConfig.Stats.Alive}, skillPoints {_playerConfig.Stats.SkillPoints}");
            }

            if (_playerConfig.Loadout != null)
                ImGui.TextUnformatted($"loadout: {DingoJsonUISampleData.FormatLoadout(_playerConfig.Loadout)}");

            if (_playerConfig.Tags != null)
                ImGui.TextUnformatted($"tags: {string.Join(", ", _playerConfig.Tags)}");

            ImGui.TextUnformatted($"nullable note: {_playerConfig.UserPreferences?.NullableNote ?? "null"}");

            if (_playerInspector.LastApplyException != null)
                ImGui.TextColored(new System.Numerics.Vector4(1f, 0.35f, 0.25f, 1f), _playerInspector.LastApplyException.Message);

            ImGui.End();
        }

        private static System.Numerics.Vector2 ToImGuiVector(Vector2 value)
        {
            return new System.Numerics.Vector2(value.x, value.y);
        }
    }
}
