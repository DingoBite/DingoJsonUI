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
        public void Templates_CreateInstancesWithOverrides()
        {
            var schema = JsonUiSchema.FromJson(@"{
                ""templates"": {
                    ""volumeSlider"": { ""type"": ""sliderFloat"", ""min"": 0, ""max"": 1 },
                    ""toolbar"": {
                        ""type"": ""row"",
                        ""children"": [
                            { ""type"": ""button"", ""label"": ""Apply"", ""action"": ""apply"" }
                        ]
                    }
                },
                ""root"": {
                    ""type"": ""section"",
                    ""children"": [
                        { ""use"": ""volumeSlider"", ""label"": ""Music"", ""path"": ""$.music"", ""width"": 120 },
                        { ""type"": ""include"", ""template"": ""toolbar"" }
                    ]
                }
            }");

            Assert.That(schema.Templates, Contains.Key("volumeSlider"));

            var include = schema.Root.SafeChildren[0];
            Assert.That(schema.TryCreateTemplateInstance(include, out var slider), Is.True);
            Assert.That(slider.Type, Is.EqualTo(JsonUiNodeType.SliderFloat));
            Assert.That(slider.Label, Is.EqualTo("Music"));
            Assert.That(slider.Path, Is.EqualTo("$.music"));
            Assert.That(slider.Min, Is.EqualTo(0f));
            Assert.That(slider.Max, Is.EqualTo(1f));
            Assert.That(slider.Width, Is.EqualTo(120f));

            var commands = new JsonUiCommandRegistry();
            commands.Register("apply", _ => { });

            var diagnostics = new JsonUiSchemaValidator().Validate(schema, commands);
            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void Validator_ReturnsTemplateDiagnostics()
        {
            var schema = JsonUiSchema.FromJson(@"{
                ""templates"": {
                    ""loop"": { ""type"": ""include"", ""template"": ""loop"" }
                },
                ""root"": {
                    ""type"": ""section"",
                    ""children"": [
                        { ""type"": ""include"", ""template"": ""missing"" },
                        { ""type"": ""include"", ""template"": ""loop"" }
                    ]
                }
            }");

            var diagnostics = new JsonUiSchemaValidator().Validate(schema);

            Assert.That(diagnostics, Has.Count.EqualTo(2));
            Assert.That(diagnostics[0].Message, Does.Contain("Unknown template"));
            Assert.That(diagnostics[1].Message, Does.Contain("Recursive template"));
        }

        [Test]
        public void FromJson_ParsesExpandedWidgets()
        {
            var schema = JsonUiSchema.FromJson(@"{
                ""root"": {
                    ""type"": ""section"",
                    ""children"": [
                        { ""type"": ""inputTextMultiline"", ""label"": ""Notes"", ""path"": ""$.notes"", ""height"": 72 },
                        { ""type"": ""dragInt"", ""label"": ""Iterations"", ""path"": ""$.iterations"", ""step"": 1, ""min"": 1, ""max"": 10 },
                        { ""type"": ""dragFloat"", ""label"": ""Intensity"", ""path"": ""$.intensity"", ""step"": 0.05, ""min"": 0, ""max"": 2 },
                        { ""type"": ""vector2"", ""label"": ""Spawn"", ""path"": ""$.spawn"", ""step"": 0.1 },
                        { ""type"": ""vector3"", ""label"": ""Position"", ""path"": ""$.position"", ""step"": 0.1 },
                        { ""type"": ""color"", ""label"": ""Tint"", ""path"": ""$.tint"" },
                        {
                            ""type"": ""radio"",
                            ""label"": ""Quality"",
                            ""path"": ""$.quality"",
                            ""options"": [
                                { ""label"": ""Fast"", ""value"": ""Fast"" },
                                { ""label"": ""Quality"", ""value"": ""Quality"" }
                            ]
                        }
                    ]
                }
            }");

            Assert.That(schema.Root.SafeChildren[0].Type, Is.EqualTo(JsonUiNodeType.InputTextMultiline));
            Assert.That(schema.Root.SafeChildren[0].Height, Is.EqualTo(72f));
            Assert.That(schema.Root.SafeChildren[1].Type, Is.EqualTo(JsonUiNodeType.DragInt));
            Assert.That(schema.Root.SafeChildren[1].Step, Is.EqualTo(1f));
            Assert.That(schema.Root.SafeChildren[4].Type, Is.EqualTo(JsonUiNodeType.Vector3));
            Assert.That(schema.Root.SafeChildren[5].Type, Is.EqualTo(JsonUiNodeType.Color));
            Assert.That(schema.Root.SafeChildren[6].SafeOptions, Has.Count.EqualTo(2));
        }

        [Test]
        public void FromJson_ParsesLayoutPolishProperties()
        {
            var schema = JsonUiSchema.FromJson(@"{
                ""templates"": {
                    ""toolbar"": {
                        ""type"": ""row"",
                        ""spacing"": 6,
                        ""wrap"": true,
                        ""children"": [
                            { ""type"": ""button"", ""label"": ""Apply"", ""action"": ""apply"", ""width"": 96, ""height"": 24 }
                        ]
                    }
                },
                ""root"": {
                    ""type"": ""section"",
                    ""labelWidth"": 120,
                    ""spacing"": 4,
                    ""indent"": 8,
                    ""children"": [
                        { ""type"": ""include"", ""template"": ""toolbar"", ""spacing"": 8 },
                        { ""type"": ""inputText"", ""label"": ""Name"", ""path"": ""$.name"", ""labelWidth"": 90 }
                    ]
                }
            }");

            Assert.That(schema.Root.LabelWidth, Is.EqualTo(120f));
            Assert.That(schema.Root.Spacing, Is.EqualTo(4f));
            Assert.That(schema.Root.Indent, Is.EqualTo(8f));
            Assert.That(schema.Root.SafeChildren[1].LabelWidth, Is.EqualTo(90f));

            Assert.That(schema.TryCreateTemplateInstance(schema.Root.SafeChildren[0], out var toolbar), Is.True);
            Assert.That(toolbar.Type, Is.EqualTo(JsonUiNodeType.Row));
            Assert.That(toolbar.Spacing, Is.EqualTo(8f));
            Assert.That(toolbar.Wrap, Is.True);
            Assert.That(toolbar.SafeChildren[0].Width, Is.EqualTo(96f));
            Assert.That(toolbar.SafeChildren[0].Height, Is.EqualTo(24f));
        }

        [Test]
        public void Validator_ReturnsExpandedWidgetDiagnostics()
        {
            var schema = JsonUiSchema.FromJson(@"{
                ""root"": {
                    ""type"": ""section"",
                    ""children"": [
                        { ""type"": ""dragFloat"", ""label"": ""Bad Step"", ""path"": ""$.value"", ""step"": 0 },
                        { ""type"": ""inputTextMultiline"", ""label"": ""Bad Height"", ""path"": ""$.notes"", ""height"": -1 },
                        { ""type"": ""radio"", ""label"": ""No Options"", ""path"": ""$.mode"" }
                    ]
                }
            }");

            var diagnostics = new JsonUiSchemaValidator().Validate(schema);

            Assert.That(diagnostics, Has.Count.EqualTo(3));
            Assert.That(diagnostics[0].Message, Does.Contain("'step'"));
            Assert.That(diagnostics[1].Message, Does.Contain("'height'"));
            Assert.That(diagnostics[2].Message, Does.Contain("radio has no options"));
        }

        [Test]
        public void Validator_ReturnsLayoutDiagnostics()
        {
            var schema = JsonUiSchema.FromJson(@"{
                ""root"": {
                    ""type"": ""section"",
                    ""children"": [
                        { ""type"": ""inputText"", ""label"": ""Bad Width"", ""path"": ""$.value"", ""width"": 0 },
                        { ""type"": ""inputText"", ""label"": ""Bad Label"", ""path"": ""$.label"", ""labelWidth"": -1 },
                        { ""type"": ""row"", ""spacing"": -1 },
                        { ""type"": ""section"", ""indent"": -2 }
                    ]
                }
            }");

            var diagnostics = new JsonUiSchemaValidator().Validate(schema);

            Assert.That(diagnostics, Has.Count.EqualTo(4));
            Assert.That(diagnostics[0].Message, Does.Contain("'width'"));
            Assert.That(diagnostics[1].Message, Does.Contain("'labelWidth'"));
            Assert.That(diagnostics[2].Message, Does.Contain("'spacing'"));
            Assert.That(diagnostics[3].Message, Does.Contain("'indent'"));
        }

        [Test]
        public void LargeData_CalculatesVisibleRanges()
        {
            var firstPage = JsonUiLargeData.CalculateVisibleRange(260, 0, 128);
            Assert.That(firstPage.IsPaged, Is.True);
            Assert.That(firstPage.Offset, Is.EqualTo(0));
            Assert.That(firstPage.Count, Is.EqualTo(128));
            Assert.That(firstPage.HasPrevious, Is.False);
            Assert.That(firstPage.HasNext, Is.True);

            var secondPage = JsonUiLargeData.CalculateVisibleRange(260, 129, 128);
            Assert.That(secondPage.Offset, Is.EqualTo(128));
            Assert.That(secondPage.Count, Is.EqualTo(128));

            var lastPage = JsonUiLargeData.CalculateVisibleRange(260, 999, 128);
            Assert.That(lastPage.Offset, Is.EqualTo(256));
            Assert.That(lastPage.Count, Is.EqualTo(4));
            Assert.That(lastPage.HasNext, Is.False);
        }

        [Test]
        public void LargeData_DisablesPagingWhenBelowLimitOrLimitDisabled()
        {
            var small = JsonUiLargeData.CalculateVisibleRange(8, 4, 128);
            Assert.That(small.IsPaged, Is.False);
            Assert.That(small.Offset, Is.EqualTo(0));
            Assert.That(small.Count, Is.EqualTo(8));

            var disabled = JsonUiLargeData.CalculateVisibleRange(260, 128, 0);
            Assert.That(disabled.IsPaged, Is.False);
            Assert.That(disabled.Count, Is.EqualTo(260));
        }

        [Test]
        public void JsonUiSession_RegistersDefaultPayloadCommandsAndValidates()
        {
            var session = JsonUi.Session(@"{""credits"":100}", @"{
                ""root"": {
                    ""type"": ""section"",
                    ""children"": [
                        { ""type"": ""button"", ""label"": ""+50"", ""action"": ""payload.add"", ""payload"": { ""path"": ""$.credits"", ""amount"": 50 } }
                    ]
                }
            }");

            Assert.That(session.Commands.TryGet(JsonUiPayloadCommands.Add, out var add), Is.True);
            Assert.That(session.HasErrors, Is.False);

            add.Execute(new JsonUiCommandContext(session.Document, new JsonUiNode(), JsonUiPayloadCommands.Add, null, new JObject
            {
                ["path"] = "$.credits",
                ["amount"] = 50,
            }));

            Assert.That(session.GetValue("$.credits", 0), Is.EqualTo(150));
        }

        [Test]
        public void JsonUiSession_KeepsPreviousSchemaOnParseFailure()
        {
            var session = JsonUi.Session(schemaJson: @"{
                ""root"": {
                    ""type"": ""section"",
                    ""children"": [
                        { ""type"": ""text"", ""text"": ""ok"" }
                    ]
                }
            }");
            var previousSchema = session.Schema;

            Assert.That(session.LoadSchemaJson("{ bad json"), Is.False);
            Assert.That(session.Schema, Is.SameAs(previousSchema));
            Assert.That(session.HasErrors, Is.True);
            Assert.That(session.Diagnostics[0].SchemaPath, Is.EqualTo(JsonUiSession.SchemaSourceDiagnosticPath));
        }

        [Test]
        public void JsonUiSession_FacadeReturnsDiagnosticsForInvalidSources()
        {
            var session = JsonUi.Session(json: "{ bad json", schemaJson: "{ bad schema");

            Assert.That(session, Is.Not.Null);
            Assert.That(session.HasErrors, Is.True);
            Assert.That(session.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(session.Diagnostics[0].SchemaPath, Is.EqualTo(JsonUiSession.JsonSourceDiagnosticPath));
            Assert.That(session.Diagnostics[1].SchemaPath, Is.EqualTo(JsonUiSession.SchemaSourceDiagnosticPath));
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
        public void PayloadCommands_ModifyDocumentFromPayload()
        {
            var document = new JsonDocumentModel(@"{""credits"":100,""enabled"":false,""source"":""copied"",""target"":""""}");
            var commands = new JsonUiCommandRegistry();
            JsonUiPayloadCommands.RegisterDefaults(commands);

            commands.TryGet(JsonUiPayloadCommands.Add, out var add);
            add.Execute(new JsonUiCommandContext(document, new JsonUiNode(), JsonUiPayloadCommands.Add, null, new JObject
            {
                ["path"] = "$.credits",
                ["amount"] = 50,
                ["max"] = 125,
            }));

            commands.TryGet(JsonUiPayloadCommands.Toggle, out var toggle);
            toggle.Execute(new JsonUiCommandContext(document, new JsonUiNode(), JsonUiPayloadCommands.Toggle, null, new JObject
            {
                ["path"] = "$.enabled",
            }));

            commands.TryGet(JsonUiPayloadCommands.Copy, out var copy);
            copy.Execute(new JsonUiCommandContext(document, new JsonUiNode(), JsonUiPayloadCommands.Copy, null, new JObject
            {
                ["from"] = "$.source",
                ["to"] = "$.target",
            }));

            commands.TryGet(JsonUiPayloadCommands.Set, out var set);
            set.Execute(new JsonUiCommandContext(document, new JsonUiNode(), JsonUiPayloadCommands.Set, null, new JObject
            {
                ["path"] = "$.mode",
                ["value"] = "Debug",
            }));

            Assert.That(document.GetValue<int>("$.credits"), Is.EqualTo(125));
            Assert.That(document.GetValue<bool>("$.enabled"), Is.True);
            Assert.That(document.GetValue<string>("$.target"), Is.EqualTo("copied"));
            Assert.That(document.GetValue<string>("$.mode"), Is.EqualTo("Debug"));
        }

        [Test]
        public void Validator_ReturnsPayloadCommandDiagnostics()
        {
            var schema = JsonUiSchema.FromJson(@"{
                ""root"": {
                    ""type"": ""section"",
                    ""children"": [
                        { ""type"": ""button"", ""label"": ""Bad Set"", ""action"": ""payload.set"", ""payload"": { ""path"": ""$.value"" } },
                        { ""type"": ""button"", ""label"": ""Bad Add"", ""action"": ""payload.add"", ""payload"": { ""path"": ""$.value"", ""amount"": ""many"" } },
                        { ""type"": ""button"", ""label"": ""Bad Copy"", ""action"": ""payload.copy"", ""payload"": { ""from"": ""$.source"" } }
                    ]
                }
            }");

            var commands = new JsonUiCommandRegistry();
            JsonUiPayloadCommands.RegisterDefaults(commands);

            var diagnostics = new JsonUiSchemaValidator().Validate(schema, commands);

            Assert.That(diagnostics, Has.Count.EqualTo(3));
            Assert.That(diagnostics[0].Message, Does.Contain("payload.value"));
            Assert.That(diagnostics[1].Message, Does.Contain("payload.amount"));
            Assert.That(diagnostics[2].Message, Does.Contain("payload.to"));
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
