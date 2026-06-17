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

## Текущий статус

| Этап | Статус |
|------|--------|
| 1 — Plugin + CopyModelsCommand.cs | ✅ Готов |
| 2+3 — WPF UI + новый JSON формат | ⏳ В работе (структура создана, ViewModels пустые) |
| 4 — Планировщик | ⏸ Позже |
| 5 — PostgreSQL | ⏸ Позже |

CopyModels.UI сейчас: RelayCommand, SelectionViewModel, ExportOptionsViewModel, ProgressViewModel — все созданы но пустые. XAML окна — пустые Grid.

## Ключевые решения
- JSON — единственный источник правды. UI будет генерировать JSON в `Documents\CopyModels\config.json`
- Этапы 2 и 3 делаем параллельно (UI создаёт JSON, Command его читает)
- Старый формат конфигов (на сервере) читается без изменений

## Зависимости
- .NET Framework 4.8
- Newtonsoft.Json (NuGet)
- RevitAPI.dll / RevitAPIUI.dll (только Plugin)