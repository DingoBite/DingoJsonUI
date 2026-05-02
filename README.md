# DingoJsonUI

[Русская версия](README_ru.md)

`DingoJsonUI` is a small runtime JSON editing layer for Unity built around two ideas:

- keep JSON state in a `JToken` document model;
- subscribe to changes by JSONPath and render/edit that state through `UImGui`.

This repository is meant to live comfortably inside `Assets/AppSDK/` as a standalone module and later be extracted or pushed as its own repository.

## Package layout

- `Runtime/`
  Core document model, JSONPath normalization/matching, and reactive subscriptions.
- `GUI/`
  Dear ImGui runtime renderer and a ready-to-drop `MonoBehaviour`.
- `Tests/Editor/`
  Edit-mode coverage for JSONPath matching and change propagation.
- `Examples/`
  A sample scene split by feature: raw JSON editor/actions/subscriptions, schema-driven screen, `JsonSerializedObject<T>` inspector, the `UImGuiJsonScreenBehaviour` wrapper, and a feature gallery for widgets, layout, payload commands, validator diagnostics, large data, and custom serialization delegates.

## Dependencies

- `com.unity.nuget.newtonsoft-json`
- `com.psydack.uimgui`

The host project already contains Newtonsoft.Json. `UImGui` is added to `Packages/manifest.json` as a Git dependency.

## Quick start

1. Ensure the scene has a `UImGui.UImGui` component on a camera.
2. Add `DingoJsonUI.GUI.UImGuiJsonEditorBehaviour` to any GameObject.
3. Assign a `TextAsset` with JSON, or leave the inline JSON text enabled.
4. Enter Play mode.

Minimal API:

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

For new fast UI code, prefer the high-level session API. It keeps the document, schema, commands, options, and diagnostics together:

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

Wildcard subscriptions are supported for one path segment:

```csharp
document.Subscribe("$.player.*", change =>
{
    UnityEngine.Debug.Log($"Player field changed at {change.Path}");
});
```

## Runtime rendering

`UImGuiJsonEditor` renders:

- `JObject` as foldout/tree nodes
- `JArray` as indexed foldouts
- `bool` as checkbox
- numeric/string scalar values as editable fields
- `null` as a readonly placeholder
- registered `JsonUiAction` entries as toolbar or inline buttons

Large documents are guarded by paging and depth limits. `MaxVisibleChildrenPerNode` defaults to `128`, `MaxRenderDepth` defaults to `64`, and `EnableLargeDataPaging` can be disabled when a small trusted document should render in full.

For faster menu prototyping, `UImGuiJsonScreen` renders a lightweight UI schema over the same `JsonDocumentModel`. The schema describes layout and widgets; C# registers command callbacks by id:

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

Schemas can define reusable `templates` and instantiate them with `type: "include"` or the shorthand `use`. Include nodes can override ordinary node fields, so compact AI-generated schemas can reuse the same control shape with different `label` and `path` values:

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

`JsonUiPayloadCommands.RegisterDefaults(commands)` adds common button patterns that are driven only by `payload`:

```json
[
  { "type": "button", "label": "+50", "action": "payload.add", "payload": { "path": "$.credits", "amount": 50, "max": 999 } },
  { "type": "button", "label": "Debug", "action": "payload.set", "payload": { "path": "$.mode", "value": "Debug" } },
  { "type": "button", "label": "Toggle", "action": "payload.toggle", "payload": { "path": "$.enabled" } },
  { "type": "button", "label": "Copy", "action": "payload.copy", "payload": { "from": "$.title", "to": "$.debug.lastCommand" } }
]
```

The schema widget set now covers the common controls needed for fast runtime tools:

- layout: `section`, `foldout`, `row`, `columns`, `tabs`, `separator`, `space`;
- fields: `field`, `text`, `inputText`, `inputTextMultiline`, `int`, `float`, `toggle`;
- numeric controls: `sliderInt`, `sliderFloat`, `dragInt`, `dragFloat`, `progress`;
- structured controls: `vector2`, `vector3`, `color`, `select`, `radio`;
- actions: `button`.

`vector2`, `vector3`, and `color` read/write JSON arrays such as `[1, 2, 3]` and `[1, 0.8, 0.2, 1]`; they also read object forms like `{ "x": 1, "y": 2 }` and `{ "r": 1, "g": 0.8, "b": 0.2, "a": 1 }`.

Layout nodes and field nodes support small polish hints: `labelWidth` is inherited by child fields, `spacing` adjusts ImGui item spacing for a subtree, `indent` offsets a subtree, and `wrap` lets `row` and `radio` move overflowing controls to the next line. `width` and `height` can be used on controls that need an explicit size, including buttons and multiline text.

For scene-level fast UI, add `UImGuiJsonScreenBehaviour` to a GameObject. It can read JSON and schema from `TextAsset` references or inline inspector strings, subscribes to the UImGui layout callback, exposes `RegisterCommand(...)`, and shows schema diagnostics in a small companion window.

```csharp
public sealed class SettingsMenuCommands : MonoBehaviour
{
    [SerializeField]
    private UImGuiJsonScreenBehaviour _screen;

    private void Awake()
    {
        _screen.RegisterCommand("applySettings", context => ApplySettings(context.Document, context.Payload));
    }
}
```

Schema parse failures keep the previous valid screen alive. Parsed schemas still render while validator diagnostics are shown, which keeps iteration fast when a button action or optional field is temporarily missing.

The diagnostics window has Validate, Reload All, and Copy buttons. It validates normal widget rules, template references, known action ids, and payload command shapes.

`visibleWhen` and `enabledWhen` accept either a path string (`"$.isVisible"`) or an object condition with `equals`, `notEquals`, `exists`, `truthy`, `gt`, `gte`, `lt`, and `lte`.

Buttons are code-bound to JSON paths:

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

For fast UI prototypes or types that need custom conversion, `JsonSerializedObject<T>` can use delegates instead of the default resolver:

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

Current scope is still intentionally small: it does not yet implement enum reflection hints, add/remove collection editing, or rich Unity object-reference editors.

## Design notes

- Exact-path subscriptions and one-level wildcards (`.*` and `[*]`) are supported.
- Ancestor paths are also notified, so changing `$.player.stats.hp` also updates listeners on `$.player.stats`, `$.player`, and `$`.
- Notifications are suppressed when the effective document content does not change.
- `RootToken` exposes the live underlying `JToken` for rendering. Mutate through `SetValue(...)` or `LoadToken(...)` if you want notifications.
