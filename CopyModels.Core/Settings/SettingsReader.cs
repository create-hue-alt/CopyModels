using CopyModels.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public IReadOnlyList<string> GetDisciplineNames()
        {
            if(!Directory.Exists(_settingsFolder))
                return Array.Empty<string>();

            return Directory.GetFiles(_settingsFolder, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>
        /// Читает настройки одной дисциплины.
        /// </summary>
        /// <returns>
        ///  Словарь: ключ - название проекта.
        /// </returns>
        public Dictionary<string, List<ProjectSettings>> ReadDiscipline(string disciplineName)
        {
            var file = Path.Combine(_settingsFolder, $"{disciplineName}.json");
            return ReadFiles(new[] { file });
        }

        /// <summary>
        /// Читает настройки всех дисциплин.
        /// </summary>
        public Dictionary<string, List<ProjectSettings>> ReadAll()
        {
            var files = Directory.GetFiles(_settingsFolder, $"*.json").ToArray();
            return ReadFiles(files);
        }

        //
        // Внутренняя логика
        //

        private Dictionary<string, List<ProjectSettings>> ReadFiles(string[] files)
        {
            var result = new Dictionary<string, List<ProjectSettings>>()
            {
                ["ALL"] = new List<ProjectSettings>()
            };

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var discipline = Path.GetFileNameWithoutExtension(file);

                if(!File.Exists(file))
                    continue;

                var json = File.ReadAllText(file, System.Text.Encoding.UTF8);
                var root = JObject.Parse(json);

                foreach (var projectProp in root.Properties())
                {
                    var project = projectProp.Name;
                    var taskDict = (JObject)projectProp.Value;

                    if(!result.ContainsKey(project))
                        result[project] = new List<ProjectSettings>();

                    foreach (var taskProp in taskDict.Properties())
                    {
                        var taskName = taskProp.Name;
                        var taskSettings = (JObject)taskProp.Value;

                        var ps = new ProjectSettings(discipline, project, taskName, taskSettings);
                        result[project].Add(ps);
                        result["ALL"].Add(ps);
                    }
                }
            }

            // Сортируем каждую группу по DicplayName
            foreach (var key in result.Keys.ToList())
                result[key] = result[key].OrderBy(ps => ps.DisplayName).ToList();

            return result;
        }
    }
}
