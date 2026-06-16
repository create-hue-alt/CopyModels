using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CopyModels.Core.Models;
using CopyModels.Plugin.Services;
using CopyModels.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CopyModels.Plugin
{
    /// <summary>
    ///  Точка входа плагина.
    /// </summary>
    [TransactionAttribute(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CopyModelsCommand : IExternalCommand
    {
        private UIApplication _uiApp;
        private FileService _fileService;
        private RevitServerService _rsnService;
        private ModelService _modelService;
        private EventService _eventService;

        // Накопленные результаты для итогового отчета
        private Dictionary<string, List<string[]>> _resultTable;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elementSet)
        {
            _uiApp = commandData.Application;
            var app = _uiApp.Application;
            var revitVersion = app.VersionNumber;

            // Логирование
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "000_CopyModels");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"CopyModels_{DateTime.Now: yyyyMMdd_HHmmss}.log");
            var logWriter = new StreamWriter(logPath, append: false, System.Text.Encoding.UTF8);

            Action<string> logInfo = m => WriteLog(logWriter, "INFO", m);
            Action<string> logWarning = m => WriteLog(logWriter, "WARNING", m);
            Action<string> logError = m => WriteLog(logWriter, "ERROR", m);

            logInfo($"Windows User  : {Environment.UserName}");
            logInfo($"PC            : {Environment.MachineName}");
            logInfo($"Revit Version : {revitVersion}");
            logInfo($"Revit User    : {app.Username}");

            // Проверка: нет открытых документов
            if (!app.Documents.IsEmpty)
            {
                logError("Please run in a clear Revit session (no open models required).");
                TaskDialog.Show("CopeModels", "Please close all Revit models before running.");
                logWriter.Dispose();
                return Result.Cancelled;
            }

            // Сервисы
            _fileService = new FileService(logInfo, logWarning, logError);
            _rsnService = new RevitServerService(revitVersion, logInfo, logWarning, logError);
            _modelService = new ModelService(app, _fileService, logInfo, logWarning, logError);
            _eventService = new EventService(app, _uiApp, logInfo, logWarning);
            _resultTable = new Dictionary<string, List<string[]>>();

            // Путь к JSON конфигам
            var documentPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "000_CopyModels");

            // Читаем единый конфигурационный файл
            var settingsReader = new SettingsReader(documentPath);
            var settings = settingsReader.ReadAll();

            logInfo($"Found {settings.Count} projects groups");

            // Защитная проверка: если в "ALL" ничего нет, значит конфиг пуст или не прочитался
            if (!settings.ContainsKey("ALL") || settings["ALL"].Count == 0)
            {
                logInfo("No tasks found in configuration files.");
                logWriter.Dispose();
                return Result.Failed;
            }

            // Собираем список номеров проектов (без группы "ALL", ее добавим наверх)
            var projects = settings
                            .Keys
                            .Where(k => k != "ALL")
                            .OrderBy(k => k)
                            .ToList();
            //projects.Insert(0, "ALL");

            // Показываем диалог выбора проекта
            var selectedProject = ShowSelectionDialog(
                                    projects,
                                    "Select Project",
                                    multiselect: false)?.FirstOrDefault();

            if (selectedProject == null) { logWriter.Dispose(); return Result.Cancelled; }

            logInfo($"Selected project/group: {selectedProject}");

            // Берем список задач для выбранного проекта (или "ALL", если выбрали все)
            var projectSettings = settings[selectedProject];

            // Показываем диалог выбора конкретных задач
            var selectedTasks = ShowSelectionDialog(
                projectSettings.Select(s => s.DisplayName).ToList(),
                $"Tasks for {selectedProject}",
                multiselect: true);

            if (selectedTasks == null || selectedTasks.Count == 0)
            { logWriter.Dispose(); return Result.Cancelled; }

            // Формируем плоский список задач для запуска.
            var tasksRun = projectSettings.
                Where(s => selectedTasks.Contains(s.DisplayName))
                .ToList();

            // Выполнение заданий
            _eventService.Subscribe();
            try
            {
                foreach (var task in tasksRun)
                {
                    logInfo($"Starting task: {task.DisplayName}");
                    RunTask(task, logInfo, logWarning, logError);
                }
            }
            finally
            {
                _eventService.Unsubscribe();
                logWriter.Dispose();
            }

            // Вывод результатов
            ShowResultReport();
            return Result.Succeeded;
        }

        // 
        // Выполнение одного задания
        // 

        private void RunTask(
            ProjectSettings task,
            Action<string> logInfo,
            Action<string> logWarning,
            Action<string> logError)
        {
            logInfo($"Task: {task.Project} - {task.DisplayName}");
            _resultTable[task.DisplayName] = new List<string[]>();

            try
            {
                // Мапинг диска
                if (task.MapDrive != null)
                    _fileService.MapDrive(task.MapDrive, task.DrivePath);

                if (task.SourcePath == null) return;

                // Разрешаем {REQUEST} в путях (пропускаем в текущей версии - добавить UI при необходимости)
                var models = BuildModelSettings(task);
                if (models.Count == 0) return;

                foreach (var model in models)
                    ProcessModel(model, task, logInfo, logWarning, logError);
            }
            catch (Exception ex)
            {
                logError($"Task error [{task.DisplayName}]: {ex.Message}");
            }
        }

        // 
        // Построение списка ModelSetting
        // 

        private List<ModelSetting> BuildModelSettings(ProjectSettings task)
        {
            // Получаем источники
            List<string> sources;
            if (FileService.IsRevitServer(task.SourcePath))
                sources = _rsnService.ReadRevitServerModels(Path.GetDirectoryName(task.SourcePath));
            else
                sources = _fileService.ReadModels(task.SourcePath, task.PathExceptions);

            // Получаем exceed-модели (есть в цели, нет в источники)
            var exceedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tgt in task.TargetPaths)
            {
                var tgtList = FileService.IsRevitServer(tgt)
                    ? _rsnService.ReadRevitServerModels(Path.GetDirectoryName(tgt))
                    : _fileService.ReadModels(tgt, task.PathExceptions);
                foreach (var m in tgtList) exceedModels.Add(m);
            }

            Func<string, double?> getDate = p =>
                FileService.IsRevitServer(p)
                    ? _rsnService.GetModelDate(p)
                    : _fileService.GetModelDate(p);

            var result = new List<ModelSetting>();
            var srcDir = Path.GetDirectoryName(task.SourcePath);

            foreach (var src in sources)
            {
                var modelName = Path.GetFileNameWithoutExtension(src);

                // Применяем исключение
                if (task.PathExceptions.Any(
                    e => Path.GetDirectoryName(src)?.ToLower().Contains(e) == true))
                    continue;
                if (task.PathExceptions.Any(e => modelName.Contains(e)))
                    continue;

                // Строим целевые пути
                var targets = BuildTargetPath(src, srcDir, task);
                foreach (var t in targets) exceedModels.Remove(t.Split('>')[0]);

                result.Add(new ModelSetting(src, targets, getDate));
            }

            // Добавляем exceed-модели
            if (task.DeleteMissed)
            {
                foreach (var ex in exceedModels)
                {
                    var exName = Path.GetFileNameWithoutExtension(ex);
                    if (task.PathExceptions.Any
                        (e => Path.GetDirectoryName(ex)?.ToLower().Contains(e) == true)) continue;
                    if (task.CopyExceptions.Any(e => exName.Contains(e))) continue;
                    result.Add(new ModelSetting(ex, null, getDate));
                }
            }

            return result;
        }

        private List<string> BuildTargetPath(
            string srcPath,
            string srcDir,
            ProjectSettings task)
        {
            var targets = new List<string>();
            var modelName = Path.GetFileNameWithoutExtension(srcPath);
            var srcExt = Path.GetExtension(srcPath);

            foreach (var tgtPattern in task.TargetPaths)
            {
                var tgtDir = Path.GetDirectoryName(tgtPattern);
                var tgtExt = Path.GetExtension(tgtPattern);

                string targetPath;
                if (task.KeepStructure)
                {
                    var rel = PathUtils.GetRelativePath(srcDir, Path.GetDirectoryName(srcPath));
                    targetPath = Path.Combine(tgtDir, rel, modelName + tgtExt);
                }
                else
                {
                    targetPath = Path.Combine(tgtDir, modelName + tgtExt);
                }

                // Нормализация слещей
                targetPath = FileService.IsRevitServer(targetPath)
                    ? targetPath.Replace("\\", "/")
                    : targetPath.Replace("/", "\\");

                // ReplaceName
                if (task.ReplaceName != null)
                    foreach (var kv in task.ReplaceName)
                        targetPath = targetPath.Replace(kv.Key, kv.Value);

                targets.Add(targetPath);
            }

            return targets;
        }

        // 
        // Обработка одной модели
        // 

        private void ProcessModel(
            ModelSetting model,
            ProjectSettings task,
            Action<string> logInfo,
            Action<string> logWarning,
            Action<string> logError)
        {
            var modelName = Path.GetFileNameWithoutExtension(model.SourcePath);
            logInfo($"Processing: {modelName}");

            // Exceed (удаление)
            if (model.IsExceed)
            {
                if (task.BackupFolder != null)
                {
                    var archived = _fileService.ArchiveModel(
                                                model.SourcePath,
                                                task.BackupFolder);
                    AddResult(task, modelName, "Archived", model.SourcePath, archived);
                }
                else
                {
                    _fileService.MarkReadWrite(model.SourcePath);
                    File.Delete(model.SourcePath);
                    AddResult(task, modelName, "Deleted", model.SourcePath, "");
                }
                return;
            }

            var srcExt = Path.GetExtension(model.SourcePath).ToUpper();

            // Нужно открыть в Revit
            if (task.Purge || model.IsOpenRequired())
            {
                Document doc = null;

                try
                {
                    doc = srcExt == ".IFC"
                        ? _modelService.OpenIfc(model.SourcePath)
                        : _modelService.OpenWithDetach(model.SourcePath, task.CloseWorksetsMask);

                    if (doc == null)
                    {
                        logError($"Could not open: {model.SourcePath}");
                        return;
                    }

                    // Убедиться что есть NavisWorks вид
                    var view = _modelService.GetViewByName(doc, "Navisworks");
                    if (view == null)
                        view = _modelService.Create3DView(doc, "Navisworks");

                    if (task.Purge)
                        _modelService.PurgeDocument(doc);

                    foreach (var targetRaw in model.Targets)
                    {
                        var (targetPath, viewName) = ModelSetting.SplitTarget(targetRaw);
                        var tgtExt = Path.GetExtension(targetPath).ToUpper();

                        bool ok;
                        if (tgtExt == ".RVT")
                        {
                            ok = _modelService.SaveAsRvt(doc, targetPath, task.BackupFolder);
                        }
                        else
                        {
                            ok = _modelService.ExportModel(
                                doc,
                                targetPath,
                                task.BackupFolder,
                                viewName ?? "Navisworks",
                                task.NwcAllProperties,
                                task.NwcRoom,
                                task.NwcDivideIntoLevels,
                                task.NwcLinkedFiles,
                                task.IfcSettings);
                        }

                        if (ok)
                        {
                            logInfo($"Saved: {targetPath}");
                            AddResult(task,
                                modelName,
                                tgtExt == ".RVT"
                                        ? "Saved RVT"
                                        : $"Exported {tgtExt}",
                                model.SourcePath,
                                targetPath);

                            if (task.Transmit.HasValue)
                            {
                                _modelService.TransmitModel(targetPath,
                                                task.Transmit.Value,
                                                task.RelativeLinks);
                                if (task.Transmit == true)
                                    _fileService.MarkReadOnly(targetPath);
                            }
                        }
                        else
                        {
                            logError($"Export failed: {targetPath}");
                        }
                    }

                }
                finally
                {
                    if (doc != null)
                        _modelService.RelinquishAndClose(doc);
                }

            }
            // Простое копирование
            else
            {
                foreach (var targetRaw in model.Targets)
                {
                    var (targetPath, _) = ModelSetting.SplitTarget(targetRaw);

                    bool ok;
                    if (FileService.IsRevitServer(model.SourcePath) &&
                        FileService.IsRevitServer(targetPath))
                    { ok = _rsnService.CopyOnRevitServer(model.SourcePath, targetPath); }
                    else
                    { ok = _fileService.CopyFile(model.SourcePath, targetPath, task.BackupFolder); }

                    if (ok)
                    {
                        AddResult(task, modelName, "Copied", model.SourcePath, targetPath);

                        if (task.Transmit.HasValue)
                        {
                            _modelService.TransmitModel(targetPath, task.Transmit.Value, task.RelativeLinks);
                            if (task.Transmit == true)
                                _fileService.MarkReadOnly(targetPath);
                        }
                    }
                    else
                    {
                        logError($"Copy failed: {model.SourcePath} -> {targetPath}");
                    }
                }
            }
        }

        // 
        // Вспомогательные методы
        // 

        private void AddResult(
            ProjectSettings task,
            string model,
            string action,
            string from,
            string to) =>
        _resultTable[task.DisplayName].Add(new[] { model, action, $"From: {from}\nTo: {to}" });

        private void ShowResultReport()
        {
            // В будущем - WPF окно. Пока - TaskDialog с кратким итогом.
            var sb = new System.Text.StringBuilder();
            foreach (var kv in _resultTable)
            {
                sb.AppendLine($"=== {kv.Key}: ({kv.Value.Count} models) ===");
                foreach (var row in kv.Value)
                    sb.AppendLine($" [{row[1]}] {row[0]}");
            }
            TaskDialog.Show("CopyModels - Report", sb.Length > 0
                                                        ? sb.ToString()
                                                        : "No operation performed.");
        }

        private static List<string> ShowSelectionDialog(
            List<string> items,
            string title,
            bool multiselect)
        {
            // TODO: заменить на WPF - диалог (CopyModels.UI).
            // Временный вариант через TaskDialog (только для одиночного выбора).
            
            // Одиночный выбор через TaskDualog
            if (!multiselect)
            {
                if (items.Count == 0) return null;
                if (items.Count == 1) return new List<string> { items[0] };

                var dlg = new TaskDialog(title);
                dlg.MainInstruction = $"Select one from {items.Count} items";

                for (int i = 0; i < Math.Min(items.Count, 10); i++)
                {
                    dlg.AddCommandLink(
                        (TaskDialogCommandLinkId)(1000 + i),
                        items[i]);
                }

                var result = dlg.Show();
                var idx = (int)result - 1000;
                return idx >= 0 && idx < items.Count
                    ? new List<string> { items[idx] }
                    : null;
            }

            // Multiselect: временно возвращает все
            // (пототм будет WPF окно с чекбоксами)

            return items;
        }

        private static void WriteLog(StreamWriter writer, string level, string message)
        {
            var line = $"{DateTime.Now:yyyy.MM.dd HH:mm:ss} [{level}] {message}";
            writer.WriteLine(line);
            writer.Flush();
        }

        // Мини-утилита для относительных путей 
        internal static class PathUtils
        {
            public static string GetRelativePath(string relativeTo, string path)
            {
                if (!relativeTo.EndsWith("\\")) relativeTo += "\\";
                var uri = new Uri(relativeTo);
                var target = new Uri(path.EndsWith("\\") ? path : path + "\\");
                return Uri.UnescapeDataString(
                    uri.MakeRelativeUri(target)
                    .ToString()
                    .Replace('/', '\\'));
            }
        }

    }
}
