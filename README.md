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
│   └── TestConfigs/
│       ├── Architecture.json
│       └── Structure.json
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

## Текущий статус (сессия 6)

### ✅ Plugin слой — FileService + RevitServerService готовы

**FileService.cs (сессия 5):**
- Копирование файлов с проверкой дат
- Архивирование с поддержкой плейсхолдеров
- Управление правами доступа (ReadOnly/ReadWrite)
- Маппинг сетевых дисков (Windows API)
- Поиск моделей рекурсивно с исключениями

**RevitServerService.cs (сессия 6):**
- HttpClient синхронный (встроен timeout, встроен встроен modern API)
- ReadRevitServerModels() — рекурсивный обход папок на Revit Server
- GetModelDate() — получение дат файлов (парсинг `/Date(...)` формата)
- CopyOnRevitServer() — копирование моделей между папками одного сервера
- Полная обработка ошибок и логирование

### ⏳ Следующие этапы

1. **ModelService.cs** — высокоуровневая логика (выбор алгоритма)
2. **EventService.cs** — подавление диалогов Revit
3. **CopyModelsCommand.cs** — точка входа плагина
4. **WPF интерфейс** (этап 2) — выбор заданий и форматов экспорта
5. **Планировщик** (этап 3) — Windows Task Scheduler интеграция