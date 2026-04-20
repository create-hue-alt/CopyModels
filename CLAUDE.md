# CopyModels — контекст для Claude

## Что это за проект
Revit плагин на C#. Переписываем с Python (PyRevit) на C# + WPF.
**Учебный проект** — я пишу код сам, Claude объясняет концепции и указывает на ошибки.
Claude не пишет готовый код если я не застрял совсем.

## Мой уровень
- Python: уверенно (был когда-то, не я написал оригинальный плагин, потому хочу разобраться в его работе)
- C#: базовый — классы, методы, простые учебные задачи
- Revit API: базовый — параметры, геометрия, простые транзакции

## Архитектура проекта

```
CopyModels.sln
├── CopyModels.Core      — БЕЗ Revit API
│   ├── Models/
│   │   ├── ProjectSettings.cs   ← пишем сейчас
│   │   └── ModelSetting.cs
│   └── Settings/
│       └── SettingsReader.cs
└── CopyModels.Plugin    — требует RevitAPI.dll
    ├── Services/
    │   ├── FileService.cs
    │   ├── RevitServerService.cs
    │   ├── ModelService.cs
    │   └── EventService.cs
    └── CopyModelsCommand.cs
```

**Главное правило:**
"Этот код скомпилируется без RevitAPI.dll?" → Core, иначе → Plugin

## Текущий статус
- `ProjectSettings.cs` — файл написан
- `ModelSetting.cs` - файл написан
- Следующий файл: `SettingsReader.cs`

## Стиль работы
- Я пишу код сам, Claude проверяет и объясняет
- Если я что-то не понимаю — объясни концепцию, потом я пишу сам
- Указывай на ошибки с объяснением почему это ошибка
- Задавай один вопрос в конце сообщения, не несколько

## Принятые решения
- JSON конфиги оставляем в том же формате (не переделываем)
- Планировщик — Windows Task Scheduler (этап 3)
- UI — WPF + MVVM (этап 2)
- Сначала делаем рабочую версию без UI (TaskDialog как заглушка)

## Зависимости
- .NET Framework 4.8
- Newtonsoft.Json (NuGet)
- RevitAPI.dll / RevitAPIUI.dll (только в Plugin)
