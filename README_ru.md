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
  Sample-сцена с JSON-редактором, inline/toolbar кнопками, подписками и `JsonSerializedObject<T>` inspector.

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

Сейчас scope всё ещё намеренно узкий: пока без schema-driven dropdowns, enum hints, add/remove редактирования коллекций и отдельного validation layer.

## Поведение шины изменений

- Есть подписки по точному пути и wildcard на один уровень: `.*` и `[*]`.
- Если меняется `$.player.stats.hp`, уведомления получают и `$.player.stats`, и `$.player`, и `$`.
- Если фактическое содержимое документа не поменялось, нотификация не отправляется.
- `RootToken` отдаёт живой `JToken` для рендера. Для корректных событий меняйте документ через `SetValue(...)` или `LoadToken(...)`.
