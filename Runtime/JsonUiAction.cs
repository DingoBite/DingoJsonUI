#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI
{
    public enum JsonUiActionPlacement
    {
        Toolbar,
        Inline,
    }

    public readonly struct JsonUiActionContext
    {
        public JsonDocumentModel Document { get; }
        public string Path { get; }
        public JToken Token { get; }
        public JsonUiAction Action { get; }

        public JsonUiActionContext(JsonDocumentModel document, string path, JToken token, JsonUiAction action)
        {
            Document = document;
            Path = JsonPath.Normalize(path);
            Token = token;
            Action = action;
        }
    }

    public sealed class JsonUiAction
    {
        public string Path { get; }
        public string Label { get; }
        public string Tooltip { get; set; }
        public JsonUiActionPlacement Placement { get; }
        public Func<JsonUiActionContext, bool> IsVisible { get; set; }
        public Func<JsonUiActionContext, bool> IsEnabled { get; set; }
        public Action<JsonUiActionContext> Callback { get; }

        internal string NormalizedPath { get; }

        public JsonUiAction(string path, string label, Action<JsonUiActionContext> callback, JsonUiActionPlacement placement = JsonUiActionPlacement.Inline)
        {
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Action label cannot be empty.", nameof(label));

            Path = string.IsNullOrWhiteSpace(path) ? JsonPath.Root : path;
            NormalizedPath = JsonPath.Normalize(Path);
            Label = label;
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            Placement = placement;
        }

        public bool CanDraw(JsonUiActionContext context)
        {
            return IsVisible?.Invoke(context) ?? true;
        }

        public bool CanExecute(JsonUiActionContext context)
        {
            return IsEnabled?.Invoke(context) ?? true;
        }

        public void Execute(JsonUiActionContext context)
        {
            Callback.Invoke(context);
        }
    }

    public sealed class JsonUiActionCollection
    {
        private readonly List<JsonUiAction> _actions = new();

        public IReadOnlyList<JsonUiAction> Actions => _actions;

        public JsonUiAction Add(JsonUiAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _actions.Add(action);
            return action;
        }

        public JsonUiAction AddButton(string path, string label, Action<JsonUiActionContext> callback, JsonUiActionPlacement placement = JsonUiActionPlacement.Inline)
        {
            return Add(new JsonUiAction(path, label, callback, placement));
        }

        public bool Remove(JsonUiAction action)
        {
            return action != null && _actions.Remove(action);
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public void Fill(string path, JsonUiActionPlacement placement, ICollection<JsonUiAction> results, JsonDocumentModel document = null, JToken token = null)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            var normalizedPath = JsonPath.Normalize(path);

            for (var i = 0; i < _actions.Count; i++)
            {
                var action = _actions[i];
                if (action.Placement != placement || !string.Equals(action.NormalizedPath, normalizedPath, StringComparison.Ordinal))
                    continue;

                var context = new JsonUiActionContext(document, normalizedPath, token, action);
                if (action.CanDraw(context))
                    results.Add(action);
            }
        }
    }
}
#endif
