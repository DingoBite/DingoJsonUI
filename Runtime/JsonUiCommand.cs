#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI
{
    public readonly struct JsonUiCommandContext
    {
        public JsonDocumentModel Document { get; }
        public JsonUiNode Node { get; }
        public string ActionId { get; }
        public JToken Value { get; }
        public JToken Payload { get; }

        public JsonUiCommandContext(JsonDocumentModel document, JsonUiNode node, string actionId, JToken value, JToken payload = null)
        {
            Document = document;
            Node = node;
            ActionId = actionId;
            Value = value;
            Payload = payload?.DeepClone();
        }
    }

    public sealed class JsonUiCommand
    {
        public string Id { get; }
        public Action<JsonUiCommandContext> Callback { get; }
        public Func<JsonUiCommandContext, bool> IsVisible { get; set; }
        public Func<JsonUiCommandContext, bool> IsEnabled { get; set; }

        public JsonUiCommand(string id, Action<JsonUiCommandContext> callback)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Command id cannot be empty.", nameof(id));

            Id = id;
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public bool CanDraw(JsonUiCommandContext context)
        {
            return IsVisible?.Invoke(context) ?? true;
        }

        public bool CanExecute(JsonUiCommandContext context)
        {
            return IsEnabled?.Invoke(context) ?? true;
        }

        public void Execute(JsonUiCommandContext context)
        {
            Callback.Invoke(context);
        }
    }

    public sealed class JsonUiCommandRegistry
    {
        private readonly Dictionary<string, JsonUiCommand> _commands = new(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, JsonUiCommand> Commands => _commands;

        public JsonUiCommand Register(string id, Action<JsonUiCommandContext> callback)
        {
            var command = new JsonUiCommand(id, callback);
            _commands[id] = command;
            return command;
        }

        public bool Remove(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && _commands.Remove(id);
        }

        public void Clear()
        {
            _commands.Clear();
        }

        public bool TryGet(string id, out JsonUiCommand command)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                command = null;
                return false;
            }

            return _commands.TryGetValue(id, out command);
        }
    }
}
#endif
