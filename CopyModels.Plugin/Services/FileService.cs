using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CopyModels.Plugin.Services
{
    /// <summary>
    /// Работа с файловой системой и Revit Server.
    /// Копирование файлов, архивирование, маппинг сетевых дисков.
    /// </summary>
    internal class FileService
    {
        private readonly Action<string> _logInfo;
        private readonly Action<string> _logWarning;
        private readonly Action<string> _logError;
        private readonly Action<string> _logDebug;

        public FileService(
            Action<string> logInfo = null,
            Action<string> logWarning = null,
            Action<string> logError = null,
            Action<string> logDebug = null)
        {
            _logInfo = logInfo ?? (_ => { });
            _logWarning = logWarning ?? (_ => { });
            _logError = logError ?? (_ => { });
            _logDebug = logDebug ?? (_ => { });
        }

        //
        // Получение даты из модели
        //

        /// <summary>
        /// Возвращает дату последнего изменения файла в Unix-секуднах.
        /// Для RSN-путей возвращает null (дата берется через Revit Server)
        /// </summary>
        public double? GetModelDate(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (IsRevitServer(path)) return null;
            if (!File.Exists(path)) return null;

            var fileInfo = new FileInfo(path);
            return (double)((DateTimeOffset)fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds();
        }

        //
        // Копирование файлов
        //

        /// <summary>
        /// Копирует файл с проверкой дат.
        /// Если целевой файл существует и указан archiveTemplate, архивирует его перед копированием.
        /// </summary>
        public bool CopyFile(string sourcePath, string targetPath, string archiveTemplate = null)
        {
            string archived = null;
            try
            {
                // Архивируем существующий целевой файл если нужно
                if (archiveTemplate != null)
                    archived = ArchiveModel(targetPath, archiveTemplate);

                // Копируем файл
                EnsureDirectory(targetPath);
                File.Copy(sourcePath, targetPath, overwrite: true);

                // Проверяем даты
                var srcDate = GetModelDate(sourcePath);
                var tgtDate = GetModelDate(targetPath);

                // Если дата цели >= даты источника, то копироание прошло успешно
                if (tgtDate != null && srcDate != null && tgtDate >= srcDate)
                    return true;

                // Дата не совпала - откатываем архив назад
                if (archived != null) File.Move(archived, targetPath);
                _logError($"Copy failed: target date mismatch\nSource: {sourcePath} (date: {srcDate})\nTarget: {targetPath} (date: {tgtDate})");
                return false;

            }
            catch (Exception ex)
            {
                // В случае ошибки откатываем архив
                if (archived != null && File.Exists(archived))
                    File.Move(archived, targetPath);
                _logError($"Copy error: {ex.Message}\nSource: {sourcePath}\nTarget: {targetPath}");
                return false;
            }
        }

        //
        // Архивирование
        //

        /// <summary>
        /// Перемещает файл в папку ахрфива с добавлением метки дат.
        /// Поддерживает плейсхолдер {MODEL_NAME} и {MODEL_DATE} в пути архива.
        /// Возвращает путь к архивному файлу или null.
        /// </summary>
        public string ArchiveModel(string modelPath, string archiveTemplate)
        {
            if (IsRevitServer(modelPath))
            {
                _logInfo($"Archiving not supported for Revit Server: {modelPath}");
                return null;
            }
            if (!File.Exists(modelPath))
            {
                _logInfo($"Nothing to archive: {modelPath}");
                return null;
            }

            var modeDate = File.GetLastWriteTime(modelPath);
            var dateStr = modeDate.ToString("yyyyMMdd_HHmmss");
            var dayStr = modeDate.ToString("yyyyMMdd");

            var fileNameNoExt = Path.GetFileNameWithoutExtension(modelPath);
            var ext = Path.GetExtension(modelPath);

            // Размещаем абсолютный/относительный путь архива
            var folder = Path.IsPathRooted(archiveTemplate)
                ? archiveTemplate
                : Path.Combine(Path.GetDirectoryName(modelPath), archiveTemplate);

            folder = folder
                .Replace("{MODEL_NAME}", fileNameNoExt)
                .Replace("{MODEL_DATE}", dayStr);

            var archiveName = $"{fileNameNoExt}_{dateStr}{ext}";
            var archivePath = EnsureUniquePath(Path.Combine(folder, archiveName));

            Directory.CreateDirectory(Path.GetDirectoryName(archivePath));
            File.Move(modelPath, archivePath);
            _logInfo($"Archived: {modelPath} -> {archivePath}");
            return archivePath;
        }

        //
        // Права доступа
        //

        /// <summary>Устанавливает файл в режим только чтение.</summary>
        public bool MarkReadOnly(string path)
        {
            try
            {
                File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
                return true;
            }
            catch (Exception ex)
            {
                _logWarning($"MarkReadOnly error: {ex.Message}\n{path}");
                return false;
            }
        }

        /// <summary>Устанавливает файл в режим чтение-запись.</summary>
        public bool MarkReadWrite(string path)
        {
            try
            {
                File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
                return true;
            }
            catch (Exception ex)
            {
                _logWarning($"MarkReadWrite error: {ex.Message}\n{path}");
                return false;
            }
        }

        //
        // Чтение списка файлов
        //

        /// <summary>
        /// Рекурсивно ищет все файлы с нужным расширением в папке.
        /// Исключает файлы/папки, содержашие строки из exceptions.
        /// </summary>
        public List<string> ReadFileServerModels(string folder, string extension, IEnumerable<string> exceptions = null)
        {
            var exc = (exceptions ?? Enumerable.Empty<string>())
                .Select(e => e.ToLower())
                .ToList();

            if (!Directory.Exists(folder))
            {
                _logWarning($"Path not found: {folder}");
                return new List<string>();
            }

            if (string.IsNullOrEmpty(extension))
            {
                extension = "*";
            }
            
            var files = Directory.EnumerateFiles(folder, "*" + extension, SearchOption.AllDirectories)
                .Where(f => !exc.Any(e => f.ToLower().Contains(e)))
                .ToList();

            _logInfo($"Found {files.Count()} models in {folder}");
            return files;

        }

        /// <summary>
        /// Точка входа: выбирает нужный метод по типу пути (RSN или файловый сервер).
        /// RSN-пути обрабатываются через RevitServerService.
        /// </summary>
        public List<string> ReadModels(string pathPattern, IEnumerable<string> exceptions = null)
        {
            if (IsRevitServer(pathPattern))
                return new List<string>();  // см. RevitServerService.ReadRevitServerModels

            var folder = Path.GetDirectoryName(pathPattern) ?? pathPattern;
            var extension = Path.GetExtension(pathPattern);
            return ReadFileServerModels(folder, extension, exceptions);
        }

        //
        // Маппинг сетевого диска (Windows API)
        //

        public bool MapDrive(string driverLetter, string networkPath)
        {
            _logInfo($"Map drive {driverLetter} -> {networkPath}");
            
            if (!Directory.Exists(networkPath))
            {
                _logError($"Network path not available: {networkPath}");
                return false;
            }

            // Нормализуем букву диска
            var driverLetterNorm = driverLetter.TrimEnd('\\', ':') + ":";
            
            // Проверяем текущие подключение
            var currentPath = GetConnectionPath(driverLetterNorm);
            if (currentPath == networkPath)
            {
                _logInfo($"Drive {driverLetterNorm} already mapped correctly.");
                return true;
            }
                        
            if (Directory.Exists(driverLetterNorm + "\\"))
            {
                var disconnectResult = WNetCancelConnection2(driverLetter, 1, true);
                if (disconnectResult != 0)
                {
                    _logError($"Disconnect drive error: {disconnectResult}");
                    return false;
                }
            }

            // Подключаем диск к новому пути
            var nr = new NETRESOURCE
            {
                dwScope = 0,        // RESOURCE_GLOBALNET
                dwType = 1,         // RESOURCETYPE_DISK
                dwDisplayType = 3,  // RESOURCEDISPLAYTYPE_SHARE
                dwUsage = 1,        // RESOURCEUSAGE_CONNECTABLE
                lpLocalName = driverLetterNorm,
                lpRemoteName = networkPath,
                lpProvider = null
            };

            var result = WNetAddConnection2(ref nr, null, null, 1);
            if (result == 0)
            {
                _logInfo($"Drive {driverLetter} mapped mapped {networkPath}.");
                return true;
            }

            _logError($"WNetAddConnection2 error: {result}");
            return false;
            
        }

        //
        // Утилиты
        //

        /// <summary>Проверяет, является ли путь адресом Revit Server (начинается с RSN).</summary>
        public static bool IsRevitServer(string path) =>
            path != null && path.StartsWith("RSN", StringComparison.OrdinalIgnoreCase);
        
        /// <summary>Создаёт директорию для файла, если она не существует.</summary>
        public void EnsureDirectory(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if ( !string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                _logInfo($"Directory created: {dir}");
            }
        }
        private static string EnsureUniquePath(string path)
        {
            if (!File.Exists(path)) return path;
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            var i = 1;
            string candidate;
            do { candidate = Path.Combine(dir, $"{name}_{i++}{ext}"); }
            while (File.Exists(candidate));
            return candidate;
        }
        private string GetConnectionPath(string driverLetter)
        {
            try
            {
                var sb = new StringBuilder(260);
                var length = 260;
                WNetGetConnection(driverLetter.TrimEnd('\\'), sb, ref length);
                return sb.ToString();
            }
            catch { return null; }
        }

        //
        // P/Invoke 
        //

        /// <summary>Подключает сетевой ресурс к локальному имени (букве диска).</summary>
        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NETRESOURCE lpNetResource, string lpPassword, string lpUsername, int dwFlags);

        /// <summary>Отключает подключение к сетевому ресурсу.</summary>
        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string lpName, int dwFlags, bool fForce);

        /// <summary>Получает сетевой путь для подключённого локального имени (буквы диска).</summary>
        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetGetConnection(string lpLocalName, StringBuilder lpRemoteName, ref int lpnLength);

        /// <summary>Структура для описания сетевого ресурса (используется в WNetAddConnection2).</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NETRESOURCE
        {
            public int dwScope;             // RESOURCE_GLOBALNET
            public int dwType;              // RESOURCETYPE_DISK
            public int dwDisplayType;       // RESOURCEDISPLAYTYPE_SHARE
            public int dwUsage;             // RESOURCEUSAGE_CONNECTABLE
            public string lpLocalName;      // буква диска (Z:, Y:)
            public string lpRemoteName;     // сетевой путь (\\server\share)
            public string lpComment;        // комментарий
            public string lpProvider;       // провайдер
        }
    }
}
