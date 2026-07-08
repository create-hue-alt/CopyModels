using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace CopyModels.Core.Models
{
    public class ModelSetting
    {
        // Источник
        public string SourcePath { get; }
        public double? SourceModelDate { get; } // Unix - время (mtime) или null для RSN

        // Цели
        /// <summary>
        /// Список строк формата "path\to\file.ext" или "path\to\file.ext>viewName".
        /// Null означает, то что модели нет в источнике (exceed/ orphan в цели).
        /// </summary>
        public List<string> Targets { get; private set; }

        // Флаги состояния
        ///<summary>Модель в цели устарела или отсутствует. </summary>
        public bool IsNotActual { get; private set; }

        ///<summary>Модель есть в цели, но отсутствует в источнике (orphan).</summary>
        public bool IsExceed {  get; private set; }

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
        
        public ModelSetting (string sourcePath,
                             List<string> targets,
                             Func<string, double?> getModelDate)
        {
            SourcePath = sourcePath;
            SourceModelDate = getModelDate(sourcePath);
            IsExceed = targets == null;

            if (!IsExceed)
            {
                Targets = targets;
                EvaluateActuality(getModelDate);
            }
            else
            {
                Targets = new List<string>();
            }
        }

        //
        // Проверка актуальности
        //

        private void EvaluateActuality(Func<string, double?> getModelDate)
        {
            if(SourceModelDate == null)
            {
                IsNotActual = true;
                return;
            }

            foreach (var targetRaw in Targets)
            {
                var target = SplitTarget(targetRaw).path;
                var ext = Path.GetExtension(target).ToLower().TrimStart('.');
                var targetDate = getModelDate(target);
                var sourceDateMinus = SourceModelDate - 60; // 1 минута допуск, как в Python

                if (targetDate == null || targetDate <= sourceDateMinus)
                {
                    IsNotActual = true;

                    var flagKey = targetDate == null
                        ? $"{ext}_is_missed"
                        : $"{ext}_not_actual";

                    StatusFlags[flagKey] = true;
                }
            }
        }

        //
        // Нужно ли открывать модель в Revit
        //

        /// <summary>
        /// Модель нужно открыть в Revit если:
        /// - расширение источника и хотя бы в одной цели не совпадают (конвертация);
        /// - источник на Revit Server (RSN);
        /// - хотя бы одна цель на Revit Server.
        /// </summary>
        public bool IsOpenRequired()
        {
            if (IsExceed) return false;

            var srcExt = Path.GetExtension(SourcePath).ToUpper();
            var srcIsRsn = SourcePath.StartsWith("RSN", StringComparison.OrdinalIgnoreCase);
            var hasRsnTgt = Targets.Any(t => SplitTarget(t).path.StartsWith("RSN", StringComparison.OrdinalIgnoreCase));
            var extMismatch = Targets.Any(t =>
                !Path.GetExtension(SplitTarget(t).path).ToUpper().Equals(srcExt, StringComparison.OrdinalIgnoreCase));

            return extMismatch || srcIsRsn || hasRsnTgt;
        }

        //
        // Вспомогательный метод
        //

        /// <summary>
        /// Разбивает строку формата "путь>имяВида" на составляющие.
        /// Если символ '>' нет - view = "navisworks" по умолчанию.
        /// </summary>
        public static (string path, string view) SplitTarget(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return (null, null);

            var parts = raw.Split(new[] {'>'}, StringSplitOptions.None );
            var path = parts[0].Trim();
            var view = parts.Length > 1 ? parts[1].Trim() : null;

            return (path, view);
        }

        public override string ToString() => DisplayName;
    }
}
