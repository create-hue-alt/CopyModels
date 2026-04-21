# CopyModels — контекст для Claude

## Что это за проект
Revit плагин на C#. Переписываем с Python (PyRevit) на C# + WPF.
**Учебный проект** — я пишу код сам, Claude проверяет и объясняет.
Claude не пишет готовый код если я не застрял совсем.

## Мой уровень
- Python: уверенно (был когда-то, не я написал оригинальный плагин, потому хочу разобраться в его работе)
- C#: базовый → становится средним (уже написали несколько классов успешно)
- Revit API: базовый — параметры, геометрия, простые транзакции

## Архитектура проекта

```
CopyModels.sln
├── CopyModels.Core                — БЕЗ Revit API ✅
│   ├── Models/
│   │   ├── ProjectSettings.cs     ✅ готов
│   │   └── ModelSetting.cs        ✅ готов
│   └── Settings/
│       └── SettingsReader.cs      ✅ готов + протестирован
│
├── CopyModels.Plugin              — требует RevitAPI.dll ⏳
│   ├── Services/
│   │   ├── FileService.cs         (следующий)
│   │   ├── RevitServerService.cs
│   │   ├── ModelService.cs
│   │   └── EventService.cs
│   └── CopyModelsCommand.cs
│
├── CopyModels.ConsoleTest         — тестирование ✅
│   ├── Program.cs
│   └── TestConfigs/
│       ├── Architecture.json
│       └── Structure.json
│
└── CopyModels.UI                  — WPF интерфейс (этап 2) ⏳
    └── ...
```

**Главное правило:**
"Этот код скомпилируется без RevitAPI.dll?" → Core, иначе → Plugin

## Текущий статус (сессия 4)

### ✅ Сделано
1. **ProjectSettings.cs** — все 30+ полей + конструктор с парсингом JSON
2. **ModelSetting.cs** — данные модели + логика сравнения дат
3. **SettingsReader.cs** — чтение JSON конфигов с парсингом структуры
4. **CopyModels.ConsoleTest** — полное тестирование
5. **6 проверок** — все пройдены ✅

### ⏳ В очереди
1. **FileService.cs** — копирование, архив, маппинг диска
2. Остальные сервисы Plugin
3. WPF UI (этап 2)
4. Планировщик (этап 3)

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
- Тестирование Core через консольное приложение (без Revit)

## Зависимости
- .NET Framework 4.8
- Newtonsoft.Json (NuGet) — для парсинга JSON
- RevitAPI.dll / RevitAPIUI.dll (только в CopyModels.Plugin, не в Core!)

## Реальные конфиги
- 3 проекта: 11899, 20145, 20111
- 30+ заданий разных типов
- Источники: FileServer (P:\...), Revit Server (RSN://...)
- Целевые форматы: RVT, NWC, IFC
- Специальные параметры: worksets, views, IFC settings, transmit