# Прогресс разработки CopyModels

## Архитектура

```
CopyModels.sln
├── CopyModels.Core                 — бизнес-логика, компилируется БЕЗ Revit API
│   ├── Models/
│   │   ├── ProjectSettings.cs      — поля одного задания + парсинг JSON ✅
│   │   └── ModelSetting.cs         — поля одной модели + логика дат ✅
│   └── Settings/
│       └── SettingsReader.cs       — читает JSON файлы, создаёт ProjectSettings ✅
│
├── CopyModels.Plugin               — требует RevitAPI.dll
│   ├── Services/
│   │   ├── FileService.cs          — копирование файлов, архив, маппинг диска (WinAPI) ✅
│   │   ├── RevitServerService.cs   — HTTP запросы к Revit Server (RSN) ✅
│   │   ├── ModelService.cs         — открытие / экспорт / сохранение моделей Revit ✅
│   │   └── EventService.cs         — подписка на события, автозакрытие диалогов ✅
│   └── CopyModelsCommand.cs        — IExternalCommand, точка входа ⏳
│
├── CopyModels.ConsoleTest          — тестирование Core логики (без Revit) ✅
│   ├── Program.cs
│   ├── TestRevitServer.cs
│   └── TestConfigs/
│       ├── Architecture.json
│       ├── Structure.json
│       └── RealProject.json
│
└── CopyModels.UI                   — WPF интерфейс (этап 2+3) ⏳
    ├── MainWindow.xaml
    └── ViewModels/
        └── MainViewModel.cs
```

### Правило разделения Core / Plugin

```
Задай себе вопрос:
"Этот код скомпилируется без RevitAPI.dll?"

Да  → Core
Нет → Plugin
```

## Соответствие Python → C#

| Python файл | C# файл | Статус |
|---|---|---|
| `settings_classes.py → ProjectSettings` | `Core/Models/ProjectSettings.cs` | ✅ |
| `settings_classes.py → ModelSetting` | `Core/Models/ModelSetting.cs` | ✅ |
| `read_setting_file()` | `Core/Settings/SettingsReader.cs` | ✅ |
| `serverTools.py` (FILE часть) | `Plugin/Services/FileService.cs` | ✅ |
| `serverTools.py` (RSN часть) | `Plugin/Services/RevitServerService.cs` | ✅ |
| `modelTools.py` | `Plugin/Services/ModelService.cs` | ✅ |
| `eventsTools.py` | `Plugin/Services/EventService.cs` | ✅ |
| `script.py` (точка входа) | `Plugin/CopyModelsCommand.cs` | ⏳ |
| `*.json` конфиги | без изменений (этап 1) | ✅ |

## Чеклист реализации

### Core/Models ✅
- [x] `ProjectSettings.cs` — все поля и конструктор
- [x] `ModelSetting.cs` — все поля и логика дат

### Core/Settings ✅
- [x] `SettingsReader.cs` — чтение JSON конфигов, парсинг структуры

### Core/Tests ✅
- [x] `CopyModels.ConsoleTest` — консольное тестирование
- [x] 9 проверок SettingsReader — все пройдены ✅
- [x] 4 теста RevitServerService — все пройдены ✅

### Plugin/Services ✅
- [x] `FileService.cs`
- [x] `RevitServerService.cs`
- [x] `ModelService.cs`
- [x] `EventService.cs`

### Plugin ⏳
- [ ] `CopyModelsCommand.cs` ← ТЕКУЩИЙ ШАГ

### UI + новый JSON (Этап 2+3) ⏳
- [ ] `UserConfig` класс — структура UI-генерируемого JSON
- [ ] WPF визард: выбор проекта → модели → форматы
- [ ] Сохранение в `C:\Users\%User%\Documents\CopyModels\`
- [ ] Command обновляется для чтения нового формата

### Планировщик (Этап 4) ⏳
- [ ] Интеграция с Windows Task Scheduler
- [ ] Чтение JSON из Documents без UI

### PostgreSQL (Этап 5) ⏳
- [ ] `SettingsRepository` — аналог `SettingsReader`, возвращает те же `ProjectSettings` из БД
- [ ] Миграция конфигов из JSON в PostgreSQL (сервер уже есть)
- [ ] Таблица истории запусков (когда, кто, какие модели, результат)
- [ ] Общий доступ к конфигам с нескольких машин
- [ ] `CopyModelsCommand` и все сервисы не меняются — только источник данных

---

## Журнал сессий

### Сессия 1 — архитектура проекта
- Изучили Python оригинал
- Спроектировали структуру C# решения
- Решение: JSON конфиги оставляем в том же формате

### Сессия 2 — ProjectSettings.cs (13.04.2026)
- Создали Solution, два проекта Core и Plugin
- Написали `ProjectSettings.cs`
- Концепции: nullable bool?, JObject, pattern matching, XML-документация

### Сессия 3 — ModelSetting.cs
- Написали `ModelSetting.cs`
- `DisplayName` форматируется для UI

### Сессия 4 — SettingsReader.cs + тестирование (21.04.2026)
- `SettingsReader.cs` + ConsoleTest проект
- Все 6 проверок пройдены ✅

### Сессия 5 — FileService.cs (10.05.2026)
- Полный `FileService.cs` — файловая система, архив, маппинг диска
- P/Invoke, DllImport, WNetAddConnection2
- **Ключевое решение:** FileService / RevitServerService / ModelService — три отдельных сервиса

### Сессия 6 — HTTP + RevitServerService.cs (18.05.2026)
- Курс по HTTP в C# (WebRequest vs HttpClient)
- Переделали на HttpClient синхронный

### Сессия 7 — Тестирование RevitServerService (20.05.2026)
- 4 теста, все пройдены ✅
- Реальный запрос к `RSN://k-2133.atptlp.local/20175_INARCTICA` — 13 моделей

### Сессия 8 — ModelService.cs
- 620 строк: открытие, экспорт, сохранение, purge, transmit, worksets, виды
- NWC(void) vs IFC(bool) — разные подходы проверки результата
- CheckAndFixView() перед ExportModel() — избегаем вложенных транзакций

### Сессия 9 — EventService.cs
- Subscribe/Unsubscribe с guard проверкой
- IDisposable паттерн — `using()` блок
- Два Application объекта: `Application` для FailuresProcessing, `UIApplication` для DialogBoxShowing
- Логирование через callbacks

### Сессия 10 — Анализ Python оригинала + планирование (27.05.2026)

**Разобрали полностью `Copy_Models_script.py`.**

#### Как работает Python скрипт (важно для C# реализации)

**Откуда берётся `discipline`:**
`discipline` = имя JSON файла без расширения (`AR.json` → `"AR"`).
Сохраняется в pyrevit config между запусками. `Shift+Click` → смена дисциплины.
`"!BIM!"` — специальный режим, читаются все JSON файлы сразу.

**Трёхуровневая структура выбора:**
```
1. Выбор дисциплины (файл конфига) — сохраняется между запусками
2. Выбор заданий — SelectFromList, multiselect, группировка по проекту
3. Выбор моделей — для каждого задания отдельно, "Not Actual" по умолчанию
```

**Три ветки логики обработки модели:**
```
is_exceed == true
    → удалить или архивировать (модель есть в Target, но нет в Source)

purge == true ИЛИ is_open_required() == true
    → открыть Revit → purge → экспорт/сохранение → закрыть
    (is_open_required: Source extension ≠ Target extension, или путь RSN)

иначе
    → простое копирование файла без открытия Revit
```

**Полный flow ветки "открыть Revit":**
```
1. Открыть (open_model_with_detach или open_ifc)
2. Если IFC → divide_on_worksets, get_coordinates
3. Найти/создать вид "NavisWorks"
4. Если purge → clean_revit_file(doc)
5. Для каждого target:
   - .RVT → save_rvt()
   - same extension → file_server_copy_model()
   - другое расширение → export_rvt() [NWC или IFC]
   - если transmit → transmit_model() + mark_ro()
6. relinquish_doc() → Close() → Dispose()
```

**Решения принятые в сессии 10:**

1. **JSON остаётся центром архитектуры.** Меняется только кто создаёт JSON — вручную сейчас, UI в будущем.

2. **Этапы 2+3 идут параллельно** — UI создаёт JSON, Command читает новый формат. Разрывать нельзя.

3. **Новый JSON формат добавит блок `Schedule`:**
   ```json
   "Schedule": { "Enabled": true, "DayOfWeek": "Monday", "Time": "23:00" }
   ```
   Старые конфиги продолжают работать без изменений.

4. **Про дублирующиеся задания в реальном конфиге** (например `"From RVT RS to RVT FS"` и `"From RVT RS to RVT FS (purged)"`) — в UI-версии это будет одна запись с галочкой Purge.

**Скелет CopyModelsCommand.cs:**
```csharp
public Result Execute(ExternalCommandData commandData, ...)
{
    var app = commandData.Application.Application;
    var uiApp = commandData.Application;

    using (var eventService = new EventService(app, uiApp, Log, LogWarning))
    {
        eventService.Subscribe();
        var discipline = SelectDiscipline();              // уровень 1
        var settings = SettingsReader.ReadDiscipline(...);
        var selectedTasks = SelectTasks(settings);        // уровень 2
        foreach (var task in selectedTasks)
        {
            var models = task.GetModelsSettings();        // уровень 3
            foreach (var model in models)
                ProcessModel(task, model, app);           // три ветки
        }
    }
    return Result.Succeeded;
}
```

---

## Разделение ответственности между сервисами

```
┌─────────────────────────────────────────────────────┐
│ ModelService (ТОЧКА ВХОДА)                          │
│ ├─ GetModelDate(anyPath)  ← проверяет тип пути      │
│ ├─ CopyModel(src, dst)    ← выбирает алгоритм       │
│ └─ Зависит от FileService + RevitServerService      │
│                                                     │
│  ┌───────────────┐     ┌────────────────────────┐   │
│  │ FileService   │     │ RevitServerService     │   │
│  │ (ФАЙЛЫ)       │     │ (REVIT SERVER / RSN)   │   │
│  └───────────────┘     └────────────────────────┘   │
└─────────────────────────────────────────────────────┘

P:\Projects\Model.rvt          → FileService
RSN://server/folder/Model.rvt  → RevitServerService
```

---

## Полезные ссылки

- [Revit API Docs](https://www.revitapidocs.com/)
- [Revit API Forum](https://forums.autodesk.com/t5/revit-api-forum/bd-p/160)
- [MVVM паттерн](https://learn.microsoft.com/ru-ru/dotnet/architecture/maui/mvvm)
- [Newtonsoft.Json](https://www.newtonsoft.com/json/help/html/Introduction.htm)
- [P/Invoke и DllImport](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
- [HttpClient в C#](https://learn.microsoft.com/ru-ru/dotnet/api/system.net.http.httpclient)