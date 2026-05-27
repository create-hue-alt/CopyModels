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
            var logPath = Path.Combine(@"C:\Log",
                $"CopyModels_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
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
            var scriptPath = Path.GetDirectoryName(
                typeof(CopyModelsCommand).Assembly.Location);
            var settingsPath = Path.Combine(scriptPath, revitVersion);
            var settingsReader = new SettingsReader(settingsPath);

            // Выбор дисциплины
            var disciplines = settingsReader.GetDisciplineNames().ToList();
            disciplines.Add("!BIM!");

            var selectedDiscipline = ShowSelectionDialog(
                disciplines,
                "Select Discipline",
                multiselect: false)
                ?.FirstOrDefault();
            if (selectedDiscipline == null) { logWriter.Dispose(); return Result.Cancelled; }

            logInfo($"Discipline: {selectedDiscipline}");

            // Чтение настроек
            var settings = selectedDiscipline == "BIM"
                ? settingsReader.ReadAll()
                : settingsReader.ReadDiscipline(selectedDiscipline);

            // Выбор заданий
            var allSettings = settings.ContainsKey("ALL")
                ? settings["ALL"]
                : new List<ProjectSettings>();
            var selectedTasks = ShowSelectionDialog(
                allSettings.Select(s => s.DisplayName).ToList(),
                selectedDiscipline,
                multiselect: true);

            if (selectedTasks == null || selectedTasks.Count == 0)
            { logWriter.Dispose(); return Result.Cancelled; }

            var tasksRun = allSettings
                .Where(s => selectedTasks.Contains(s.DisplayName))
                .ToList();

            // Выполнение заданий
            _eventService.Subscribe();
            try
            {
                foreach (var task in tasksRun)
                    RunTask(task, logInfo, logWarning, logError);
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
                    _fileService.MapDrive(task.MapDrive, task.Drivepath);

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
                sources = _fileService.ReadModels(task.SourcePath,task.PathExceptions);

            // Получаем exceed-модели (есть в цели, нет в источники)
            var exceedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tgt in task.TargetPaths)
            {
                var tgtList = FileService.IsRevitServer(tgt)
                    ? _rsnService.ReadRevitServerModels(Path.GetDirectoryName(tgt))
                    : _fileService.ReadModels(tgt,task.PathExceptions);
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
                    foreach (var kv  in task.ReplaceName)
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
            throw new NotImplementedException();
        }

        // 
        // Вспомогательные методы
        // 

        private void AddResult(
            ProjectSettings task,
            string model,
            string action,
            string from,
            string to)
        {
            throw new NotImplementedException();
        }

        private void ShowResultReport()
        {
            throw new NotImplementedException();
        }

        private static List<string> ShowSelectionDialog(
            List<string> items,
            string title,
            bool multiselect)
        {
            throw new NotImplementedException();
        }

        private static void WriteLog(StreamWriter writer, string level, string message)
        {
            throw new NotImplementedException();
        }

        // Мини-утилита для относительных путей 
        internal static class PathUtils
        {
            public static string GetRelativePath(string relativeTo, string path)
            {
                throw new NotImplementedException();
            }
        }

    }
}
