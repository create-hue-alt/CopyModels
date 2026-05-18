# CopyModels — контекст для Claude

## Что это за проект
Revit плагин на C#. Переписываем с Python (PyRevit) на C# + WPF.
**Учебный проект** — я пишу код сам, Claude проверяет и объясняет.
Claude не пишет готовый код если я не застрял совсем.

## Мой уровень
- Python: уверенно (был когда-то, не я написал оригинальный плагин, потому хочу разобраться в его работе)
- C#: базовый → становится средним (уже написали несколько классов успешно)
- Revit API: базовый — параметры, геометрия, простые транзакции
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
│   │   ├── ModelService.cs        ⏳ следующий
│   │   └── EventService.cs        ⏳ потом
│   └── CopyModelsCommand.cs       ⏳ потом
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

## Текущий статус (сессия 6)

### ✅ Сделано
1. **ProjectSettings.cs** — все 30+ полей + конструктор с парсингом JSON ✅
2. **ModelSetting.cs** — данные модели + логика сравнения дат ✅
3. **SettingsReader.cs** — чтение JSON конфигов с парсингом структуры ✅
4. **CopyModels.ConsoleTest** — полное тестирование ✅
5. **FileService.cs** — копирование, архив, маппинг диска (Windows API) ✅
6. **RevitServerService.cs** — HTTP запросы к Revit Server REST API ✅ **[новое в сессии 6]**
   - HttpClient синхронный (встроен timeout)
   - ReadRevitServerModels() — рекурсивный обход
   - GetModelDate() — парсинг `/Date(...)` формата Revit Server
   - CopyOnRevitServer() — копирование на RSN

### ⏳ В очереди
1. **ModelService.cs** — высокоуровневая логика, выбор алгоритма
2. **EventService.cs** — обработка диалогов и ошибок
3. **CopyModelsCommand.cs** — IExternalCommand точка входа
4. WPF UI (этап 2)
5. Планировщик (этап 3)

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

### HTTP и асинхронность (сессия 6)

**Ключевое решение:** асинхронность НЕ нужна!
- ❌ async/await — усложнит код без выигрыша
- ✅ HttpClient синхронный (.Result) — достаточно
- ✅ Task.Run() + Dispatcher.Invoke() для отзывчивого UI

**Почему:**
- Пользователь ждёт (нажал Export → стоит и ждит)
- Revit блокирует (одна транзакция в потоке)
- Progress bar можно сделать без async

### Разделение ответственности между сервисами (сессия 5)

**ЭТО КЛЮЧЕВОЕ РЕШЕНИЕ!** Подробный разбор в `PROGRESS.md` (раздел "Разделение ответственности между сервисами").

**Короткая версия:**
- `FileService` — ТОЛЬКО файловая система (P:\, C:\, и т.д.)
- `RevitServerService` — ТОЛЬКО Revit Server (RSN://...)
- `ModelService` — выбирает нужный сервис по типу пути

**Почему:**
- `FileService.GetModelDate(path)` возвращает `null` для RSN путей — это обрабатывается в `RevitServerService`
- `CopyModel()` и `ReadModels()` будут в `ModelService`, а не в `FileService`
- Каждый сервис обрабатывает свой тип пути, нет смешивания логики

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

## Документы сессии 6

**Полный курс по HTTP запросам** (11 файлов в `/outputs/HTTP/`):
- Почему НЕ асинхронность
- Progress bar и отзывчивый UI
- WebRequest vs HttpClient
- REST API примеры
- Шпаргалки и тестирование