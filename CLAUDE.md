# CopyModels — контекст для Claude

## Проект
Revit плагин на C#. Переписываем с Python (PyRevit) на C# + WPF.
Учебный проект — пользователь пишет код сам в Visual Studio, Claude — ментор.

## Архитектура

CopyModels.sln
├── CopyModels.Core      — без Revit API ✅ (Models, Settings)
├── CopyModels.Plugin    — требует RevitAPI.dll ✅ (Services, CopyModelsCommand)
├── CopyModels.ConsoleTest — тестирование ✅
└── CopyModels.UI        — WPF интерфейс ⏳ (этап 2, в работе)



Правило: "Скомпилируется без RevitAPI.dll?" → Core, иначе → Plugin

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
2+3 — WPF UI + JSON формат	⏳ В работе
4 — Планировщик	⏸ Позже
5 — PostgreSQL	⏸ Позже
CopyModels.UI: RelayCommand ✅, ViewModels созданы но пустые, XAML окна пустые.

Ключевые решения
Концепция "дисциплин" удалена. Разделение: Проект → Задача.
JSON — единственный источник правды. UI будет генерировать JSON.
Этапы 2 и 3 параллельно: UI создаёт JSON, Command читает его.
Зависимости
.NET Framework 4.8
Newtonsoft.Json (NuGet)
RevitAPI.dll / RevitAPIUI.dll (только Plugin)