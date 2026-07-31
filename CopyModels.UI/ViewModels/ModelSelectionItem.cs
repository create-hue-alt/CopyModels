using CopyModels.Core.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace CopyModels.UI.ViewModels
{
    /// <summary>
    /// Обертка над ModelSetting для биндинга в ModelSelectionWindow -
    /// добавляет IsSelected (чекбоксы) и Badges (статусные метки) поверх данных модели.
    /// </summary>
    public class ModelSelectionItem : INotifyPropertyChanged
    {
        public ModelSetting Model { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private bool _isHighlighted;
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set { _isHighlighted = value; OnPropertyChanged(); }
        }

        public string DisplayName { get; }

        public IEnumerable<BadgeItem> Badges =>
            Model.StatusFlags.Count == 0
            ? new[] { new BadgeItem("Actual", Brushes.Gray) }
            : Model.StatusFlags.Keys.Select(key =>
                new BadgeItem(key, key.EndsWith("_is_missed") ? Brushes.Red : Brushes.Orange));

        public ModelSelectionItem(ModelSetting model, string displayName)
        {
            Model = model;
            DisplayName = displayName;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
