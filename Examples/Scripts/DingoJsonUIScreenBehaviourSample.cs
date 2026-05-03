using DingoJsonUI.GUI;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DingoJsonUI.Examples.Scripts
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UImGuiJsonScreenBehaviour))]
    public sealed class DingoJsonUIScreenBehaviourSample : MonoBehaviour
    {
        [SerializeField] private UImGuiJsonScreenBehaviour _screen;
        [SerializeField] private Vector2 _initialWindowPosition = new(530f, 520f);
        [SerializeField] private Vector2 _initialWindowSize = new(500f, 240f);

        private void Reset()
        {
            _screen = GetComponent<UImGuiJsonScreenBehaviour>();
        }

        private void OnEnable()
        {
            Configure();
        }

        [ContextMenu("Configure Screen Behaviour Sample")]
        public void Configure()
        {
            _screen ??= GetComponent<UImGuiJsonScreenBehaviour>();
            if (_screen == null)
                return;

            _screen.WindowTitle = "04 Screen Behaviour";
            _screen.ShowDiagnostics = true;
            _screen.ShowDiagnosticsWhenValid = true;
            _screen.ApplyInitialWindowPlacement = true;
            _screen.InitialWindowPosition = _initialWindowPosition;
            _screen.InitialWindowSize = _initialWindowSize;

            _screen.LoadJsonToken(DingoJsonUISampleData.CreateBehaviourJsonToken());
            var session = _screen.Session;
            session.Commands.Clear();
            session.RegisterDefaultPayloadCommands();
            session.RegisterCommand("resetBehaviourJson", _ => session.LoadToken(DingoJsonUISampleData.CreateBehaviourJsonToken()));
            session.RegisterCommand("buyUpgrade", context =>
            {
                var price = context.Payload?["price"]?.Value<int>() ?? 75;
                var credits = context.Document.GetValue("$.menu.credits", 0);
                if (credits < price)
                    return;

                context.Document.SetValue("$.menu.credits", new JValue(credits - price));
                var progress = context.Document.GetValue("$.menu.progress", 0f);
                context.Document.SetValue("$.menu.progress", new JValue(Mathf.Min(1f, progress + 0.25f)));
                SetLastCommand(context.Document, $"upgrade -{price}");
            });

            _screen.LoadSchemaToken(DingoJsonUISampleData.CreateBehaviourSchemaToken());
        }

        private static void SetLastCommand(JsonDocumentModel document, string value)
        {
            document.SetValue("$.debug.lastCommand", new JValue(value));
        }
    }
}
