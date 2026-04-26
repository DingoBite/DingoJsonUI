#if NEWTONSOFT_EXISTS
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DingoJsonUI.Tests
{
    public class JsonSerializedObjectTests
    {
        [Serializable]
        private sealed class InspectorTarget
        {
            public int Health = 10;

            [SerializeField]
            [JsonProperty("displayName")]
            private string _displayName = "Dingo";

            [NonSerialized]
            public int RuntimeOnly = 99;

            public string DisplayName => _displayName;
        }

        private sealed class DelegateTarget
        {
            public int RawValue = 2;
        }

        [Test]
        public void JsonSerializedObject_UsesUnityInspectorLikeFields()
        {
            var target = new InspectorTarget();
            var serializedObject = new JsonSerializedObject<InspectorTarget>(target);

            Assert.That(serializedObject.Document.GetValue<int>("$.Health"), Is.EqualTo(10));
            Assert.That(serializedObject.Document.GetValue<string>("$.displayName"), Is.EqualTo("Dingo"));
            Assert.That(serializedObject.Document.TryGetToken("$.RuntimeOnly", out _), Is.False);

            serializedObject.Document.SetValue("$.Health", new JValue(25));
            serializedObject.Document.SetValue("$.displayName", new JValue("Player"));
            serializedObject.ApplyDocumentToTarget();

            Assert.That(target.Health, Is.EqualTo(25));
            Assert.That(target.DisplayName, Is.EqualTo("Player"));
            Assert.That(target.RuntimeOnly, Is.EqualTo(99));
        }

        [Test]
        public void JsonSerializedObject_UsesCustomSerializeDelegates()
        {
            var target = new DelegateTarget();
            var serializedObject = new JsonSerializedObject<DelegateTarget>(
                target,
                (value, _) => new JObject
                {
                    ["display"] = value.RawValue * 10,
                },
                (token, value, _) =>
                {
                    value.RawValue = token.Value<int>("display") / 10;
                    return value;
                });

            Assert.That(serializedObject.Document.GetValue<int>("$.display"), Is.EqualTo(20));

            serializedObject.Document.SetValue("$.display", new JValue(70));
            serializedObject.ApplyDocumentToTarget();

            Assert.That(target.RawValue, Is.EqualTo(7));
            Assert.That(serializedObject.Target, Is.SameAs(target));
        }

        [Test]
        public void JsonUiActionCollection_FiltersButtonsByPathAndPlacement()
        {
            var document = new JsonDocumentModel(@"{""player"":{""hp"":10}}");
            var actions = new JsonUiActionCollection();
            var results = new System.Collections.Generic.List<JsonUiAction>();
            var called = false;

            actions.AddButton("$.player.hp", "Heal", context =>
            {
                called = true;
                context.Document.SetValue(context.Path, new JValue(100));
            });

            actions.Fill("$.player.hp", JsonUiActionPlacement.Inline, results, document, document.GetToken("$.player.hp"));

            Assert.That(results.Count, Is.EqualTo(1));

            var action = results[0];
            var context = new JsonUiActionContext(document, "$.player.hp", document.GetToken("$.player.hp"), action);
            action.Execute(context);

            Assert.That(called, Is.True);
            Assert.That(document.GetValue<int>("$.player.hp"), Is.EqualTo(100));
        }
    }
}
#endif
