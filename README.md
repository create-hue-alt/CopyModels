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
- [ ] `ProjectSettings.cs` — класс данных одного задания
- [ ] `ModelSetting.cs` — класс данных одной модели
- [ ] `SettingsReader.cs` — чтение JSON конфигов
- [ ] `FileService.cs` — копирование файлов, архив
- [ ] `RevitServerService.cs` — работа с RSN через REST
- [ ] `ModelService.cs` — открытие / экспорт моделей
- [ ] `EventService.cs` — подавление диалогов Revit
- [ ] `CopyModelsCommand.cs` — точка входа плагина
- [ ] WPF интерфейс с галочками
- [ ] Планировщик (Windows Task Scheduler)

## Структура проекта

```
CopyModels.sln
├── CopyModels.Core      — бизнес-логика, БЕЗ зависимости от Revit API
│   ├── Models/
│   │   ├── ProjectSettings.cs
│   │   └── ModelSetting.cs
│   └── Settings/
│       └── SettingsReader.cs
│
├── CopyModels.Plugin    — требует RevitAPI.dll
│   ├── Services/
│   │   ├── FileService.cs
│   │   ├── RevitServerService.cs
│   │   ├── ModelService.cs
│   │   └── EventService.cs
│   └── CopyModelsCommand.cs
│
└── CopyModels.UI        — WPF интерфейс (этап 2)
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
