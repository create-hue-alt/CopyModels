using Autodesk.Revit.DB.Events;
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
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    internal class CopyModelsApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            if (Environment.GetEnvironmentVariable("COPYMODELS_AUTORUN") != "1")
                return Result.Succeeded;

            application.ControlledApplication.ApplicationInitialized += OnInitialized;
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application) =>
            Result.Succeeded;

        private void OnInitialized(object sender, ApplicationInitializedEventArgs e)
        {
            var app = (Autodesk.Revit.ApplicationServices.Application)sender;
            var projectId = Environment.GetEnvironmentVariable("COPYMODELS_PROJECT");

            var logDir = AppPaths.LogDir;
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"CopyModels_Autorun_{DateTime.Now:yyyyMMdd_HHmm}.log");
            var logWriter = new StreamWriter(logPath, append: false, System.Text.Encoding.UTF8);

            Action<string> logInfo = m => CopyModelsExecutor.WriteLog(logWriter, "INFO", m);
            Action<string> logWarning = m => CopyModelsExecutor.WriteLog(logWriter, "WARNING", m);
            Action<string> logError = m => CopyModelsExecutor.WriteLog(logWriter, "ERROR", m);

            // Сервисы (без UIApplication - headless)
            var fileService = new FileService(logInfo, logWarning, logError);
            var rsnService = new RevitServerService(app.VersionNumber, logInfo, logWarning, logError);
            var modelService = new ModelService(app, fileService, logInfo, logWarning, logError);

            // Читаем конфиги
            var documentPath = AppPaths.ConfigDir;
            var settingsReader = new SettingsReader(documentPath);
            var settings = settingsReader.ReadAll();

            // Ищем проект по ID
            var allProjects = settings.ContainsKey("ALL")
                ? settings["ALL"]
                : new List<ProjectSettings>();
            var task = allProjects.FirstOrDefault(p => p.Project == projectId);
            if (task == null) { /* logError*/ return; }

            // Map drive если нужно
            if (task.MapDrive != null) fileService.MapDrive(task.MapDrive, task.DrivePath);

            // Executor - все модели без диалогв
            var executor = new CopyModelsExecutor(fileService, rsnService, modelService,
                logInfo, logWarning, logError);
            var models = executor.HeadlessModelSettings(task);
            executor.RunTask(task, models);

            try
            {
                logInfo($"Starting Headless task: {task.DisplayName} ");
            }
            finally
            {
                logWriter.Dispose();
            }


            // Выход из Revit
            var uiApp = new UIApplication(app);
            uiApp.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.ExitRevit));
        }
    }
}
