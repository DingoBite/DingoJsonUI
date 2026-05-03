using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DingoJsonUI.Examples.Scripts
{
    public static class DingoJsonUISampleData
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
            public ScriptableObject PreviewAsset;

            [SerializeField] [JsonProperty("displayName")]
            private string _displayName = "Inspector Dingo";

            [SerializeField] private string _privateInspectorNote = "Private SerializeField is visible";

            [JsonProperty("level")] public int Level { get; set; } = 3;

            [JsonIgnore] public string HiddenByJsonIgnore = "JsonIgnore hides this";
            [NonSerialized] public int RuntimeOnly = 99;

            public string DisplayName => _displayName;
            public string PrivateInspectorNote => _privateInspectorNote;
        }

        public const string GameplayJson = @"{
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

        public const string FastUiSchema = @"{
                                          ""title"": ""02 Schema Screen"",
                                          ""templates"": {
                                            ""topToolbar"": {
                                              ""type"": ""row"",
                                              ""children"": [
                                                { ""type"": ""button"", ""label"": ""Reset"", ""action"": ""resetJson"", ""tooltip"": ""Reload sample state."" },
                                                { ""type"": ""button"", ""label"": ""Combat Preset"", ""action"": ""combatPreset"", ""tooltip"": ""Apply several gameplay changes."" },
                                                { ""type"": ""button"", ""label"": ""+ Debug"", ""action"": ""debugClick"" }
                                              ]
                                            },
                                            ""textInput"": { ""type"": ""inputText"", ""width"": 260 },
                                            ""intSlider"": { ""type"": ""sliderInt"", ""min"": 0, ""max"": 100 },
                                            ""floatSlider"": { ""type"": ""sliderFloat"", ""min"": 0, ""max"": 1 },
                                            ""toggle"": { ""type"": ""toggle"" },
                                            ""playerActions"": {
                                              ""type"": ""row"",
                                              ""children"": [
                                                { ""type"": ""button"", ""label"": ""Heal"", ""action"": ""healPlayer"" },
                                                { ""type"": ""button"", ""label"": ""Damage"", ""action"": ""damagePlayer"", ""payload"": { ""amount"": 25 }, ""enabledWhen"": { ""path"": ""$.player.stats.hp"", ""gt"": 0 } },
                                                { ""type"": ""button"", ""label"": ""Complete Quest"", ""action"": ""completeQuest"", ""enabledWhen"": { ""path"": ""$.quests[0].complete"", ""equals"": false } }
                                              ]
                                            },
                                            ""difficultySelect"": {
                                              ""type"": ""select"",
                                              ""options"": [
                                                { ""label"": ""Easy"", ""value"": ""Easy"" },
                                                { ""label"": ""Normal"", ""value"": ""Normal"" },
                                                { ""label"": ""Hard"", ""value"": ""Hard"" }
                                              ]
                                            },
                                            ""debugLine"": { ""type"": ""text"" }
                                          },
                                          ""root"": {
                                            ""type"": ""section"",
                                            ""children"": [
                                              { ""type"": ""include"", ""template"": ""topToolbar"" },
                                              { ""type"": ""separator"" },
                                              {
                                                ""type"": ""tabs"",
                                                ""children"": [
                                                  {
                                                    ""label"": ""Player"",
                                                    ""children"": [
                                                      { ""use"": ""textInput"", ""label"": ""Name"", ""path"": ""$.player.profile.name"" },
                                                      { ""use"": ""intSlider"", ""label"": ""HP"", ""path"": ""$.player.stats.hp"", ""max"": 120 },
                                                      { ""use"": ""floatSlider"", ""label"": ""Speed"", ""path"": ""$.player.stats.speed"", ""max"": 15 },
                                                      { ""use"": ""toggle"", ""label"": ""Alive"", ""path"": ""$.player.stats.alive"" },
                                                      { ""type"": ""include"", ""template"": ""playerActions"" }
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
                                                      { ""use"": ""difficultySelect"", ""label"": ""Difficulty"", ""path"": ""$.settings.difficulty"" },
                                                      { ""use"": ""floatSlider"", ""label"": ""Music"", ""path"": ""$.settings.musicVolume"" },
                                                      { ""use"": ""toggle"", ""label"": ""Show Hints"", ""path"": ""$.settings.showHints"" },
                                                      { ""use"": ""textInput"", ""label"": ""Note"", ""path"": ""$.settings.nullableNote"" }
                                                    ]
                                                  },
                                                  {
                                                    ""label"": ""Debug"",
                                                    ""children"": [
                                                      { ""type"": ""field"", ""label"": ""Clicks"", ""path"": ""$.debug.clicks"" },
                                                      { ""use"": ""debugLine"", ""label"": ""Last HP"", ""path"": ""$.debug.lastExactHpChange"" },
                                                      { ""use"": ""debugLine"", ""label"": ""Player Wildcard"", ""path"": ""$.debug.lastPlayerWildcard"" },
                                                      { ""use"": ""debugLine"", ""label"": ""Inventory Wildcard"", ""path"": ""$.debug.lastInventoryWildcard"" }
                                                    ]
                                                  }
                                                ]
                                              }
                                            ]
                                          }
                                        }";

        public const string BehaviourJson = @"{
                                          ""menu"": {
                                            ""title"": ""Behaviour sample"",
                                            ""credits"": 100,
                                            ""mode"": ""Prototype"",
                                            ""quality"": ""Balanced"",
                                            ""volume"": 0.65,
                                            ""iterations"": 3,
                                            ""intensity"": 0.5,
                                            ""spawn"": [2.0, 3.0],
                                            ""position"": [0.0, 1.0, 0.0],
                                            ""tint"": [0.25, 0.6, 1.0, 1.0],
                                            ""danger"": false,
                                            ""progress"": 0.25,
                                            ""notes"": ""Multiline notes are useful for quick AI-generated menu copy.\nEdit this text at runtime.""
                                          },
                                          ""debug"": {
                                            ""lastCommand"": ""none""
                                          }
                                        }";

        public const string BehaviourSchema = @"{
                                          ""title"": ""04 Screen Behaviour"",
                                          ""root"": {
                                            ""type"": ""section"",
                                            ""labelWidth"": 118,
                                            ""spacing"": 6,
                                            ""children"": [
                                              {
                                                ""type"": ""row"",
                                                ""spacing"": 6,
                                                ""wrap"": true,
                                                ""children"": [
                                                  { ""type"": ""button"", ""label"": ""Reset"", ""action"": ""resetBehaviourJson"", ""height"": 24 },
                                                  { ""type"": ""button"", ""label"": ""+ Credits"", ""action"": ""payload.add"", ""payload"": { ""path"": ""$.menu.credits"", ""amount"": 50, ""max"": 999 }, ""height"": 24 },
                                                  { ""type"": ""button"", ""label"": ""Buy Upgrade"", ""action"": ""buyUpgrade"", ""payload"": { ""price"": 75 }, ""enabledWhen"": { ""path"": ""$.menu.credits"", ""gte"": 75 }, ""height"": 24 }
                                                ]
                                              },
                                              {
                                                ""type"": ""row"",
                                                ""spacing"": 6,
                                                ""wrap"": true,
                                                ""children"": [
                                                  { ""type"": ""button"", ""label"": ""Debug Mode"", ""action"": ""payload.set"", ""payload"": { ""path"": ""$.menu.mode"", ""value"": ""Debug"" }, ""height"": 24 },
                                                  { ""type"": ""button"", ""label"": ""Toggle Danger"", ""action"": ""payload.toggle"", ""payload"": { ""path"": ""$.menu.danger"" }, ""height"": 24 },
                                                  { ""type"": ""button"", ""label"": ""Copy Title"", ""action"": ""payload.copy"", ""payload"": { ""from"": ""$.menu.title"", ""to"": ""$.debug.lastCommand"" }, ""height"": 24 }
                                                ]
                                              },
                                              { ""type"": ""separator"" },
                                              {
                                                ""type"": ""columns"",
                                                ""columns"": 2,
                                                ""children"": [
                                                  {
                                                    ""type"": ""section"",
                                                    ""label"": ""Menu"",
                                                    ""labelWidth"": 92,
                                                    ""children"": [
                                                      { ""type"": ""inputText"", ""label"": ""Title"", ""path"": ""$.menu.title"" },
                                                      { ""type"": ""inputTextMultiline"", ""label"": ""Notes"", ""path"": ""$.menu.notes"", ""height"": 64 },
                                                      {
                                                        ""type"": ""select"",
                                                        ""label"": ""Mode"",
                                                        ""path"": ""$.menu.mode"",
                                                        ""options"": [
                                                          { ""label"": ""Prototype"", ""value"": ""Prototype"" },
                                                          { ""label"": ""Debug"", ""value"": ""Debug"" },
                                                          { ""label"": ""Release"", ""value"": ""Release"" }
                                                        ]
                                                      },
                                                      {
                                                        ""type"": ""radio"",
                                                        ""label"": ""Quality"",
                                                        ""path"": ""$.menu.quality"",
                                                        ""wrap"": true,
                                                        ""options"": [
                                                          { ""label"": ""Fast"", ""value"": ""Fast"" },
                                                          { ""label"": ""Balanced"", ""value"": ""Balanced"" },
                                                          { ""label"": ""Quality"", ""value"": ""Quality"" }
                                                        ]
                                                      },
                                                      { ""type"": ""int"", ""label"": ""Credits"", ""path"": ""$.menu.credits"" },
                                                      { ""type"": ""toggle"", ""label"": ""Danger"", ""path"": ""$.menu.danger"" }
                                                    ]
                                                  },
                                                  {
                                                    ""type"": ""section"",
                                                    ""label"": ""Tuning"",
                                                    ""labelWidth"": 92,
                                                    ""children"": [
                                                      { ""type"": ""dragInt"", ""label"": ""Iterations"", ""path"": ""$.menu.iterations"", ""step"": 1, ""min"": 1, ""max"": 10 },
                                                      { ""type"": ""dragFloat"", ""label"": ""Intensity"", ""path"": ""$.menu.intensity"", ""step"": 0.05, ""min"": 0, ""max"": 2 },
                                                      { ""type"": ""sliderFloat"", ""label"": ""Volume"", ""path"": ""$.menu.volume"", ""min"": 0, ""max"": 1 },
                                                      { ""type"": ""vector2"", ""label"": ""Spawn"", ""path"": ""$.menu.spawn"", ""step"": 0.1 },
                                                      { ""type"": ""vector3"", ""label"": ""Position"", ""path"": ""$.menu.position"", ""step"": 0.1 },
                                                      { ""type"": ""color"", ""label"": ""Tint"", ""path"": ""$.menu.tint"" },
                                                      { ""type"": ""progress"", ""label"": ""Progress"", ""path"": ""$.menu.progress"", ""min"": 0, ""max"": 1 },
                                                      { ""type"": ""text"", ""label"": ""Last"", ""path"": ""$.debug.lastCommand"" }
                                                    ]
                                                  }
                                                ]
                                              }
                                            ]
                                          }
                                        }";

        public static JObject CreateBehaviourJsonToken()
        {
            return new JObject
            {
                ["menu"] = new JObject
                {
                    ["title"] = "Behaviour sample",
                    ["credits"] = 100,
                    ["mode"] = "Prototype",
                    ["quality"] = "Balanced",
                    ["volume"] = 0.65,
                    ["iterations"] = 3,
                    ["intensity"] = 0.5,
                    ["spawn"] = new JArray(2.0, 3.0),
                    ["position"] = new JArray(0.0, 1.0, 0.0),
                    ["tint"] = new JArray(0.25, 0.6, 1.0, 1.0),
                    ["danger"] = false,
                    ["progress"] = 0.25,
                    ["notes"] = "Multiline notes are useful for quick AI-generated menu copy.\nEdit this text at runtime.",
                },
                ["debug"] = new JObject
                {
                    ["lastCommand"] = "none",
                },
            };
        }

        public static JObject CreateBehaviourSchemaToken()
        {
            return new JObject
            {
                ["title"] = "04 Screen Behaviour",
                ["root"] = new JObject
                {
                    ["type"] = "section",
                    ["labelWidth"] = 118,
                    ["spacing"] = 6,
                    ["children"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "row",
                            ["spacing"] = 6,
                            ["wrap"] = true,
                            ["children"] = new JArray
                            {
                                Button("Reset", "resetBehaviourJson"),
                                Button("+ Credits", "payload.add", new JObject
                                {
                                    ["path"] = "$.menu.credits",
                                    ["amount"] = 50,
                                    ["max"] = 999,
                                }),
                                WithEnabledWhen(Button("Buy Upgrade", "buyUpgrade", new JObject
                                {
                                    ["price"] = 75,
                                }), "$.menu.credits", "gte", 75),
                            },
                        },
                        new JObject
                        {
                            ["type"] = "row",
                            ["spacing"] = 6,
                            ["wrap"] = true,
                            ["children"] = new JArray
                            {
                                Button("Debug Mode", "payload.set", new JObject
                                {
                                    ["path"] = "$.menu.mode",
                                    ["value"] = "Debug",
                                }),
                                Button("Toggle Danger", "payload.toggle", new JObject
                                {
                                    ["path"] = "$.menu.danger",
                                }),
                                Button("Copy Title", "payload.copy", new JObject
                                {
                                    ["from"] = "$.menu.title",
                                    ["to"] = "$.debug.lastCommand",
                                }),
                            },
                        },
                        new JObject { ["type"] = "separator" },
                        new JObject
                        {
                            ["type"] = "columns",
                            ["columns"] = 2,
                            ["children"] = new JArray
                            {
                                new JObject
                                {
                                    ["type"] = "section",
                                    ["label"] = "Menu",
                                    ["labelWidth"] = 92,
                                    ["children"] = new JArray
                                    {
                                        Field("inputText", "Title", "$.menu.title"),
                                        new JObject
                                        {
                                            ["type"] = "inputTextMultiline",
                                            ["label"] = "Notes",
                                            ["path"] = "$.menu.notes",
                                            ["height"] = 64,
                                        },
                                        Select("Mode", "$.menu.mode", ("Prototype", "Prototype"), ("Debug", "Debug"), ("Release", "Release")),
                                        With(Radio("Quality", "$.menu.quality", ("Fast", "Fast"), ("Balanced", "Balanced"), ("Quality", "Quality")), "wrap", true),
                                        Field("int", "Credits", "$.menu.credits"),
                                        Field("toggle", "Danger", "$.menu.danger"),
                                    },
                                },
                                new JObject
                                {
                                    ["type"] = "section",
                                    ["label"] = "Tuning",
                                    ["labelWidth"] = 92,
                                    ["children"] = new JArray
                                    {
                                        Numeric("dragInt", "Iterations", "$.menu.iterations", step: 1, min: 1, max: 10),
                                        Numeric("dragFloat", "Intensity", "$.menu.intensity", step: 0.05, min: 0, max: 2),
                                        Numeric("sliderFloat", "Volume", "$.menu.volume", min: 0, max: 1),
                                        Numeric("vector2", "Spawn", "$.menu.spawn", step: 0.1),
                                        Numeric("vector3", "Position", "$.menu.position", step: 0.1),
                                        Field("color", "Tint", "$.menu.tint"),
                                        Numeric("progress", "Progress", "$.menu.progress", min: 0, max: 1),
                                        Field("text", "Last", "$.debug.lastCommand"),
                                    },
                                },
                            },
                        },
                    },
                },
            };

            static JObject Button(string label, string action, JObject payload = null)
            {
                var node = new JObject
                {
                    ["type"] = "button",
                    ["label"] = label,
                    ["action"] = action,
                    ["height"] = 24,
                };

                if (payload != null)
                    node["payload"] = payload;

                return node;
            }

            static JObject Field(string type, string label, string path)
            {
                return new JObject
                {
                    ["type"] = type,
                    ["label"] = label,
                    ["path"] = path,
                };
            }

            static JObject Numeric(string type, string label, string path, double? step = null, double? min = null, double? max = null)
            {
                var node = Field(type, label, path);
                if (step.HasValue)
                    node["step"] = step.Value;
                if (min.HasValue)
                    node["min"] = min.Value;
                if (max.HasValue)
                    node["max"] = max.Value;
                return node;
            }

            static JObject Select(string label, string path, params (string Label, string Value)[] options)
            {
                return WithOptions(Field("select", label, path), options);
            }

            static JObject Radio(string label, string path, params (string Label, string Value)[] options)
            {
                return WithOptions(Field("radio", label, path), options);
            }

            static JObject With(JObject node, string property, JToken value)
            {
                node[property] = value;
                return node;
            }

            static JObject WithEnabledWhen(JObject node, string path, string op, JToken value)
            {
                node["enabledWhen"] = new JObject
                {
                    ["path"] = path,
                    [op] = value,
                };
                return node;
            }

            static JObject WithOptions(JObject node, params (string Label, string Value)[] options)
            {
                var optionArray = new JArray();
                for (var i = 0; i < options.Length; i++)
                {
                    optionArray.Add(new JObject
                    {
                        ["label"] = options[i].Label,
                        ["value"] = options[i].Value,
                    });
                }

                node["options"] = optionArray;
                return node;
            }
        }

        public static void LoadGameplayJson(JsonDocumentModel document)
        {
            document.LoadJson(GameplayJson);
        }

        public static void AddLargeDataSample(JsonDocumentModel document, int count = 48)
        {
            var rows = new JArray();
            for (var i = 0; i < count; i++)
            {
                rows.Add(new JObject
                {
                    ["id"] = i + 1,
                    ["name"] = $"Generated Row {i + 1}",
                    ["enabled"] = i % 3 == 0,
                    ["weight"] = Math.Round(0.25 + i * 0.125, 3),
                });
            }

            document.SetValue("$.largeData.rows", rows);
            document.SetValue("$.largeData.note", new JValue("Open this array in the raw editor to see paged rendering."));
        }

        public static void ApplyCombatPreset(JsonDocumentModel document)
        {
            document.SetValue("$.player.stats.hp", new JValue(32));
            document.SetValue("$.player.stats.shield", new JValue(4.5));
            document.SetValue("$.player.flags.poisoned", new JValue(true));
            document.SetValue("$.settings.difficulty", new JValue("Hard"));
            document.SetValue("$.debug.lastAppliedPreset", new JValue("combat"));
        }

        public static void HealPlayer(JsonDocumentModel document)
        {
            var maxHp = document.GetValue("$.player.stats.maxHp", 100);
            document.SetValue("$.player.stats.hp", new JValue(maxHp));
        }

        public static void DamagePlayer(JsonDocumentModel document, int amount)
        {
            var current = document.GetValue("$.player.stats.hp", 0);
            document.SetValue("$.player.stats.hp", new JValue(Math.Max(0, current - amount)));
        }

        public static void CompleteFirstQuest(JsonDocumentModel document)
        {
            document.SetValue("$.quests[0].progress", new JValue(1.0));
            document.SetValue("$.quests[0].complete", new JValue(true));
        }

        public static void AddGeneratedInventoryItem(JsonDocumentModel document)
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

        public static void IncrementDebugClicks(JsonDocumentModel document)
        {
            var current = document.GetValue("$.debug.clicks", 0);
            document.SetValue("$.debug.clicks", new JValue(current + 1));
        }

        public static string FormatLoadout(IReadOnlyList<PlayerConfig.InventorySlot> loadout)
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

        public static string FormatToken(JToken token)
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
