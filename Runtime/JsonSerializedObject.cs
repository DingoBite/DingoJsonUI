#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace DingoJsonUI
{
    public sealed class JsonSerializedObject<T> where T : class
    {
        private static readonly Func<T, JsonSerializer, JToken> DefaultSerializeTarget = (target, serializer) => JToken.FromObject(target, serializer);
        private static readonly Func<JToken, T, JsonSerializer, T> DefaultApplyDocumentToTarget = (token, target, serializer) =>
        {
            using var reader = token.CreateReader();
            serializer.Populate(reader, target);
            return target;
        };

        public T Target { get; private set; }
        public JsonDocumentModel Document { get; }
        public JsonSerializer Serializer { get; }
        public Func<T, JsonSerializer, JToken> SerializeTarget { get; set; }
        public Func<JToken, T, JsonSerializer, T> ApplyDocument { get; set; }

        public JsonSerializedObject(
            T target,
            JsonSerializerSettings serializerSettings = null,
            Func<T, JsonSerializer, JToken> serializeTarget = null,
            Func<JToken, T, JsonSerializer, T> applyDocumentToTarget = null)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Serializer = JsonSerializer.Create(serializerSettings ?? JsonInspectorSerialization.CreateSettings());
            SerializeTarget = serializeTarget ?? DefaultSerializeTarget;
            ApplyDocument = applyDocumentToTarget ?? DefaultApplyDocumentToTarget;
            Document = new JsonDocumentModel();
            ReloadFromTarget(notifyRoot: false);
        }

        public JsonSerializedObject(T target, Func<T, JsonSerializer, JToken> serializeTarget, Func<JToken, T, JsonSerializer, T> applyDocumentToTarget)
            : this(target, null, serializeTarget, applyDocumentToTarget)
        {
        }

        public JsonSerializedObject(T target, Func<T, JToken> serializeTarget, Func<JToken, T, T> applyDocumentToTarget)
            : this(
                target,
                null,
                serializeTarget == null ? null : (value, _) => serializeTarget(value),
                applyDocumentToTarget == null ? null : (token, value, _) => applyDocumentToTarget(token, value))
        {
        }

        public void ReloadFromTarget(bool notifyRoot = true)
        {
            var token = SerializeTarget.Invoke(Target, Serializer) ?? JValue.CreateNull();
            Document.LoadToken(token, notifyRoot);
        }

        public void LoadJson(string json, bool applyToTarget = false, bool notifyRoot = true)
        {
            Document.LoadJson(json, notifyRoot);

            if (applyToTarget)
                ApplyDocumentToTarget();
        }

        public void ApplyDocumentToTarget()
        {
            var rootToken = Document.RootToken?.DeepClone() ?? JValue.CreateNull();
            Target = ApplyDocument.Invoke(rootToken, Target, Serializer) ?? throw new InvalidOperationException("JsonSerializedObject apply delegate returned null target.");
        }

        public bool TryApplyDocumentToTarget(out Exception exception)
        {
            try
            {
                ApplyDocumentToTarget();
                exception = null;
                return true;
            }
            catch (Exception e)
            {
                exception = e;
                return false;
            }
        }

        public string ToJson(Formatting formatting = Formatting.Indented)
        {
            return Document.ToJson(formatting);
        }
    }

    public static class JsonInspectorSerialization
    {
        public static JsonSerializerSettings CreateSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new UnityInspectorContractResolver(),
                Formatting = Formatting.Indented,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
            };
        }
    }

    public sealed class UnityInspectorContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var result = new List<JsonProperty>();
            var propertyNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var member in GetSerializableMembers(type))
            {
                var property = CreateProperty(member, memberSerialization);
                property.Ignored = HasAttribute<JsonIgnoreAttribute>(member);
                property.Readable = CanRead(member);
                property.Writable = CanWrite(member);

                if (!property.Ignored && property.Readable && propertyNames.Add(property.PropertyName))
                    result.Add(property);
            }

            return result;
        }

        private static IEnumerable<MemberInfo> GetSerializableMembers(Type type)
        {
            var hierarchy = new Stack<Type>();
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
                hierarchy.Push(current);

            while (hierarchy.Count > 0)
            {
                var current = hierarchy.Pop();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                foreach (var field in current.GetFields(flags))
                {
                    if (IsSerializableField(field))
                        yield return field;
                }

                foreach (var property in current.GetProperties(flags))
                {
                    if (IsSerializableProperty(property))
                        yield return property;
                }
            }
        }

        private static bool IsSerializableField(FieldInfo field)
        {
            if (field.IsStatic || field.IsLiteral || field.IsInitOnly)
                return false;

            if (HasAttribute<NonSerializedAttribute>(field) || HasAttribute<JsonIgnoreAttribute>(field))
                return false;

            return field.IsPublic || HasAttribute<SerializeField>(field) || HasAttribute<JsonPropertyAttribute>(field);
        }

        private static bool IsSerializableProperty(PropertyInfo property)
        {
            if (property.GetIndexParameters().Length > 0)
                return false;

            if (HasAttribute<JsonIgnoreAttribute>(property))
                return false;

            return HasAttribute<JsonPropertyAttribute>(property) && property.GetMethod != null && property.SetMethod != null;
        }

        private static bool CanRead(MemberInfo member)
        {
            return member switch
            {
                FieldInfo => true,
                PropertyInfo property => property.GetMethod != null,
                _ => false,
            };
        }

        private static bool CanWrite(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => !field.IsInitOnly,
                PropertyInfo property => property.SetMethod != null,
                _ => false,
            };
        }

        private static bool HasAttribute<TAttribute>(MemberInfo member) where TAttribute : Attribute
        {
            return Attribute.IsDefined(member, typeof(TAttribute), inherit: true);
        }
    }
}
#endif
