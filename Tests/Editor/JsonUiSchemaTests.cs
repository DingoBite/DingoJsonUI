#if NEWTONSOFT_EXISTS
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DingoJsonUI.Tests
{
    public sealed class JsonUiSchemaTests
    {
        [Test]
        public void FromJson_ParsesRootAndOptions()
        {
            var schema = JsonUiSchema.FromJson(@"{
                ""title"": ""Menu"",
                ""root"": {
                    ""type"": ""section"",
                    ""children"": [
                        {
                            ""type"": ""select"",
                            ""label"": ""Difficulty"",
                            ""path"": ""$.difficulty"",
                            ""enabledWhen"": { ""path"": ""$.advanced"", ""equals"": true },
                            ""options"": [
                                { ""label"": ""Easy"", ""value"": ""Easy"" },
                                { ""label"": ""Hard"", ""value"": ""Hard"" }
                            ]
                        },
                        {
                            ""type"": ""button"",
                            ""label"": ""Damage"",
                            ""action"": ""damage"",
                            ""payload"": { ""amount"": 25 },
                            ""enabledWhen"": ""$.canDamage""
                        }
                    ]
                }
            }");

            Assert.That(schema.Title, Is.EqualTo("Menu"));
            Assert.That(schema.Root.Type, Is.EqualTo(JsonUiNodeType.Section));
            Assert.That(schema.Root.SafeChildren.Count, Is.EqualTo(2));

            var select = schema.Root.SafeChildren[0];
            Assert.That(select.Type, Is.EqualTo(JsonUiNodeType.Select));
            Assert.That(select.Path, Is.EqualTo("$.difficulty"));
            Assert.That(select.EnabledWhen.Path, Is.EqualTo("$.advanced"));
            Assert.That(select.EnabledWhen.EqualValue.Value<bool>(), Is.True);
            Assert.That(select.SafeOptions.Count, Is.EqualTo(2));
            Assert.That(select.SafeOptions[1].Value.Value<string>(), Is.EqualTo("Hard"));

            var button = schema.Root.SafeChildren[1];
            Assert.That(button.Payload.Value<int>("amount"), Is.EqualTo(25));
            Assert.That(button.EnabledWhen.Path, Is.EqualTo("$.canDamage"));
        }

        [Test]
        public void CommandRegistry_ExecutesRegisteredCommand()
        {
            var document = new JsonDocumentModel(@"{""clicks"":0}");
            var commands = new JsonUiCommandRegistry();

            commands.Register("click", context =>
            {
                var current = context.Document.GetValue("$.clicks", 0);
                var amount = context.Payload?.Value<int>("amount") ?? 1;
                context.Document.SetValue("$.clicks", new JValue(current + amount));
            });

            Assert.That(commands.TryGet("click", out var command), Is.True);

            command.Execute(new JsonUiCommandContext(document, new JsonUiNode { Action = "click" }, "click", null, new JObject { ["amount"] = 3 }));

            Assert.That(document.GetValue<int>("$.clicks"), Is.EqualTo(3));
        }

        [Test]
        public void Condition_EvaluatesComparisons()
        {
            var document = new JsonDocumentModel(@"{""hp"":10,""mode"":""advanced"",""alive"":true}");

            Assert.That(new JsonUiCondition { Path = "$.hp", GreaterThan = 0 }.Evaluate(document), Is.True);
            Assert.That(new JsonUiCondition { Path = "$.mode", EqualValue = new JValue("advanced") }.Evaluate(document), Is.True);
            Assert.That(new JsonUiCondition { Path = "$.alive" }.Evaluate(document), Is.True);
            Assert.That(new JsonUiCondition { Path = "$.missing", Exists = false }.Evaluate(document), Is.True);
        }

        [Test]
        public void Validator_ReturnsActionPathAndTypeDiagnostics()
        {
            var schema = JsonUiSchema.FromJson(@"{
                ""root"": {
                    ""type"": ""section"",
                    ""children"": [
                        { ""type"": ""sliderFlaot"", ""label"": ""Typo"", ""path"": ""$.value"" },
                        { ""type"": ""sliderFloat"", ""label"": ""Missing Path"" },
                        { ""type"": ""button"", ""label"": ""Missing Action"" },
                        { ""type"": ""button"", ""label"": ""Unknown Action"", ""action"": ""missing"" },
                        { ""type"": ""toggle"", ""label"": ""Bad Condition"", ""path"": ""$.flag"", ""visibleWhen"": { ""equals"": true } }
                    ]
                }
            }");

            var commands = new JsonUiCommandRegistry();
            commands.Register("known", _ => { });

            var diagnostics = new JsonUiSchemaValidator().Validate(schema, commands);

            Assert.That(diagnostics, Has.Count.EqualTo(5));
            Assert.That(diagnostics[0].Message, Does.Contain("Unknown widget type"));
            Assert.That(diagnostics[1].Message, Does.Contain("requires 'path'"));
            Assert.That(diagnostics[2].Message, Does.Contain("requires 'action'"));
            Assert.That(diagnostics[3].Message, Does.Contain("Unknown action id"));
            Assert.That(diagnostics[4].Message, Does.Contain("Condition requires 'path'"));
        }
    }
}
#endif
