using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyModels.Models
{
    internal class ModelSetting
    {
        // Источник
        public string SourcePath { get; }
        public double? SourceModelDate { get; } // Unix - время (mtime) или null для RSN

        // Цели
        /// <summary>
        /// Список строк формата "path\to\file.ext" или "path\to\file.ext>viewName".
        /// Null означает, то что модели нет в источнике (exceed/ orphan в цели).
        /// </summary>
        public List<string> TargetPaths { get; }

        // Флфги состояния
        ///<summary>Модель в цеди устарела или отсутствует. </summary>
        public bool IsNotActual { get; private set; }

        ///<summary>Модель есть в цели, но отсутствует в источнике (orphan).</summary>
        public bool IsExceed {  get; }

        ///<summary>Флаги отсутствия/устаревания по расширению. Ключ: "rvt_is_missed" и т.п.</summary>
        public Dictionary<string, bool> StatusFlags { get; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // UI
        public string DisplayName => SourcePath;

        //
        // Констпуктор
        //

        /// <param name="sourcePath">Полный путь к исходной модели (файловый сервер или RSN://...).</param>
        /// <param name="targets">
        ///   Список целевых путей. Передайте null, чтобы пометить модель как exceed
        ///   (существует только в цели, но не в источнике).
        /// </param>
        /// <param name="getModelDate">
        ///   Делегат для получения даты файла — инжектируется снаружи,
        ///   чтобы класс данных не зависел от Revit API или файловой системы напрямую.
        ///   Должен вернуть Unix-время (seconds) или null если файл не найден.
        /// </param>
        


    }
}
