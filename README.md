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
  A sample scene covering JSON editing, inline/toolbar buttons, subscriptions, and the `JsonSerializedObject<T>` inspector.

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

Current scope is still intentionally small: it does not yet implement schema-aware dropdowns, enum hints, add/remove collection editing, or validation layers.

## Design notes

- Exact-path subscriptions and one-level wildcards (`.*` and `[*]`) are supported.
- Ancestor paths are also notified, so changing `$.player.stats.hp` also updates listeners on `$.player.stats`, `$.player`, and `$`.
- Notifications are suppressed when the effective document content does not change.
- `RootToken` exposes the live underlying `JToken` for rendering. Mutate through `SetValue(...)` or `LoadToken(...)` if you want notifications.
