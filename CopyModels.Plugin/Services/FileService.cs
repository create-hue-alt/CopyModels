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
    /// </summary>
    internal class FileService
    {
        private readonly Action<string> _logInfo;
        private readonly Action<string> _logWarning;
        private readonly Action<string> _logError;

        public FileService(
            Action<string> logInfo = null,
            Action<string> logWarninf = null,
            Action<string> logError = null)
        {
            _logInfo = logInfo ?? (_ => { });
            _logWarning = logWarninf ?? (_ => { });
            _logError = logError ?? (_ => { });
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

            return (double)((DateTimeOffset)File.GetLastWriteTimeUtc(path)).ToUnixTimeSeconds();
        }

        //
        // Копирование файлов
        //

        /// <summary>Копирует файл с проверкой дат. Архивирует существующую цель если указана папка.</summary>
        public bool CopyFail(string sourcePaht, string targetPath, string archiveFolder = null)
        {
            string archived = null;
            try
            {
                if (archiveFolder != null)
                    archived = ArchiveModel(targetPath, archiveFolder);

                EnsureDirectory(targetPath);
                File.Copy(sourcePaht, targetPath, overwrite: true);

                var srcDate = GetModelDate(sourcePaht);
                var tgtDate = GetModelDate(targetPath);

                if (srcDate == null || tgtDate == null || tgtDate >= srcDate)
                    return true;

                // Дата не совпала - откатываем
                if (archived != null) File.Move(sourcePaht, targetPath);
                _logError($"Copy date mismatch: {sourcePaht} -> {targetPath}");
                return false;

            }
            catch (Exception ex)
            {
                if (archived != null && File.Exists(archived))
                    File.Move(archived, targetPath);
                _logError($"Copy error: {ex.Message}\n{sourcePaht} -> {targetPath}");
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
        public string ArchiveModel(string modelPath, string archiveFolder)
        {
            if (IsRevitServer(modelPath))
            {
                _logInfo($"Archiving not supported for Revit Server: {modelPath}");
                return null;
            }
            if (!File.Exists(modelPath))
            {
                _logInfo($": {modelPath}");
                return null;
            }

            var modeDate = File.GetLastWriteTime(modelPath);
            var dateStr = modeDate.ToString("yyyyMMdd_HHmmss");
            var dayStr = modeDate.ToString("yyyyMMdd");

            var fileNameNoExt = Path.GetFileNameWithoutExtension(modelPath);
            var ext = Path.GetExtension(modelPath);

            // Размещаем абсолютный/относительный путь архива
            var folder = Path.IsPathRooted(archiveFolder)
                ? archiveFolder
                : Path.Combine(Path.GetDirectoryName(modelPath), archiveFolder);

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

        public bool MapDrive(string driveLetter, string networkPath)
        {
            _logInfo($"Map drive {driveLetter} -> {networkPath}");
            
            if (!Directory.Exists(networkPath))
            {
                _logError($"Network path not available: {networkPath}");
                return false;
            }

            var currentPath = GetConnectionPath(driveLetter);
            if (currentPath == networkPath)
            {
                _logInfo($"Drive {driveLetter} already mapped correctly.");
                return true;
            }

            if (Directory.Exists(driveLetter + "\\"))
            {
                var discounnResult = WNetCancelConnection2(driveLetter, 1, true);
                if (discounnResult != 0)
                {
                    _logError($"Disconnect drive error: {discounnResult}");
                    return false;
                }
            }

            var nr = new NETRESOURCE
            {
                dwType = 1,     // RESOURCETYPE_DISK
                lpLocalName = driveLetter,
                lpRemoteName = networkPath,
                lpProvider = null
            };

            var result = WNetAddConnection2(ref nr, null, null, 1);
            if (result == 0)
            {
                _logInfo($"Drive {driveLetter} mapped mapped {networkPath}.");
                return true;
            }

            _logError($"WNetAddConnection2 error: {result}");
            return false;
            
        }

        //
        // Утилиты
        //

        public static bool IsRevitServer(string path)
        {
            throw new NotImplementedException();
        }
        public void EnsureDirectory(string filePatn)
        {
            throw new NotImplementedException();
        }
        public static string EnsureUniquePath(string path)
        {
            throw new NotImplementedException();
        }
        public string GetConnectionPath(string driverLetter)
        {
            throw new NotImplementedException();
        }

        //
        // P/Invoke 
        //

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NETRESOURCE lpNetResource, string lpPassword, string lpUsername, int dwFlags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string lpName, int dwFlags, bool fForce);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetGetConnection(string lpLocalName, StringBuilder lpRemoteName, ref int lpnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct NETRESOURCE
        {
            public int dwScope;
            public int dwType;
            public int dwDisplayType;
            public int dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        }
    }
}
