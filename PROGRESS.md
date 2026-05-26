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
│   │   ├── ModelService.cs         — открытие / экспорт / сохранение моделей Revit ⏳
│   │   └── EventService.cs         — подписка на события, автозакрытие диалогов ⏳
│   └── CopyModelsCommand.cs        — IExternalCommand, точка входа ⏳
│
├── CopyModels.ConsoleTest          — тестирование Core логики (без Revit) ✅
│   ├── Program.cs                  — тесты SettingsReader
│   ├── TestRevitServer.cs          — тесты RevitServerService (сессия 7)
│   └── TestConfigs/                — JSON файлы для тестирования
│       ├── Architecture.json
│       ├── Structure.json
│       └── RealProject.json
│
└── CopyModels.UI                   — WPF интерфейс (этап 2) ⏳
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
| `modelTools.py` | `Plugin/Services/ModelService.cs` | ⏳ |
| `eventsTools.py` | `Plugin/Services/EventService.cs` | ⏳ |
| `script.py` (точка входа) | `Plugin/CopyModelsCommand.cs` | ⏳ |
| `*.json` конфиги | без изменений | ✅ |

## Чеклист реализации

### Core/Models ✅
- [x] `ProjectSettings.cs` — все поля и конструктор
- [x] `ModelSetting.cs` — все поля и логика дат

### Core/Settings ✅
- [x] `SettingsReader.cs` — чтение JSON конфигов, парсинг структуры

### Core/Tests ✅
- [x] `CopyModels.ConsoleTest` — консольное тестирование
- [x] Простые JSON конфиги для проверки
- [x] 9 проверок SettingsReader — все пройдены ✅
- [x] 4 теста RevitServerService — все пройдены ✅ (сессия 7)

### Plugin/Services ✅✅
- [x] `FileService.cs` — копирование файлов, архив, маппинг диска ✅
- [x] `RevitServerService.cs` — HTTP запросы к Revit Server ✅
  - [x] HttpClient (синхронный, встроен timeout)
  - [x] ReadRevitServerModels() — рекурсивный обход папок
  - [x] GetModelDate() — парсинг `/Date(...)` формата
  - [x] CopyOnRevitServer() — копирование на RSN
  - [x] Полная обработка ошибок и логирование
- [x] `ModelService.cs` — открытие / экспорт / сохранение моделей
- [x] `EventService.cs` — подавление диалогов Revit

### Plugin ⏳
- [ ] `CopyModelsCommand.cs`

### UI (Этап 2) ⏳
- [ ] `MainWindow.xaml`
- [ ] `MainViewModel.cs`
- [ ] Список заданий с галочками
- [ ] Список моделей с галочками
- [ ] Выбор форматов экспорта

### Планировщик (Этап 3) ⏳
- [ ] Интеграция с Windows Task Scheduler
- [ ] UI для настройки расписания

---

## Журнал сессий

### Сессия 1 — знакомство с проектом и архитектура

**Дата:** (первая сессия)

**Что сделали:**
- Изучили все Python файлы оригинала
- Составили архитектурную карту проекта
- Спроектировали структуру C# решения
- Создали README.md и PROGRESS.md

**Принятые решения:**
- JSON конфиги оставляем в том же формате
- Архитектура: 3 проекта в Solution (Core, Plugin, UI)
- Планировщик через Windows Task Scheduler
- UI на WPF по паттерну MVVM

---

### Сессия 2 — первый код, ProjectSettings.cs

**Дата:** 13.04.2026

**Что сделали:**
- Пересмотрели архитектуру: уточнили границу Core / Plugin
- Создали Solution в Visual Studio — два проекта `Core` и `Plugin`
- Подключили `CopyModels.Core` как зависимость в `CopyModels.Plugin`
- Разобрались зачем несколько проектов вместо папок
- Начали писать `ProjectSettings.cs`

**Разобранные концепции:**

| Концепция | Суть |
|---|---|
| `.Core` / `.Plugin` в названии | Соглашение об именовании, не требование языка |
| Несколько проектов в Solution | Физический контроль зависимостей — Core не может случайно импортировать Revit API |
| `{ get; }` без setter | Свойство только для чтения, задаётся только в конструкторе |
| `{ get; private set; }` | Читать можно снаружи, писать только внутри класса |
| `bool?` (nullable) | Три состояния: true / false / null. Нужно для `Transmit` — отсутствие ≠ false |
| `JObject` в конструкторе | Передаём весь JSON-блок, не 15 отдельных параметров |
| `?.Value<string>()` | Безопасное чтение из JSON — если ключа нет, не падаем |
| `?? false` | Значение по умолчанию если JSON вернул null |
| `is JArray arr` | Проверка типа и каст в одну строку (pattern matching) |
| `/// <summary>` | XML-документация — всплывает в IntelliSense при наведении |
| `ReplacePlaceholders()` | Подставляет {PN}, {DATE}, {TIME} в пути при чтении |
| `ParseStringList()` | Вспомогательный метод для чтения массивов строк из JSON |

**Написанный код:**
- Все поля `ProjectSettings` описаны
- Конструктор: `Discipline`, `Project`, `SourcePath` (с ReplacePlaceholders), `TargetPath` (JArray), `BackupFolder`, `Purge`, `KeepStructure`, `DeleteMissed`, `Transmit`
- Методы: `ReplacePlaceholders()`, `ParseStringList()`

---

### Сессия 3 — ModelSetting.cs

**Что сделали:**
- Полностью написаны классы `ProjectSettings` и `ModelSetting`

**Ключевые вещи:**
- `ProjectSettings` содержит все параметры одного задания из JSON
- `ModelSetting` содержит информацию об одной модели (источник + целевые пути)
- `DisplayName` форматируется красиво с отступами для UI

---

### Сессия 4 — SettingsReader.cs и первое тестирование ✅

**Дата:** 21.04.2026

**Что сделали:**
- Написали `SettingsReader.cs` — главный класс для чтения JSON конфигов
- Создали `CopyModels.ConsoleTest` проект для тестирования
- Создали простые JSON файлы с тестовыми данными
- Написали полный `Program.cs` с 6 проверками
- **Запустили и ВСЕ ПРОВЕРКИ ПРОЙДЕНЫ!** ✅

**Результаты тестирования:**
```
✓ Проверка 1: найдено 2 дисциплины (Architecture, Structure)
✓ Проверка 2: дисциплины найдены в списке
✓ Проверка 3: Architecture содержит ProjectA и ProjectB
✓ Проверка 4: Architecture содержит 3 задания
✓ Проверка 5: ReadAll() содержит 5 заданий
✓ Проверка 6: DisplayName не пустой и отформатирован
```

**Что делает SettingsReader:**
1. `GetDisciplineNames()` — возвращает список имён дисциплин (.json файлы)
2. `ReadDiscipline(name)` — читает одну дисциплину
3. `ReadAll()` — читает все дисциплины сразу
4. Возвращает `Dictionary<string, List<ProjectSettings>>` с ключами "ALL", "ProjectA", "ProjectB" и т.д.
5. Автоматически сортирует по `DisplayName`

**Разобранные концепции:**
- `Directory.GetFiles()` — поиск файлов по маске
- `Path.GetFileNameWithoutExtension()` — получить имя без расширения
- `JObject.Parse()` — парсить JSON
- `foreach (var projectProp in root.Properties())` — итерация по объектам JSON
- `IReadOnlyList<T>` — возвращаемый тип (только для чтения)

---

### Сессия 5 — FileService.cs ✅

**Дата:** 10.05.2026

**Что сделали:**
- Написали полный `FileService.cs` — все операции с файловой системой
- Исправили опечатки и логические ошибки в откатывании архива
- Добавили правильное использование UTC времени для дат
- Реализовали маппинг сетевых дисков через Windows API
- Уточнили разделение ответственности между сервисами

**Разобранные концепции:**

| Концепция | Суть |
|---|---|
| `LastWriteTimeUtc` вместо `LastWriteTime` | Всегда UTC, не зависит от локального времени машины |
| `File.Move(src, dst, overwrite: true)` | Перемещение файла с опцией перезаписи |
| `EnsureUniquePath()` | Проверка уникальности пути — добавляет `_1`, `_2` если файл существует |
| `Path.IsPathRooted()` | Проверка абсолютности пути |
| Плейсхолдеры архива | `{MODEL_NAME}` и `{MODEL_DATE}` в пути архива |
| P/Invoke и DllImport | Вызов функций Windows API из C# (mpr.dll) |
| NETRESOURCE структура | Полное описание сетевого ресурса для WNetAddConnection2 |
| Нормализация driveLetter | `TrimEnd('\\', ':') + ":"` для единообразия |

**Ключевое решение — разделение ответственности (см. раздел ниже):**
- `FileService` — ТОЛЬКО операции с файлами
- `RevitServerService` — ТОЛЬКО операции с Revit Server
- `ModelService` — выбирает нужный сервис по типу пути


**Добавлено в код:**
- XML-документация `/// <summary>` для всех public методов
- Детальные комментарии в P/Invoke декларациях
- Логирование с полной информацией об ошибках
- Try-catch блоки в критических методах

---

### Сессия 6 — HTTP запросы и RevitServerService.cs ✅

**Дата:** 18.05.2026

**Что сделали:**
- Создали полный курс "HTTP запросы в C#" (11 документов)
- Разобрались с асинхронностью в контексте проекта
- Выяснили как работает progress bar с синхронным кодом
- Переделали RevitServerService с WebRequest на HttpClient
- Исправили все ошибки в коде

---

### Сессия 7 — Тестирование RevitServerService ✅ **[НОВОЕ]**

**Дата:** 20.05.2026

**Что сделали:**
- Создали класс `TestRevitServer` в ConsoleTest проекте
- Написали 4 теста для RevitServerService:
  - **Тест 1:** ExtractServer — парсинг RSN пути ✅
  - **Тест 2:** BuildBaseUrl — формирование REST API URL ✅
  - **Тест 3:** Логирование callback-ов ✅
  - **Тест 4:** Реальный запрос к Revit Server ✅
- Все тесты успешно прошли
- **Результат:** 13 моделей успешно прочитаны с реального сервера `RSN://k-2133.atptlp.local/20175_INARCTICA`

**Проверки (Assertions):**
- ✅ 3 дисциплины найдены
- ✅ SettingsReader правильно парсит конфиги
- ✅ RevitServerService корректно работает с REST API
- ✅ Рекурсивный обход папок функционирует

**Технические детали:**
- Использована рефлексия для тестирования private методов
- Callback-функции для логирования работают
- HTTP запросы к реальному Revit Server успешны
- Парсинг дат в формате Revit Server (`/Date(...)`) работает

## Разделение ответственности между сервисами

Это ключевое архитектурное решение, принятое в сессии 5.

```
┌─────────────────────────────────────────────────────────────────┐
│ ModelService (ТОЧКА ВХОДА)                                      │
│ ├─ GetModelDate(anyPath)     ← проверяет тип пути              │
│ ├─ CopyModel(source, target) ← выбирает алгоритм               │
│ ├─ ReadModels(path)          ← выбирает алгоритм               │
│ └─ Зависит от FileService + RevitServerService                 │
│                                                                  │
│  ┌──────────────────┐          ┌──────────────────────────┐    │
│  │ FileService      │          │ RevitServerService       │    │
│  │ (ФАЙЛЫ)          │          │ (REVIT SERVER / RSN)     │    │
│  ├─ GetModelDate()  │          ├─ GetModelDate()          │    │
│  ├─ CopyFile()      │          ├─ ReadRevitServerModels() │    │
│  ├─ ArchiveModel()  │          ├─ CopyOnRevitServer()     │    │
│  ├─ MarkReadWrite() │          └─ HTTP REST запросы       │    │
│  ├─ ReadFileServerModels() │                               │    │
│  └─ MapDrive()      │                                       │    │
│     (Windows API)   │                                       │    │
└─────────────────────────────────────────────────────────────────┘

FilePath: P:\Projects\Model.rvt        → FileService
RSN Path: RSN://server/folder/Model.rvt → RevitServerService
```

## Сессия 8: ModelService.cs ✅

### ✅ Реализовано (620 строк кода)

- OpenWithDetach() — RVT с детачем + worksets
- OpenIfc() — открытие IFC файлов
- SaveAsRvt() — сохранение как Central с архивом
- ExportModel() — NWC/IFC (обработка void vs bool)
- PurgeDocument() — очистка через PerformanceAdviser
- TransmitModel() — transmit + relative links
- Виды: GetViewByName(), Create3DView(), CheckAndFixView()
- Опции: BuildNwcOptions(12) + BuildIfcOptions(30+)
- ApplyWorksetConfiguration() — открытие/закрытие worksets

### 🔑 Ключевые решения

1. **Архивирование ДО экспорта** — старый файл в архиве при падении
2. **Проверка файла для обоих форматов** — NWC(void) + IFC(bool)
3. **Избегаем вложенных транзакций** — CheckAndFixView() перед экспортом
4. **Разные подходы опций** — NWC(properties) vs IFC(AddOption)

---

## Сессия 9: EventService.cs

**Что сделали**
- EventService.cs — полная реализация
- OnFailureProcessing() — обработка ошибок
- OnDialogBoxShowing() — подавление диалогов

**Ключевое решение**
1. Два типа Application в EventService:
    - FailuresProcessing → Application._app
    - DialogBoxShowing → UIApplication._uiApp
2. Guard проверка при Subscribe:
    - if (_subscribed) return;  // Избегаем двойной подписки
3. IDisposable паттерн:
    - Позволяет использовать using() блок
    - Автоматическая отписка от событий
4. Логирование через callbacks:
    - Не привязано к конкретной системе логирования
    - Гибко подстраивается под разные логгеры

## Следующие шаги:

CopyModelsCommand.cs — IExternalCommand точка входа:

    - Инициализация всех сервисов
    - Чтение конфигов JSON
    - Выбор заданий через TaskDialog
    - Выбор форматов (NWC/IFC/RVT)
    - Запуск копирования/экспорта
    - EventService регистрация для автоматической обработки
    - Логирование результатов

---

## Полезные ссылки

- [Revit API Docs](https://www.revitapidocs.com/)
- [Revit API Forum](https://forums.autodesk.com/t5/revit-api-forum/bd-p/160)
- [MVVM паттерн](https://learn.microsoft.com/ru-ru/dotnet/architecture/maui/mvvm)
- [Newtonsoft.Json документация](https://www.newtonsoft.com/json/help/html/Introduction.htm)
- [P/Invoke и DllImport](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
- [Windows API mpr.dll](https://learn.microsoft.com/en-us/windows/win32/api/winnetwk/nf-winnetwk-wnetaddconnection2w)
- [HttpClient в C#](https://learn.microsoft.com/ru-ru/dotnet/api/system.net.http.httpclient)
- [REST API тестирование](https://www.postman.com/)