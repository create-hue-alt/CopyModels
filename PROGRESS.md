# Прогресс разработки CopyModels

## Архитектура

```
CopyModels.sln
├── CopyModels.Core                 — бизнес-логика, компилируется БЕЗ Revit API
│   ├── Models/
│   │   ├── ProjectSettings.cs      — поля одного задания + парсинг JSON
│   │   └── ModelSetting.cs         — поля одной модели + логика дат
│   └── Settings/
│       └── SettingsReader.cs       — читает JSON файлы, создаёт ProjectSettings
│
├── CopyModels.Plugin               — требует RevitAPI.dll
│   ├── Services/
│   │   ├── FileService.cs          — копирование файлов, архив, маппинг диска (WinAPI)
│   │   ├── RevitServerService.cs   — HTTP запросы к Revit Server (RSN)
│   │   ├── ModelService.cs         — открытие / экспорт / сохранение моделей Revit
│   │   └── EventService.cs         — подписка на события, автозакрытие диалогов
│   └── CopyModelsCommand.cs        — IExternalCommand, точка входа
│
└── CopyModels.UI                   — WPF интерфейс (этап 2)
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

| Python файл | C# файл | Примечание |
|---|---|---|
| `settings_classes.py → ProjectSettings` | `Core/Models/ProjectSettings.cs` | |
| `settings_classes.py → ModelSetting` | `Core/Models/ModelSetting.cs` | |
| `serverTools.py` | `Plugin/Services/FileService.cs` | Без Revit API, но в Plugin |
| `serverTools.py` (RSN часть) | `Plugin/Services/RevitServerService.cs` | HTTP запросы |
| `modelTools.py` | `Plugin/Services/ModelService.cs` | Требует Revit API |
| `eventsTools.py` | `Plugin/Services/EventService.cs` | Требует Revit API |
| `script.py` (точка входа) | `Plugin/CopyModelsCommand.cs` | IExternalCommand |
| `*.json` конфиги | без изменений | Читаем те же файлы |

## Чеклист реализации

### Core/Models
- [x] `ProjectSettings.cs` — поля описаны, конструктор в процессе
- [x] `ModelSetting.cs`

### Core/Settings
- [ ] `SettingsReader.cs` — чтение JSON конфигов

### Plugin/Services
- [ ] `FileService.cs`
- [ ] `RevitServerService.cs`
- [ ] `ModelService.cs`
- [ ] `EventService.cs`

### Plugin
- [ ] `CopyModelsCommand.cs`

### UI (Этап 2)
- [ ] `MainWindow.xaml`
- [ ] `MainViewModel.cs`
- [ ] Список заданий с галочками
- [ ] Список моделей с галочками
- [ ] Выбор форматов экспорта

### Планировщик (Этап 3)
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

**Следующий шаг:**
- Создать Solution в Visual Studio
- Начать с `ProjectSettings.cs`

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

**Найденные и исправленные ошибки:**
- `"Target Paths"` → `"Target Path"` (опечатка в ключе JSON — компилятор не поймает)
- `{DATA}` → `{DATE}` (опечатка в плейсхолдере)
- `Transmit = ... ?? false` → убрали `?? false`, нужен чистый `bool?`
- `NwcLinkedFiles` — добавлено осознанно для управления экспортом связанных файлов

**Следующий шаг:**
- Дописать оставшиеся поля конструктора: `SelectableCopy`, `CloseWorksetMask`, `CopyExceptions`, `PathExceptions`, `RelativeLinks`, `MapDrive`, `DisplayName`, IFC-поля, NWC-поля, Recipients
- Перейти к `ModelSetting.cs`

---

## Вопросы и решения

| Вопрос | Решение |
|---|---|
| Как хранить конфиги? | JSON файлы, тот же формат что в Python версии |
| Планировщик — как реализовать? | Windows Task Scheduler, управляется из UI |
| UI паттерн? | MVVM — стандарт для WPF |
| ModelService в Core или Plugin? | Plugin — он требует открытый Revit, нарушение границы в первой версии было ошибкой |
| Выравнивание колонками в VS? | Расширение Align Assignments, но не критично |
| Codeium конфликт с VS? | Отключить `Tools → Options → IntelliCode → C# whole line completions` |

---

## Полезные ссылки

- [Revit API Docs](https://www.revitapidocs.com/)
- [Revit API Forum](https://forums.autodesk.com/t5/revit-api-forum/bd-p/160)
- [MVVM паттерн](https://learn.microsoft.com/ru-ru/dotnet/architecture/maui/mvvm)
- [Newtonsoft.Json документация](https://www.newtonsoft.com/json/help/html/Introduction.htm)
- [Codeium для VS](https://marketplace.visualstudio.com/items?itemName=Codeium.codeium-visual-studio)
