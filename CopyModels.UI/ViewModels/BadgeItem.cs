using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CopyModels.UI.ViewModels
{
    /// <summary>
    /// Одна статусная метка модели в UI (текст + цвет) - например "rvt_not_actual".
    /// </summary>
    public class BadgeItem
    {
        public string Text { get; }
        public Brush Brush { get; }

        public BadgeItem(string text, Brush brush)
        {
            Text = text;
            Brush = brush;
        }
    }
}
