using Autodesk.Revit.UI.Events;
using Autodesk.Revit.UI;
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
    /// Headles - точка входа плагина: запускает задачу без диалогв при старте Revit,
    /// если выставлена переменная окружения COPYMODELS_AUTORUN=1 (используется Task Scheduler).
    /// </summary>

    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class CopyModelsApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            if (Environment.GetEnvironmentVariable(AppDefaults.EnvAutorun) != "1")
                return Result.Succeeded;

            application.Idling += OnIdling;
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application) =>
            Result.Succeeded;

        private void OnIdling(object sender, IdlingEventArgs e)
        {
            var uiApp = (UIApplication)sender;
            uiApp.Idling -= OnIdling;

            var app = uiApp.Application;
            var projectId = Environment.GetEnvironmentVariable(AppDefaults.EnvProject);

            var logDir = AppPaths.LogDir;
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"CopyModels_Autorun_{DateTime.Now:yyyyMMdd_HHmm}.log");
            var logWriter = new StreamWriter(logPath, append: false, System.Text.Encoding.UTF8);

            Action<string> logInfo = m => CopyModelsExecutor.WriteLog(logWriter, "INFO", m);
            Action<string> logWarning = m => CopyModelsExecutor.WriteLog(logWriter, "WARNING", m);
            Action<string> logError = m => CopyModelsExecutor.WriteLog(logWriter, "ERROR", m);

            bool debugEnable = Environment.GetEnvironmentVariable(AppDefaults.EnvDebug) == "1";
            Action<string> logDebug = debugEnable
                ? (Action<string>)(m => CopyModelsExecutor.WriteLog(logWriter, "DEBUG", m))
                : (m => { });

            // Сервисы (без UIApplication - headless)
            var fileService = new FileService(logInfo, logWarning, logError, logDebug);
            var rsnService = new RevitServerService(app.VersionNumber, logInfo, logWarning, logError, logDebug);
            var modelService = new ModelService(app, fileService, logInfo, logWarning, logError, logDebug);
            var eventService = new EventService(app, uiApp, logInfo, logWarning, logDebug);

            // Читаем конфиги
            var documentPath = AppPaths.ConfigDir;
            var settingsReader = new SettingsReader(documentPath);
            var settings = settingsReader.ReadAll();

            // Ищем проект по ID
            var allProjects = settings.ContainsKey(AppDefaults.AllProjectsKey)
                ? settings[AppDefaults.AllProjectsKey]
                : new List<ProjectSettings>();
            var task = allProjects.FirstOrDefault(p => p.Project == projectId);
            if (task == null)
            {
                logError($"Project not found {projectId}");
                logWriter.Dispose();
                return;
            }

            // Map drive если нужно
            if (task.MapDrive != null) fileService.MapDrive(task.MapDrive, task.DrivePath);

            // Executor - все модели без диалогв
            var executor = new CopyModelsExecutor(fileService, rsnService, modelService,
                logInfo, logWarning, logError, logDebug);

            var watchdog = new DialogWatchdogService(logWarning, logDebug);
            watchdog.Start();

            eventService.Subscribe();
            try
            {
                logInfo($"Starting Headless task: {task.DisplayName} ");
                var models = executor.HeadlessModelSettings(task);
                executor.RunTask(task, models);
            }
            finally
            {
                eventService.Unsubscribe();
                watchdog.Stop();
                logWriter.Dispose();
            }


            // Выход из Revit
            uiApp.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.ExitRevit));
        }
    }
}
