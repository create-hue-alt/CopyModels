using CopyModels.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CopyModels.UI.ViewModels
{
    /// <summary>
    /// Обертка над ExportResultItem для биндинга  в ResultsWindow -
    /// добавляет иконку/цвет для отбображения успеха/оштбки.
    /// </summary>
    public class ResultItemViewModel
    {
        public ExportResultItem Item { get; }

        public string ModelName => Item.ModelName;
        public bool Success => Item.Success;
        public string Path => Item.Path;
        public string ErrorMessage => Item.ErrorMessage;

        // Stage MDL2 Assets: E73E = галочка, E711 = крестик
        public string IconGlyph => Success ? "\uE73E" : "\uE711";
        public Brush IconBrush => Success ? Brushes.Green : Brushes.Red;

        public ResultItemViewModel(ExportResultItem item)
        {
            Item = item;
        }
    }
}
