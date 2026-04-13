using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace CopyModels.Core.Models
{
    /// <summary>
    /// Настройки одного задания копирования (аналог класса ProjectSettings в Python).
    /// Создаётся из одной записи JSON-конфига.
    /// </summary>
    public class ProjectSettings
    {
        // Идентификация
        public string Discipline { get; }
        public string Project { get; }

        /// <summary>Отображение имя строки в UI списке </summary>
        public string DisplayName { get; }

        // Маппинг диска
        public string MapDrive { get; }    // например "P:"
        public string Drivepath { get; }    // UNC-путь, который нужно подмониторить

        // Путь источника к цели
        public string SourcePath { get; }    // Может быть null
        public List<string> TargetPath { get; }

        // Опция копирования
        public bool SelectableCopy { get; }
        public bool KeepStructure { get; }
        public List<string> CopyExceptions { get; }
        public List<string> PathExceptions { get; }
        public bool DeleteMissed { get; }

        // Опция открытия / очистки
        public bool Purge { get; }
        public List<string> FullOpenMask { get; }
        public List<string> CloseWorksetsMask { get; }

        // Архив
        public string BackupFolder { get; }
        public bool CleanBackup { get; }

        // Transmit
        public bool? Transmit { get; }
        public bool RelativeLinks { get; }

        // IFC
        public List<string> IfcWorksetDivisionParameters { get; }
        public bool IfcDivision { get; }
        public string IfcSharedCoordinatesFiles { get; }
        public Dictionary<string, object> IfcSettings { get; }

        // NWC
        public bool NwcAllProperties { get; }
        public bool NwcDivideIntoLevels { get; }
        public bool NwcRoom { get; }
        public bool NwcLinkedFiles { get; }

        // Views
        /// <summary>
        /// Словарь: имя модели (или "*") -> расширение -> словарь вид -> суффикс.
        /// Храниться как JObject, разбивается при построение модели ModelSettings.
        /// </summary>
        public JObject Views { get; }

        // Отчеты
        public bool     HasRecipients       { get; private set; }
        public string   Recipients          { get; }
        public string   ChangeExcelReport   { get; }

        // Замена имен
        public Dictionary<string, string> ReplaceName { get; }

        //
        // Конструктор
        //
        public ProjectSettings(string discipline,
                                string project,
                                string name,
                                JObject settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            Discipline = discipline;
            Project = project;

            var now = DateTime.Now;

            // Маппинг диска
            MapDrive = settings["Map Drive"]?.Value<string>();
            Drivepath = ReplacePlaceholders( settings["Drive Path"]?.Value<string>(), 
                                             project,
                                             now);

            // Источник
            SourcePath = ReplacePlaceholders(settings["Source Path"]?.Value<string>(),
                                             project,
                                             now);

            // Цели
            TargetPath = new List<string>();
            if (settings["Target Path"] is JArray targets)
            {
                foreach (var t in targets)
                {
                    TargetPath.Add(ReplacePlaceholders(t.Value<string>(),
                                                        project,
                                                        now));
                }
            }    
            
            
            BackupFolder = settings["BackUp Folder"]?.Value<string>();

            Purge = settings["Purge"]?.Value<bool>() ?? false;
            KeepStructure = settings["Keep Structure"]?.Value<bool>() ?? true;
            DeleteMissed = settings["Delete Missed"]?.Value<bool>() ?? false;

            Transmit = settings["Transmit"]?.Value<bool>();

         
        }

        //
        // Вспомогательгые методы
        //

        /// <summary>
        /// Подставляет плейсхолдеры {PN}, {DATA}, {TIME} В строку по пути.
        /// Возвращает null, если входная строка null.
        /// </summary>
        private static string ReplacePlaceholders(string path,
                                                  string projectName,
                                                  DateTime now)
        {
            if (path == null) return null;

            return path
                .Replace("{PN}", projectName)
                .Replace("{DATE}", now.ToString("yyyyMMdd"))
                .Replace("{TIME}", now.ToString("HHmmss"));
        }

        private static List<string> ParseStringList(JToken token)
        {
            if (token is JArray arr)
                return arr.Select(x => x.Value<string>()).Where(x => x != null).ToList();
            return new List<string>();
        }


    }
}
