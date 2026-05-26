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
- [x] `RevitServerService.cs` — HTTP запросы к Revit Server REST API ✅ **[сессия 6-7]**
- [x] `ModelService.cs` — открытие / экспорт / сохранение моделей ✅ **[сессия 8]**
- [x] `EventService.cs` — подавление диалогов и ошибок ✅ **[сессия 9]**
- [ ] `CopyModelsCommand.cs` — точка входа плагина ⏳ **[сессия 10]**
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
│   │   ├── RevitServerService.cs         ✅ [сессия 6-7]
│   │   ├── ModelService.cs               ✅ [сессия 8]
│   │   └── EventService.cs               ✅ [сессия 9]
│   └── CopyModelsCommand.cs              ⏳ [сессия 10]
│
├── CopyModels.ConsoleTest    — тестирование Core (без Revit) ✅
│   ├── Program.cs
│   ├── TestRevitServer.cs                ✅
│   └── TestConfigs/
│       ├── Architecture.json
│       ├── Structure.json
│       └── RealProject.json
│
└── CopyModels.UI             — WPF интерфейс (этап 2) ⏳
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

## Текущий статус (сессия 9)

### ✅ EventService.cs — ПОЛНОСТЬЮ ГОТОВ

**Архитектура:**
- Два разных Application объекта:
  - `Application` для `FailuresProcessing` события
  - `UIApplication` для `DialogBoxShowing` события
- Subscribe/Unsubscribe паттерн с guard проверкой
- IDisposable для автоматической очистки

**Обработка ошибок (OnFailureProcessing):**
- DeleteAllWarnings() для очистки неважных ошибок
- Специфическая обработка трёх типов ошибок:
  - LinearConstraintNotParallel → ResolveFailure()
  - DimensionReferencesInvalid → SetCurrentResolutionType(FixElements)
  - GenericNonFatalError → логируем и игнорируем
- Логирование необработанных ошибок с Guid для анализа

**Подавление диалогов (OnDialogBoxShowing):**
- TaskDialog_Missing_Third_Party_Updater(s) → CommandLink1
- TaskDialog_Unresolved_References → CommandLink2
- TaskDialog_Update_Resources / TaskDialog_Macro_Security_Alert → CommandLink1
- Dialog_Revit_DocWarnDialog / пусто → Close
- Неизвестные диалоги → логирование с просьбой отчета

### ⏳ Следующие этапы

1. **CopyModelsCommand.cs** (сессия 10) — точка входа плагина
   - Инициализация сервисов (FileService, RevitServerService, ModelService, EventService)
   - Чтение JSON конфигов и выбор заданий
   - Выбор форматов экспорта (NWC/IFC/RVT)
   - Скоординированная работа всех сервисов
   - Логирование результатов

2. **WPF интерфейс** (этап 2) — выбор заданий и форматов
   - MainWindow с галочками для форматов
   - TreeView для выбора заданий
   - Progress bar для отслеживания
   - Вывод логов

3. **Планировщик** (этап 3) — Windows Task Scheduler
   - Интеграция с расписанием
   - Автозапуск по расписанию

## Прогресс

| Компонент | Статус | Сессия |
|-----------|--------|--------|
| Core (ProjectSettings + ModelSetting + SettingsReader) | ✅ | 1-4 |
| FileService | ✅ | 5 |
| RevitServerService | ✅ | 6-7 |
| ModelService | ✅ | 8 |
| EventService | ✅ | 9 |
| CopyModelsCommand | ⏳ | 10 |
| WPF UI | ⏳ | 11+ |
| Scheduler | ⏳ | 12+ |

**Завершено:** 60% основной логики (все сервисы готовы)  
**Осталось:** CopyModelsCommand + WPF UI + Scheduler