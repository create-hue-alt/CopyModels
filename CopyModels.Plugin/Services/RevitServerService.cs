using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;

namespace CopyModels.Plugin.Services
{
    /// <summary>
    /// Работа с Revit Server через REST API.
    /// Аналог функций read_revit_server_models / revit_server_content / revit_server_date из serverTools.py.
    ///
    /// Путь RSN имеет вид RSN://servername/folder/subfolder
    /// </summary>
    public class RevitServerService
    {
        private readonly string _revitVersion;
        private readonly HttpClient _httpClient;
        private readonly Action<string> _logInfo;
        private readonly Action<string> _logWarning;
        private readonly Action<string> _logError;
        private readonly Action<string> _logDebug;

        public RevitServerService(
            string revitVersion,
            Action<string> logInfo = null,
            Action<string> logWarning = null,
            Action<string> logError = null,
            Action<string> logDebug = null)
        {
            _revitVersion = revitVersion;
            _logInfo = logInfo ?? (_ => { });
            _logWarning = logWarning ?? (_ => { });
            _logError = logError ?? (_ => { });
            _logDebug = logDebug ?? (_ => { });

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        //
        // Чтение содержимого
        //

        /// <summary>
        /// Рекурсивно возвращает все RSN-пути моделей в указанной папке RSN.
        /// <paramref name="rsnFolderPath"/> - например RSN://server/ProjectFolder
        /// </summary>

        public List<string> ReadRevitServerModels(string rsnFolderPath)
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

            return RevitServerData(baseUrl, modelForRequest);
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

                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    AddRevitSeverHeaders(request);
                    var response = _httpClient.SendAsync(request).Result;

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"HTTP {response.StatusCode}");
                }

                var srcDate = GetModelDate(sourcePath);
                var tgtDate = GetModelDate(targetPath);

                if (srcDate != null && tgtDate != null && tgtDate >= srcDate)
                    return true;

                _logError($"RSN copy date mismatch: {sourcePath} -> {targetPath}");
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
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    AddRevitSeverHeaders(request);

                    var response = _httpClient.SendAsync(request).Result;

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"HTTP {response.StatusCode}");

                    var json = JObject.Parse(response.Content.ReadAsStringAsync().Result);

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

        private double? RevitServerData(string baseUrl, string model)
        {
            try
            {
                var url = $"{baseUrl}{model}/modelInfo";

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    AddRevitSeverHeaders(request);

                    var response = _httpClient.SendAsync(request).Result;

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"HTTP {response.StatusCode}");

                    var json = JObject.Parse(response.Content.ReadAsStringAsync().Result);

                    var raw = json["DateModified"].Value<string>();
                    var dt = DateTime.ParseExact(
                        raw,
                        "MM/dd/yyyy HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

                    return new DateTimeOffset(dt).ToUnixTimeSeconds();
                }
            }
            catch (Exception ex)
            {
                _logWarning($"RevitServer date error: {ex.Message}");
                return null;
            }
        }

        private string BuildBaseUrl(string server) =>
            $"http://{server}/RevitServerAdminRESTService{_revitVersion}/AdminRESTService.svc/";

        private static string ExtractServer(string rsnPath)
        {
            // RSN://servername/... → servername
            var parts = rsnPath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? parts[1] : throw new ArgumentException($"Invalid RSN path: {rsnPath}");
        }

        private static void AddRevitSeverHeaders(HttpRequestMessage request)
        {
            request.Headers.Add("User-Name", Environment.UserName);
            request.Headers.Add("User-Machine-Name", Environment.MachineName);
            request.Headers.Add("Operation-GUID", Guid.NewGuid().ToString());
        }
    }
}
