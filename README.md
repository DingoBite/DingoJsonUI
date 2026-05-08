# DingoJsonUI

[Русская версия](README_ru.md)

`DingoJsonUI` is an independent Unity module for building runtime UI from JSON state.

The module keeps application state in a Newtonsoft `JToken` document, exposes JSONPath-based change subscriptions, and can render that state through `UImGui` either as a raw JSON editor or as a schema-driven fast UI screen.

This file is the module-level technical documentation for the repository itself.

## Why this module exists

Unity runtime tools and prototype menus often need the same pieces again and again:

- editable runtime state without writing a custom `MonoBehaviour` inspector for every test;
- fast menu layout that can be generated from a compact JSON schema;
- buttons and actions that stay decoupled from the rendered UI;
- diagnostics that make AI-generated or hand-written schemas safe to iterate on;
- a lightweight bridge from `[Serializable]` C# objects into JSON-backed UI.

`DingoJsonUI` packages those concerns into one small runtime layer. It is meant for quick debug panels, gameplay tuning menus, AI-assisted UI experiments, test harnesses, and simple in-game tools.

It is not a full retained UI framework and not a replacement for Unity UI Toolkit, uGUI, or a production design system. The main goal is fast iteration over mechanical UI and runtime state.

## Key advantages

- JSON is the source of truth, so UI can be generated, copied, patched, diffed, or logged easily.
- JSONPath subscriptions let systems react to exact paths, one-level wildcards, and ancestor changes.
- The raw editor works immediately for arbitrary `JObject`, `JArray`, scalar, and `null` values.
- The schema renderer supports practical menu widgets: tabs, rows, columns, sliders, toggles, vectors, colors, selects, radio groups, progress bars, and buttons.
- Command callbacks are registered by id, while common button behavior can be driven purely by payload data.
- Schema diagnostics and preview reports keep malformed generated UI from failing silently.
- Large JSON documents are guarded by paging, scroll tuning, depth limits, and object property-name caching.
- `JsonSerializedObject<T>` can inspect Unity-style serializable objects, with delegate hooks for custom serialization.

## Package contents

| Folder | Responsibility | Main types |
| --- | --- | --- |
| `Runtime/` | JSON document model, JSONPath helpers, subscriptions, schema model, fluent schema authoring, commands, diagnostics, schema preview reports, serialization wrapper | `JsonDocumentModel`, `JsonPath`, `JsonPathSubscription`, `JsonUiSchema`, `JsonUiSchemaBuilder`, `Ui`, `JsonUiSession`, `JsonUiSchemaReport`, `JsonUiCommandRegistry`, `JsonSerializedObject<T>` |
| `GUI/` | UImGui renderers and ready-to-drop behaviours | `UImGuiJsonEditor`, `UImGuiJsonScreen`, `UImGuiJsonEditorBehaviour`, `UImGuiJsonScreenBehaviour`, `UImGuiJsonSchemaDiagnosticsWindow` |
| `Tests/Editor/` | EditMode coverage for document changes, paths, schema validation, and serialization behavior | `DingoJsonUI.Tests` |
| `Examples/` | Sample scene split by feature | raw editor, schema screen, serialized object inspector, screen behaviour wrapper, feature gallery |

Namespace:

```csharp
using DingoJsonUI;
using DingoJsonUI.GUI;
```

## Dependencies

### External Unity packages

| Dependency | Required by | Notes |
| --- | --- | --- |
| `com.unity.nuget.newtonsoft-json` | Runtime and tests | Required. The assemblies use `NEWTONSOFT_EXISTS` version defines. |
| `com.psydack.uimgui` | `GUI/` and samples | Required for runtime rendering through Dear ImGui. |

`package.json` declares both dependencies for Unity Package Manager consumers.

### Repository dependencies

`DingoJsonUI` has no hard dependency on sibling AppSDK modules. It can be used as a standalone package, a git submodule, or copied into a Unity project.

## Installation

### Option 1. Unity Package Manager Git dependency

Add the package to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.dingobite.dingo-json-ui": "https://github.com/DingoBite/DingoJsonUI.git"
  }
}
```

### Option 2. Git submodule

```bash
git submodule add https://github.com/DingoBite/DingoJsonUI.git Assets/AppSDK/DingoJsonUI
```

### Option 3. Copy into project

Copy the repository folder into any location under `Assets/`, for example:

```text
Assets/Plugins/DingoJsonUI/
```

Make sure the project also has `com.unity.nuget.newtonsoft-json` and `com.psydack.uimgui` installed.

## Quick start

1. Add a `UImGui.UImGui` component to a scene camera.
2. Add `DingoJsonUI.GUI.UImGuiJsonEditorBehaviour` or `DingoJsonUI.GUI.UImGuiJsonScreenBehaviour` to a GameObject.
3. Provide JSON state and, for schema mode, a JSON UI schema.
4. Enter Play Mode.

Minimal document API:

```csharp
using DingoJsonUI;
using Newtonsoft.Json.Linq;

var document = new JsonDocumentModel(@"{""player"":{""hp"":100,""alive"":true}}");

document.Subscribe("$.player.hp", change =>
{
    UnityEngine.Debug.Log($"HP: {change.PreviousValue} -> {change.CurrentValue}");
}, fireImmediately: true);

document.SetValue("$.player.hp", new JValue(75));
document.SetValue("$.player.name", new JValue("Dingo"));
```

Minimal schema UI:

```csharp
var session = JsonUi.Session(
    json: @"{""volume"":0.75,""debug"":false}",
    schemaJson: @"{
      ""root"": {
        ""type"": ""section"",
        ""children"": [
          { ""type"": ""sliderFloat"", ""label"": ""Volume"", ""path"": ""$.volume"", ""min"": 0, ""max"": 1 },
          { ""type"": ""toggle"", ""label"": ""Debug"", ""path"": ""$.debug"" }
        ]
      }
    }");

var screen = UImGuiJsonUi.Screen(session);
screen.Draw();
```

`JToken` and `JObject` are supported as input overloads. They are serialized back to JSON and then loaded through the same string parser and diagnostics pipeline:

```csharp
var data = new JObject
{
    ["volume"] = 0.75f,
    ["debug"] = false,
};

var schemaToken = new JObject
{
    ["root"] = new JObject
    {
        ["type"] = "section",
        ["children"] = new JArray
        {
            new JObject { ["type"] = "sliderFloat", ["label"] = "Volume", ["path"] = "$.volume", ["min"] = 0, ["max"] = 1 },
            new JObject { ["type"] = "toggle", ["label"] = "Debug", ["path"] = "$.debug" },
        },
    },
};

var tokenSession = JsonUi.Session(data, schemaToken);
```

For C# authoring, prefer the fluent schema DSL. It builds `JsonUiSchema` and `JsonUiNode` directly, while JSON remains the interchange format:

```csharp
var schema = Ui.Schema("Settings",
    Ui.Section(
        Ui.SliderFloat("Volume", "$.volume", 0f, 1f),
        Ui.Toggle("Debug", "$.debug"),
        Ui.SelectEnum<Difficulty>("Difficulty", "$.difficulty"),
        Ui.Button("Apply", "applySettings")
            .Payload("source", "settings")
            .EnabledWhen(Ui.Gte("$.volume", 0.5))));
```

`SelectEnum<TEnum>` and `RadioEnum<TEnum>` build options from enum values. By default, option values are enum names as strings, labels are nicified (`VeryHard` -> `Very Hard`), and `[Obsolete]` members are skipped. Use `labelSelector`, `valueSelector`, or `includeObsolete` when a dev UI needs a different shape.

Use `JsonUiPath`/`Ui.Path` when paths get longer than one property:

```csharp
var hp = Ui.Path["player"]["stats"]["hp"];
var inventory = Ui.Path["inventory"];
var item = Ui.Item;

var schema = Ui.Schema("Settings",
    Ui.Section(
        Ui.Int("HP", hp),
        Ui.List("Inventory", inventory,
                Ui.InputText("Label", item["label"]))
            .ItemLabelPath(item["label"]),
        Ui.Button("+10 HP", JsonUiPayload.Add(hp, 10, max: 120))));
```

`Ui.Path` creates absolute JSONPath (`$.player.stats.hp`). `Ui.Item` creates relative paths for list item templates (`label`, `stats.hp`). The type converts back to `string`, so existing APIs still accept it.

The builder form is useful when a screen is assembled procedurally:

```csharp
var schema = JsonUiSchemaBuilder.Create("Settings")
    .Root(root => root.Section()
        .SliderFloat("Volume", "$.volume", 0f, 1f)
        .Toggle("Debug", "$.debug")
        .Button("Apply", "applySettings", new JObject
        {
            ["source"] = "settings",
        }))
    .Build();
```

## Runtime document model

`JsonDocumentModel` owns a live `JToken` root and exposes mutation through JSONPath:

- `LoadJson(...)`
- `LoadToken(...)`
- `SetValue(...)`
- `GetToken(...)`
- `GetValue<T>(...)`
- `Subscribe(...)`

Subscriptions support exact paths and one-level wildcards:

```csharp
document.Subscribe("$.player.*", change =>
{
    UnityEngine.Debug.Log($"Player field changed at {change.Path}");
});

document.Subscribe("$.inventory[*]", change =>
{
    UnityEngine.Debug.Log($"Inventory item changed: {change.Path}");
});
```

Changing `$.player.stats.hp` also notifies ancestors such as `$.player.stats`, `$.player`, and `$`. Notifications are suppressed when the effective JSON content does not change.

## Raw JSON editor

`UImGuiJsonEditor` renders arbitrary JSON:

- `JObject` as foldout/tree nodes;
- `JArray` as indexed foldouts;
- `bool` as checkbox;
- numeric and string scalar values as editable fields;
- `null` as a readonly placeholder;
- registered `JsonUiAction` entries as toolbar or inline buttons.

Large documents are guarded by paging and depth limits. `MaxVisibleChildrenPerNode` defaults to `128`, `MaxRenderDepth` defaults to `64`, and `EnableLargeDataPaging` can be disabled when a small trusted document should render in full.

Inline and toolbar actions are registered against JSON paths:

```csharp
var editor = new UImGuiJsonEditor(document);

editor.Actions.AddButton("$.player.hp", "Heal", context =>
{
    context.Document.SetValue(context.Path, new JValue(100));
});

editor.Actions.AddButton(JsonPath.Root, "Reset", context =>
{
    context.Document.LoadJson(@"{""player"":{""hp"":100,""alive"":true}}");
}, JsonUiActionPlacement.Toolbar);
```

## Schema-driven fast UI

`UImGuiJsonScreen` renders a compact JSON UI schema over the same `JsonDocumentModel`.

The schema describes layout and widgets, while C# registers command callbacks by action id:

```csharp
var schema = JsonUiSchema.FromJson(@"{
  ""title"": ""Settings"",
  ""root"": {
    ""type"": ""section"",
    ""children"": [
      { ""type"": ""sliderFloat"", ""label"": ""Volume"", ""path"": ""$.audio.volume"", ""min"": 0, ""max"": 1 },
      { ""type"": ""toggle"", ""label"": ""Debug"", ""path"": ""$.debug.enabled"" },
      {
        ""type"": ""button"",
        ""label"": ""Apply"",
        ""action"": ""applySettings"",
        ""payload"": { ""source"": ""settings"" },
        ""enabledWhen"": { ""path"": ""$.debug.enabled"", ""equals"": true }
      }
    ]
  }
}");

var commands = new JsonUiCommandRegistry();
commands.Register("applySettings", context => ApplySettings(context.Document, context.Payload));

var diagnostics = new JsonUiSchemaValidator().Validate(schema, commands);
var screen = new UImGuiJsonScreen(document, schema, commands);
screen.Draw();
```

### Widget set

- layout: `section`, `foldout`, `row`, `columns`, `tabs`, `separator`, `space`;
- fields: `field`, `text`, `inputText`, `inputTextMultiline`, `int`, `float`, `toggle`;
- numeric controls: `sliderInt`, `sliderFloat`, `dragInt`, `dragFloat`, `progress`;
- structured controls: `vector2`, `vector3`, `color`, `select`, `radio`;
- actions: `button`.

`vector2`, `vector3`, and `color` read/write JSON arrays such as `[1, 2, 3]` and `[1, 0.8, 0.2, 1]`. They can also read object forms such as `{ "x": 1, "y": 2 }` and `{ "r": 1, "g": 0.8, "b": 0.2, "a": 1 }`.

### Layout hints

Layout nodes and field nodes support:

- `labelWidth`
- `spacing`
- `indent`
- `wrap`
- `width`
- `height`

`labelWidth`, `spacing`, and `indent` are inherited by child fields. `wrap` lets `row` and `radio` move overflowing controls to the next line.

### Templates

Schemas can define reusable `templates` and instantiate them with `type: "include"` or the shorthand `use`:

```json
{
  "templates": {
    "volumeSlider": { "type": "sliderFloat", "min": 0, "max": 1 }
  },
  "root": {
    "type": "section",
    "children": [
      { "use": "volumeSlider", "label": "Music", "path": "$.audio.music" },
      { "use": "volumeSlider", "label": "SFX", "path": "$.audio.sfx" }
    ]
  }
}
```

Include nodes can override ordinary node fields, so generated schemas can reuse the same control shape with different labels, paths, conditions, and payloads.

## Commands and payload patterns

`JsonUiPayloadCommands.RegisterDefaults(commands)` adds common button patterns driven only by `payload`:

```json
[
  { "type": "button", "label": "+50", "action": "payload.add", "payload": { "path": "$.credits", "amount": 50, "max": 999 } },
  { "type": "button", "label": "Debug", "action": "payload.set", "payload": { "path": "$.mode", "value": "Debug" } },
  { "type": "button", "label": "Toggle", "action": "payload.toggle", "payload": { "path": "$.enabled" } },
  { "type": "button", "label": "Copy", "action": "payload.copy", "payload": { "from": "$.title", "to": "$.debug.lastCommand" } }
]
```

`JsonUi.Session(...)` registers these default payload commands unless `JsonUiOptions.RegisterDefaultPayloadCommands` is set to `false`.

For C# authored schemas, use typed payload builders instead of hand-writing payload objects:

```csharp
Ui.Row(
    Ui.Button("+50", JsonUiPayload.Add("$.credits", 50, max: 999)),
    Ui.Button("Debug", JsonUiPayload.Set("$.mode", "Debug")),
    Ui.Button("Toggle", JsonUiPayload.Toggle("$.enabled")),
    Ui.Button("Copy", JsonUiPayload.Copy("$.title", "$.debug.lastCommand")),
    Ui.Button("+1 Current Path", JsonUiPayload.Add(1)).Path("$.credits"));
```

The builders still serialize to the same `action` + `payload` shape, so JSON, `JObject`, and fluent schemas stay interchangeable.

## Schema preview and validation API

Use `JsonUiSchemaReport` when generated or hand-authored schemas should be checked before rendering. It wraps parse diagnostics, validator diagnostics, and a compact preview index of controls, data paths, actions, templates, and type counts.

```csharp
var commands = JsonUi.Commands();
commands.Register("applySettings", context => ApplySettings(context.Document, context.Payload));

var report = JsonUi.ValidateSchemaJson(schemaJson, commands);
if (!report.IsValid)
    Debug.LogWarning(report.ToText());

foreach (var path in report.Preview.DataPaths)
    Debug.Log($"Schema touches {path}");
```

The same API is available for fluent schemas and tokens:

```csharp
JsonUiSchemaReport fluentReport = JsonUi.Preview(schema, commands);
JsonUiSchemaReport tokenReport = JsonUi.ValidateSchemaToken(schemaToken, commands);
JsonUiSchemaReport sessionReport = session.CreateSchemaReport();
```

`FromJson` and the `JsonUi.ValidateSchemaJson(...)` facade do not throw for malformed schema JSON; parse failures become diagnostics at `$.schema`.

## Conditions and diagnostics

`visibleWhen` and `enabledWhen` accept either a path string or an object condition:

```json
{
  "type": "button",
  "label": "Buy",
  "action": "buyUpgrade",
  "enabledWhen": { "path": "$.credits", "gte": 75 }
}
```

Supported condition operators:

- `equals`
- `notEquals`
- `exists`
- `truthy`
- `gt`
- `gte`
- `lt`
- `lte`

Schema parse failures keep the previous valid screen alive inside `JsonUiSession`. Parsed schemas still render while validator diagnostics are shown, which keeps iteration fast when an action id, payload shape, or template reference is temporarily wrong.

`UImGuiJsonSchemaDiagnosticsWindow` provides Validate, Reload All, and Copy actions for diagnostics.

## Serializable object wrapper

`JsonSerializedObject<T>` wraps a `[Serializable]` object with a Unity-inspector-like Newtonsoft resolver. It serializes public fields, private `[SerializeField]` fields, and `[JsonProperty]` members into a `JsonDocumentModel`; the UImGui inspector edits those JSON properties and can apply them back to the target object.

```csharp
[Serializable]
public sealed class PlayerConfig
{
    public int Health = 100;

    [SerializeField]
    [JsonProperty("displayName")]
    private string _displayName = "Dingo";
}

var jsonObject = new JsonSerializedObject<PlayerConfig>(config);
var inspector = new UImGuiJsonSerializedObjectInspector<PlayerConfig>(jsonObject, "Player Config");
inspector.Draw();
```

Unsupported Unity-specific members such as `UnityEngine.Object` references are rendered as readonly fallback strings like `not supported field (UnityEngine.ScriptableObject)`.

For types that need custom conversion, pass delegates instead of relying on the default resolver:

```csharp
var jsonObject = new JsonSerializedObject<MenuState>(
    state,
    (target, serializer) => new JObject
    {
        ["selectedTab"] = target.SelectedTab,
        ["volume"] = target.Volume,
    },
    (json, target, serializer) =>
    {
        target.SelectedTab = json.Value<string>("selectedTab");
        target.Volume = json.Value<float>("volume");
        return target;
    });
```

## Samples

The sample scene in `Examples/Sample.unity` is split by feature:

| Sample | Shows |
| --- | --- |
| `01 Raw JSON Editor` | raw document editing, actions, subscriptions, wildcard paths, large data paging |
| `02 Schema Screen` | schema-driven UI over a shared document, authored with the fluent `Ui` DSL |
| `03 Serialized Object` | `[Serializable]` object inspection and apply/reload flow |
| `04 Screen Behaviour` | drop-in `UImGuiJsonScreenBehaviour` setup with `JObject`/`JArray` token-authored JSON and schema |
| `05 Feature Gallery` | widgets, layout hints, templates, payload commands, conditions, diagnostics, schema preview reports, large data, custom serialization delegates |

## Current scope

The module is intentionally small. It does not yet provide:

- enum reflection hints;
- add/remove/reorder collection editing in the schema screen;
- rich Unity object-reference editors;
- retained-mode layout or styling beyond the supported schema hints;
- production-level theming or design-system integration.
