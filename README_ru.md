# DingoJsonUI

[English version](README.md)

`DingoJsonUI` это небольшой runtime-модуль для Unity, который решает две задачи:

- хранит JSON как `JToken`-документ;
- даёт реактивные подписки по JSONPath и runtime-редактор поверх `UImGui`.

Модуль собран так, чтобы спокойно жить внутри `Assets/AppSDK/`, а потом без переделки уехать в отдельный репозиторий.

## Структура

- `Runtime/`
  Ядро: модель документа, JSONPath normalizer/matcher и подписки.
- `GUI/`
  Dear ImGui renderer и готовый `MonoBehaviour`.
- `Tests/Editor/`
  Edit-mode тесты на matching путей и доставку изменений.
- `Examples/`
  Sample-сцена, разделённая по фичам: raw JSON editor/actions/subscriptions, schema-driven screen, `JsonSerializedObject<T>` inspector, `UImGuiJsonScreenBehaviour` wrapper и feature gallery для widgets, layout, payload-команд, validator diagnostics, large data и custom serialization delegates.

## Зависимости

- `com.unity.nuget.newtonsoft-json`
- `com.psydack.uimgui`

`Newtonsoft.Json` в проекте уже есть. `UImGui` добавлен в `Packages/manifest.json` как Git dependency.

## Быстрый старт

1. Убедитесь, что на камере в сцене есть компонент `UImGui.UImGui`.
2. Повесьте `DingoJsonUI.GUI.UImGuiJsonEditorBehaviour` на любой `GameObject`.
3. Назначьте `TextAsset` с JSON или оставьте встроенный JSON-текст.
4. Запустите Play mode.

Минимальный API:

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

Для нового быстрого UI лучше начинать с high-level session API. Он держит вместе document, schema, commands, options и diagnostics:

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

Поддерживаются и wildcard-пути на один сегмент:

```csharp
document.Subscribe("$.player.*", change =>
{
    UnityEngine.Debug.Log($"Изменилось поле player: {change.Path}");
});
```

## Что рисует runtime renderer

`UImGuiJsonEditor` отображает:

- `JObject` как tree/foldout;
- `JArray` как список индексов;
- `bool` как checkbox;
- числа и строки как editable input;
- `null` как readonly placeholder;
- зарегистрированные `JsonUiAction` как toolbar или inline кнопки.

Большие документы защищены paging-ом и лимитом глубины. `MaxVisibleChildrenPerNode` по умолчанию `128`, `MaxRenderDepth` по умолчанию `64`, а `EnableLargeDataPaging` можно отключить, если небольшой доверенный документ нужно рисовать целиком.

Для быстрой вёрстки меню есть `UImGuiJsonScreen`: он рисует lightweight UI schema поверх того же `JsonDocumentModel`. Schema описывает layout и widgets, а C# регистрирует callbacks по action id:

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

В schema можно описывать переиспользуемые `templates` и вставлять их через `type: "include"` или короткое поле `use`. Include-узлы могут переопределять обычные поля ноды, поэтому компактные AI-generated schemas могут использовать одну форму контрола с разными `label` и `path`:

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

`JsonUiPayloadCommands.RegisterDefaults(commands)` добавляет стандартные button patterns, которые управляются только через `payload`:

```json
[
  { "type": "button", "label": "+50", "action": "payload.add", "payload": { "path": "$.credits", "amount": 50, "max": 999 } },
  { "type": "button", "label": "Debug", "action": "payload.set", "payload": { "path": "$.mode", "value": "Debug" } },
  { "type": "button", "label": "Toggle", "action": "payload.toggle", "payload": { "path": "$.enabled" } },
  { "type": "button", "label": "Copy", "action": "payload.copy", "payload": { "from": "$.title", "to": "$.debug.lastCommand" } }
]
```

Текущий набор schema widgets покрывает базовые controls для быстрых runtime tools:

- layout: `section`, `foldout`, `row`, `columns`, `tabs`, `separator`, `space`;
- поля: `field`, `text`, `inputText`, `inputTextMultiline`, `int`, `float`, `toggle`;
- numeric controls: `sliderInt`, `sliderFloat`, `dragInt`, `dragFloat`, `progress`;
- structured controls: `vector2`, `vector3`, `color`, `select`, `radio`;
- actions: `button`.

`vector2`, `vector3` и `color` читают/пишут JSON arrays вроде `[1, 2, 3]` и `[1, 0.8, 0.2, 1]`; читать они также умеют object формы вроде `{ "x": 1, "y": 2 }` и `{ "r": 1, "g": 0.8, "b": 0.2, "a": 1 }`.

Layout-ноды и поля поддерживают небольшие hints для polish-а: `labelWidth` наследуется дочерними полями, `spacing` меняет ImGui item spacing для поддерева, `indent` сдвигает поддерево, а `wrap` позволяет `row` и `radio` переносить controls на следующую строку при нехватке ширины. `width` и `height` можно использовать на controls, которым нужен явный размер, включая кнопки и multiline text.

Для scene-level быстрого UI можно добавить `UImGuiJsonScreenBehaviour` на GameObject. Он читает JSON и schema из `TextAsset` ссылок или inline-строк в инспекторе, сам подписывается на UImGui layout callback, открывает `RegisterCommand(...)` и показывает schema diagnostics в маленьком соседнем окне.

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

Ошибки парсинга schema оставляют прошлый валидный экран живым. Если schema распарсилась, но validator нашёл diagnostics, экран всё равно рендерится, а ошибки показываются отдельно; так быстрее итерации, когда action или optional field временно не готовы.

В diagnostics window есть кнопки Validate, Reload All и Copy. Проверяются обычные widget rules, template references, известные action id и форма payload command'ов.

`visibleWhen` и `enabledWhen` принимают или строку-путь (`"$.isVisible"`), или объект condition с `equals`, `notEquals`, `exists`, `truthy`, `gt`, `gte`, `lt`, `lte`.

Кнопки привязываются кодом к JSONPath:

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

`JsonSerializedObject<T>` это wrapper над `[Serializable]` объектом с UnityInspector-подобным Newtonsoft resolver'ом. Он сериализует public-поля, private `[SerializeField]` поля и `[JsonProperty]` members в `JsonDocumentModel`; UImGui inspector редактирует эти JSON properties и может применить их обратно в target object.

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

Для быстрых UI-прототипов или типов, которым нужна своя конвертация, `JsonSerializedObject<T>` можно создать с делегатами вместо default resolver'а:

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

Сейчас scope всё ещё намеренно узкий: пока без enum reflection hints, add/remove редактирования коллекций и богатых Unity object-reference editors.

## Поведение шины изменений

- Есть подписки по точному пути и wildcard на один уровень: `.*` и `[*]`.
- Если меняется `$.player.stats.hp`, уведомления получают и `$.player.stats`, и `$.player`, и `$`.
- Если фактическое содержимое документа не поменялось, нотификация не отправляется.
- `RootToken` отдаёт живой `JToken` для рендера. Для корректных событий меняйте документ через `SetValue(...)` или `LoadToken(...)`.
