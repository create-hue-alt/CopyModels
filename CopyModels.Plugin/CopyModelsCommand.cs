using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CopyModels.Core.Models;
using CopyModels.Plugin.Services;
using CopyModels.Settings;
using CopyModels.UI.Windows;
using System;
using System.Collections.Generic;
using System.IO;

namespace CopyModels.Plugin
{
    /// <summary>
    ///  Точка входа плагина.
    /// </summary>
    [TransactionAttribute(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CopyModelsCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elementSet)
        {
            var uiApp = commandData.Application;
            var app = uiApp.Application;
            var revitVersion = app.VersionNumber;

            // Логирование
            var logDir = AppPaths.LogDir;
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"CopyModels_{DateTime.Now:yyyyMMdd_HHmm}.log");
            var logWriter = new StreamWriter(logPath, append: false, System.Text.Encoding.UTF8);

            Action<string> logInfo = m => CopyModelsExecutor.WriteLog(logWriter, "INFO", m);
            Action<string> logWarning = m => CopyModelsExecutor.WriteLog(logWriter, "WARNING", m);
            Action<string> logError = m => CopyModelsExecutor.WriteLog(logWriter, "ERROR", m);

            bool debugEnable = Environment.GetEnvironmentVariable(AppDefaults.EnvDebug) == "1";
            Action<string> logDebug = debugEnable
                ? (Action<string>)(m => CopyModelsExecutor.WriteLog(logWriter, "DEBUG", m))
                : (m => { });

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
            var fileService = new FileService(logInfo, logWarning, logError, logDebug);
            var rsnService = new RevitServerService(revitVersion, logInfo, logWarning, logError, logDebug);
            var modelService = new ModelService(app, fileService, logInfo, logWarning, logError, logDebug);
            var eventService = new EventService(app, uiApp, logInfo, logWarning, logDebug);

            var executor = new CopyModelsExecutor(fileService, rsnService, modelService,
                logInfo, logWarning, logError, logDebug);

            // Путь к JSON конфигам
            var documentPath = AppPaths.ConfigDir;

            // Читаем единый конфигурационный файл
            var settingsReader = new SettingsReader(documentPath);
            var settings = settingsReader.ReadAll();

            logInfo($"Found {settings.Count} projects groups");

            // Защитная проверка: если в "ALL" ничего нет, значит конфиг пуст или не прочитался
            if (!settings.ContainsKey(AppDefaults.AllProjectsKey) || settings[AppDefaults.AllProjectsKey].Count == 0)
            {
                logInfo("No tasks found in configuration files.");
                logWriter.Dispose();
                return Result.Failed;
            }

            // Показываем окно выбора проекта; selectedProject заполняется через callback onOk
            ProjectSettings selectedProject = null;

            var window = new ProjectSelectionWindow();
            window.ViewModel.LoadSettings(documentPath);
            window.ViewModel.SetCallbacks(
                onOk: selected => { selectedProject = selected; window.Close(); },
                onCancel: () => window.Close()
                );

            window.ShowDialog();

            if (selectedProject == null)
            { logWriter.Dispose(); return Result.Cancelled; }

            // Мапинг диска
            if (selectedProject.MapDrive != null)
                fileService.MapDrive(selectedProject.MapDrive, selectedProject.DrivePath);

            if (selectedProject.SourcePath == null)
            { logWriter.Dispose(); return Result.Cancelled; }

            // Строим список моделей выбранного проекта
            var allModels = executor.BuildModelSettings(selectedProject);

            if (allModels.Count == 0)
            { logWriter.Dispose(); return Result.Cancelled; }

            // Показываем окно выбора моделей
            List<ModelSetting> selectedModels = null;

            var modelWindow = new ModelSelectionWindow();
            modelWindow.ViewModel.LoadModels(allModels);
            modelWindow.ViewModel.SetCallbacks(
                onOk: selected => { selectedModels = selected; modelWindow.Close(); },
                onCancel: () => modelWindow.Close()
                );

            modelWindow.ShowDialog();

            if (selectedModels == null || selectedModels.Count == 0)
            { logWriter.Dispose(); return Result.Cancelled; }

            // Выполнение задания
            eventService.Subscribe();
            var watchdog = new DialogWatchdogService(logWarning, logDebug);
            watchdog.Start();
            try
            {
                logInfo($"Starting task: {selectedProject.DisplayName}");
                executor.RunTask(selectedProject, selectedModels);
            }
            finally
            {
                eventService.Unsubscribe();
                watchdog.Stop();
                logWriter.Dispose();
            }

            // Вывод результатов
            executor.ShowResultReport();
            return Result.Succeeded;
        }
    }
}
