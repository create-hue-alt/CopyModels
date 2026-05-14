using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Net;

namespace CopyModels.Plugin.Services
{
    /// <summary>
    /// Работа с Revit Server через REST API.
    /// Аналог функций read_revit_server_models / revit_server_content / revit_server_date из serverTools.py.
    ///
    /// Путь RSN имеет вид RSN://servername/folder/subfolder
    /// </summary>
    internal class RevitServerService
    {
        private readonly string _revitVersion;
        private readonly Action<string> _logInfo;
        private readonly Action<string> _logWarning;
        private readonly Action<string> _logError;

        public RevitServerService(
            string revitVersion,
            Action<string> logInfo = null, 
            Action<string> logWarning = null, 
            Action<string> logError = null)
        {
            _revitVersion = revitVersion;
            _logInfo = logInfo          ?? (_ => { });
            _logWarning = logWarning    ?? (_ => { });
            _logError= logError         ?? (_ => { });
        }

        //
        // Чтение содержимого
        //

        /// <summary>
        /// Рекурсивно возвращает все RSN-пути моделей в указанной папке RSN.
        /// <paramref name="rsnFolderPath"/> - например RSN://server/ProjectFolder
        /// </summary>
        
        public List<string> ReadrRevitServerModels(string rsnFolderPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>Возвращает дату последнего изменения модели а Revit Server (Unix-секунды)</summary>
        public double? GetModelDate(string rsnPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Копирует модель внутри одного Revit Server через REST API.
        /// Возвращает false если серверы разные.
        /// </summary>
        public bool CopyOnRevitServer(string sourcePath, string targetPath, bool overwrite = true)
        {
            throw new NotImplementedException();
        }

        // 
        // Внутренние методы
        // 

        private List<string> RevitServerContent(string baseUrl, string rsn, string folder)
        {
            throw new NotImplementedException();
        }

        private double? RevitServerData(string baseUrl, string rsn, string model)
        {
            throw new NotImplementedException();
        }

        private string BuildBaseUrl(string server)
        {
            throw new NotImplementedException();
        }

        private static string ExtractServer(string rsnPath)
        {
            throw new NotImplementedException();
        }

        private static void AddRevitSeverHeaders(HttpWebRequest request)
        {
            throw new NotImplementedException;
        }
    }
}
