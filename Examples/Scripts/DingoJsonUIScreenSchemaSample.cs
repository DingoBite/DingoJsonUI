using DingoJsonUI.GUI;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DingoJsonUI.Examples.Scripts
{
    [DefaultExecutionOrder(-50)]
    public sealed class DingoJsonUIScreenSchemaSample : MonoBehaviour
    {
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
            _screen = new UImGuiJsonScreen(Document, JsonUiSchema.FromJson(DingoJsonUISampleData.FastUiSchema), _commands, "02 Schema Screen");
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

        private static System.Numerics.Vector2 ToImGuiVector(Vector2 value)
        {
            return new System.Numerics.Vector2(value.x, value.y);
        }
    }
}
