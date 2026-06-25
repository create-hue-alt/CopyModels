# Прогресс разработки CopyModels

> ⚠️ Архитектурное решение (сессия 12): концепция "дисциплин" удалена.  
> JSON структура: **Проект → Задача** (двухуровневая, без дисциплин).  
> SettingsReader читает все `*.json` из папки, имя файла не важно.


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

## Чеклист

### Этап 1 ✅ Завершён
- [x] ProjectSettings.cs
- [x] ModelSetting.cs
- [x] SettingsReader.cs
- [x] FileService.cs
- [x] RevitServerService.cs
- [x] ModelService.cs
- [x] EventService.cs
- [x] CopyModelsCommand.cs

### Этап 2+3 ⏳ В работе
- [x] RelayCommand.cs
- [ ] SelectionViewModel.cs
- [ ] ExportOptionsViewModel.cs
- [ ] ProgressViewModel.cs
- [ ] SelectionWindow.xaml
- [ ] ExportOptionsWindow.xaml
- [ ] ProgressWindow.xaml

### Этап 4 ⏸
- [ ] Windows Task Scheduler интеграция

### Этап 5 ⏸
- [ ] SettingsRepository (PostgreSQL)
- [ ] История запусков

---

## Разделение ответственности между сервисами

FileService        — локальные файлы (P:, C:)
RevitServerService — Revit Server (RSN://)
ModelService       — открытие / экспорт / сохранение моделей Revit
EventService       — подписка на события Revit, автозакрытие диалогов



---

## Журнал сессий

### Сессии 1-11 — Plugin (завершено)
Разработаны все сервисы Core и Plugin. CopyModelsCommand.cs отлажен.  
Подробности в git-истории.

### Сессия 12 — Начало WPF UI
- Написан RelayCommand.cs (с generic версией RelayCommand\<T\>)
- Создана структура ViewModels и Windows (пустые)
- Архитектурное решение: убрали дисциплины, перешли на структуру Проект → Задача
- SettingsReader.cs переписан под новую структуру
- Следующий шаг: SelectionViewModel + SelectionWindow
---

## Полезные ссылки

- [Revit API Docs](https://www.revitapidocs.com/)
- [Revit API Forum](https://forums.autodesk.com/t5/revit-api-forum/bd-p/160)
- [MVVM паттерн](https://learn.microsoft.com/ru-ru/dotnet/architecture/maui/mvvm)
- [Newtonsoft.Json](https://www.newtonsoft.com/json/help/html/Introduction.htm)
- [P/Invoke и DllImport](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
- [HttpClient в C#](https://learn.microsoft.com/ru-ru/dotnet/api/system.net.http.httpclient)