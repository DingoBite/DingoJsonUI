#if NEWTONSOFT_EXISTS
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DingoJsonUI.Tests
{
    public class JsonDocumentModelTests
    {
        [Test]
        public void SetValue_NotifiesExactPath()
        {
            var document = new JsonDocumentModel(@"{""player"":{""hp"":100}}");
            JsonChange received = default;
            var called = false;

            using var subscription = document.Subscribe("$.player.hp", change =>
            {
                called = true;
                received = change;
            });

            var changed = document.SetValue("$.player.hp", new JValue(75));

            Assert.That(changed, Is.True);
            Assert.That(called, Is.True);
            Assert.That(received.Path, Is.EqualTo("$.player.hp"));
            Assert.That(received.PreviousValue?.Value<int>(), Is.EqualTo(100));
            Assert.That(received.CurrentValue?.Value<int>(), Is.EqualTo(75));
        }

        [Test]
        public void SetValue_NotifiesAncestorsAndWildcards()
        {
            var document = new JsonDocumentModel(@"{""player"":{""stats"":{""hp"":100,""mp"":50}}}");
            var paths = new List<string>();

            using var wildcard = document.Subscribe("$.player.stats.*", change => paths.Add("wild:" + change.Path));
            using var parent = document.Subscribe("$.player.stats", change => paths.Add("parent:" + change.Path));
            using var root = document.Subscribe("$", change => paths.Add("root:" + change.Path));

            document.SetValue("$.player.stats.hp", new JValue(25));

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "wild:$.player.stats.hp",
                    "parent:$.player.stats",
                    "root:$",
                },
                paths);
        }

        [Test]
        public void SetValue_CreatesMissingObjectPath()
        {
            var document = new JsonDocumentModel("{}");

            var changed = document.SetValue("$.settings.graphics.vsync", new JValue(true));

            Assert.That(changed, Is.True);
            Assert.That(document.GetValue<bool>("$.settings.graphics.vsync"), Is.True);
            Assert.That(document.GetToken("$.settings.graphics"), Is.TypeOf<JObject>());
        }
    }
}
#endif
