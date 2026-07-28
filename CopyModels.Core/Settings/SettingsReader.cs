using CopyModels.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CopyModels.Settings
{
    /// <summary>
    /// Читает единый конфигурационный файл CopyModels.json и возвращает структуру настроек.
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

        /// <summary>
        /// Читает все .json файлы в папке и возвращает словаррь настроек.
        /// Ключ словаря - номер проекта.
        /// </summary>
        public Dictionary<string, List<ProjectSettings>> ReadAll()
        {                   
            if (!Directory.Exists(_settingsFolder))
            {
                return new Dictionary<string, List<ProjectSettings>>() 
                { [AppDefaults.AllProjectsKey] = new List<ProjectSettings>() };
            }

            // Забираем все json файлы из папки 
            var files = Directory.GetFiles(_settingsFolder, "*.json");

            return ReadFiles(files);
        }

        //
        // Внутренняя логика
        //

        private Dictionary<string, List<ProjectSettings>> ReadFiles(string[] files)
        {
            var result = new Dictionary<string, List<ProjectSettings>>()
            {
                [AppDefaults.AllProjectsKey] = new List<ProjectSettings>()
            };

            foreach (var file in files)
            {
                var json = File.ReadAllText(file, System.Text.Encoding.UTF8);
                var root = JObject.Parse(json);

                // Уровень 1: Проект
                foreach (var projectProp in root.Properties())
                {
                    var project = projectProp.Name;
                    var taskDict = projectProp.Value as JObject;

                    if (taskDict == null) continue;

                    if (!result.ContainsKey(project))
                        result[project] = new List<ProjectSettings>();

                    // Уровень 2: Задача ("From RVT FS to NWC FS")
                    foreach (var taskProp in taskDict.Properties())
                    {
                        var taskName = taskProp.Name;
                        var taskSettings = taskProp.Value;

                        var ps = new ProjectSettings(project, taskName, taskSettings);

                        result[project].Add(ps);
                        result[AppDefaults.AllProjectsKey].Add(ps);
                    }
                }
            }

            // Сортируем каждую группу по DisplayName
            foreach (var key in result.Keys.ToList())
                result[key] = result[key].OrderBy(ps => ps.DisplayName).ToList();

            return result;
        }
    }
}
