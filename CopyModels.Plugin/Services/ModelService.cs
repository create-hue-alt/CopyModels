using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using CopyModels.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CopyModels.Plugin.Services
{
    /// <summary>
    /// Открытие, экспорт и сохранение моделей через RevitAPI
    /// (RVT/NWC/IFC, purge, worksets, transmit)
    /// </summary>
    internal class ModelService
    {
        private readonly Application _app;
        private readonly FileService _fileService;
        private readonly Action<string> _logInfo;
        private readonly Action<string> _logWarning;
        private readonly Action<string> _logError;
        private readonly Action<string> _logDebug;

        public ModelService(
            Application app,
            FileService fileService,
            Action<string> logInfo = null,
            Action<string> logWarning = null,
            Action<string> logError = null,
            Action<string> logDebug = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logInfo = logInfo ?? (_ => { });
            _logWarning = logWarning ?? (_ => { });
            _logError = logError ?? (_ => { });
            _logDebug = logDebug ?? (_ => { });
        }

        // 
        // Открытие моделей
        // 

        /// <summary>Открывает модель с DetachAndPreserveWorksets</summary>
        public Document OpenWithDetach(string path, IList<string> closeWorksetMask = null)
        {
            _logInfo($"Opening (detach): {path}");
            try
            {
                var modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(path);
                var options = new OpenOptions
                {
                    DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets,
                    Audit = true
                };

                ApplyWorksetConfiguration(options, modelPath, path, closeWorksetMask);

                var doc = _app.OpenDocumentFile(modelPath, options);
                _logInfo($"Opened: {path}");
                return doc;
            }
            catch (Exception ex)
            {
                _logError($"OpenWithDetach failed: {ex.Message}\n{path}");
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

        public void RelinquishAndClose(Document doc, bool skipRelinquish =false)
        {
            if (doc == null) return;
            try
            {
                if (!skipRelinquish)
                    RelinquishOwnership(doc);
                doc.Close(false);
                _logInfo("Document closed");

            }
            catch (Exception ex)
            {
                _logError($"Close error: {ex.Message}");
            }
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
                _fileService.EnsureDirectory(targetPath);

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
            string viewName = AppDefaults.NavisworksViewName,
            bool nwcAllProperties = true,
            bool nwcRoom = false,
            bool nwcDivideIntoLevels = true,
            bool nwcLinks = false,
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
            if (view == null && viewName.Equals(AppDefaults.NavisworksViewName, StringComparison.OrdinalIgnoreCase))
            {
                _logWarning($"No Navisworks view found in {doc.Title}, creating new.");
                view = Create3DView(doc, AppDefaults.NavisworksViewName);
            }
            if (view == null)
            {
                _logError($"View '{viewName}' not found in {doc.Title}. Export aborted.");
                return false;
            }

            CheckAndFixView(view);

            // Архивировать старый файл перед экспортом
            try
            {
                if (File.Exists(targetPath))
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
                                                    nwcAllProperties,
                                                    nwcRoom,
                                                    nwcDivideIntoLevels,
                                                    nwcLinks);
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

                        if (!exported)
                        {
                            _logError($"IFC export failed!");
                            return false;
                        }
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
                _logError($"Export failed: file not created at {tmpPath}");
                return false;
            }

            // Переместить в цель.
            try
            {
                _fileService.EnsureDirectory(targetPath);
                File.Copy(tmpPath, targetPath, overwrite: true);
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

        /// <summary>Выполняет purge модели через PerformanceAdviser</summary>
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
                    _logInfo($"Purge {deleted} elements.");
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
            if (AppDefaults.IsRevitServer(path))
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
            var views = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .WhereElementIsNotElementType()
                .Cast<View>();

            return views.FirstOrDefault(v =>
                !v.IsTemplate &&
                v.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public View3D Create3DView(Document doc, string name = null)
        {
            var viewType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vt => vt.ViewFamily == ViewFamily.ThreeDimensional);

            if (viewType == null) return null;

            using (var t = new Transaction(doc, "Create 3D View"))
            {
                t.Start();

                var view = View3D.CreateIsometric(doc, viewType.Id);
                if (name != null) view.Name = name;

                t.Commit();

                return view;
            }
        }

        private void CheckAndFixView(View view)
        {
            var doc = view.Document;
            using (var t = new Transaction(doc, "Fix view for export"))
            {
                t.Start();
                try
                {
                    if (view.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL)?.AsInteger() < 3)
                        view.DetailLevel = ViewDetailLevel.Fine;
                    if (view.CropBoxActive)
                        view.CropBoxActive = false;
                    if (view is View3D v3d && v3d.IsSectionBoxActive)
                        v3d.IsSectionBoxActive = false;
                }
                finally { doc.Regenerate(); t.Commit(); }
            }
        }

        // 
        // Опции экспорта
        // 

        private NavisworksExportOptions BuildNwcOptions(
                                        View view,
                                        bool allProps,
                                        bool room,
                                        bool divideIntoLevels,
                                        bool links = false)
        {
            if (!OptionalFunctionalityUtils.IsNavisworksExporterAvailable())
            {
                _logError("Navisworks exporter not available.");
                return null;
            }

            var opts = new NavisworksExportOptions
            {
                ConvertElementProperties = allProps,
                Coordinates = NavisworksCoordinates.Shared,
                DivideFileIntoLevels = divideIntoLevels,
                ExportElementIds = true,
                ExportLinks = links,
                ExportParts = true,
                ExportRoomAsAttribute = false,
                ExportRoomGeometry = room,
                ExportScope = NavisworksExportScope.View,
                ExportUrls = true,
                FindMissingMaterials = false,
                Parameters = NavisworksParameters.All,
                ViewId = view.Id
            };

            return opts;
        }

        private IFCExportOptions BuildIfcOptions(
                                        View view,
                                        Dictionary<string, object> settings)
        {
            if (!OptionalFunctionalityUtils.IsIFCAvailable())
            {
                _logError("IFC exporter not available");
                return null;
            }

            string Get(string key, string def) =>
                settings.TryGetValue(key, out var v) ? v?.ToString() ?? def : def;
            bool GetBool(string key, bool def) =>
                settings.TryGetValue(key, out var v) &&
                bool.TryParse(v?.ToString(), out var b) ? b : def;

            var opts = new IFCExportOptions
            {
                WallAndColumnSplitting = GetBool("WallAndColumnSplitting", true),
                ExportBaseQuantities = GetBool("ExportBaseQuantities", true),
                FilterViewId = view.Id,
                SpaceBoundaryLevel = settings.
                        TryGetValue("SpaceBoundaryLevel", out var sbl)
                        ? Convert.ToInt32(sbl) : 0
            };

            // FileVersion
            if (settings.TryGetValue("FileVersion", out var fv))
            {
                if (Enum.TryParse<IFCVersion>(fv?.ToString(), false, out var parsed))
                    opts.FileVersion = parsed;
            }
            else
            {
                opts.FileVersion = IFCVersion.IFC2x3CV2;
            }

            opts.AddOption("ActiveViewId", view.Id.IntegerValue.ToString());
            opts.AddOption("ExportInternalRevitPropertySets", Get("ExportInternalRevitPropertySets", "true"));
            opts.AddOption("ExportIfcCommonPropertySets", Get("ExportIfcCommonPropertySets", "true"));
            opts.AddOption("ExportAnnotations", Get("ExportAnnotations", "false"));
            opts.AddOption("ExportLinkedFiles", Get("ExportLinkedFiles", "true"));
            opts.AddOption("ExportVisibleElementsInView", Get("ExportVisibleElementsInView", "true"));
            opts.AddOption("ExportPartsAsBuildingElements", Get("ExportPartsAsBuildingElements", "true"));
            opts.AddOption("UseActiveViewGeometry", Get("UseActiveViewGeometry", "true"));
            opts.AddOption("ExportSolidModelRep", Get("ExportSolidModelRep", "true"));
            opts.AddOption("Use2DRoomBoundaryForVolume", Get("Use2DRoomBoundaryForVolume", "false"));
            opts.AddOption("UseFamilyAndTypeNameForReference", Get("UseFamilyAndTypeNameForReference", "false"));
            opts.AddOption("ExportSpecificSchedules", Get("ExportSpecificSchedules", "false"));
            opts.AddOption("ExportBoundingBox", Get("ExportBoundingBox", "false"));
            opts.AddOption("ExportSchedulesAsPsets", Get("ExportSchedulesAsPsets", "false"));
            opts.AddOption("ExportUserDefinedPsets", Get("ExportUserDefinedPsets", "false"));
            opts.AddOption("ExportUserDefinedPsetsFileName", Get("ExportUserDefinedPsetsFileName", ""));
            opts.AddOption("ExportUserDefinedParameterMapping", Get("ExportUserDefinedParameterMapping", "false"));
            opts.AddOption("ExportUserDefinedParameterMappingFileName", Get("ExportUserDefinedParameterMappingFileName", ""));
            opts.AddOption("SitePlacement", Get("SitePlacement", "0"));
            opts.AddOption("TessellationLevelOfDetail", Get("TessellationLevelOfDetail", "0"));
            opts.AddOption("UseOnlyTriangulation", Get("UseOnlyTriangulation", "false"));
            opts.AddOption("StoreIFCGUID", Get("StoreIFCGUID", "false"));
            opts.AddOption("FileType", Get("FileType", "Ifc"));
            opts.AddOption("ExportRoomsInView", Get("ExportRoomsInView", "false"));
            opts.AddOption("ExcludeFilter", Get("ExcludeFilter", ""));
            opts.AddOption("IncludeSteelElements", Get("IncludeSteelElements", "true"));
            opts.AddOption("COBieCompanyInfo", Get("COBieCompanyInfo", ""));
            opts.AddOption("COBieProjectInfo", Get("COBieProjectInfo", ""));
            opts.AddOption("UseTypeNameOnlyForIfcType", Get("UseTypeNameOnlyForIfcType", "false"));
            opts.AddOption("UseVisibleRevitNameAsEntityName", Get("UseVisibleRevitNameAsEntityName", "false"));

            return opts;
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
            if (closeWorksetMark == null || closeWorksetMark.Count == 0) return;

            try
            {
                if (!AppDefaults.IsRevitServer(pathString))
                {
                    var info = BasicFileInfo.Extract(pathString);
                    if (!info.IsWorkshared || !info.IsCentral) return;
                }

                var worksets = WorksharingUtils.GetUserWorksetInfo(modelPath);
                var config = new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets);
                var toOpen = new List<WorksetId>();

                foreach (var ws in worksets)
                {
                    var shouldClose = closeWorksetMark
                        .Any(m => ws.Name.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!shouldClose)
                        toOpen.Add(ws.Id);

                    _logInfo($"Workset '{ws.Name}' : {(shouldClose ? "Close" : "Open")}");
                }

                if (toOpen.Count > 0)
                    config.Open(toOpen);

                options.SetOpenWorksetsConfiguration(config);
            }
            catch (Exception ex)
            {
                _logWarning($"Workset config error, opening all: {ex.Message}");
                options.SetOpenWorksetsConfiguration(
                    new WorksetConfiguration(WorksetConfigurationOption.OpenAllWorksets));
            }
        }
    }
}
