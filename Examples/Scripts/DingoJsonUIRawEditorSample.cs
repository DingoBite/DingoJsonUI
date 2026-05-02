using System;
using System.Collections.Generic;
using DingoJsonUI.GUI;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DingoJsonUI.Examples.Scripts
{
    [DefaultExecutionOrder(-100)]
    public sealed class DingoJsonUIRawEditorSample : MonoBehaviour
    {
        [SerializeField] private bool _draw = true;
        [SerializeField] private string _windowTitle = "01 Raw JSON Editor";
        [SerializeField] private Vector2 _initialWindowPosition = new(24f, 18f);
        [SerializeField] private Vector2 _initialWindowSize = new(490f, 250f);
        [SerializeField] [Min(1)] private int _maxVisibleChildrenPerNode = 8;
        [SerializeField] [Min(0)] private int _maxRenderDepth = 16;

        private readonly List<JsonPathSubscription> _subscriptions = new();
        private JsonDocumentModel _document;
        private UImGuiJsonEditor _editor;
        private bool _initialized;
        private bool _recordingDebugChange;
        private int _changeSequence;

        public JsonDocumentModel Document
        {
            get
            {
                EnsureInitialized();
                return _document;
            }
        }

        public UImGuiJsonEditor Editor
        {
            get
            {
                EnsureInitialized();
                return _editor;
            }
        }

        private void OnEnable()
        {
            EnsureInitialized();
            UImGui.UImGuiUtility.Layout += OnLayout;
        }

        private void OnDisable()
        {
            UImGui.UImGuiUtility.Layout -= OnLayout;
            DisposeSubscriptions();
            _initialized = false;
        }

        [ContextMenu("Reload Sample JSON")]
        public void ReloadSampleJson()
        {
            EnsureDocumentCreated();
            _changeSequence = 0;
            DingoJsonUISampleData.LoadGameplayJson(_document);
            DingoJsonUISampleData.AddLargeDataSample(_document);
        }

        private void OnLayout(UImGui.UImGui uImGui)
        {
            if (!_draw)
                return;

            EnsureInitialized();
            ImGui.SetNextWindowPos(ToImGuiVector(_initialWindowPosition), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(ToImGuiVector(_initialWindowSize), ImGuiCond.FirstUseEver);
            _editor.Draw();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            EnsureDocumentCreated();
            ReloadSampleJson();
            RegisterButtons();
            RegisterSubscriptions();
            _initialized = true;
        }

        private void EnsureDocumentCreated()
        {
            _document ??= new JsonDocumentModel();
            _editor ??= UImGuiJsonUi.Editor(_document, _windowTitle, CreateOptions());
            _editor.WindowTitle = _windowTitle;
            UImGuiJsonUi.ApplyOptions(_editor, CreateOptions());
        }

        private JsonUiOptions CreateOptions()
        {
            return new JsonUiOptions
            {
                WindowTitle = _windowTitle,
                MaxVisibleChildrenPerNode = Math.Max(1, _maxVisibleChildrenPerNode),
                MaxRenderDepth = Math.Max(0, _maxRenderDepth),
            };
        }

        private void RegisterButtons()
        {
            var actions = _editor.Actions;
            actions.Clear();

            actions.AddButton(JsonPath.Root, "Reset JSON", _ => ReloadSampleJson(), JsonUiActionPlacement.Toolbar).Tooltip = "Reloads the sample JSON document.";
            actions.AddButton(JsonPath.Root, "Combat Preset", context =>
            {
                DingoJsonUISampleData.ApplyCombatPreset(context.Document);
            }, JsonUiActionPlacement.Toolbar).Tooltip = "Updates several nested values and triggers subscriptions.";

            actions.AddButton("$.player.profile.name", "Rename", context =>
            {
                var current = context.Document.GetValue(context.Path, "Dingo");
                var next = current == "Dingo" ? "Dingo Prime" : "Dingo";
                context.Document.SetValue(context.Path, new JValue(next));
            }).Tooltip = "Toggles a string field.";

            actions.AddButton("$.player.stats.hp", "Heal", context =>
            {
                DingoJsonUISampleData.HealPlayer(context.Document);
            }).Tooltip = "Sets player hp to maxHp.";

            var damageButton = actions.AddButton("$.player.stats.hp", "-25", context =>
            {
                DingoJsonUISampleData.DamagePlayer(context.Document, 25);
            });
            damageButton.Tooltip = "Subtracts 25 hp while hp is above zero.";
            damageButton.IsEnabled = context => context.Token?.Value<int>() > 0;

            var defeatButton = actions.AddButton("$.player.stats.alive", "Defeat", context => { context.Document.SetValue(context.Path, new JValue(false)); });
            defeatButton.Tooltip = "Visible only while alive is true.";
            defeatButton.IsVisible = context => context.Token?.Type == JTokenType.Boolean && context.Token.Value<bool>();

            var reviveButton = actions.AddButton("$.player.stats.alive", "Revive", context => { context.Document.SetValue(context.Path, new JValue(true)); });
            reviveButton.Tooltip = "Visible only while alive is false.";
            reviveButton.IsVisible = context => context.Token?.Type == JTokenType.Boolean && !context.Token.Value<bool>();

            actions.AddButton("$.inventory", "Add Item", context =>
            {
                DingoJsonUISampleData.AddGeneratedInventoryItem(context.Document);
            }).Tooltip = "Appends an object to the inventory array.";

            var usePotionButton = actions.AddButton("$.inventory[0].count", "Use", context =>
            {
                var current = context.Document.GetValue(context.Path, 0);
                context.Document.SetValue(context.Path, new JValue(Math.Max(0, current - 1)));
            });
            usePotionButton.Tooltip = "Disabled when count reaches zero.";
            usePotionButton.IsEnabled = context => context.Token?.Value<int>() > 0;

            actions.AddButton("$.quests[0].progress", "Complete", context =>
            {
                DingoJsonUISampleData.CompleteFirstQuest(context.Document);
            }).Tooltip = "Sets quest progress and completion fields.";

            actions.AddButton("$.settings.nullableNote", "Set Note", context =>
            {
                var current = context.Token?.Type == JTokenType.Null ? string.Empty : context.Document.GetValue(context.Path, string.Empty);
                context.Document.SetValue(context.Path, new JValue(string.IsNullOrEmpty(current) ? "Edited from inline action" : null));
            }).Tooltip = "Toggles a null/string value.";

            actions.AddButton("$['special keys']['dash-name']", "Upper", context =>
            {
                var current = context.Document.GetValue(context.Path, string.Empty);
                context.Document.SetValue(context.Path, new JValue(current.ToUpperInvariant()));
            }).Tooltip = "Shows quoted JSONPath segments for non-identifier keys.";

            actions.AddButton("$.debug.clicks", "+1", context =>
            {
                DingoJsonUISampleData.IncrementDebugClicks(context.Document);
            }).Tooltip = "Increments debug.clicks.";
        }

        private void RegisterSubscriptions()
        {
            DisposeSubscriptions();
            _subscriptions.Add(_document.Subscribe("$.player.stats.hp", change => { RecordDebugChange("$.debug.lastExactHpChange", change); }));
            _subscriptions.Add(_document.Subscribe("$.player.stats.*", change => { RecordDebugChange("$.debug.lastPlayerWildcard", change); }));
            _subscriptions.Add(_document.Subscribe("$.inventory[*]", change => { RecordDebugChange("$.debug.lastInventoryWildcard", change); }));
        }

        private void DisposeSubscriptions()
        {
            for (var i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();

            _subscriptions.Clear();
        }

        private void RecordDebugChange(string debugPath, JsonChange change)
        {
            if (_recordingDebugChange)
                return;

            _recordingDebugChange = true;
            try
            {
                _changeSequence++;
                _document.SetValue("$.debug.sequence", _changeSequence);
                _document.SetValue(debugPath, $"{change.Path}: {DingoJsonUISampleData.FormatToken(change.PreviousValue)} -> {DingoJsonUISampleData.FormatToken(change.CurrentValue)}");
            }
            finally
            {
                _recordingDebugChange = false;
            }
        }

        private static System.Numerics.Vector2 ToImGuiVector(Vector2 value)
        {
            return new System.Numerics.Vector2(value.x, value.y);
        }
    }
}
