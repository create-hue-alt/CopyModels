# CopyModels — контекст для Claude (обновлено сессия 10)

## Что это за проект
Revit плагин на C#. Переписываем с Python (PyRevit) на C# + WPF.
**Учебный проект** — я пишу код сам, Claude проверяет и объясняет.
Claude не пишет готовый код если я не застрял совсем.

## Мой уровень
- Python: уверенно (был когда-то, не я написал оригинальный плагин, потому хочу разобраться в его работе)
- C#: базовый → становится средним (уже написили несколько классов успешно)
- Revit API: базовый → средний (открытие/закрытие документов, экспорт, транзакции, worksets)
- HTTP API: ✅ изучил в сессии 6 (асинхронность, WebRequest vs HttpClient, REST API)
- События Revit: ✅ изучил в сессии 9 (FailuresProcessing, DialogBoxShowing, Subscribe/Unsubscribe)

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
├── CopyModels.Plugin              — требует RevitAPI.dll ✅✅✅✅
│   ├── Services/
│   │   ├── FileService.cs         ✅ готов (сессия 5)
│   │   ├── RevitServerService.cs  ✅ готов (сессия 6-7)
│   │   ├── ModelService.cs        ✅ готов (сессия 8)
│   │   ├── EventService.cs        ✅ готов (сессия 9)
│   │   └── CopyModelsCommand.cs   ⏳ следующий (сессия 10)
│
├── CopyModels.ConsoleTest         — тестирование ✅
│   ├── Program.cs
│   ├── TestRevitServer.cs
│   └── TestConfigs/
│
└── CopyModels.UI                  — WPF интерфейс (этап 2) ⏳
```

**Главное правило:**
"Этот код скомпилируется без RevitAPI.dll?" → Core, иначе → Plugin

## Текущий статус (сессия 10 — анализ и планирование)

### ✅ Сделано (сессии 1-9)
Все сервисы готовы — см. PROGRESS.md для деталей.

### ⏳ В очереди
1. **CopyModelsCommand.cs** — IExternalCommand точка входа (сессия 10)
2. WPF UI (этап 2)
3. Планировщик (этап 3)

---

## КАК РАБОТАЕТ ОРИГИНАЛЬНЫЙ PYTHON СКРИПТ (разобрано в сессии 10)

Это важно — C# версия должна воспроизвести эту же логику.

### Откуда берётся `discipline`

`discipline` = **имя JSON файла** без расширения.
Если файл `AR.json` → discipline = `"AR"`.
Если файл `MEP.json` → discipline = `"MEP"`.

```python
# script.py строка 126
project_setting = ProjectSettings(settings_file_item, project, setting, ...)
#                                  ^^^^^^^^^^^^^^^^^
#                                  это имя файла — и есть discipline
```

Discipline сохраняется в pyrevit config между сессиями.
`Shift+Click` на кнопке плагина → принудительная смена дисциплины.
`"!BIM!"` — специальный режим: читаются ВСЕ JSON файлы сразу.

### Трёхуровневая структура выбора

```
Уровень 1: Выбор дисциплины (= файл конфига)
    ↓  сохраняется между запусками
Уровень 2: Выбор заданий (SelectFromList, multiselect, группировка по проекту)
    ↓  можно выбрать несколько сразу
Уровень 3: Выбор моделей (для каждого задания отдельно, "Not Actual" по умолчанию)
    ↓
Обработка каждой модели
```

### Три ветки логики обработки модели

```
model_setting.is_exceed == true
    → удалить или архивировать лишнюю модель (её нет в Source, но есть в Target)

selected_setting.purge == true ИЛИ model_setting.is_open_required() == true
    → ОТКРЫТЬ Revit → purge → экспорт/сохранение → закрыть
    (is_open_required: расширение Source ≠ Target, или путь RSN)

иначе
    → простое копирование файла без открытия Revit
```

### Полный flow обработки (ветка "открыть Revit")

```
1. Открыть модель (open_model_with_detach или open_ifc)
2. Если IFC → divide_on_worksets, get_coordinates
3. Найти/создать вид "NavisWorks" (get_view_by_name → create_3d_view)
4. Если purge → clean_revit_file(doc)
5. Для каждого target:
   - Если target .RVT → save_rvt()
   - Если target == source extension → file_server_copy_model()
   - Иначе (конвертация) → export_rvt() [NWC или IFC]
   - Если transmit → transmit_model() + mark_ro()
6. relinquish_doc(doc) → doc.Close() → doc.Dispose()
```

---

## Принятые решения (сессия 10)

### Стратегия развития продукта

**Два сценария использования:**

**Сценарий 1 — "Выгрузка здесь и сейчас"**
Пользователь работает в UI: выбирает проект → модели → форматы → запускает.

**Сценарий 2 — "Планировщик"**
Настройки сохраняются в JSON, планировщик запускает без UI по расписанию.

**Ключевое решение:**
JSON остаётся единственным источником правды. Меняется только **кто его создаёт**:
- Сейчас: вручную (старые конфиги на сервере)
- Этап 2+3: UI генерирует JSON → `C:\Users\%User%\Documents\CopyModels\config.json`
- Планировщик читает тот же JSON

### Этапы разработки (уточнённые)

| Этап | Что делаем | Результат |
|------|-----------|-----------|
| **1** | `CopyModelsCommand.cs` + отладка | Рабочий плагин, читает старый JSON, UI = TaskDialog заглушки |
| **2+3** | WPF UI + новый JSON формат (параллельно!) | UI создаёт JSON, Command читает новый формат |
| **4** | Планировщик | Автозапуск читает тот же JSON из Documents |
| **5** | PostgreSQL | `SettingsRepository` вместо JSON, история запусков, общий доступ. PostgreSQL уже есть на предприятии. Все сервисы не меняются — только источник данных. |

⚠️ **Этапы 2 и 3 делаем параллельно** — UI создаёт JSON, Command его читает. Разрывать нельзя.

### Что изменится в JSON формате

Текущий формат (на сервере) остаётся читаемым — SettingsReader не трогаем.

Для UI-генерируемого конфига добавится блок Schedule:
```json
{
  "Schedule": {
    "Enabled": true,
    "DayOfWeek": "Monday",
    "Time": "23:00"
  }
}
```

### Замечание про дублирующиеся задания

В реальном конфиге есть:
```json
"From RVT RS to RVT FS":         { "Purge": false }
"From RVT RS to RVT FS (purged)": { "Purge": true  }
```
Это одно задание с разным флагом. В UI-версии это будет одна запись с галочкой Purge.
Старые конфиги всё равно читаются без изменений.

---

## Структура CopyModelsCommand.cs (следующий шаг)

```csharp
public class CopyModelsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var app = commandData.Application.Application;   // для EventService
        var uiApp = commandData.Application;              // для EventService

        using (var eventService = new EventService(app, uiApp, Log, LogWarning))
        {
            eventService.Subscribe();

            // Уровень 1: выбор дисциплины (файл конфига)
            var discipline = SelectDiscipline();

            // Уровень 2: выбор заданий (multiselect, группировка по проекту)
            var settings = SettingsReader.ReadDiscipline(configPath, discipline);
            var selectedTasks = SelectTasks(settings);

            // Уровень 3 + обработка: внутри каждого задания
            foreach (var task in selectedTasks)
            {
                var models = task.GetModelsSettings(); // показывает SelectFromList
                ProcessModels(task, models, app);
            }
        }
        return Result.Succeeded;
    }

    private void ProcessModel(ProjectSettings task, ModelSetting model, Application app)
    {
        if (model.IsExceed)
            HandleExceedModel(task, model);         // ветка 1: удалить/архивировать
        else if (task.Purge || model.IsOpenRequired())
            HandleOpenAndExport(task, model, app);  // ветка 2: открыть Revit → обработать
        else
            HandleSimpleCopy(task, model);          // ветка 3: просто скопировать
    }
}
```

---

## Стиль работы
- Я пишу код сам, Claude проверяет и объясняет
- Если я что-то не понимаю — объясни концепцию, потом я пишу сам
- Указывай на ошибки с объяснением почему это ошибка
- Задавай один вопрос в конце сообщения, не несколько

## Зависимости
- .NET Framework 4.8
- Newtonsoft.Json (NuGet)
- RevitAPI.dll / RevitAPIUI.dll (только в CopyModels.Plugin, не в Core!)