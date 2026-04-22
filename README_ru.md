# DingoJsonUI

[English version](README.md)

`DingoJsonUI` это небольшой runtime-модуль для Unity, который решает две задачи:

- хранит JSON как `JToken`-документ;
- даёт реактивные подписки по JSONPath и runtime-редактор поверх `UImGui`.

Модуль собран так, чтобы спокойно жить внутри `Assets/AppSDK/`, а потом без переделки уехать в отдельный репозиторий.

## Структура

- `Runtime/`
  Ядро: модель документа, JSONPath normalizer/matcher и подписки.
- `UImGui/`
  Dear ImGui renderer и готовый `MonoBehaviour`.
- `Tests/Editor/`
  Edit-mode тесты на matching путей и доставку изменений.

## Зависимости

- `com.unity.nuget.newtonsoft-json`
- `com.psydack.uimgui`

`Newtonsoft.Json` в проекте уже есть. `UImGui` добавлен в `Packages/manifest.json` как Git dependency.

## Быстрый старт

1. Убедитесь, что на камере в сцене есть компонент `UImGui.UImGui`.
2. Повесьте `DingoJsonUI.UImGui.UImGuiJsonEditorBehaviour` на любой `GameObject`.
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
- `null` как readonly placeholder.

Сейчас scope намеренно узкий: модуль хорошо редактирует существующие значения и умеет создавать недостающие object/array пути через `SetValue(...)`, но пока без schema-driven dropdowns, enum hints, add/remove кнопок и отдельного validation layer.

## Поведение шины изменений

- Есть подписки по точному пути и wildcard на один уровень: `.*` и `[*]`.
- Если меняется `$.player.stats.hp`, уведомления получают и `$.player.stats`, и `$.player`, и `$`.
- Если фактическое содержимое документа не поменялось, нотификация не отправляется.
- `RootToken` отдаёт живой `JToken` для рендера. Для корректных событий меняйте документ через `SetValue(...)` или `LoadToken(...)`.
