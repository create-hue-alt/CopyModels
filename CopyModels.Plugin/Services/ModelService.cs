using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyModels.Plugin.Services
{
    internal class ModelService
    {
        private readonly Application _app;
        private readonly FileService _fileService;
        private readonly Action<string> _logInfo;
        private readonly Action<string> _logWarning;
        private readonly Action<string> _logError;

        public ModelService(
            Application app,
            FileService fileService,
            Action<string> logInfo = null,
            Action<string> logWarning = null,
            Action<string> logError = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logInfo = logInfo ?? (_ => { });
            _logWarning = logWarning ?? (_ => { });
            _logError = logError ?? (_ => { });
        }

        // 
        // Открытие моделей
        // 

        /// <summary>Открывает модель с DetachAndPreserveWorksets</summary>
        public Document OpenWithDetach(string patth, IList<string> closeWorksetMask = null)
        {
            _logInfo($"Opening (detach): {patth}");
            try
            {
                var modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(patth);
                var options = new OpenOptions
                {
                    DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets,
                    Audit = true
                };

                ApplyWorksetConfiguration(options, modelPath, patth, closeWorksetMask);

                var doc = _app.OpenDocumentFile(modelPath, options);
                _logInfo($"Opened: {patth}");
                return doc;
            }
            catch (Exception ex)
            {
                _logError($"OpneWithDetach filed: {ex.Message}\n{patth}");
                return null;
            }
        }

        /// <summary>Открывает IFC-файлы</summary>
        public Document OpenIfc(string path)
        {
            _logInfo($"Opening IFC: {path}");
            if (!OptionalFunctionalityUtils.IsIFCAvailable())
            {
                _logError("IFC module is not avaliable.");
                return null;
            }
            if (!File.Exists(path))
            {
                _logError($"IFC file not found: {path}");
                return null;
            }

            var opts = new IFCImportOptions
            {
                Action = IFCImportAction.Open,
                AutocorrectOffAxisLines = false,
                AutoJoin = false,
                Intent = IFCImportIntent.Reference
            };

            try
            {
                return _app.OpenIFCDocument(path, opts);
            }
            catch
            {
                opts.Intent = IFCImportIntent.Reference;
                opts.AutoJoin = true;
                return _app.OpenIFCDocument(path, opts);
            }
        }

        // 
        // Закрытие / сброс прав
        // 

        public void RelinquishAndClose(Document doc)
        {
            if (doc == null) return;
            RelinquishOwnership(doc);
            doc.Close();
            doc.Dispose();
            _logInfo("Document closed");
        }

        private void RelinquishOwnership(Document doc)
        {
            if (!doc.IsWorkshared || string.IsNullOrEmpty(doc.PathName)) return;
            try
            {
                WorksharingUtils.RelinquishOwnership(
                    doc,
                    new RelinquishOptions(true),
                    new TransactWithCentralOptions());
                _logInfo("Relinquish done.");
            }
            catch (Exception ex)
            {
                _logWarning($"Relinquish error: {ex.Message}");
            }
        }

        // 
        // Сохранение моделей
        // 

        /// <summary>Сохраняет модель в указанный путь как Central.</summary>
        public bool SaveAsRvt(Document doc, string targetPath, string archiveFolder = null)
        {
            try
            {
                if (archiveFolder != null)
                    _fileService.ArchiveModel(targetPath, archiveFolder);
                if (File.Exists(targetPath))
                    _fileService.MarkReadWrite(targetPath);

                var modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(targetPath);
                var saveOpts = new SaveAsOptions
                {
                    OverwriteExistingFile = true,
                    Compact = true,
                    MaximumBackups = 1,
                };

                if (doc.IsWorkshared)
                {
                    var wsOpts = new WorksharingSaveAsOptions { SaveAsCentral = true };
                    saveOpts.SetWorksharingOptions(wsOpts);
                }

                doc.SaveAs(modelPath, saveOpts);
                _logInfo($"Saved: {targetPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logError($"SaveAsRvt error: {ex.Message}\n{targetPath}");
                return false;
            }
        }

        // 
        // Экспорт
        // 

        /// <summary>
        /// Экспортирует модель в NWC или IFC.
        /// Сначала экспортирует во времменный файл, затем превращает в целевой.
        /// </summary>
        public bool ExportModel(
            Document doc,
            string targetPath,
            string archiveFolder = null,
            string viewName = "navisworks",
            bool nwcAllProprties = true,
            bool nwcRoom = false,
            bool nwcDivideIntoLevels = true,
            Dictionary<string, object> ifcSettings = null)
        {
            throw new NotImplementedException();
        }

        // 
        // Purge
        // 

        /// <summary>Выполняет purge модели через PerformanceAdvisir</summary>
        public int PurgeDocument(Document doc)
        {
            throw new NotImplementedException();
        }

        // 
        // Transmit
        // 

        public bool TransmitModel(string path,
                            bool transmit = true,
                            bool relativeLinks = false)
        {
            throw new NotImplementedException();
        }

        //
        // Виды
        //

        public View GetViewByName(Document doc, string name)
        {
            throw new NotImplementedException();
        }

        public View3D Create3DView(Document doc, string name = null)
        {
            throw new NotImplementedException();
        }

        private void CheckAndFixView(View view)
        {
            throw new NotImplementedException();
        }

        // 
        // Опции экспорта
        // 

        private NavisworksExportOptions BuildNwcOptions(
            View view, bool allProps, bool room, bool divideIntoLevels)
        {
            throw new NotImplementedException();
        }

        private IFCExportOptions BuildIfcOptions(
            View view, Dictionary<string, object> settings)
        {
            throw new NotImplementedException();
        }

        // 
        // Рабочие наборы
        // 

        private void ApplyWorksetConfiguration(
            OpenOptions options,
            ModelPath modelPath,
            string pathString,
            IList<string> closeWorksetMark)
        {
            throw new NotImplementedException();
        }
    }
}
