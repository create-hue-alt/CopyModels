# CopyModels — Revit Plugin

Плагин для пакетного копирования и экспорта моделей Revit.

## Описание

Переписывается с PyRevit (Python) на C# с WPF интерфейсом.  
Цель — добавить планировщик автозапуска и нормальный UI с выбором форматов.

## Функциональность

### Оригинал (Python) — реализовано
- Чтение настроек дисциплин из JSON конфигов
- Выбор заданий и моделей через UI
- Копирование моделей между файловыми серверами
- Копирование моделей с/на Revit Server (RSN)
- Экспорт в форматы: NWC, RVT, IFC
- Очистка (purge) моделей перед экспортом
- Архивирование предыдущих версий
- Управление рабочими наборами (Worksets)
- Передача (Transmit) моделей
- Отчёт в Excel
- Отправка email-отчёта

### C# версия — в разработке
- [x] Архитектура проекта спроектирована
- [x] `ProjectSettings.cs` — класс данных одного задания ✅
- [x] `ModelSetting.cs` — класс данных одной модели ✅
- [x] `SettingsReader.cs` — чтение JSON конфигов ✅
- [x] `CopyModels.ConsoleTest` — тестирование Core логики ✅
- [x] `FileService.cs` — копирование файлов, архив, маппинг диска ✅
- [x] `RevitServerService.cs` — HTTP запросы к Revit Server REST API (HttpClient, синхронный) ✅ **[сессия 6]**
- [x] **Тестирование RevitServerService** ✅ **[сессия 7]**
- [ ] `ModelService.cs` — открытие / экспорт моделей
- [ ] `EventService.cs` — подавление диалогов Revit
- [ ] `CopyModelsCommand.cs` — точка входа плагина
- [ ] WPF интерфейс с галочками
- [ ] Планировщик (Windows Task Scheduler)

## Структура проекта

```
CopyModels.sln
├── CopyModels.Core           — бизнес-логика, БЕЗ зависимости от Revit API
│   ├── Models/
│   │   ├── ProjectSettings.cs     ✅
│   │   └── ModelSetting.cs        ✅
│   └── Settings/
│       └── SettingsReader.cs      ✅
│
├── CopyModels.Plugin         — требует RevitAPI.dll
│   ├── Services/
│   │   ├── FileService.cs                ✅
│   │   ├── RevitServerService.cs         ✅ [сессия 6]
│   │   ├── ModelService.cs               ⏳
│   │   └── EventService.cs               ⏳
│   └── CopyModelsCommand.cs              ⏳
│
├── CopyModels.ConsoleTest    — тестирование Core (без Revit) ✅
│   ├── Program.cs
│   ├── TestRevitServer.cs                ✅ [сессия 7]
│   └── TestConfigs/
│       ├── Architecture.json
│       ├── Structure.json
│       └── RealProject.json
│
└── CopyModels.UI             — WPF интерфейс (этап 2)
    ├── MainWindow.xaml
    └── ViewModels/
        └── MainViewModel.cs
```

## Требования

- Autodesk Revit 2022+
- .NET Framework 4.8
- Navisworks Exporter (для экспорта NWC)
- IFC Exporter (для экспорта IFC)
- Newtonsoft.Json (NuGet)

## Оригинальный проект (Python)

| Python файл | Назначение |
|---|---|
| `Copy_Models_script.py` | Точка входа, основная логика |
| `settings_classes.py` | Классы ProjectSettings и ModelSetting |
| `modelTools.py` | Работа с Revit API |
| `serverTools.py` | Файловая система и Revit Server |
| `eventsTools.py` | Обработка диалогов и ошибок |

## Текущий статус (сессия 7)

### ✅ Тестирование — FileService + RevitServerService готовы

**SettingsReader — 9 проверок пройдены:**
- ✅ 3 дисциплины найдены
- ✅ 13 заданий прочитаны из конфигов
- ✅ Все поля парсятся правильно (Transmit, Purge, CloseWorksets)
- ✅ Плейсхолдеры подставляются ({DATE}, {TIME}, {PN})

**RevitServerService — 4 теста пройдены:**
- ✅ Тест 1: ExtractServer — корректно парсит RSN пути
- ✅ Тест 2: BuildBaseUrl — правильно формирует REST API URL
- ✅ Тест 3: Логирование callback-ов работает
- ✅ Тест 4: Реальный запрос к Revit Server
  - Успешно прочитаны 13 моделей с `RSN://k-2133.atptlp.local/20175_INARCTICA`
  - Рекурсивный обход папок работает
  - HTTP запросы и парсинг JSON работают

### ⏳ Следующие этапы

1. **ModelService.cs** — высокоуровневая логика (выбор алгоритма)
2. **EventService.cs** — подавление диалогов Revit
3. **CopyModelsCommand.cs** — точка входа плагина
4. **WPF интерфейс** (этап 2) — выбор заданий и форматов экспорта
5. **Планировщик** (этап 3) — Windows Task Scheduler интеграция