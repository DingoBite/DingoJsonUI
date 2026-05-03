# DingoJsonUI

[English version](README.md)

`DingoJsonUI` - это независимый Unity-модуль для построения runtime UI поверх JSON-состояния.

Модуль хранит состояние приложения в Newtonsoft `JToken`, дает подписки на изменения через JSONPath и умеет рендерить это состояние через `UImGui`: либо как сырой JSON editor, либо как schema-driven fast UI screen.

Этот файл является технической документацией уровня самого репозитория.

## Зачем нужен модуль

Runtime tools и prototype menus в Unity часто требуют один и тот же набор механик:

- редактируемое runtime-состояние без отдельного custom inspector под каждый тест;
- быстрая вёрстка меню из компактной JSON schema;
- кнопки и действия, отделённые от конкретного UI renderer;
- diagnostics, чтобы безопасно итерировать AI-generated или hand-written schemas;
- легкий мост от `[Serializable]` C# объектов к JSON-backed UI.

`DingoJsonUI` собирает эти задачи в небольшой runtime layer. Он хорошо подходит для debug panels, gameplay tuning menus, AI-assisted UI experiments, test harnesses и простых in-game tools.

Это не полноценный retained UI framework и не замена Unity UI Toolkit, uGUI или production design system. Главная цель - быстрая итерация над механическим UI и runtime state.

## Ключевые преимущества

- JSON является source of truth, поэтому UI легко генерировать, копировать, патчить, diff-ить и логировать.
- JSONPath subscriptions позволяют реагировать на точные пути, wildcard на один уровень и ancestor changes.
- Raw editor сразу работает с произвольными `JObject`, `JArray`, scalar и `null` значениями.
- Schema renderer поддерживает практичные menu widgets: tabs, rows, columns, sliders, toggles, vectors, colors, selects, radio groups, progress bars и buttons.
- Command callbacks регистрируются по id, а типовые button patterns могут полностью управляться через payload.
- Schema diagnostics защищают generated UI от тихих ошибок.
- Большие JSON-документы защищены paging-ом, настройкой scroll speed, depth limits и кешем имён свойств объектов.
- `JsonSerializedObject<T>` умеет инспектировать Unity-style serializable objects и поддерживает delegates для custom serialization.

## Состав пакета

| Папка | Ответственность | Основные типы |
| --- | --- | --- |
| `Runtime/` | JSON document model, JSONPath helpers, subscriptions, schema model, fluent schema authoring, commands, diagnostics, serialization wrapper | `JsonDocumentModel`, `JsonPath`, `JsonPathSubscription`, `JsonUiSchema`, `JsonUiSchemaBuilder`, `Ui`, `JsonUiSession`, `JsonUiCommandRegistry`, `JsonSerializedObject<T>` |
| `GUI/` | UImGui renderers и готовые behaviours | `UImGuiJsonEditor`, `UImGuiJsonScreen`, `UImGuiJsonEditorBehaviour`, `UImGuiJsonScreenBehaviour`, `UImGuiJsonSchemaDiagnosticsWindow` |
| `Tests/Editor/` | EditMode coverage для document changes, paths, schema validation и serialization behavior | `DingoJsonUI.Tests` |
| `Examples/` | Sample-сцена, разделённая по фичам | raw editor, schema screen, serialized object inspector, screen behaviour wrapper, feature gallery |

Namespace:

```csharp
using DingoJsonUI;
using DingoJsonUI.GUI;
```

## Зависимости

### Внешние Unity packages

| Зависимость | Где нужна | Примечания |
| --- | --- | --- |
| `com.unity.nuget.newtonsoft-json` | Runtime и tests | Обязательная. Assemblies используют `NEWTONSOFT_EXISTS` version defines. |
| `com.psydack.uimgui` | `GUI/` и samples | Обязательная для runtime rendering через Dear ImGui. |

`package.json` объявляет обе зависимости для Unity Package Manager consumers.

### Репозиторные зависимости

У `DingoJsonUI` нет жёсткой зависимости от соседних AppSDK-модулей. Его можно использовать как standalone package, git submodule или просто скопировать в Unity-проект.

## Установка

### Вариант 1. Unity Package Manager Git dependency

Добавьте package в `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.dingobite.dingo-json-ui": "https://github.com/DingoBite/DingoJsonUI.git"
  }
}
```

### Вариант 2. Git submodule

```bash
git submodule add https://github.com/DingoBite/DingoJsonUI.git Assets/AppSDK/DingoJsonUI
```

### Вариант 3. Копирование в проект

Скопируйте папку репозитория в любую папку внутри `Assets/`, например:

```text
Assets/Plugins/DingoJsonUI/
```

Убедитесь, что в проекте также установлены `com.unity.nuget.newtonsoft-json` и `com.psydack.uimgui`.

## Быстрый старт

1. Добавьте `UImGui.UImGui` component на scene camera.
2. Повесьте `DingoJsonUI.GUI.UImGuiJsonEditorBehaviour` или `DingoJsonUI.GUI.UImGuiJsonScreenBehaviour` на GameObject.
3. Передайте JSON state и, для schema mode, JSON UI schema.
4. Запустите Play Mode.

Минимальный document API:

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

Минимальный schema UI:

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

`JToken` и `JObject` поддерживаются как input overloads. Они сериализуются обратно в JSON и затем загружаются через тот же string parser и diagnostics pipeline:

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

Для C# authoring лучше использовать fluent schema DSL. Он строит `JsonUiSchema` и `JsonUiNode` напрямую, а JSON остаётся форматом обмена:

```csharp
var schema = Ui.Schema("Settings",
    Ui.Section(
        Ui.SliderFloat("Volume", "$.volume", 0f, 1f),
        Ui.Toggle("Debug", "$.debug"),
        Ui.Button("Apply", "applySettings")
            .Payload("source", "settings")
            .EnabledWhen(Ui.Gte("$.volume", 0.5))));
```

Builder-форма удобна, когда screen собирается процедурно:

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

`JsonDocumentModel` владеет live `JToken` root и меняет его через JSONPath:

- `LoadJson(...)`
- `LoadToken(...)`
- `SetValue(...)`
- `GetToken(...)`
- `GetValue<T>(...)`
- `Subscribe(...)`

Подписки поддерживают точные пути и wildcard на один уровень:

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

Изменение `$.player.stats.hp` также уведомляет ancestors: `$.player.stats`, `$.player` и `$`. Если фактическое JSON-содержимое не поменялось, notification не отправляется.

## Raw JSON editor

`UImGuiJsonEditor` отображает произвольный JSON:

- `JObject` как foldout/tree nodes;
- `JArray` как indexed foldouts;
- `bool` как checkbox;
- numeric и string scalar values как editable fields;
- `null` как readonly placeholder;
- зарегистрированные `JsonUiAction` как toolbar или inline buttons.

Большие документы защищены paging-ом и depth limits. `MaxVisibleChildrenPerNode` по умолчанию `128`, `MaxRenderDepth` по умолчанию `64`, а `EnableLargeDataPaging` можно отключить, если небольшой доверенный документ нужно рисовать целиком.

Inline и toolbar actions регистрируются на JSON paths:

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

`UImGuiJsonScreen` рисует компактную JSON UI schema поверх того же `JsonDocumentModel`.

Schema описывает layout и widgets, а C# регистрирует command callbacks по action id:

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

`vector2`, `vector3` и `color` читают/пишут JSON arrays вроде `[1, 2, 3]` и `[1, 0.8, 0.2, 1]`. Они также умеют читать object forms вроде `{ "x": 1, "y": 2 }` и `{ "r": 1, "g": 0.8, "b": 0.2, "a": 1 }`.

### Layout hints

Layout nodes и field nodes поддерживают:

- `labelWidth`
- `spacing`
- `indent`
- `wrap`
- `width`
- `height`

`labelWidth`, `spacing` и `indent` наследуются дочерними полями. `wrap` позволяет `row` и `radio` переносить overflowing controls на следующую строку.

### Templates

В schemas можно описывать переиспользуемые `templates` и вставлять их через `type: "include"` или короткое поле `use`:

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

Include nodes могут переопределять обычные поля node, поэтому generated schemas могут переиспользовать одну форму control с разными labels, paths, conditions и payloads.

## Commands и payload patterns

`JsonUiPayloadCommands.RegisterDefaults(commands)` добавляет типовые button patterns, которые управляются только через `payload`:

```json
[
  { "type": "button", "label": "+50", "action": "payload.add", "payload": { "path": "$.credits", "amount": 50, "max": 999 } },
  { "type": "button", "label": "Debug", "action": "payload.set", "payload": { "path": "$.mode", "value": "Debug" } },
  { "type": "button", "label": "Toggle", "action": "payload.toggle", "payload": { "path": "$.enabled" } },
  { "type": "button", "label": "Copy", "action": "payload.copy", "payload": { "from": "$.title", "to": "$.debug.lastCommand" } }
]
```

`JsonUi.Session(...)` регистрирует эти default payload commands, если `JsonUiOptions.RegisterDefaultPayloadCommands` не выключен.

## Conditions и diagnostics

`visibleWhen` и `enabledWhen` принимают path string или object condition:

```json
{
  "type": "button",
  "label": "Buy",
  "action": "buyUpgrade",
  "enabledWhen": { "path": "$.credits", "gte": 75 }
}
```

Поддерживаемые condition operators:

- `equals`
- `notEquals`
- `exists`
- `truthy`
- `gt`
- `gte`
- `lt`
- `lte`

Ошибки парсинга schema оставляют прошлый валидный screen живым внутри `JsonUiSession`. Если schema распарсилась, но validator нашёл diagnostics, экран всё равно рендерится, а ошибки показываются отдельно. Это ускоряет итерации, когда action id, payload shape или template reference временно неверные.

`UImGuiJsonSchemaDiagnosticsWindow` даёт Validate, Reload All и Copy actions для diagnostics.

## Serializable object wrapper

`JsonSerializedObject<T>` это wrapper над `[Serializable]` объектом с UnityInspector-подобным Newtonsoft resolver. Он сериализует public fields, private `[SerializeField]` fields и `[JsonProperty]` members в `JsonDocumentModel`; UImGui inspector редактирует эти JSON properties и может применить их обратно в target object.

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

Неподдерживаемые Unity-specific members вроде `UnityEngine.Object` references отображаются readonly fallback-строками вида `not supported field (UnityEngine.ScriptableObject)`.

Для типов, которым нужна custom conversion, передайте delegates вместо default resolver:

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

Sample scene в `Examples/Sample.unity` разделена по фичам:

| Sample | Что показывает |
| --- | --- |
| `01 Raw JSON Editor` | raw document editing, actions, subscriptions, wildcard paths, large data paging |
| `02 Schema Screen` | schema-driven UI поверх shared document, собранный через fluent `Ui` DSL |
| `03 Serialized Object` | `[Serializable]` object inspection и apply/reload flow |
| `04 Screen Behaviour` | drop-in `UImGuiJsonScreenBehaviour` setup с JSON и schema, собранными через `JObject`/`JArray` tokens |
| `05 Feature Gallery` | widgets, layout hints, templates, payload commands, conditions, diagnostics, large data, custom serialization delegates |

## Текущий scope

Модуль намеренно небольшой. Сейчас он не предоставляет:

- enum reflection hints;
- add/remove/reorder collection editing в schema screen;
- rich Unity object-reference editors;
- retained-mode layout или styling за пределами поддержанных schema hints;
- production-level theming или design-system integration.
