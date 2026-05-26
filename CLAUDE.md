# CopyModels — контекст для Claude

## Что это за проект
Revit плагин на C#. Переписываем с Python (PyRevit) на C# + WPF.
**Учебный проект** — я пишу код сам, Claude проверяет и объясняет.
Claude не пишет готовый код если я не застрял совсем.

## Мой уровень
- Python: уверенно (был когда-то, не я написал оригинальный плагин, потому хочу разобраться в его работе)
- C#: базовый → становится средним (уже написали несколько классов успешно)
- Revit API: базовый → средний (открытие/закрытие документов, экспорт, транзакции, worksets)
- HTTP API: ✅ изучил в сессии 6 (асинхронность, WebRequest vs HttpClient, REST API)

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
├── CopyModels.Plugin              — требует RevitAPI.dll ✅✅
│   ├── Services/
│   │   ├── FileService.cs         ✅ готов (сессия 5)
│   │   ├── RevitServerService.cs  ✅ готов (сессия 6) — HttpClient REST API
│   │   ├── ModelService.cs        ✅ готов (сессия 8) — открытие/экспорт/сохранение
│   │   └── EventService.cs        ⏳ следующий (сессия 9)
│   └── CopyModelsCommand.cs       ⏳ потом (сессия 10)
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

## Текущий статус (сессия 8)

### ✅ Сделано
1. **ProjectSettings.cs** — все 30+ полей + конструктор с парсингом JSON ✅
2. **ModelSetting.cs** — данные модели + логика сравнения дат ✅
3. **SettingsReader.cs** — чтение JSON конфигов с парсингом структуры ✅
4. **CopyModels.ConsoleTest** — полное тестирование ✅
5. **FileService.cs** — копирование, архив, маппинг диска (Windows API) ✅
6. **RevitServerService.cs** — HTTP запросы к Revit Server REST API ✅
   - HttpClient синхронный (встроен timeout)
   - ReadRevitServerModels() — рекурсивный обход
   - GetModelDate() — парсинг `/Date(...)` формата Revit Server
   - CopyOnRevitServer() — копирование на RSN
7. **ModelService.cs** — открытие / экспорт / сохранение моделей ✅ **[новое в сессии 8]**
   - OpenWithDetach() — открытие RVT с детачем
   - OpenIfc() — открытие IFC файлов
   - SaveAsRvt() — сохранение как Central с архивом
   - ExportModel() — экспорт в NWC/IFC с опциями
   - PurgeDocument() — очистка модели через PerformanceAdviser
   - TransmitModel() — настройка transmit для링ов
   - GetViewByName() + Create3DView() — работа с видами
   - BuildNwcOptions() + BuildIfcOptions() — 30+ опций экспорта
   - ApplyWorksetConfiguration() — открытие/закрытие worksets

### ⏳ В очереди
1. **EventService.cs** — обработка диалогов и ошибок Revit
2. **CopyModelsCommand.cs** — IExternalCommand точка входа
3. WPF UI (этап 2)
4. Планировщик (этап 3)

## Стиль работы
- Я пишу код сам, Claude проверяет и объясняет
- Если я что-то не понимаю — объясни концепцию, потом я пишу сам
- Указывай на ошибки с объяснением почему это ошибка
- Задавай один вопрос в конце сообщения, не несколько

## Принятые решения

### Архитектура (сессия 5)
- JSON конфиги оставляем в том же формате (не переделываем)
- Разделение: FileService (P:\) + RevitServerService (RSN://) + ModelService (выбор)
- Сначала делаем рабочую версию без UI (TaskDialog как заглушка)
- Тестирование Core через консольное приложение (без Revit)

### HTTP и асинхронность (сессия 6)
- ❌ async/await — усложнит код без выигрыша
- ✅ HttpClient синхронный (.Result) — достаточно
- ✅ Task.Run() + Dispatcher.Invoke() для отзывчивого UI

### Разделение ответственности (сессия 5)
**ЭТО КЛЮЧЕВОЕ РЕШЕНИЕ!**
- `FileService` — ТОЛЬКО файловая система (P:\, C:\, и т.д.)
- `RevitServerService` — ТОЛЬКО Revit Server (RSN://...)
- `ModelService` — выбирает нужный сервис по типу пути

### ModelService.cs (сессия 8)
- **CheckAndFixView()** вызывается ПЕРЕД ExportModel() (избегаем вложенных транзакций)
- NWC экспорт возвращает `void`, проверяем наличие файла
- IFC экспорт возвращает `bool`, проверяем значение
- Архивирование происходит ПЕРЕД экспортом (если старый файл существует)
- Экспорт во временный файл, потом копируем в целевой путь

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

## Для справки (Python файлы в /mnt/project/)
- eventsTools.py — обработка диалогов Revit (нужна для EventService)
- serverTools.py — работа с файлами и Revit Server (основа FileService + RevitServerService)
- settings_classes.py — классы данных (основа ProjectSettings + ModelSetting)
- TEMPLATE.json — структура JSON конфигов