# CopyModels — контекст для Claude

## Проект
Revit плагин на C#. Переписываем с Python (PyRevit) на C# + WPF.
Учебный проект — пользователь пишет код сам в Visual Studio, Claude — ментор.

## Архитектура

CopyModels.sln
├── CopyModels.Core      — без Revit API ✅ (Models, Settings)
├── CopyModels.Plugin    — требует RevitAPI.dll ✅ (Services, CopyModelsExecutor, CopyModelsCommand, CopyModelsApplication)
├── CopyModels.ConsoleTest — тестирование ✅
└── CopyModels.UI        — WPF интерфейс ⏳ (этап 2, в основном готово)

Правило: "Скомпилируется без RevitAPI.dll?" → Core, иначе → Plugin

### Точки входа Plugin
- `CopyModelsCommand` — `IExternalCommand`, интерактивный запуск по кнопке в ленте,
  показывает окна выбора проекта/моделей из CopyModels.UI.
- `CopyModelsApplication` — `IExternalApplication`, headless-автозапуск при старте Revit
  (читает `COPYMODELS_AUTORUN`/`COPYMODELS_PROJECT`/`COPYMODELS_DEBUG` из окружения).
- `CopyModelsExecutor` — общий движок обработки моделей, используется обоими входами.
- `DialogWatchdogService` — UI Automation вотчдог, автоматически закрывает системный
  диалог NWC-экспортера ("No suitable geometry found"), который иначе блокирует
  прогон намертво. Подключён и к `CopyModelsCommand` (интерактивный режим),
  и к `CopyModelsApplication` (headless).

## Структура JSON конфига

Один файл `*.json` в папке `Documents\000_CopyModels\`.
Двухуровневая структура: **Проект → Задача** (дисциплин нет).

```json
"000700": {
  "From RVT FS to NWC FS": {
    "Source Path": "...",
    "Target Path": ["..."]
  }
}
```

Текущий статус
Этап	Статус
1 — Plugin + CopyModelsCommand.cs	✅ Готов
2+3 — WPF UI + JSON формат	⏳ В основном готово
4 — Планировщик (headless autorun + Task Scheduler)	✅ Готов
5 — PostgreSQL	⏸ Позже
6 — Нормальный логгер (Serilog/NLog вместо Action<string>)	⏸ Если дойдём
CopyModels.UI: RelayCommand ✅, ProjectSelectionWindow ✅ (подключено к CopyModelsCommand),
ModelSelectionWindow ✅ (подключено к CopyModelsCommand), ProgressWindow — файлы есть,
но не подключены (выполнение пока синхронное без индикации прогресса),
ExportOptionsWindow — не создано (ViewModel пустая заглушка).

Ключевые решения
Концепция "дисциплин" удалена. Разделение: Проект → Задача.
JSON — единственный источник правды. UI будет генерировать JSON.
Этапы 2 и 3 параллельно: UI создаёт JSON, Command читает его.
Логика Command/Application разделена: CopyModelsExecutor — общий движок,
CopyModelsCommand — интерактивный UI, CopyModelsApplication — headless без диалогов.
Магические строки/константы (env-переменные, ключ "ALL", допуски) вынесены в AppDefaults.

Зависимости
.NET Framework 4.8
Newtonsoft.Json (NuGet)
RevitAPI.dll / RevitAPIUI.dll (только Plugin)
