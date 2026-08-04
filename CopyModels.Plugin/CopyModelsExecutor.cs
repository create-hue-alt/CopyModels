using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using CopyModels.Core.Models;
using CopyModels.Plugin.Services;
using CopyModels.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CopyModels.Plugin
{
    /// <summary>
    /// Общий движок обработки моделей: обход списка, retry модели при сбое,
    /// вызов сервисов (File/RevitServer/Model). Используется CopyModelsCommand,
    /// и в CopyModelsApplication.
    /// </summary>
    internal class CopyModelsExecutor
    {
        private readonly FileService _fileService;
        private readonly RevitServerService _rsnService;
        private readonly ModelService _modelService;
        private readonly Action<string> _logInfo;
        private readonly Action<string> _logWarning;
        private readonly Action<string> _logError;
        private readonly Action<string> _logDebug;
        private List<ExportResultItem> _results;

        internal CopyModelsExecutor(
            FileService fileService,
            RevitServerService rsnService,
            ModelService modelService,
            Action<string> logInfo,
            Action<string> logWarning,
            Action<string> logError,
            Action<string> logDebug)
        {
            _fileService = fileService;
            _rsnService = rsnService;
            _modelService = modelService;
            _logInfo = logInfo;
            _logWarning = logWarning;
            _logError = logError;
            _results = new List<ExportResultItem>();
            _logDebug = logDebug;
        }

        // 
        // Построение списка ModelSetting
        // 

        internal List<ModelSetting> BuildModelSettings(ProjectSettings task)
        {
            // Получаем источники
            List<string> sources;
            if (AppDefaults.IsRevitServer(task.SourcePath))
                sources = _rsnService.ReadRevitServerModels(PathUtiles.GetRsnDirectoryName(task.SourcePath));
            else
                sources = _fileService.ReadModels(task.SourcePath, task.PathExceptions);

            // Получаем exceed-модели (есть в цели, нет в источники)
            var exceedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tgt in task.TargetPaths)
            {
                var tgtList = AppDefaults.IsRevitServer(tgt)
                    ? _rsnService.ReadRevitServerModels(PathUtiles.GetRsnDirectoryName(tgt))
                    : _fileService.ReadModels(tgt, task.PathExceptions);
                foreach (var m in tgtList) exceedModels.Add(m);
            }

            var getDate = GetModelDateFunc();

            var result = new List<ModelSetting>();
            var srcDir = AppDefaults.IsRevitServer(task.SourcePath)
                ? PathUtiles.GetRsnDirectoryName(task.SourcePath)
                : Path.GetDirectoryName(task.SourcePath);

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

        // 
        // Обработка выбранных моделей
        // 

        internal void RunTask(ProjectSettings task, List<ModelSetting> models)
        {
            _logInfo($"Project: {task.Project} - {task.DisplayName}");
            
            try
            {
                for (int i = 0; i < models.Count; i++)
                {
                    bool ok = ProcessModel(models[i], task, reportFailure: i != 0);
                    _logDebug($"ProcessModel result [{i}] {Path.GetFileNameWithoutExtension(models[i].SourcePath)}: {ok}");
                    if (!ok && i == 0)
                    {
                        _logWarning("First model failed, retrying full open/export cycle once");
                        bool retryOk = ProcessModel(models[i], task);
                        _logDebug($"Retry result: {retryOk}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logError($"Task error [{task.DisplayName}]: {ex.Message}");
            }
        }

        // 
        // Обработка одной модели
        // 

        private bool ProcessModel(ModelSetting model, ProjectSettings task, bool reportFailure = true)
        {
            bool success = true;

            var modelName = Path.GetFileNameWithoutExtension(model.SourcePath);
            _logInfo(new string('=',60));
            _logInfo($"Processing: {modelName}");

            // Exceed (удаление)
            if (model.IsExceed)
            {
                if (task.BackupFolder != null)
                {
                    var archived = _fileService.ArchiveModel(
                                                model.SourcePath,
                                                task.BackupFolder);
                    AddResult(modelName, archived);
                }
                else
                {
                    _fileService.MarkReadWrite(model.SourcePath);
                    File.Delete(model.SourcePath);
                    AddResult(modelName, model.SourcePath);
                }
                return true;
            }

            var srcExt = Path.GetExtension(model.SourcePath).ToUpper();

            // Нужно открыть в Revit
            if (task.Purge || model.IsOpenRequired())
            {
                Document doc = null;
                bool wasDetached = false;

                try
                {
                    doc = srcExt == ".IFC"
                        ? _modelService.OpenIfc(model.SourcePath)
                        : _modelService.OpenWithDetach(model.SourcePath, task.CloseWorksetsMask);

                    if (doc == null)
                    {
                        _logError($"Could not open: {model.SourcePath}");
                        if (reportFailure) AddFailure(modelName, "Failed to open the source model");
                        return false;
                    }

                    wasDetached = doc.IsDetached;

                    // Убедиться что есть NavisWorks вид
                    var defaultView = AppDefaults.NavisworksViewName;

                    var view = _modelService.GetViewByName(doc, defaultView);
                    if (view == null)
                        view = _modelService.Create3DView(doc, defaultView);

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
                                viewName ?? defaultView,
                                task.NwcAllProperties,
                                task.NwcRoom,
                                task.NwcDivideIntoLevels,
                                task.NwcLinkedFiles,
                                task.IfcSettings);
                        }

                        if (ok)
                        {
                            AddResult(modelName,targetPath);

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
                            _logError($"Export failed: {targetPath}");
                            success = false;
                            if (reportFailure) AddFailure(modelName, $"Export/save failed: {targetPath}");
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logError($"Unexpected error processing {modelName} : {ex.Message}\n{ex.StackTrace}");
                    success = false;
                    if (reportFailure) AddFailure(modelName, $"Unexpected error: {ex.Message}");
                }
                finally
                {
                    if (doc != null)
                        _modelService.RelinquishAndClose(doc,wasDetached);
                }
                return success;

            }
            // Простое копирование
            else
            {
                foreach (var targetRaw in model.Targets)
                {
                    var (targetPath, _) = ModelSetting.SplitTarget(targetRaw);

                    bool ok;
                    if (AppDefaults.IsRevitServer(model.SourcePath) &&
                        AppDefaults.IsRevitServer(targetPath))
                    { ok = _rsnService.CopyOnRevitServer(model.SourcePath, targetPath); }
                    else
                    { ok = _fileService.CopyFile(model.SourcePath, targetPath, task.BackupFolder); }

                    if (ok)
                    {
                        AddResult(modelName, targetPath);

                        if (task.Transmit.HasValue)
                        {
                            _modelService.TransmitModel(targetPath, task.Transmit.Value, task.RelativeLinks);
                            if (task.Transmit == true)
                                _fileService.MarkReadOnly(targetPath);
                        }
                    }
                    else
                    {
                        _logError($"Copy failed: {model.SourcePath} -> {targetPath}");
                        if (reportFailure) AddFailure(modelName, $"Copy failed: {targetPath}");
                    }
                }

                return true;
            }
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
                    var rel = PathUtiles.GetRelativePath(
                        srcDir,
                        AppDefaults.IsRevitServer(srcPath)
                            ? PathUtiles.GetRsnDirectoryName(srcPath)
                            : Path.GetDirectoryName(srcPath));
                    targetPath = Path.Combine(tgtDir, rel, modelName + tgtExt);
                }
                else
                {
                    targetPath = Path.Combine(tgtDir, modelName + tgtExt);
                }

                // Нормализация слещей
                targetPath = AppDefaults.IsRevitServer(targetPath)
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

        //S
        // Вспомогательные методы
        //

        private Func<string, double?> GetModelDateFunc() =>
            p => AppDefaults.IsRevitServer(p)
                ? _rsnService.GetModelDate(p)
                : _fileService.GetModelDate(p);

        private void AddResult(string model, string path) =>
        _results.Add(new ExportResultItem(model, success: true, path, errorMessage: null));

        private void AddFailure(string model,string reason) =>
            _results.Add(new ExportResultItem(model, success:false, path:null, errorMessage: reason));
               
        internal List<ExportResultItem> GetResults() => _results;

        internal static void WriteLog(StreamWriter writer, string level, string message)
        {
            var line = $"{DateTime.Now:yyyy.MM.dd HH:mm:ss} [{level}] {message}";
            writer.WriteLine(line);
            writer.Flush();
        }

        // Строит модели напрямую по списку из JSON, без сканирования папки — для headless-режима.
        internal List<ModelSetting> HeadlessModelSettings(ProjectSettings task)
        {
            var srcDir = Path.GetDirectoryName(task.SourcePath);
            var getDate = GetModelDateFunc();

            var result = new List<ModelSetting>();
            foreach (var relative in task.SelectedModels)
            {
                var src = Path.Combine(srcDir, relative);
                var targets = BuildTargetPath(src, srcDir, task);
                result.Add(new ModelSetting(src, targets, getDate));
            }
            return result;
        }

    }

    // Мини-утилита для относительных путей 
    internal static class PathUtiles
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

        /// <summary>
        /// Аналог Path.GetDirectoryName, но безопасный для RSN-псевдо-путей-
        /// Path.GetDirectoryName схлопывает "//" в "/" и ломает "RSN://server/..."
        /// </summary>
        /// <param name="rsnPath"></param>
        /// <returns></returns>
        public static string GetRsnDirectoryName(string rsnPath)
        {
            var idx = rsnPath.LastIndexOf('/');
            return idx < 0 ? rsnPath : rsnPath.Substring(0, idx);
        }
    }
}
