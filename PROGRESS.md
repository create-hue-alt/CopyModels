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
│       ├── SettingsReader.cs       — читает JSON файлы, создаёт ProjectSettings ✅
│       ├── AppPaths.cs             — пути к конфигам/логам (Documents\000_CopyModels) ✅
│       └── AppDefaults.cs          — общие константы (env-переменные, "ALL", допуски) ✅
│
├── CopyModels.Plugin               — требует RevitAPI.dll
│   ├── Services/
│   │   ├── FileService.cs              — копирование файлов, архив, маппинг диска (WinAPI) ✅
│   │   ├── RevitServerService.cs       — HTTP запросы к Revit Server (RSN) ✅
│   │   ├── ModelService.cs             — открытие / экспорт / сохранение моделей Revit ✅
│   │   ├── EventService.cs             — подписка на события, автозакрытие диалогов ✅
│   │   └── DialogWatchdogService.cs    — UI Automation вотчдог для зависающего
│   │                                     системного диалога NWC-экспортера ✅
│   ├── CopyModelsExecutor.cs       — общий движок обработки моделей ✅
│   ├── CopyModelsCommand.cs        — IExternalCommand, интерактивная точка входа,
│   │                                  показывает окна выбора из CopyModels.UI ✅
│   └── CopyModelsApplication.cs    — IExternalApplication, headless-автозапуск
│                                      (Task Scheduler, без диалогов) ✅
│
├── CopyModels.ConsoleTest          — тестирование Core логики (без Revit) ✅
│   ├── Program.cs
│   ├── TestRevitServer.cs
│   └── TestConfigs/
│       ├── Architecture.json
│       ├── Structure.json
│       └── RealProject.json
│
└── CopyModels.UI                   — WPF интерфейс (этап 2+3) ⏳ в основном готово
    ├── Commands/
    │   └── RelayCommand.cs             ✅
    ├── ViewModels/
    │   ├── ProjectSelectionViewModel.cs ✅ подключена к CopyModelsCommand
    │   ├── ModelSelectionViewModel.cs   ✅ подключена к CopyModelsCommand
    │   ├── ModelSelectionItem.cs        ✅ вспомогательная модель для чекбоксов
    │   ├── BadgeItem.cs                 ✅ вспомогательная модель для UI-бейджей
    │   ├── ProgressViewModel.cs         ⏳ пустая заглушка, не подключена
    │   └── ExportOptionsViewModel.cs    ⏳ пустая заглушка, окна нет вообще
    └── Windows/
        ├── ProjectSelectionWindow.xaml  ✅ подключено к CopyModelsCommand
        ├── ModelSelectionWindow.xaml    ✅ подключено к CopyModelsCommand
        └── ProgressWindow.xaml          ⏳ файл есть, нигде не используется
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

### Этап 2+3 ⏳ В основном готово
- [x] RelayCommand.cs
- [x] ProjectSelectionViewModel.cs + ProjectSelectionWindow.xaml — выбор проекта
- [x] ModelSelectionViewModel.cs + ModelSelectionWindow.xaml — выбор моделей (чекбоксы, Check/Uncheck/Toggle All)
- [ ] ProgressViewModel.cs + ProgressWindow.xaml — заглушки, не подключены (выполнение сейчас синхронное, без индикации)
- [ ] ExportOptionsViewModel.cs + ExportOptionsWindow.xaml — окна нет, ViewModel пустая

### Этап 4 ✅ Завершён — headless autorun + Task Scheduler
- [x] CopyModelsExecutor.cs — движок вынесен из CopyModelsCommand, общий для Command/Application
- [x] CopyModelsApplication.cs — IExternalApplication, читает COPYMODELS_AUTORUN/PROJECT/DEBUG
- [x] Retry первой модели при сбое NWC-экспорта (баг холодного старта Revit-сессии)
- [x] Toggleable debug-логи (COPYMODELS_DEBUG) + понижение шумных логов
- [x] DialogWatchdogService.cs — UI Automation вотчдог для диалога
      "No suitable geometry found" (поиск по AutomationId, не по локализованному тексту)
- [x] AppDefaults.cs — вынесены магические строки/числа (env-переменные, "ALL", допуски)
- [x] Bat-файл + Windows Task Scheduler — настроено и протестировано реальным прогоном

### Этап 5 ⏸
- [ ] SettingsRepository (PostgreSQL)
- [ ] История запусков

---

## Разделение ответственности между сервисами

FileService        — локальные файлы (P:, C:)
RevitServerService — Revit Server (RSN://)
ModelService       — открытие / экспорт / сохранение моделей Revit
EventService       — подписка на события Revit, автозакрытие диалогов
DialogWatchdogService — сторонний Win32/UI Automation вотчдог для диалогов,
                        которые EventService не ловит (не Revit'овские MessageBox)

CopyModelsExecutor  — общий движок: обход моделей, retry первой модели, вызов сервисов
CopyModelsCommand   — интерактивная точка входа (кнопка в ленте), показывает окна CopyModels.UI
CopyModelsApplication — headless точка входа (Task Scheduler, без диалогов)

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

### Сессия 13 — Headless autorun (ветка feature/headless-autorun)
- CopyModelsExecutor вынесен из CopyModelsCommand — общий движок для
  интерактивного и headless режимов
- CopyModelsApplication (IExternalApplication) — точка входа для автозапуска
  без диалогов, читает COPYMODELS_AUTORUN/COPYMODELS_PROJECT/COPYMODELS_DEBUG
- Найден и починен баг холодного старта: первый вызов NWC-экспорта иногда падает
  с системным диалогом "No suitable geometry found" — исправлено полным повторным
  проходом (open+export+close) для первой модели
- DialogWatchdogService — UI Automation вотчдог, автоматически закрывает
  этот диалог (поиск кнопки по AutomationId, устойчиво к локализации Windows)
- Магические строки/числа вынесены в AppDefaults; устранено дублирование
  проверки IsRevitServer между Core и Plugin
- Bat-файл + Windows Task Scheduler настроены и протестированы полным
  автономным прогоном (LogonType=InteractiveToken — обязательно для UI Automation)
- Сверка документации (CLAUDE.md/PROGRESS.md/README.md) с реальным кодом:
  обнаружено, что CopyModels.UI на самом деле значительно дальше, чем считалось —
  ProjectSelectionWindow и ModelSelectionWindow полностью рабочие и подключены
  к CopyModelsCommand; пустыми заглушками остаются только ProgressWindow
  (файлы есть, не подключены) и ExportOptionsWindow (нет файла вообще)
- Следующий шаг: подключить ProgressWindow для индикации выполнения задания;
  отдельно на заметку — подпись DLL сертификатом, чтобы не подтверждать
  "Always Load" после каждой пересборки

---

## Полезные ссылки

- [Revit API Docs](https://www.revitapidocs.com/)
- [Revit API Forum](https://forums.autodesk.com/t5/revit-api-forum/bd-p/160)
- [MVVM паттерн](https://learn.microsoft.com/ru-ru/dotnet/architecture/maui/mvvm)
- [Newtonsoft.Json](https://www.newtonsoft.com/json/help/html/Introduction.htm)
- [P/Invoke и DllImport](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
- [HttpClient в C#](https://learn.microsoft.com/ru-ru/dotnet/api/system.net.http.httpclient)
