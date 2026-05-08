using DingoJsonUI.GUI;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DingoJsonUI.Examples.Scripts
{
    [DefaultExecutionOrder(-50)]
    public sealed class DingoJsonUIScreenSchemaSample : MonoBehaviour
    {
        private enum DifficultyPreset
        {
            Easy,
            Normal,
            Hard,
        }

        [SerializeField] private DingoJsonUIRawEditorSample _sourceSample;
        [SerializeField] private bool _draw = true;
        [SerializeField] private Vector2 _initialWindowPosition = new(530f, 18f);
        [SerializeField] private Vector2 _initialWindowSize = new(500f, 250f);

        private JsonDocumentModel _fallbackDocument;
        private JsonUiCommandRegistry _commands;
        private UImGuiJsonScreen _screen;
        private bool _initialized;

        private JsonDocumentModel Document
        {
            get
            {
                EnsureSource();
                if (_sourceSample != null)
                    return _sourceSample.Document;

                _fallbackDocument ??= new JsonDocumentModel(DingoJsonUISampleData.GameplayJson);
                return _fallbackDocument;
            }
        }

        private void Reset()
        {
            _sourceSample = FindFirstObjectByType<DingoJsonUIRawEditorSample>();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            UImGui.UImGuiUtility.Layout += OnLayout;
        }

        private void OnDisable()
        {
            UImGui.UImGuiUtility.Layout -= OnLayout;
            _screen = null;
            _commands = null;
            _initialized = false;
        }

        private void OnLayout(UImGui.UImGui uImGui)
        {
            if (!_draw)
                return;

            EnsureInitialized();
            ImGui.SetNextWindowPos(ToImGuiVector(_initialWindowPosition), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(ToImGuiVector(_initialWindowSize), ImGuiCond.FirstUseEver);
            _screen.Draw();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            EnsureSource();
            RegisterCommands();
            _screen = new UImGuiJsonScreen(Document, CreateSchema(), _commands, "02 Schema Screen");
            _initialized = true;
        }

        private void EnsureSource()
        {
            if (_sourceSample == null)
                _sourceSample = FindFirstObjectByType<DingoJsonUIRawEditorSample>();
        }

        private void RegisterCommands()
        {
            _commands = new JsonUiCommandRegistry();
            _commands.Register("resetJson", _ =>
            {
                if (_sourceSample != null)
                    _sourceSample.ReloadSampleJson();
                else
                    DingoJsonUISampleData.LoadGameplayJson(Document);
            });
            _commands.Register("combatPreset", context => DingoJsonUISampleData.ApplyCombatPreset(context.Document));
            _commands.Register("healPlayer", context => DingoJsonUISampleData.HealPlayer(context.Document));
            _commands.Register("damagePlayer", context => DingoJsonUISampleData.DamagePlayer(context.Document, context.Payload?["amount"]?.Value<int>() ?? 25));
            _commands.Register("completeQuest", context => DingoJsonUISampleData.CompleteFirstQuest(context.Document));
            _commands.Register("addItem", context => DingoJsonUISampleData.AddGeneratedInventoryItem(context.Document));
            _commands.Register("debugClick", context => DingoJsonUISampleData.IncrementDebugClicks(context.Document));
        }

        private static JsonUiSchema CreateSchema()
        {
            var rootPath = Ui.Path;
            var player = rootPath["player"];
            var playerStats = player["stats"];
            var playerProfile = player["profile"];
            var inventory = rootPath["inventory"];
            var quests = rootPath["quests"];
            var settings = rootPath["settings"];
            var debug = rootPath["debug"];
            var item = Ui.Item;

            return JsonUiSchemaBuilder.Create("02 Schema Screen")
                .Template("topToolbar", Ui.Row(
                    Ui.Button("Reset", "resetJson").Tooltip("Reload sample state."),
                    Ui.Button("Combat Preset", "combatPreset").Tooltip("Apply several gameplay changes."),
                    Ui.Button("+ Debug", "debugClick")))
                .Template("textInput", Ui.InputText(null, null).Width(260))
                .Template("intSlider", Ui.SliderInt(null, null, 0, 100))
                .Template("floatSlider", Ui.SliderFloat(null, null, 0, 1))
                .Template("toggle", Ui.Toggle(null, null))
                .Template("playerActions", Ui.Row(
                    Ui.Button("Heal", "healPlayer"),
                    Ui.Button("Damage", "damagePlayer")
                        .Payload("amount", 25)
                        .EnabledWhen(Ui.Gt(playerStats["hp"], 0)),
                    Ui.Button("Complete Quest", "completeQuest")
                        .EnabledWhen(Ui.Eq(quests[0]["complete"], false))))
                .Template("difficultySelect", Ui.SelectEnum<DifficultyPreset>(null, null))
                .Template("debugLine", Ui.Text(null, null))
                .Root(root => root.Section()
                    .Include("topToolbar")
                    .Separator()
                    .Tabs(tabs => tabs
                        .Tab("Player", tab => tab
                            .Add(Ui.Include("textInput").Label("Name").Path(playerProfile["name"]))
                            .Add(Ui.Include("intSlider").Label("HP").Path(playerStats["hp"]).Max(120))
                            .Add(Ui.Include("floatSlider").Label("Speed").Path(playerStats["speed"]).Max(15))
                            .Add(Ui.Include("toggle").Label("Alive").Path(playerStats["alive"]))
                            .Include("playerActions"))
                        .Tab("Inventory", tab => tab
                            .Add(Ui.List("Inventory", inventory,
                                    new JObject
                                    {
                                        ["id"] = "new-item",
                                        ["label"] = "New Item",
                                        ["count"] = 1,
                                        ["equipped"] = false,
                                    },
                                    Ui.InputText("Id", item["id"]),
                                    Ui.InputText("Label", item["label"]),
                                    Ui.Int("Count", item["count"]),
                                    Ui.Toggle("Equipped", item["equipped"]))
                                .ItemLabelPath(item["label"])
                                .AddLabel("+ Item")
                                .EmptyText("No inventory items.")))
                        .Tab("Settings", tab => tab
                            .Add(Ui.Include("difficultySelect").Label("Difficulty").Path(settings["difficulty"]))
                            .Add(Ui.Include("floatSlider").Label("Music").Path(settings["musicVolume"]))
                            .Add(Ui.Include("toggle").Label("Show Hints").Path(settings["showHints"]))
                            .Add(Ui.Include("textInput").Label("Note").Path(settings["nullableNote"])))
                        .Tab("Debug", tab => tab
                            .Field("Clicks", debug["clicks"])
                            .Add(Ui.Include("debugLine").Label("Last HP").Path(debug["lastExactHpChange"]))
                            .Add(Ui.Include("debugLine").Label("Player Wildcard").Path(debug["lastPlayerWildcard"]))
                            .Add(Ui.Include("debugLine").Label("Inventory Wildcard").Path(debug["lastInventoryWildcard"])))))
                .Build();
        }

        private static System.Numerics.Vector2 ToImGuiVector(Vector2 value)
        {
            return new System.Numerics.Vector2(value.x, value.y);
        }
    }
}
