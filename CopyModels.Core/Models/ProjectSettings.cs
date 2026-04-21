using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public string Name { get; }

        /// <summary>Отображение имя строки в UI списке </summary>
        public string DisplayName { get; }

        // Маппинг диска
        public string MapDrive { get; }    // например "P:"
        public string Drivepath { get; }    // UNC-путь, который нужно подмониторить

        // Путь источника к цели
        public string SourcePath { get; }    // Может быть null
        public List<string> TargetPaths { get; }

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
            Name = name;

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
            TargetPaths = new List<string>();
            if (settings["Target Path"] is JArray targets)
            {
                foreach (var t in targets)
                {
                    TargetPaths.Add(ReplacePlaceholders(t.Value<string>(),
                                                        project,
                                                        now));
                }
            }

            // Имя для UI
            var nameExtension = TargetPaths
                                .Select(t => Path.GetExtension(t).ToUpper())
                                .Distinct()
                                .ToList();
            DisplayName = $"{discipline,-20} | {project, -35} | {name, -35}";
            if (SourcePath != null)
            {
                DisplayName += " | " + Path.GetExtension(SourcePath).ToUpper();
                DisplayName += " > " + string.Join(" + ", nameExtension);
            }

            // Опция копирования
            SelectableCopy = settings["Selectable Copy"]?.Value<bool>() ?? true;
            KeepStructure = settings["Keep Structure"]?.Value<bool>() ?? true;
            DeleteMissed = settings["Delete Missed"]?.Value<bool>() ?? false;

            CopyExceptions = ParseStringList(settings["Copy Exceptions"]);
            PathExceptions = ParseStringList(settings["Path Exceptions"]);

            // Очистка /открытие
            Purge = settings["Purge"]?.Value<bool>() ?? false;
            FullOpenMask = ParseStringList(settings["Full Open Mask"]);
            CloseWorksetsMask = ParseStringList(settings["Close Workset Mask"]);

            // Архив
            BackupFolder = settings["BackUp Folder"]?.Value<string>();
            CleanBackup = settings["Clean BackUp"]?.Value<bool>() ?? true;

            // Transmit
            Transmit = settings["Transmit"]?.Value<bool>();
            RelativeLinks = settings["Relative Links"]?.Value<bool>() ?? false;

            // IFC
            IfcWorksetDivisionParameters = ParseStringList(settings["IFC workset division parameters"]);
            IfcDivision = IfcWorksetDivisionParameters.Count > 0;

            var ifcCoords = settings["IFC shared coordinates file"]?.Value<string>();
            IfcSharedCoordinatesFiles = string.IsNullOrEmpty(ifcCoords) ? null : ifcCoords;

            IfcSettings = settings["IFC_settings"] is JObject ifcObj
                                    ?ifcObj.ToObject<Dictionary<string, object>>()
                                    : new Dictionary<string, object>();

            // NWC
            NwcAllProperties = settings["NWC all properties"]?.Value<bool>() ?? true;
            NwcDivideIntoLevels = settings["NWC Divide into Levels"]?.Value<bool>() ?? true;
            NwcRoom = settings["NWC Room"]?.Value<bool>() ?? false;
            NwcLinkedFiles = settings["NWC Linked files"]?.Value<bool>() ?? false;

            // Views
            Views = settings["Views"] as JObject
                            ?? JObject.Parse(@"{ ""*"": { "".NWC"": { ""navisworks"": """" }, "".IFC"": { ""navisworks"": """" } } }");   

            // Отчеты
            Recipients = settings["Recipients"]?.Value<string>();
            HasRecipients = !string.IsNullOrEmpty(Recipients);
            ChangeExcelReport = settings["Change excel report"]?.Value<string>();

            // Замена имен
            ReplaceName = settings["Replace name"] is JObject rn
                            ? rn.ToObject<Dictionary<string, string>>()
                            : null;

            if (Purge)
                DisplayName += " + purge";                     
        }

        //
        // Вспомогательные методы
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
