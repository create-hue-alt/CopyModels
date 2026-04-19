using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyModels.Settings
{
    /// <summary>
    /// Читает JSON-конфиги дисциплин и возвращает список ProjectSettings.
    /// Аналог функции read_setting_file() и get_config() из Python.
    /// </summary>
    public class SettingsReader
    {
        private readonly string _settingsFolder;

        /// <param name="settinsFolder">
        ///  Папка, в которой лежат JSON-файлы конфигов.
        ///  В Python это было: SCRIPT_PATH / REVIT_VERSION / discipline.json
        /// </param>
        public SettingsReader(string settingsFolder)
        {
            _settingsFolder = settingsFolder
                ?? throw new ArgumentNullException(nameof(settingsFolder));
        }

        //
        // Публичный API
        //

        /// <summary>Возвращает список имен дисциплин (имена .json файлов без расширения) </summary>
        public IReadOnlyList<string> GetDisciolineNames()
        {
            if(!Directory.Exists(_settingsFolder))
                return Array.Empty<string>();

            return Directory.GetFiles(_settingsFolder, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .OrderBy(n => n)
                .ToList();
        }
    }
}
