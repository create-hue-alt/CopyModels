using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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
            _logInfo = logInfo ?? (_ => { });
            _logWarning = logWarning ?? (_ => { });
            _logError = logError ?? (_ => { });
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
            rsnFolderPath = rsnFolderPath.Replace("\\", "/");
            var server = ExtractServer(rsnFolderPath);
            var rsn = $"RSN://{server}/";
            var baseUrl = BuildBaseUrl(server);

            var folder = rsnFolderPath.Substring(rsn.Length);
            var folderForRequest = folder.Replace("/", "|");
            if (string.IsNullOrEmpty(folderForRequest)) folderForRequest = "|";

            var models = RevitServerContent(baseUrl, rsn, folderForRequest);
            _logInfo($"Found {models.Count} models on RSN {rsnFolderPath}");
            return models;
        }

        /// <summary>Возвращает дату последнего изменения модели а Revit Server (Unix-секунды)</summary>
        public double? GetModelDate(string rsnPath)
        {
            rsnPath = rsnPath.Replace("\\", "/");
            var server = ExtractServer(rsnPath);
            var rsn = $"RSN://{server}/";
            var baseUrl = BuildBaseUrl(server);
            var modelForRequest = rsnPath.Substring(rsn.Length).Replace("/", "|");

            return RevitServerData(baseUrl, rsn, modelForRequest);
        }

        /// <summary>
        /// Копирует модель внутри одного Revit Server через REST API.
        /// Возвращает false если серверы разные.
        /// </summary>
        public bool CopyOnRevitServer(string sourcePath, string targetPath, bool overwrite = true)
        {
            sourcePath = sourcePath.Replace("\\", "/");
            targetPath = targetPath.Replace("\\", "/");

            var srcServer = ExtractServer(sourcePath);
            var tgtServer = ExtractServer(targetPath);

            if (!srcServer.Equals(tgtServer, StringComparison.OrdinalIgnoreCase))
            {
                _logError($"Cross-server copy is not supported: {sourcePath} -> {targetPath}");
                return false;
            }

            try
            {
                var baseUrl = BuildBaseUrl(srcServer);
                var rsn = $"RSN://{srcServer}/";

                var srcModel = sourcePath.Substring(rsn.Length).Replace("/", "|");
                var tgtModel = targetPath.Substring(rsn.Length).Replace("/", "|");
                var replace = overwrite ? "true" : "false";

                var url = $"{baseUrl}{srcModel}?destinationObjectPath={tgtModel}&pasteAction=Copy&replaceExisting={replace}";

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentLength = 0;
                AddRevitSeverHeaders(request);

                using (var response = request.GetResponse())
                using (new StreamReader(response.GetResponseStream())) { }

                var srcDate = GetModelDate(sourcePath);
                var tgtDate = GetModelDate(targetPath);

                if (srcDate != null && tgtDate != null && tgtDate >= srcDate)
                    return true;

                _logError($"$RSN copy date mismatch: {sourcePath} -> {targetPath}");
                return false;
            }
            catch (Exception ex)
            {
                _logError($"RevitServer CopyModel error: {ex.Message}");
                return false;
            }
        }

        // 
        // Внутренние методы
        // 

        private List<string> RevitServerContent(string baseUrl, string rsn, string folder)
        {
            var fileList = new List<string>();
            var url = $"{baseUrl}{folder}/contents";

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                AddRevitSeverHeaders(request);

                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var json = JObject.Parse(reader.ReadToEnd());

                    foreach (var file in json["Models"])
                    {
                        var link = folder == "|"
                            ? "/" + file["Name"].Value<string>()
                            : folder.Replace("|", "/") + "/" + file["Name"].Value<string>();
                        fileList.Add(rsn + link);
                    }

                    foreach (var sub in json["Folders"])
                    {
                        var subFolder = folder == "|"
                            ? sub["Name"].Value<string>()
                            : folder + "|" + sub["Name"].Value<string>();

                        fileList.AddRange(RevitServerContent(baseUrl, rsn, subFolder));
                    }
                }
            }
            catch (Exception ex)
            {
                _logError($"RevitServer Content error for {url}: {ex.Message}");
            }
            return fileList;
        }

        private double? RevitServerData(string baseUrl, string rsn, string model)
        {
            try
            {
                var url = $"{baseUrl}{model.Replace(rsn,"").Replace("/", "|")}/modelInfo";
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                AddRevitSeverHeaders(request);

                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var json = JObject.Parse(reader.ReadToEnd());

                    var raw = json["DateModifiled"].Value<string>()
                        .Replace("/Date(", "").Replace(")/", "");
                    return double.Parse(raw) / 1000.0;
                }
            }
            catch (Exception ex)
            {
                _logWarning($"RevitServer date error: {ex.Message}");
                return null;
            }
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
            throw new NotImplementedException();
        }
    }
}
