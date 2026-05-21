using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
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
            throw new NotImplementedException();
        }

        /// <summary>Открывает IFC-файлы</summary>
        public Document OpneIfc(string path)
        {
            throw new NotImplementedException();
        }

        // 
        // Закрытие / сброс прав
        // 

        public void RelinquishAndClose(Document doc)
        {
            throw new NotImplementedException();
        }

        private void RelinquishOwnership(Document doc)
        {
            throw new NotImplementedException();
        }

        // 
        // Сохранение моделей
        // 

        /// <summary>Сохраняет модель в указанный путь как Central.</summary>
        public bool SaveAsRvt (Document doc, string targetPath, string archiveFolder = null)
        {
            throw new NotImplementedException();
        }

        // 
        // Экспорт
        // 

        /// <summary>
        /// Экспортирует модель в NWC или IFC.
        /// Сначала экспортирует во времменный файл, затем превращает в целевой.
        /// </summary>
        public bool ExportModel (
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
