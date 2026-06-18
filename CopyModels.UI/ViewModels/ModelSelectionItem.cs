using CopyModels.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CopyModels.UI.ViewModels
{
    public class ModelSelectionItem : INotifyPropertyChanged
    {
        public ModelSetting Model {  get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged();}
        }

        public string DisplayName => Model.DisplayName;

        // Бейджи статусов - берем ключи из Model.StatusFlags
        public string Badges => string.Join(", ", Model.StatusFlags.Keys);

        public ModelSelectionItem(ModelSetting model)
        {
            Model = model;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string  name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
