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
│   │   ├── RevitServerService.cs         ✅ [сессия 6-7]
│   │   ├── ModelService.cs               ✅ [сессия 8]
│   │   └── EventService.cs               ⏳ [сессия 9]
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

## Текущий статус (сессия 8)

### ✅ ModelService.cs — ПОЛНОСТЬЮ ГОТОВ

**Методы открытия:**
- ✅ OpenWithDetach() — открытие RVT с DetachAndPreserveWorksets
- ✅ OpenIfc() — открытие IFC файлов с импортом
- ✅ RelinquishAndClose() — закрытие с передачей прав (дляworkshared)

**Методы сохранения:**
- ✅ SaveAsRvt() — сохранение как Central с автоматическим архивом

**Методы экспорта:**
- ✅ ExportModel() — универсальный экспорт в NWC/IFC
  - Архивирование старых файлов
  - Экспорт во временный файл
  - Проверка успеха (void для NWC, bool для IFC)
  - Перемещение в финальный путь

**Методы очистки:**
- ✅ PurgeDocument() — удаление неиспользуемых элементов через PerformanceAdviser

**Методы передачи:**
- ✅ TransmitModel() — настройка transmit и relative links

**Вспомогательные методы:**
- ✅ GetViewByName() — поиск вида по названию
- ✅ Create3DView() — создание новой 3D вида (для Navisworks)
- ✅ CheckAndFixView() — подготовка вида (detail level, crop/section box)
- ✅ BuildNwcOptions() — 12 опций экспорта Navisworks
- ✅ BuildIfcOptions() — 30+ опций экспорта IFC
- ✅ ApplyWorksetConfiguration() — открытие/закрытие worksets при открытии

**Ошибки (исправлены):**
- ✅ 6 критичных логических ошибок
- ⚠️ 7 опечаток в сообщениях (не влияют на работу)

### ⏳ Следующие этапы

1. **EventService.cs** (сессия 9) — подавление диалогов Revit
   - on_failure_processing() — автоматическая обработка ошибок
   - on_dialog_open() — закрытие непредвиденных диалогов

2. **CopyModelsCommand.cs** (сессия 10) — точка входа плагина
   - Инициализация сервисов
   - UI выбора заданий/форматов
   - Координация работы

3. **WPF интерфейс** (этап 2) — выбор заданий и форматов
   - MainWindow с TaskDialog пока (временно)
   - Галочки для форматов NWC/IFC/RVT
   - Progress bar (позже)

4. **Планировщик** (этап 3) — Windows Task Scheduler
   - Интеграция с расписанием
   - Автозапуск по расписанию

## Прогресс

| Компонент | Статус | Сессия |
|-----------|--------|--------|
| Core (ProjectSettings + ModelSetting + SettingsReader) | ✅ | 1-4 |
| FileService | ✅ | 5 |
| RevitServerService | ✅ | 6-7 |
| ModelService | ✅ | 8 |
| EventService | ⏳ | 9 |
| CopyModelsCommand | ⏳ | 10 |
| WPF UI | ⏳ | 11+ |
| Scheduler | ⏳ | 12+ |

**Завершено:** 50% основной логики  
**Осталось:** EventService + Command + UI + Scheduler