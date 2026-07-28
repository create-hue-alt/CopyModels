# CopyModels — Revit Plugin

Плагин для пакетного копирования и экспорта моделей Revit.

## Описание

Переписывается с PyRevit (Python) на C# с WPF интерфейсом.  
Цель — добавить планировщик автозапуска и нормальный UI с выбором форматов.

## Функциональность

### Оригинал (Python) — реализовано
- Чтение настроек дисциплин из JSON конфигов
- Выбор заданий и моделей через UI (трёхуровневый)
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

**Этап 1 — рабочий плагин (завершен ✅)**
- [x] `ProjectSettings.cs` ✅
- [x] `ModelSetting.cs` ✅
- [x] `SettingsReader.cs` ✅
- [x] `FileService.cs` ✅
- [x] `RevitServerService.cs` ✅
- [x] `ModelService.cs` ✅
- [x] `EventService.cs` ✅
- [x] `CopyModelsCommand.cs` ✅ — точка входа, отлажена

**Этап 2+3 — WPF UI + новый JSON формат (в основном готово ⏳)**
- [x] `ProjectSelectionWindow` + `ProjectSelectionViewModel` — выбор проекта, подключено к `CopyModelsCommand`
- [x] `ModelSelectionWindow` + `ModelSelectionViewModel` — выбор моделей (чекбоксы, Check/Uncheck/Toggle All)
- [ ] `ProgressWindow` + `ProgressViewModel` — файлы созданы, не подключены (выполнение пока синхронное)
- [ ] `ExportOptionsWindow` + `ExportOptionsViewModel` — не создано, ViewModel пустая заглушка
- [ ] `UserConfig` класс — UI-генерируемый JSON (создание/редактирование конфига через UI)

**Этап 4 — Планировщик (завершён ✅)**
- [x] `CopyModelsApplication.cs` — `IExternalApplication`, headless-автозапуск без диалогов
- [x] `CopyModelsExecutor.cs` — общий движок, вынесен из `CopyModelsCommand`
- [x] `DialogWatchdogService.cs` — UI Automation вотчдог для зависающих системных диалогов
- [x] Windows Task Scheduler интеграция — bat-файл + задача настроены и протестированы

**Этап 5 — PostgreSQL вместо JSON**
- [ ] `SettingsRepository` — новый класс, возвращает те же `ProjectSettings` из БД
- [ ] Миграция конфигов из JSON в PostgreSQL (уже есть на предприятии)
- [ ] История запусков — когда, кто, какие модели, результат
- [ ] Общий доступ к конфигам проектов с нескольких машин
- [ ] Все сервисы и Command остаются без изменений

** Этап 6 ⏸ Если дойдём
- [ ] Переход с самодельного логирования (Action<string> logInfo/logWarning/logError/logDebug)
      на Serilog или NLog
- [ ] Sinks (файл/консоль/email при ERROR), ротация и retention логов
- [ ] Структурированные логи вместо plain text

## Архитектура

```
CopyModels.sln
├── CopyModels.Core           — бизнес-логика, БЕЗ зависимости от Revit API
│   ├── Models/
│   │   ├── ProjectSettings.cs     ✅
│   │   └── ModelSetting.cs        ✅
│   └── Settings/
│       ├── SettingsReader.cs      ✅
│       ├── AppPaths.cs            ✅ — пути к конфигам/логам
│       └── AppDefaults.cs         ✅ — общие константы (env-переменные, "ALL", допуски)
│
├── CopyModels.Plugin         — требует RevitAPI.dll
│   ├── Services/
│   │   ├── FileService.cs                ✅
│   │   ├── RevitServerService.cs         ✅
│   │   ├── ModelService.cs               ✅
│   │   ├── EventService.cs               ✅
│   │   └── DialogWatchdogService.cs      ✅ — вотчдог для системных диалогов
│   ├── CopyModelsExecutor.cs             ✅ — общий движок обработки моделей
│   ├── CopyModelsCommand.cs              ✅ — интерактивная точка входа
│   └── CopyModelsApplication.cs          ✅ — headless точка входа (Task Scheduler)
│
├── CopyModels.ConsoleTest    — тестирование Core (без Revit) ✅
│   ├── Program.cs
│   ├── TestRevitServer.cs
│   └── TestConfigs/
│
└── CopyModels.UI             — WPF интерфейс (этап 2+3) ⏳ в основном готово
    ├── Commands/RelayCommand.cs                    ✅
    ├── ViewModels/ProjectSelectionViewModel.cs     ✅
    ├── ViewModels/ModelSelectionViewModel.cs       ✅
    ├── ViewModels/ProgressViewModel.cs             ⏳ заглушка
    ├── ViewModels/ExportOptionsViewModel.cs        ⏳ заглушка
    └── Windows/ (ProjectSelectionWindow, ModelSelectionWindow ✅; ProgressWindow ⏳)
```

## Структура JSON конфига

Файл `*.json` в папке `C:\Users\%UserName%\Documents\000_CopyModels\`.  
Структура: **Проект → Задача** (без разбиения по дисциплинам).

```json
"000700": {
  "From RVT FS to NWC FS": {
    "Source Path": "P:\\...\\*.rvt",
    "Target Path": ["C:\\...\\{PN}.nwc"],
    "Purge": false,
    "Keep Structure": true
  }
}
```

## Логика обработки модели
```
is_exceed → удалить/архивировать
purge OR is_open_required → открыть Revit → обработать → закрыть
иначе → простое копирование
```

## Стратегия JSON конфигов

JSON остаётся единственным источником правды. Меняется только **кто его создаёт**:

| Этап | Кто создаёт JSON | Где лежит |
|------|-----------------|-----------|
| Сейчас | Вручную | На файловом сервере |
| Этап 2+3 | WPF UI | `C:\Users\%User%\Documents\CopyModels\` |
| Этап 4 | Планировщик читает | Тот же Documents |
| Этап 5 | PostgreSQL | `SettingsRepository` вместо `SettingsReader`, все сервисы не меняются |

`SettingsReader` не меняется — читает оба формата.

## Требования

- Autodesk Revit 2022+
- .NET Framework 4.8
- Navisworks Exporter (для NWC)
- IFC Exporter (для IFC)
- Newtonsoft.Json (NuGet)

## Оригинальный проект (Python)

| Python файл | C# аналог | Статус |
|---|---|---|
| `Copy_Models_script.py` | `CopyModelsCommand.cs` / `CopyModelsApplication.cs` | ✅ |
| `settings_classes.py` | `ProjectSettings.cs` + `ModelSetting.cs` | ✅ |
| `modelTools.py` | `ModelService.cs` | ✅ |
| `serverTools.py` | `FileService.cs` + `RevitServerService.cs` | ✅ |
| `eventsTools.py` | `EventService.cs` | ✅ |
