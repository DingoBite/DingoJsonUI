using System;
using System.Collections.Generic;
using DingoJsonUI.GUI;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DingoJsonUI.Examples.Scripts
{
    [DefaultExecutionOrder(150)]
    public sealed class DingoJsonUIFeatureGallerySample : MonoBehaviour
    {
        [SerializeField] private bool _drawFeatureGallery = true;
        [SerializeField] private bool _drawLargeDataEditor = true;
        [SerializeField] private bool _drawValidatorDiagnostics = true;
        [SerializeField] private bool _drawCustomSerialization = true;
        [SerializeField] private bool _drawSchemaPreview = true;

        [SerializeField] private Vector2 _featureGalleryPosition = new(24f, 540f);
        [SerializeField] private Vector2 _featureGallerySize = new(490f, 300f);
        [SerializeField] private Vector2 _validatorPosition = new(530f, 780f);
        [SerializeField] private Vector2 _validatorSize = new(500f, 230f);
        [SerializeField] private Vector2 _largeDataPosition = new(1045f, 18f);
        [SerializeField] private Vector2 _largeDataSize = new(500f, 350f);
        [SerializeField] private Vector2 _customPosition = new(1045f, 390f);
        [SerializeField] private Vector2 _customSize = new(500f, 260f);
        [SerializeField] private Vector2 _schemaPreviewPosition = new(1045f, 670f);
        [SerializeField] private Vector2 _schemaPreviewSize = new(500f, 250f);

        private JsonUiSession _featureSession;
        private UImGuiJsonScreen _featureScreen;

        private JsonUiSession _validatorSession;
        private UImGuiJsonSchemaDiagnosticsWindow _validatorDiagnostics;

        private JsonDocumentModel _largeDataDocument;
        private UImGuiJsonEditor _largeDataEditor;

        private CustomMenuState _customState;
        private UImGuiJsonSerializedObjectInspector<CustomMenuState> _customInspector;
        private JsonUiSchemaReport _schemaPreviewReport;

        private void OnEnable()
        {
            EnsureInitialized();
            UImGui.UImGuiUtility.Layout += OnLayout;
        }

        private void OnDisable()
        {
            UImGui.UImGuiUtility.Layout -= OnLayout;
            _customInspector?.Dispose();
            _customInspector = null;
            _featureScreen = null;
            _featureSession = null;
            _validatorDiagnostics = null;
            _validatorSession = null;
            _largeDataEditor = null;
            _largeDataDocument = null;
            _schemaPreviewReport = null;
        }

        private void OnLayout(UImGui.UImGui uImGui)
        {
            EnsureInitialized();

            if (_drawFeatureGallery)
            {
                ImGui.SetNextWindowPos(ToImGuiVector(_featureGalleryPosition), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(ToImGuiVector(_featureGallerySize), ImGuiCond.FirstUseEver);
                _featureScreen.Draw();
            }

            if (_drawLargeDataEditor)
            {
                ImGui.SetNextWindowPos(ToImGuiVector(_largeDataPosition), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(ToImGuiVector(_largeDataSize), ImGuiCond.FirstUseEver);
                _largeDataEditor.Draw();
            }

            if (_drawCustomSerialization)
            {
                ImGui.SetNextWindowPos(ToImGuiVector(_customPosition), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(ToImGuiVector(_customSize), ImGuiCond.FirstUseEver);
                _customInspector.Draw();
            }

            if (_drawValidatorDiagnostics)
            {
                _validatorDiagnostics.Diagnostics = _validatorSession.Diagnostics;
                ImGui.SetNextWindowPos(ToImGuiVector(_validatorPosition), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(ToImGuiVector(_validatorSize), ImGuiCond.FirstUseEver);
                _validatorDiagnostics.Draw();
            }

            if (_drawSchemaPreview)
            {
                ImGui.SetNextWindowPos(ToImGuiVector(_schemaPreviewPosition), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(ToImGuiVector(_schemaPreviewSize), ImGuiCond.FirstUseEver);
                DrawSchemaPreview();
            }
        }

        private void EnsureInitialized()
        {
            EnsureValidatorSample();
            EnsureFeatureGallery();
            EnsureSchemaPreviewSample();
            EnsureLargeDataSample();
            EnsureCustomSerializationSample();
        }

        private void EnsureFeatureGallery()
        {
            if (_featureSession != null)
                return;

            _featureSession = JsonUi.Session(
                FeatureGalleryJson,
                FeatureGallerySchema,
                RegisterFeatureCommands,
                new JsonUiOptions
                {
                    WindowTitle = "05 Feature Gallery",
                    ScrollWheelPixelsPerStep = 280f,
                });

            _featureScreen = UImGuiJsonUi.Screen(_featureSession, "05 Feature Gallery");
        }

        private void RegisterFeatureCommands(JsonUiCommandRegistry commands)
        {
            commands.Register("feature.reset", context => context.Document.LoadJson(FeatureGalleryJson));
            commands.Register("feature.progress", context =>
            {
                var current = context.Document.GetValue("$.widgets.progress", 0f);
                var next = current >= 1f ? 0f : Mathf.Min(1f, current + 0.1f);
                context.Document.SetValue("$.widgets.progress", new JValue(Math.Round(next, 2)));
            });

            commands.Register("feature.event", context =>
            {
                var count = context.Document.GetValue("$.session.events", 0) + 1;
                context.Document.SetValue("$.session.events", new JValue(count));
                context.Document.SetValue("$.session.lastEvent", new JValue(context.Payload?["name"]?.Value<string>() ?? "event"));
            });

            commands.Register("validator.invalid", context =>
            {
                EnsureValidatorSample();
                _validatorSession.LoadSchemaJson(InvalidValidatorSchema);
                context.Document.SetValue("$.session.validator", new JValue("invalid schema loaded"));
            });

            commands.Register("validator.valid", context =>
            {
                EnsureValidatorSample();
                _validatorSession.LoadSchemaJson(ValidValidatorSchema);
                context.Document.SetValue("$.session.validator", new JValue("valid schema loaded"));
            });

            commands.Register("session.badJson", context =>
            {
                EnsureValidatorSample();
                var ok = _validatorSession.LoadJson("{ bad json");
                context.Document.SetValue("$.session.json", new JValue(ok ? "ok" : "json diagnostic created"));
            });
        }

        private void EnsureValidatorSample()
        {
            if (_validatorSession == null)
            {
                _validatorSession = JsonUi.Session(
                    json: @"{""title"":""Validator Target"",""enabled"":true,""value"":0.5,""counter"":1}",
                    schemaJson: InvalidValidatorSchema,
                    options: new JsonUiOptions { WindowTitle = "08 Validator Diagnostics" });
            }

            _validatorDiagnostics ??= new UImGuiJsonSchemaDiagnosticsWindow
            {
                WindowTitle = "08 Validator Diagnostics",
                DrawWhenValid = true,
                OnValidate = () => _validatorSession.ValidateCurrentSchema(),
                OnReloadAll = () => _validatorSession.LoadSchemaJson(InvalidValidatorSchema),
            };
        }

        private void EnsureLargeDataSample()
        {
            if (_largeDataDocument != null)
                return;

            _largeDataDocument = new JsonDocumentModel(CreateLargeDataRoot(96));
            _largeDataEditor = UImGuiJsonUi.Editor(
                _largeDataDocument,
                "06 Large Data Safety",
                new JsonUiOptions
                {
                    MaxVisibleChildrenPerNode = 12,
                    MaxRenderDepth = 8,
                    ScrollWheelPixelsPerStep = 420f,
                });

            _largeDataEditor.Actions.AddButton(JsonPath.Root, "96 Rows", _ => LoadLargeData(96), JsonUiActionPlacement.Toolbar);
            _largeDataEditor.Actions.AddButton(JsonPath.Root, "180 Rows", _ => LoadLargeData(180), JsonUiActionPlacement.Toolbar);
        }

        private void LoadLargeData(int count)
        {
            _largeDataDocument.LoadToken(CreateLargeDataRoot(count));
        }

        private void EnsureCustomSerializationSample()
        {
            if (_customInspector != null)
                return;

            _customState = new CustomMenuState();
            var serializedObject = new JsonSerializedObject<CustomMenuState>(
                _customState,
                SerializeCustomState,
                ApplyCustomState);

            _customInspector = new UImGuiJsonSerializedObjectInspector<CustomMenuState>(serializedObject, "07 Custom Serialization")
            {
                AutoApplyOnChange = true,
                ScrollWheelPixelsPerStep = 320f,
            };

            _customInspector.Actions.AddButton("$.volumePercent", "75%", context => context.Document.SetValue(context.Path, new JValue(75)));
            _customInspector.Actions.AddButton("$.seed", "+10", context =>
            {
                var current = context.Document.GetValue(context.Path, 0);
                context.Document.SetValue(context.Path, new JValue(current + 10));
            });
        }

        private void EnsureSchemaPreviewSample()
        {
            if (_schemaPreviewReport != null)
                return;

            EnsureFeatureGallery();
            _schemaPreviewReport = _featureSession.CreateSchemaReport();
        }

        private void DrawSchemaPreview()
        {
            if (ImGui.Begin("09 Schema Preview API"))
            {
                if (ImGui.Button("Refresh"))
                    _schemaPreviewReport = _featureSession.CreateSchemaReport();

                ImGui.SameLine();
                if (ImGui.Button("Copy"))
                    ImGui.SetClipboardText(_schemaPreviewReport?.ToText() ?? string.Empty);

                var report = _schemaPreviewReport;
                if (report == null)
                {
                    ImGui.TextUnformatted("No schema report.");
                    ImGui.End();
                    return;
                }

                var statusColor = report.IsValid
                    ? new System.Numerics.Vector4(0.35f, 0.95f, 0.45f, 1f)
                    : new System.Numerics.Vector4(1f, 0.35f, 0.25f, 1f);

                ImGui.TextColored(statusColor, $"Valid: {report.IsValid}  Errors: {report.ErrorCount}  Warnings: {report.WarningCount}");
                ImGui.TextUnformatted($"Nodes: {report.Preview.NodeCount}  Max Depth: {report.Preview.MaxDepth}");
                ImGui.TextUnformatted($"Paths: {report.Preview.DataPathCount}  Actions: {report.Preview.ActionCount}  Templates: {report.Preview.TemplateCount}");
                ImGui.Separator();

                DrawStringList("Paths", report.Preview.DataPaths, 18);
                DrawStringList("Actions", report.Preview.Actions, 12);
                DrawStringList("Templates", report.Preview.Templates, 12);

                if (report.Diagnostics.Count > 0 && ImGui.CollapsingHeader("Diagnostics"))
                {
                    for (var i = 0; i < report.Diagnostics.Count; i++)
                        ImGui.TextWrapped(report.Diagnostics[i].ToString());
                }
            }

            ImGui.End();
        }

        private static void DrawStringList(string label, IReadOnlyList<string> values, int limit)
        {
            if (values == null || values.Count == 0 || !ImGui.CollapsingHeader($"{label} ({values.Count})"))
                return;

            var count = Math.Min(values.Count, limit);
            for (var i = 0; i < count; i++)
                ImGui.TextWrapped(values[i]);

            if (values.Count > count)
                ImGui.TextDisabled($"+ {values.Count - count} more");
        }

        private static JToken SerializeCustomState(CustomMenuState target, JsonSerializer serializer)
        {
            return new JObject
            {
                ["selectedTab"] = target.SelectedTab,
                ["volumePercent"] = Mathf.RoundToInt(target.Volume * 100f),
                ["seed"] = target.Seed,
                ["enabled"] = target.Enabled,
                ["summary"] = $"{target.SelectedTab}:{target.Seed}",
            };
        }

        private static CustomMenuState ApplyCustomState(JToken token, CustomMenuState target, JsonSerializer serializer)
        {
            if (target == null)
                target = new CustomMenuState();

            target.SelectedTab = token.Value<string>("selectedTab") ?? target.SelectedTab;
            target.Volume = Mathf.Clamp01(token.Value<float?>("volumePercent").GetValueOrDefault(Mathf.RoundToInt(target.Volume * 100f)) / 100f);
            target.Seed = token.Value<int?>("seed") ?? target.Seed;
            target.Enabled = token.Value<bool?>("enabled") ?? target.Enabled;
            return target;
        }

        private static JObject CreateLargeDataRoot(int count)
        {
            var rows = new JArray();
            var properties = new JObject();

            for (var i = 0; i < count; i++)
            {
                rows.Add(new JObject
                {
                    ["id"] = i + 1,
                    ["name"] = $"Paged Row {i + 1}",
                    ["enabled"] = i % 2 == 0,
                    ["weight"] = Math.Round(0.25 + i * 0.125, 3),
                });

                properties[$"property_{i + 1:000}"] = i;
            }

            return new JObject
            {
                ["note"] = "Open largeArray and largeObject to see paging and cached object-property enumeration.",
                ["largeArray"] = rows,
                ["largeObject"] = properties,
            };
        }

        private static System.Numerics.Vector2 ToImGuiVector(Vector2 value)
        {
            return new System.Numerics.Vector2(value.x, value.y);
        }

        [Serializable]
        private sealed class CustomMenuState
        {
            public string SelectedTab = "Audio";
            public float Volume = 0.45f;
            public int Seed = 7;
            public bool Enabled = true;
            public string RuntimeOnly = "not serialized by custom delegates";
        }

        private const string FeatureGalleryJson = @"{
                                          ""widgets"": {
                                            ""title"": ""Widget Gallery"",
                                            ""notes"": ""Multiline text buffer demo.\nThis field keeps layout stable while edited."",
                                            ""intValue"": 5,
                                            ""floatValue"": 0.35,
                                            ""dragInt"": 3,
                                            ""dragFloat"": 0.65,
                                            ""sliderInt"": 45,
                                            ""sliderFloat"": 0.7,
                                            ""toggle"": true,
                                            ""mode"": ""Prototype"",
                                            ""quality"": ""Balanced"",
                                            ""vector2"": [1.0, 2.0],
                                            ""vector3"": [0.0, 1.0, 2.0],
                                            ""color"": [0.2, 0.55, 1.0, 1.0],
                                            ""progress"": 0.35
                                          },
                                          ""payload"": {
                                            ""credits"": 100,
                                            ""mode"": ""Prototype"",
                                            ""enabled"": false,
                                            ""source"": ""Copied from source"",
                                            ""target"": ""none"",
                                            ""counter"": 0
                                          },
                                          ""conditions"": {
                                            ""advanced"": false,
                                            ""danger"": false,
                                            ""threshold"": 0.5
                                          },
                                          ""layout"": {
                                            ""left"": ""Left column"",
                                            ""right"": ""Right column"",
                                            ""wrapped"": ""Row wraps when the window is narrow""
                                          },
                                          ""session"": {
                                            ""validator"": ""invalid schema loaded"",
                                            ""json"": ""not tested"",
                                            ""events"": 0,
                                            ""lastEvent"": ""none""
                                          }
                                        }";

        private const string FeatureGallerySchema = @"{
                                          ""title"": ""05 Feature Gallery"",
                                          ""templates"": {
                                            ""compactText"": { ""type"": ""inputText"", ""width"": 220 },
                                            ""percentSlider"": { ""type"": ""sliderFloat"", ""min"": 0, ""max"": 1 },
                                            ""smallButton"": { ""type"": ""button"", ""height"": 24 }
                                          },
                                          ""root"": {
                                            ""type"": ""tabs"",
                                            ""labelWidth"": 112,
                                            ""spacing"": 6,
                                            ""children"": [
                                              {
                                                ""label"": ""Widgets"",
                                                ""children"": [
                                                  { ""use"": ""compactText"", ""label"": ""Input"", ""path"": ""$.widgets.title"" },
                                                  { ""type"": ""inputTextMultiline"", ""label"": ""Multiline"", ""path"": ""$.widgets.notes"", ""height"": 70 },
                                                  { ""type"": ""int"", ""label"": ""Int"", ""path"": ""$.widgets.intValue"" },
                                                  { ""type"": ""float"", ""label"": ""Float"", ""path"": ""$.widgets.floatValue"" },
                                                  { ""type"": ""dragInt"", ""label"": ""Drag Int"", ""path"": ""$.widgets.dragInt"", ""step"": 1, ""min"": 0, ""max"": 10 },
                                                  { ""type"": ""dragFloat"", ""label"": ""Drag Float"", ""path"": ""$.widgets.dragFloat"", ""step"": 0.05, ""min"": 0, ""max"": 2 },
                                                  { ""type"": ""sliderInt"", ""label"": ""Slider Int"", ""path"": ""$.widgets.sliderInt"", ""min"": 0, ""max"": 100 },
                                                  { ""use"": ""percentSlider"", ""label"": ""Slider Float"", ""path"": ""$.widgets.sliderFloat"" },
                                                  { ""type"": ""toggle"", ""label"": ""Toggle"", ""path"": ""$.widgets.toggle"" },
                                                  {
                                                    ""type"": ""select"",
                                                    ""label"": ""Select"",
                                                    ""path"": ""$.widgets.mode"",
                                                    ""options"": [
                                                      { ""label"": ""Prototype"", ""value"": ""Prototype"" },
                                                      { ""label"": ""Debug"", ""value"": ""Debug"" },
                                                      { ""label"": ""Release"", ""value"": ""Release"" }
                                                    ]
                                                  },
                                                  {
                                                    ""type"": ""radio"",
                                                    ""label"": ""Radio"",
                                                    ""path"": ""$.widgets.quality"",
                                                    ""wrap"": true,
                                                    ""options"": [
                                                      { ""label"": ""Fast"", ""value"": ""Fast"" },
                                                      { ""label"": ""Balanced"", ""value"": ""Balanced"" },
                                                      { ""label"": ""Quality"", ""value"": ""Quality"" }
                                                    ]
                                                  },
                                                  { ""type"": ""vector2"", ""label"": ""Vector2"", ""path"": ""$.widgets.vector2"", ""step"": 0.1 },
                                                  { ""type"": ""vector3"", ""label"": ""Vector3"", ""path"": ""$.widgets.vector3"", ""step"": 0.1 },
                                                  { ""type"": ""color"", ""label"": ""Color"", ""path"": ""$.widgets.color"" },
                                                  { ""type"": ""progress"", ""label"": ""Progress"", ""path"": ""$.widgets.progress"", ""min"": 0, ""max"": 1 },
                                                  { ""type"": ""button"", ""label"": ""Advance Progress"", ""action"": ""feature.progress"" }
                                                ]
                                              },
                                              {
                                                ""label"": ""Layout"",
                                                ""children"": [
                                                  { ""type"": ""include"", ""template"": ""compactText"", ""label"": ""Template"", ""path"": ""$.layout.left"" },
                                                  { ""type"": ""separator"" },
                                                  {
                                                    ""type"": ""row"",
                                                    ""wrap"": true,
                                                    ""spacing"": 6,
                                                    ""children"": [
                                                      { ""use"": ""smallButton"", ""label"": ""One"", ""action"": ""feature.event"", ""payload"": { ""name"": ""row one"" } },
                                                      { ""use"": ""smallButton"", ""label"": ""Two"", ""action"": ""feature.event"", ""payload"": { ""name"": ""row two"" } },
                                                      { ""use"": ""smallButton"", ""label"": ""Three"", ""action"": ""feature.event"", ""payload"": { ""name"": ""row three"" } }
                                                    ]
                                                  },
                                                  { ""type"": ""field"", ""label"": ""Wrapped"", ""path"": ""$.layout.wrapped"" },
                                                  { ""type"": ""space"", ""height"": 6 },
                                                  {
                                                    ""type"": ""columns"",
                                                    ""columns"": 2,
                                                    ""children"": [
                                                      { ""type"": ""section"", ""label"": ""Left"", ""indent"": 8, ""children"": [ { ""type"": ""inputText"", ""label"": ""Text"", ""path"": ""$.layout.left"" } ] },
                                                      { ""type"": ""section"", ""label"": ""Right"", ""indent"": 8, ""children"": [ { ""type"": ""inputText"", ""label"": ""Text"", ""path"": ""$.layout.right"" } ] }
                                                    ]
                                                  },
                                                  {
                                                    ""type"": ""foldout"",
                                                    ""label"": ""Foldout"",
                                                    ""defaultOpen"": true,
                                                    ""children"": [
                                                      { ""type"": ""text"", ""label"": ""Events"", ""path"": ""$.session.events"" },
                                                      { ""type"": ""text"", ""label"": ""Last Event"", ""path"": ""$.session.lastEvent"" }
                                                    ]
                                                  }
                                                ]
                                              },
                                              {
                                                ""label"": ""Payload"",
                                                ""children"": [
                                                  { ""type"": ""field"", ""label"": ""Credits"", ""path"": ""$.payload.credits"" },
                                                  { ""type"": ""field"", ""label"": ""Mode"", ""path"": ""$.payload.mode"" },
                                                  { ""type"": ""field"", ""label"": ""Enabled"", ""path"": ""$.payload.enabled"" },
                                                  { ""type"": ""field"", ""label"": ""Target"", ""path"": ""$.payload.target"" },
                                                  {
                                                    ""type"": ""row"",
                                                    ""wrap"": true,
                                                    ""spacing"": 6,
                                                    ""children"": [
                                                      { ""type"": ""button"", ""label"": ""+25 Credits"", ""action"": ""payload.add"", ""payload"": { ""path"": ""$.payload.credits"", ""amount"": 25, ""max"": 999 } },
                                                      { ""type"": ""button"", ""label"": ""Release Mode"", ""action"": ""payload.set"", ""payload"": { ""path"": ""$.payload.mode"", ""value"": ""Release"" } },
                                                      { ""type"": ""button"", ""label"": ""Toggle Enabled"", ""action"": ""payload.toggle"", ""payload"": { ""path"": ""$.payload.enabled"" } },
                                                      { ""type"": ""button"", ""label"": ""Copy Source"", ""action"": ""payload.copy"", ""payload"": { ""from"": ""$.payload.source"", ""to"": ""$.payload.target"" } }
                                                    ]
                                                  }
                                                ]
                                              },
                                              {
                                                ""label"": ""Conditions"",
                                                ""children"": [
                                                  { ""type"": ""toggle"", ""label"": ""Advanced"", ""path"": ""$.conditions.advanced"" },
                                                  { ""type"": ""toggle"", ""label"": ""Danger"", ""path"": ""$.conditions.danger"" },
                                                  { ""type"": ""sliderFloat"", ""label"": ""Threshold"", ""path"": ""$.conditions.threshold"", ""min"": 0, ""max"": 1 },
                                                  { ""type"": ""inputText"", ""label"": ""Visible Advanced"", ""path"": ""$.layout.left"", ""visibleWhen"": { ""path"": ""$.conditions.advanced"", ""equals"": true } },
                                                  { ""type"": ""button"", ""label"": ""Danger Action"", ""action"": ""feature.event"", ""payload"": { ""name"": ""danger"" }, ""enabledWhen"": { ""path"": ""$.conditions.danger"", ""equals"": true } },
                                                  { ""type"": ""text"", ""label"": ""High Threshold"", ""path"": ""$.conditions.threshold"", ""visibleWhen"": { ""path"": ""$.conditions.threshold"", ""gte"": 0.75 } }
                                                ]
                                              },
                                              {
                                                ""label"": ""Session"",
                                                ""children"": [
                                                  { ""type"": ""text"", ""label"": ""Validator"", ""path"": ""$.session.validator"" },
                                                  { ""type"": ""text"", ""label"": ""JSON"", ""path"": ""$.session.json"" },
                                                  {
                                                    ""type"": ""row"",
                                                    ""wrap"": true,
                                                    ""spacing"": 6,
                                                    ""children"": [
                                                      { ""type"": ""button"", ""label"": ""Load Invalid Schema"", ""action"": ""validator.invalid"" },
                                                      { ""type"": ""button"", ""label"": ""Load Valid Schema"", ""action"": ""validator.valid"" },
                                                      { ""type"": ""button"", ""label"": ""Bad JSON"", ""action"": ""session.badJson"" },
                                                      { ""type"": ""button"", ""label"": ""Reset Gallery"", ""action"": ""feature.reset"" }
                                                    ]
                                                  }
                                                ]
                                              }
                                            ]
                                          }
                                        }";

        private const string InvalidValidatorSchema = @"{
                                          ""title"": ""Invalid Schema Sample"",
                                          ""root"": {
                                            ""type"": ""section"",
                                            ""children"": [
                                              { ""type"": ""sliderFlaot"", ""label"": ""Typo Widget"", ""path"": ""$.value"" },
                                              { ""type"": ""sliderFloat"", ""label"": ""Missing Path"" },
                                              { ""type"": ""button"", ""label"": ""Unknown Action"", ""action"": ""validator.unknown"" },
                                              { ""type"": ""button"", ""label"": ""Bad Payload"", ""action"": ""payload.add"", ""payload"": { ""path"": ""$.counter"", ""amount"": ""many"" } },
                                              { ""type"": ""include"", ""template"": ""missingTemplate"" }
                                            ]
                                          }
                                        }";

        private const string ValidValidatorSchema = @"{
                                          ""title"": ""Valid Schema Sample"",
                                          ""root"": {
                                            ""type"": ""section"",
                                            ""labelWidth"": 90,
                                            ""children"": [
                                              { ""type"": ""inputText"", ""label"": ""Title"", ""path"": ""$.title"" },
                                              { ""type"": ""toggle"", ""label"": ""Enabled"", ""path"": ""$.enabled"" },
                                              { ""type"": ""sliderFloat"", ""label"": ""Value"", ""path"": ""$.value"", ""min"": 0, ""max"": 1 },
                                              { ""type"": ""button"", ""label"": ""+ Counter"", ""action"": ""payload.add"", ""payload"": { ""path"": ""$.counter"", ""amount"": 1 } }
                                            ]
                                          }
                                        }";
    }
}
