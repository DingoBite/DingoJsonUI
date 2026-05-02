#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI
{
    public static class JsonUiPayloadCommands
    {
        public const string Set = "payload.set";
        public const string Add = "payload.add";
        public const string Toggle = "payload.toggle";
        public const string Copy = "payload.copy";

        public static void RegisterDefaults(JsonUiCommandRegistry registry, bool replaceExisting = true)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            Register(registry, Set, ExecuteSet, replaceExisting);
            Register(registry, Add, ExecuteAdd, replaceExisting);
            Register(registry, Toggle, ExecuteToggle, replaceExisting);
            Register(registry, Copy, ExecuteCopy, replaceExisting);
        }

        public static void ValidatePayload(JsonUiNode node, string schemaPath, ICollection<JsonUiSchemaDiagnostic> diagnostics)
        {
            if (node == null || diagnostics == null)
                return;

            switch (node.Action)
            {
                case Set:
                    ValidatePath(GetPayloadPathRaw(node), schemaPath, "payload.path", diagnostics);
                    if (node.Payload is not JObject setPayload || !setPayload.ContainsKey("value"))
                        AddError(diagnostics, schemaPath, "payload.set requires payload.value.");
                    break;
                case Add:
                    ValidatePath(GetPayloadPathRaw(node), schemaPath, "payload.path", diagnostics);
                    ValidateNumber(GetPayloadToken(node.Payload, "amount"), schemaPath, "payload.amount", diagnostics, required: false);
                    ValidateNumber(GetPayloadToken(node.Payload, "min"), schemaPath, "payload.min", diagnostics, required: false);
                    ValidateNumber(GetPayloadToken(node.Payload, "max"), schemaPath, "payload.max", diagnostics, required: false);
                    ValidateMinMax(node.Payload, schemaPath, diagnostics);
                    break;
                case Toggle:
                    ValidatePath(GetPayloadPathRaw(node), schemaPath, "payload.path", diagnostics);
                    break;
                case Copy:
                    ValidatePath(GetPayloadString(node.Payload, "from"), schemaPath, "payload.from", diagnostics);
                    ValidatePath(GetPayloadString(node.Payload, "to"), schemaPath, "payload.to", diagnostics);
                    break;
            }
        }

        private static void Register(JsonUiCommandRegistry registry, string id, Action<JsonUiCommandContext> callback, bool replaceExisting)
        {
            if (!replaceExisting && registry.TryGet(id, out _))
                return;

            registry.Register(id, callback);
        }

        private static void ExecuteSet(JsonUiCommandContext context)
        {
            var path = GetPayloadPath(context);
            if (path == null)
                return;

            var value = context.Payload is JObject payload && payload.TryGetValue("value", out var token)
                ? token
                : JValue.CreateNull();

            context.Document.SetValue(path, value);
        }

        private static void ExecuteAdd(JsonUiCommandContext context)
        {
            var path = GetPayloadPath(context);
            if (path == null)
                return;

            var currentToken = context.Document.GetToken(path);
            if (!TryGetDouble(currentToken, out var current))
                current = 0d;

            var payload = context.Payload as JObject;
            var amountToken = payload?["amount"];
            var amount = TryGetDouble(amountToken, out var parsedAmount) ? parsedAmount : 1d;
            var next = current + amount;

            if (TryGetDouble(payload?["min"], out var min))
                next = Math.Max(min, next);

            if (TryGetDouble(payload?["max"], out var max))
                next = Math.Min(max, next);

            var integralResult = IsIntegral(currentToken)
                                 && IsIntegral(amountToken)
                                 && IsIntegral(payload?["min"])
                                 && IsIntegral(payload?["max"]);
            context.Document.SetValue(path, integralResult ? new JValue(Convert.ToInt64(next)) : new JValue(next));
        }

        private static void ExecuteToggle(JsonUiCommandContext context)
        {
            var path = GetPayloadPath(context);
            if (path == null)
                return;

            var current = context.Document.GetValue(path, false);
            context.Document.SetValue(path, new JValue(!current));
        }

        private static void ExecuteCopy(JsonUiCommandContext context)
        {
            var from = GetPayloadString(context.Payload, "from");
            var to = GetPayloadString(context.Payload, "to");
            if (from == null || to == null)
                return;

            context.Document.SetValue(to, context.Document.GetToken(from) ?? JValue.CreateNull());
        }

        private static string GetPayloadPath(JsonUiCommandContext context)
        {
            return NormalizePath(GetPayloadString(context.Payload, "path") ?? context.Node?.Path);
        }

        private static string GetPayloadPathRaw(JsonUiNode node)
        {
            return GetPayloadString(node.Payload, "path") ?? node.Path;
        }

        private static string GetPayloadString(JToken payload, string property)
        {
            return payload is JObject obj ? obj.Value<string>(property) : null;
        }

        private static JToken GetPayloadToken(JToken payload, string property)
        {
            return payload is JObject obj && obj.TryGetValue(property, out var token) ? token : null;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                return JsonPath.Normalize(path);
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetDouble(JToken token, out double value)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                value = 0d;
                return false;
            }

            try
            {
                value = token.Value<double>();
                return true;
            }
            catch
            {
                value = 0d;
                return false;
            }
        }

        private static bool IsIntegral(JToken token)
        {
            return token == null || token.Type == JTokenType.Integer;
        }

        private static void ValidatePath(string path, string schemaPath, string propertyName, ICollection<JsonUiSchemaDiagnostic> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                AddError(diagnostics, schemaPath, $"{propertyName} is required.");
                return;
            }

            try
            {
                JsonPath.Normalize(path);
            }
            catch (Exception e)
            {
                AddError(diagnostics, schemaPath, $"Invalid {propertyName} '{path}': {e.Message}");
            }
        }

        private static void ValidateNumber(JToken token, string schemaPath, string propertyName, ICollection<JsonUiSchemaDiagnostic> diagnostics, bool required)
        {
            if (token == null)
            {
                if (required)
                    AddError(diagnostics, schemaPath, $"{propertyName} is required.");

                return;
            }

            if (!TryGetDouble(token, out _))
                AddError(diagnostics, schemaPath, $"{propertyName} must be numeric.");
        }

        private static void ValidateMinMax(JToken payload, string schemaPath, ICollection<JsonUiSchemaDiagnostic> diagnostics)
        {
            if (!TryGetDouble(GetPayloadToken(payload, "min"), out var min) || !TryGetDouble(GetPayloadToken(payload, "max"), out var max))
                return;

            if (min > max)
                AddError(diagnostics, schemaPath, "payload.min cannot be greater than payload.max.");
        }

        private static void AddError(ICollection<JsonUiSchemaDiagnostic> diagnostics, string schemaPath, string message)
        {
            diagnostics.Add(new JsonUiSchemaDiagnostic(JsonUiSchemaDiagnosticSeverity.Error, schemaPath, message));
        }
    }
}
#endif
