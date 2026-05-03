#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DingoJsonUI
{
    public static class JsonUi
    {
        public static JsonDocumentModel Document(string json = null)
        {
            return string.IsNullOrWhiteSpace(json)
                ? new JsonDocumentModel()
                : new JsonDocumentModel(json);
        }

        public static JsonDocumentModel Document(JToken token)
        {
            return new JsonDocumentModel(token);
        }

        public static JsonUiSchema Schema(string json = null)
        {
            return JsonUiSchema.FromJson(json);
        }

        public static JsonUiSchema Schema(JToken token)
        {
            return JsonUiSchema.FromToken(token);
        }

        public static JsonUiCommandRegistry Commands(bool registerDefaultPayloadCommands = true)
        {
            var commands = new JsonUiCommandRegistry();
            if (registerDefaultPayloadCommands)
                JsonUiPayloadCommands.RegisterDefaults(commands);

            return commands;
        }

        public static JsonUiSession Session(string json = null, string schemaJson = null, Action<JsonUiCommandRegistry> configureCommands = null, JsonUiOptions options = null)
        {
            var sessionOptions = options?.Clone() ?? new JsonUiOptions();
            var session = new JsonUiSession(options: sessionOptions);

            if (configureCommands != null)
                session.ConfigureCommands(configureCommands);

            if (!string.IsNullOrWhiteSpace(json))
                session.LoadJson(json);

            if (!string.IsNullOrWhiteSpace(schemaJson))
                session.LoadSchemaJson(schemaJson);

            return session;
        }

        public static JsonUiSession Session(JToken json, JToken schemaToken, Action<JsonUiCommandRegistry> configureCommands = null, JsonUiOptions options = null)
        {
            return Session(ToJsonString(json), ToJsonString(schemaToken), configureCommands, options);
        }

        public static IReadOnlyList<JsonUiSchemaDiagnostic> Validate(JsonUiSchema schema, JsonUiCommandRegistry commands = null)
        {
            return new JsonUiSchemaValidator().Validate(schema, commands);
        }

        public static bool IsValid(JsonUiSchema schema, JsonUiCommandRegistry commands = null)
        {
            return new JsonUiSchemaValidator().IsValid(schema, commands);
        }

        private static string ToJsonString(JToken token)
        {
            return token == null || token.Type == JTokenType.Null
                ? null
                : token.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
#endif
