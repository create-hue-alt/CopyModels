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
            var ext = Path.GetExtension(targetPath).ToUpper();
            var tmpDir = Path.GetTempPath();
            var tmpName = Path.GetFileNameWithoutExtension(targetPath)
                        + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                        + Path.GetExtension(targetPath);
            var tmpPath = Path.Combine(tmpDir, tmpName);

            _logInfo($"Exporting to: {targetPath}");

            // Найти вид
            var view = GetViewByName(doc, viewName);
            if (view == null && viewName.Equals("navisworks", StringComparison.OrdinalIgnoreCase))
            {
                _logWarning($"No NavisWorks view found in {doc.Title}, creating new.");
                view = Create3DView(doc, "NavisWorks");
            }
            if (view == null)
            {
                _logError($"View '{viewName}' not found in {doc.Title}. Export aborted.");
                return false;
            }

            CheckAndFixView(view);

            // Архивировать старый файл перед жкспортом
            try
            {
                if (File.Exists(tmpPath))
                {
                    _logInfo($"Archiving existing file: {targetPath}");
                    if (archiveFolder != null)
                    {
                        // ArchiveModel перемещает файл в архив папку
                        _fileService.ArchiveModel(targetPath, archiveFolder);
                    }
                    else
                    {
                        // Если архива нет - просто удаляем
                        _fileService.MarkReadWrite(targetPath);
                        File.Delete(targetPath);
                        _logInfo($"Deleted old file: {targetPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logError($"Archive error: {ex.Message}");
                return false;
            }

            // Подготовить опции экспорта
            bool exported = false;
            Transaction t = null;

            try
            {
                switch (ext)
                {
                    case ".NWC":
                        var nwcOpts = BuildNwcOptions(view,
                                                    nwcAllProprties,
                                                    nwcRoom,
                                                    nwcDivideIntoLevels);
                        if (nwcOpts == null) return false;

                        _fileService.EnsureDirectory(tmpPath);
                        doc.Export(Path.GetDirectoryName(tmpPath),
                                   Path.GetFileName(tmpPath),
                                   nwcOpts);
                        break;

                    case ".IFC":
                        var ifcopts = BuildIfcOptions(view,
                                                    ifcSettings
                                                    ?? new Dictionary<string, object>());
                        if (ifcopts == null) return false;

                        // IFC требует транзакции
                        t = new Transaction(doc, "Export IFC");
                        t.Start();

                        _fileService.EnsureDirectory(tmpPath);
                        exported = doc.Export(Path.GetDirectoryName(tmpPath),
                                                    Path.GetFileName(tmpPath),
                                                    ifcopts);

                        t.Commit();
                        break;

                    default:
                        _logError($"Export to {ext} not implemented");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logError($"Export error: {ex.Message}");
                t?.RollBack();
                return false;
            }

            // Проверяем создался ли файл
            if (!File.Exists(tmpPath))
            {
                _logError($"Export failed: file not crreated at {tmpPath}");
                return false;
            }

            // Переместить в цель.
            try
            {
                _fileService.EnsureDirectory(targetPath);
                File.Copy(tmpPath, tmpPath, overwrite: true);
                File.Delete(tmpPath);
                _logInfo($"Export saved: {targetPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logError($"Move export error: {ex.Message}\nTemp: {tmpPath}");
                return false;
            }
        }

        // 
        // Purge
        // 

        /// <summary>Выполняет purge модели через PerformanceAdvisir</summary>
        public int PurgeDocument(Document doc)
        {
            var purgeGuid = new Guid("e8c63650-70b7-435a-9010-ec97660c1bda");
            PerformanceAdviserRuleId ruleId = null;

            var adviser = PerformanceAdviser.GetPerformanceAdviser();
            foreach (var rule in adviser.GetAllRuleIds())
                if (rule.Guid.Equals(purgeGuid)) { ruleId = rule; break; }

            if (ruleId == null) return 0;

            var messages = adviser.ExecuteRules(doc,
                                    new List<PerformanceAdviserRuleId>() { ruleId });
            if (messages.Count == 0) return 0;

            using (var t = new Transaction(doc, "Purge unused"))
            {
                try
                {
                    t.Start();
                    var ids = messages[0].GetFailingElements();
                    var deleted = doc.Delete(ids).Count;
                    doc.Regenerate();
                    t.Commit();
                    _logInfo($"Purge {deleted} elemrnts.");
                    return deleted;
                }
                catch (Exception ex)
                {
                    _logError($"Purge error: {ex.Message}");
                    t.RollBack();
                    return 0;
                }
            }
        }

        // 
        // Transmit
        // 

        public bool TransmitModel(string path,
                            bool transmit = true,
                            bool relativeLinks = false)
        {
            if (FileService.IsRevitServer(path))
            {
                _logInfo("Transmit not required for Revit Server");
                return true;
            }

            try
            {
                var modelPath = new FilePath(path);
                var tmd = TransmissionData.ReadTransmissionData(modelPath);
                tmd.IsTransmitted = transmit;

                foreach (var linkId in tmd.GetAllExternalFileReferenceIds())
                {
                    var refData = tmd.GetLastSavedReferenceData(linkId);

                    if (relativeLinks && 
                        refData.ExternalFileReferenceType == ExternalFileReferenceType.RevitLink)
                    {
                        var linkPath = refData.GetAbsolutePath();
                        var userPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(linkPath);
                        var modelName = Path.GetFileName(userPath);
                        tmd.SetDesiredReferenceData(linkId,
                                                    new FilePath(modelName),
                                                    PathType.Relative,
                                                    true);
                    }
                    else
                    {
                        tmd.SetDesiredReferenceData(linkId,
                                                    refData.GetPath(),
                                                    refData.PathType,
                                                    true);
                    }
                }
                TransmissionData.WriteTransmissionData(modelPath, tmd);
                return true;
            }
            catch (Exception ex)
            {
                _logError($"TransmitModel error: {ex.Message}");
                return false;
            }
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
