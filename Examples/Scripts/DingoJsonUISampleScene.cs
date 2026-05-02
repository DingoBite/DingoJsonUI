using System;
using System.Collections.Generic;
using System.Globalization;
using DingoJsonUI.GUI;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DingoJsonUI.Examples.Scripts
{
    public sealed class DingoJsonUISampleScene : MonoBehaviour
    {
        [Serializable]
        public sealed class PlayerConfig
        {
            [Serializable]
            public sealed class RuntimeStats
            {
                public int Health = 45;
                public int MaxHealth = 120;
                public float Speed = 6.5f;
                public bool Alive = true;

                [JsonProperty("skillPoints")] public int SkillPoints { get; set; } = 2;
            }

            [Serializable]
            public sealed class InventorySlot
            {
                public string Id = "potion";
                public string Label = "Health Potion";
                public int Count = 2;
                public bool Equipped;
            }

            [Serializable]
            public sealed class Preferences
            {
                public bool ShowHints = true;
                public float MusicVolume = 0.75f;
                public string NullableNote = null;
            }

            public RuntimeStats Stats = new();

            public InventorySlot[] Loadout =
            {
                new() { Id = "potion", Label = "Health Potion", Count = 2, Equipped = false },
                new() { Id = "key", Label = "Copper Key", Count = 1, Equipped = true },
            };

            public List<string> Tags = new() { "serialized", "runtime", "json" };
            public Preferences UserPreferences = new();

            [SerializeField] [JsonProperty("displayName")]
            private string _displayName = "Inspector Dingo";

            [SerializeField] private string _privateInspectorNote = "Private SerializeField is visible";

            [JsonProperty("level")] public int Level { get; set; } = 3;

            [JsonIgnore] public string HiddenByJsonIgnore = "JsonIgnore hides this";
            [NonSerialized] public int RuntimeOnly = 99;

            public string DisplayName => _displayName;
            public string PrivateInspectorNote => _privateInspectorNote;
        }

        private const string SAMPLE_JSON = @"{
                                          ""player"": {
                                            ""profile"": {
                                              ""name"": ""Dingo"",
                                              ""title"": ""JSON Tamer"",
                                              ""nickname"": null
                                            },
                                            ""stats"": {
                                              ""hp"": 45,
                                              ""maxHp"": 120,
                                              ""speed"": 6.5,
                                              ""alive"": true,
                                              ""shield"": 12.5
                                            },
                                            ""flags"": {
                                              ""godMode"": false,
                                              ""poisoned"": false
                                            }
                                          },
                                          ""inventory"": [
                                            {
                                              ""id"": ""potion"",
                                              ""label"": ""Health Potion"",
                                              ""count"": 2,
                                              ""equipped"": false
                                            },
                                            {
                                              ""id"": ""key"",
                                              ""label"": ""Copper Key"",
                                              ""count"": 1,
                                              ""equipped"": true
                                            }
                                          ],
                                          ""quests"": [
                                            {
                                              ""name"": ""Find the relic"",
                                              ""progress"": 0.35,
                                              ""complete"": false
                                            },
                                            {
                                              ""name"": ""Open the gate"",
                                              ""progress"": 1.0,
                                              ""complete"": true
                                            }
                                          ],
                                          ""settings"": {
                                            ""difficulty"": ""Normal"",
                                            ""showHints"": true,
                                            ""musicVolume"": 0.75,
                                            ""nullableNote"": null
                                          },
                                          ""special keys"": {
                                            ""dash-name"": ""quoted path works""
                                          },
                                          ""debug"": {
                                            ""clicks"": 0,
                                            ""sequence"": 0,
                                            ""lastExactHpChange"": ""none"",
                                            ""lastPlayerWildcard"": ""none"",
                                            ""lastInventoryWildcard"": ""none"",
                                            ""lastAppliedPreset"": ""default""
                                          }
                                        }";

        private const string FAST_UI_SCHEMA = @"{
                                          ""title"": ""Dingo Fast UI"",
                                          ""root"": {
                                            ""type"": ""section"",
                                            ""children"": [
                                              {
                                                ""type"": ""row"",
                                                ""children"": [
                                                  { ""type"": ""button"", ""label"": ""Reset"", ""action"": ""resetJson"", ""tooltip"": ""Reload sample state."" },
                                                  { ""type"": ""button"", ""label"": ""Combat Preset"", ""action"": ""combatPreset"", ""tooltip"": ""Apply several gameplay changes."" },
                                                  { ""type"": ""button"", ""label"": ""+ Debug"", ""action"": ""debugClick"" }
                                                ]
                                              },
                                              { ""type"": ""separator"" },
                                              {
                                                ""type"": ""tabs"",
                                                ""children"": [
                                                  {
                                                    ""label"": ""Player"",
                                                    ""children"": [
                                                      { ""type"": ""inputText"", ""label"": ""Name"", ""path"": ""$.player.profile.name"" },
                                                      { ""type"": ""sliderInt"", ""label"": ""HP"", ""path"": ""$.player.stats.hp"", ""min"": 0, ""max"": 120 },
                                                      { ""type"": ""sliderFloat"", ""label"": ""Speed"", ""path"": ""$.player.stats.speed"", ""min"": 0, ""max"": 15 },
                                                      { ""type"": ""toggle"", ""label"": ""Alive"", ""path"": ""$.player.stats.alive"" },
                                                      {
                                                        ""type"": ""row"",
                                                        ""children"": [
                                                          { ""type"": ""button"", ""label"": ""Heal"", ""action"": ""healPlayer"" },
                                                          { ""type"": ""button"", ""label"": ""Damage"", ""action"": ""damagePlayer"", ""payload"": { ""amount"": 25 }, ""enabledWhen"": { ""path"": ""$.player.stats.hp"", ""gt"": 0 } },
                                                          { ""type"": ""button"", ""label"": ""Complete Quest"", ""action"": ""completeQuest"", ""enabledWhen"": { ""path"": ""$.quests[0].complete"", ""equals"": false } }
                                                        ]
                                                      }
                                                    ]
                                                  },
                                                  {
                                                    ""label"": ""Inventory"",
                                                    ""children"": [
                                                      { ""type"": ""field"", ""label"": ""Potion"", ""path"": ""$.inventory[0].label"" },
                                                      { ""type"": ""int"", ""label"": ""Potion Count"", ""path"": ""$.inventory[0].count"" },
                                                      { ""type"": ""toggle"", ""label"": ""Key Equipped"", ""path"": ""$.inventory[1].equipped"" },
                                                      { ""type"": ""button"", ""label"": ""Add Item"", ""action"": ""addItem"" }
                                                    ]
                                                  },
                                                  {
                                                    ""label"": ""Settings"",
                                                    ""children"": [
                                                      {
                                                        ""type"": ""select"",
                                                        ""label"": ""Difficulty"",
                                                        ""path"": ""$.settings.difficulty"",
                                                        ""options"": [
                                                          { ""label"": ""Easy"", ""value"": ""Easy"" },
                                                          { ""label"": ""Normal"", ""value"": ""Normal"" },
                                                          { ""label"": ""Hard"", ""value"": ""Hard"" }
                                                        ]
                                                      },
                                                      { ""type"": ""sliderFloat"", ""label"": ""Music"", ""path"": ""$.settings.musicVolume"", ""min"": 0, ""max"": 1 },
                                                      { ""type"": ""toggle"", ""label"": ""Show Hints"", ""path"": ""$.settings.showHints"" },
                                                      { ""type"": ""inputText"", ""label"": ""Note"", ""path"": ""$.settings.nullableNote"" }
                                                    ]
                                                  },
                                                  {
                                                    ""label"": ""Debug"",
                                                    ""children"": [
                                                      { ""type"": ""field"", ""label"": ""Clicks"", ""path"": ""$.debug.clicks"" },
                                                      { ""type"": ""text"", ""label"": ""Last HP"", ""path"": ""$.debug.lastExactHpChange"" },
                                                      { ""type"": ""text"", ""label"": ""Player Wildcard"", ""path"": ""$.debug.lastPlayerWildcard"" },
                                                      { ""type"": ""text"", ""label"": ""Inventory Wildcard"", ""path"": ""$.debug.lastInventoryWildcard"" }
                                                    ]
                                                  }
                                                ]
                                              }
                                            ]
                                          }
                                        }";

        [SerializeField] private UImGuiJsonEditorBehaviour _jsonEditor;
        [SerializeField] private bool _drawFastUi = true;
        [SerializeField] private bool _drawSerializedObjectInspector = true;
        [SerializeField] private bool _drawSerializedObjectMirror = true;
        [SerializeField] private bool _autoApplySerializedObject = true;
        [SerializeField] private PlayerConfig _playerConfig = new();

        private readonly List<JsonPathSubscription> _subscriptions = new();
        private JsonUiCommandRegistry _fastUiCommands;
        private UImGuiJsonScreen _fastUiScreen;
        private UImGuiJsonSerializedObjectInspector<PlayerConfig> _playerInspector;
        private bool _initialized;
        private bool _recordingDebugChange;
        private int _changeSequence;

        private void Reset() => _jsonEditor = GetComponent<UImGuiJsonEditorBehaviour>();
        private void Start() => EnsureInitialized();
        private void OnEnable() => UImGui.UImGuiUtility.Layout += OnLayout;

        private void OnDisable()
        {
            UImGui.UImGuiUtility.Layout -= OnLayout;
            DisposeSubscriptions();
            _fastUiScreen = null;
            _fastUiCommands = null;
            _playerInspector?.Dispose();
            _playerInspector = null;
            _initialized = false;
        }

        [ContextMenu("Reload Sample JSON")]
        public void ReloadSampleJson()
        {
            EnsureEditor();
            _changeSequence = 0;
            _jsonEditor.LoadJson(SAMPLE_JSON);
        }

        private void OnLayout(UImGui.UImGui uImGui)
        {
            EnsureInitialized();

            if (_drawFastUi && _fastUiScreen != null)
                _fastUiScreen.Draw();

            if (_drawSerializedObjectInspector && _playerInspector != null)
            {
                _playerInspector.AutoApplyOnChange = _autoApplySerializedObject;
                _playerInspector.Draw();
            }

            if (_drawSerializedObjectMirror && _playerInspector != null)
                DrawSerializedObjectMirror();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            EnsureEditor();
            ReloadSampleJson();
            RegisterJsonButtons();
            RegisterSubscriptions();
            RegisterFastUi();

            _playerInspector = new UImGuiJsonSerializedObjectInspector<PlayerConfig>(new JsonSerializedObject<PlayerConfig>(_playerConfig), "Dingo Serialized Object");
            _playerInspector.AutoApplyOnChange = _autoApplySerializedObject;
            RegisterSerializedObjectButtons();

            _initialized = true;
        }

        private void EnsureEditor()
        {
            _jsonEditor ??= GetComponent<UImGuiJsonEditorBehaviour>();

            if (_jsonEditor == null)
                throw new InvalidOperationException("DingoJsonUISampleScene requires UImGuiJsonEditorBehaviour on the same GameObject or assigned in the inspector.");
        }

        private void RegisterJsonButtons()
        {
            var actions = _jsonEditor.Actions;
            actions.Clear();

            actions.AddButton(JsonPath.Root, "Reset JSON", _ => ReloadSampleJson(), JsonUiActionPlacement.Toolbar).Tooltip = "Reloads the sample JSON document.";
            actions.AddButton(JsonPath.Root, "Combat Preset", context =>
            {
                ApplyCombatPreset(context.Document);
            }, JsonUiActionPlacement.Toolbar).Tooltip = "Updates several nested values and triggers subscriptions.";

            actions.AddButton("$.player.profile.name", "Rename", context =>
            {
                var current = context.Document.GetValue(context.Path, "Dingo");
                var next = current == "Dingo" ? "Dingo Prime" : "Dingo";
                context.Document.SetValue(context.Path, new JValue(next));
            }).Tooltip = "Toggles a string field.";

            actions.AddButton("$.player.stats.hp", "Heal", context =>
            {
                HealPlayer(context.Document);
            }).Tooltip = "Sets player hp to maxHp.";

            var damageButton = actions.AddButton("$.player.stats.hp", "-25", context =>
            {
                DamagePlayer(context.Document, 25);
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
                AddGeneratedInventoryItem(context.Document);
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
                CompleteFirstQuest(context.Document);
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
                IncrementDebugClicks(context.Document);
            }).Tooltip = "Increments debug.clicks.";
        }

        private void RegisterFastUi()
        {
            _fastUiCommands = new JsonUiCommandRegistry();
            _fastUiCommands.Register("resetJson", _ => ReloadSampleJson());
            _fastUiCommands.Register("combatPreset", context => ApplyCombatPreset(context.Document));
            _fastUiCommands.Register("healPlayer", context => HealPlayer(context.Document));
            _fastUiCommands.Register("damagePlayer", context => DamagePlayer(context.Document, context.Payload?["amount"]?.Value<int>() ?? 25));
            _fastUiCommands.Register("completeQuest", context => CompleteFirstQuest(context.Document));
            _fastUiCommands.Register("addItem", context => AddGeneratedInventoryItem(context.Document));
            _fastUiCommands.Register("debugClick", context => IncrementDebugClicks(context.Document));

            _fastUiScreen = new UImGuiJsonScreen(_jsonEditor.Document, JsonUiSchema.FromJson(FAST_UI_SCHEMA), _fastUiCommands, "Dingo Fast UI");
        }

        private void RegisterSerializedObjectButtons()
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

        private void RegisterSubscriptions()
        {
            DisposeSubscriptions();

            _subscriptions.Add(_jsonEditor.Subscribe("$.player.stats.hp", change => { RecordDebugChange("$.debug.lastExactHpChange", change); }));

            _subscriptions.Add(_jsonEditor.Subscribe("$.player.stats.*", change => { RecordDebugChange("$.debug.lastPlayerWildcard", change); }));

            _subscriptions.Add(_jsonEditor.Subscribe("$.inventory[*]", change => { RecordDebugChange("$.debug.lastInventoryWildcard", change); }));
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
                _jsonEditor.SetValue("$.debug.sequence", _changeSequence);
                _jsonEditor.SetValue(debugPath, $"{change.Path}: {FormatToken(change.PreviousValue)} -> {FormatToken(change.CurrentValue)}");
            }
            finally
            {
                _recordingDebugChange = false;
            }
        }

        private static void ApplyCombatPreset(JsonDocumentModel document)
        {
            document.SetValue("$.player.stats.hp", new JValue(32));
            document.SetValue("$.player.stats.shield", new JValue(4.5));
            document.SetValue("$.player.flags.poisoned", new JValue(true));
            document.SetValue("$.settings.difficulty", new JValue("Hard"));
            document.SetValue("$.debug.lastAppliedPreset", new JValue("combat"));
        }

        private static void HealPlayer(JsonDocumentModel document)
        {
            var maxHp = document.GetValue("$.player.stats.maxHp", 100);
            document.SetValue("$.player.stats.hp", new JValue(maxHp));
        }

        private static void DamagePlayer(JsonDocumentModel document, int amount)
        {
            var current = document.GetValue("$.player.stats.hp", 0);
            document.SetValue("$.player.stats.hp", new JValue(Math.Max(0, current - amount)));
        }

        private static void CompleteFirstQuest(JsonDocumentModel document)
        {
            document.SetValue("$.quests[0].progress", new JValue(1.0));
            document.SetValue("$.quests[0].complete", new JValue(true));
        }

        private static void AddGeneratedInventoryItem(JsonDocumentModel document)
        {
            if (document.GetToken("$.inventory") is not JArray inventory)
                return;

            var index = inventory.Count;
            var item = new JObject
            {
                ["id"] = $"item-{index + 1}",
                ["label"] = $"Generated Item {index + 1}",
                ["count"] = 1,
                ["equipped"] = false,
            };

            document.SetValue(JsonPath.BuildIndexPath("$.inventory", index), item);
        }

        private static void IncrementDebugClicks(JsonDocumentModel document)
        {
            var current = document.GetValue("$.debug.clicks", 0);
            document.SetValue("$.debug.clicks", new JValue(current + 1));
        }

        private void DrawSerializedObjectMirror()
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(430f, 250f), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin("Dingo Applied Target"))
            {
                ImGui.End();
                return;
            }

            ImGui.Checkbox("Auto Apply", ref _autoApplySerializedObject);

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
                ImGui.TextUnformatted($"loadout: {FormatLoadout(_playerConfig.Loadout)}");

            if (_playerConfig.Tags != null)
                ImGui.TextUnformatted($"tags: {string.Join(", ", _playerConfig.Tags)}");

            ImGui.TextUnformatted($"nullable note: {_playerConfig.UserPreferences?.NullableNote ?? "null"}");

            if (_playerInspector.LastApplyException != null)
                ImGui.TextColored(new System.Numerics.Vector4(1f, 0.35f, 0.25f, 1f), _playerInspector.LastApplyException.Message);

            ImGui.End();
        }

        private static string FormatLoadout(IReadOnlyList<PlayerConfig.InventorySlot> loadout)
        {
            if (loadout.Count == 0)
                return "empty";

            var labels = new string[loadout.Count];
            for (var i = 0; i < loadout.Count; i++)
            {
                var slot = loadout[i];
                labels[i] = slot == null ? "null" : $"{slot.Label} x{slot.Count}";
            }

            return string.Join(", ", labels);
        }

        private static string FormatToken(JToken token)
        {
            if (token == null)
                return "missing";

            if (token.Type == JTokenType.Null)
                return "null";

            if (token is JValue value)
                return Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? "null";

            return token.ToString(Formatting.None);
        }
    }
}
