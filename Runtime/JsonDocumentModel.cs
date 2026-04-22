#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI
{
    public sealed class JsonDocumentModel
    {
        private sealed class SubscriptionEntry
        {
            public JsonPathPattern Pattern { get; set; }
            public Action<JsonChange> Callback { get; set; }
        }

        private readonly List<SubscriptionEntry> _subscriptions = new();
        private JToken _root;

        public JToken RootToken => _root;

        public JsonDocumentModel()
        {
            _root = new JObject();
        }

        public JsonDocumentModel(string json)
        {
            _root = ParseOrDefault(json);
        }

        public JsonDocumentModel(JToken token)
        {
            _root = token?.DeepClone() ?? JValue.CreateNull();
        }

        public bool TryLoadJson(string json, out Exception exception, bool notifyRoot = true)
        {
            try
            {
                LoadJson(json, notifyRoot);
                exception = null;
                return true;
            }
            catch (Exception e)
            {
                exception = e;
                return false;
            }
        }

        public void LoadJson(string json, bool notifyRoot = true)
        {
            LoadToken(ParseOrDefault(json), notifyRoot);
        }

        public void LoadToken(JToken token, bool notifyRoot = true)
        {
            var previousRoot = _root?.DeepClone();
            _root = token?.DeepClone() ?? JValue.CreateNull();

            if (!notifyRoot || JToken.DeepEquals(previousRoot, _root))
                return;

            NotifyAffectedPaths(previousRoot, _root, JsonPath.Root);
        }

        public JsonPathSubscription Subscribe(string path, Action<JsonChange> callback, bool fireImmediately = false)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            var entry = new SubscriptionEntry
            {
                Pattern = new JsonPathPattern(path),
                Callback = callback,
            };

            _subscriptions.Add(entry);

            if (fireImmediately)
            {
                var normalizedPath = entry.Pattern.Pattern;
                var currentValue = CloneToken(SelectToken(_root, normalizedPath));
                callback.Invoke(new JsonChange(normalizedPath, CloneToken(currentValue), currentValue));
            }

            return new JsonPathSubscription(() => _subscriptions.Remove(entry));
        }

        public JToken GetToken(string path)
        {
            return CloneToken(SelectToken(_root, JsonPath.Normalize(path)));
        }

        public bool TryGetToken(string path, out JToken token)
        {
            token = CloneToken(SelectToken(_root, JsonPath.Normalize(path)));
            return token != null;
        }

        public T GetValue<T>(string path, T defaultValue = default)
        {
            var token = SelectToken(_root, JsonPath.Normalize(path));
            if (token == null)
                return defaultValue;

            try
            {
                return token.ToObject<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        public bool SetValue<T>(string path, T value)
        {
            return SetValue(path, value is JToken token ? token : value == null ? JValue.CreateNull() : JToken.FromObject(value));
        }

        public bool SetValue(string path, JToken value)
        {
            var normalizedPath = JsonPath.Normalize(path);
            var nextValue = value?.DeepClone() ?? JValue.CreateNull();
            var previousRoot = _root?.DeepClone();

            if (!ApplyValue(normalizedPath, nextValue))
                return false;

            if (JToken.DeepEquals(previousRoot, _root))
                return true;

            NotifyAffectedPaths(previousRoot, _root, normalizedPath);
            return true;
        }

        public string ToJson(Formatting formatting = Formatting.Indented)
        {
            return _root?.ToString(formatting) ?? string.Empty;
        }

        private bool ApplyValue(string normalizedPath, JToken nextValue)
        {
            if (normalizedPath == JsonPath.Root)
            {
                _root = nextValue;
                return true;
            }

            var existingToken = SelectToken(_root, normalizedPath);
            if (existingToken != null)
            {
                existingToken.Replace(nextValue);
                return true;
            }

            var segments = JsonPath.ParseSegments(normalizedPath);
            if (segments.Count == 0)
            {
                _root = nextValue;
                return true;
            }

            if (_root is null or JValue)
                _root = CreateContainerFor(segments[0]);

            var current = _root;
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var isLast = i == segments.Count - 1;

                switch (segment.Kind)
                {
                    case JsonPathSegmentKind.Property:
                        if (current is not JObject currentObject)
                            return false;

                        if (!TryResolveProperty(currentObject, segment.PropertyName, isLast, isLast ? nextValue : CreateContainerFor(segments[i + 1]), out current))
                            return false;
                        break;

                    case JsonPathSegmentKind.Index:
                        if (current is not JArray currentArray)
                            return false;

                        if (!TryResolveIndex(currentArray, segment.Index, isLast, isLast ? nextValue : CreateContainerFor(segments[i + 1]), out current))
                            return false;
                        break;

                    default:
                        return false;
                }
            }

            return true;
        }

        private static bool TryResolveProperty(JObject currentObject, string propertyName, bool isLast, JToken fallbackValue, out JToken resolvedToken)
        {
            if (currentObject.TryGetValue(propertyName, out resolvedToken))
            {
                if (isLast)
                {
                    resolvedToken.Replace(fallbackValue);
                    resolvedToken = currentObject[propertyName];
                    return true;
                }

                if (resolvedToken is JObject or JArray)
                    return true;

                if (resolvedToken.Type == JTokenType.Null)
                {
                    currentObject[propertyName] = fallbackValue;
                    resolvedToken = currentObject[propertyName];
                    return true;
                }

                return false;
            }

            currentObject[propertyName] = fallbackValue;
            resolvedToken = currentObject[propertyName];
            return true;
        }

        private static bool TryResolveIndex(JArray currentArray, int index, bool isLast, JToken fallbackValue, out JToken resolvedToken)
        {
            if (index < 0)
            {
                resolvedToken = null;
                return false;
            }

            while (currentArray.Count <= index)
                currentArray.Add(JValue.CreateNull());

            resolvedToken = currentArray[index];
            if (isLast)
            {
                currentArray[index] = fallbackValue;
                resolvedToken = currentArray[index];
                return true;
            }

            if (resolvedToken is JObject or JArray)
                return true;

            if (resolvedToken.Type == JTokenType.Null)
            {
                currentArray[index] = fallbackValue;
                resolvedToken = currentArray[index];
                return true;
            }

            return false;
        }

        private void NotifyAffectedPaths(JToken previousRoot, JToken currentRoot, string changedPath)
        {
            if (_subscriptions.Count == 0)
                return;

            var snapshot = _subscriptions.ToArray();
            var affectedPaths = JsonPath.GetAffectedPaths(changedPath);

            for (var subscriptionIndex = 0; subscriptionIndex < snapshot.Length; subscriptionIndex++)
            {
                var entry = snapshot[subscriptionIndex];

                for (var pathIndex = 0; pathIndex < affectedPaths.Count; pathIndex++)
                {
                    var affectedPath = affectedPaths[pathIndex];
                    if (!entry.Pattern.IsMatch(affectedPath))
                        continue;

                    var previousValue = CloneToken(SelectToken(previousRoot, affectedPath));
                    var currentValue = CloneToken(SelectToken(currentRoot, affectedPath));
                    entry.Callback.Invoke(new JsonChange(affectedPath, previousValue, currentValue));
                }
            }
        }

        private static JToken SelectToken(JToken root, string path)
        {
            if (root == null)
                return null;

            return path == JsonPath.Root ? root : root.SelectToken(path, false);
        }

        private static JToken CloneToken(JToken token) => token?.DeepClone();

        private static JToken CreateContainerFor(JsonPathSegment nextSegment)
        {
            return nextSegment.Kind switch
            {
                JsonPathSegmentKind.Index or JsonPathSegmentKind.AnyIndex => new JArray(),
                _ => new JObject(),
            };
        }

        private static JToken ParseOrDefault(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new JObject();

            return JToken.Parse(json);
        }
    }
}
#endif
