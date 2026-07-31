using CopyModels.Core.Models;
using CopyModels.UI.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CopyModels.UI.ViewModels
{
    /// <summary>
    /// Второе окно: выбор моделей из ранее выбранного проекта (первое окно)
    /// 
    /// Логика:
    /// 1. Пользователь выбирает одну или подмножество моделей для экспорта
    /// </summary>
    public class ModelSelectionViewModel : INotifyPropertyChanged
    {
        // ======= СВОЙСТВА ДЛЯ UI =======

        // Выбранные пользователем модели
        private ObservableCollection<ModelSelectionItem> _items;
        public ObservableCollection<ModelSelectionItem> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(); }
        }

        // Статус загрузки
        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // ======= КОМАНДЫ =======
        public RelayCommand OkCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand CheckAllCommand { get; }
        public RelayCommand UncheckAllCommand { get; }
        public RelayCommand ToggleAllCommand { get; }
        

        // ======= СВОЙСТВА ОКНА =======
        private Action<List<ModelSetting>> _onOkCallback;
        private Action _onCancelCallback;

        // ======= КОНСТРУКТОР =======
        public ModelSelectionViewModel()
        {
            Items = new ObservableCollection<ModelSelectionItem>();

            OkCommand = new RelayCommand(ExecuteOk, CanExecuteOk);
            CancelCommand = new RelayCommand(ExecuteCancel);

            CheckAllCommand = new RelayCommand(ExecuteCheckAll);
            UncheckAllCommand = new RelayCommand(ExecuteUncheckAll);
            ToggleAllCommand = new RelayCommand(ExecuteToggleAll);

        }


        // ======= МЕТОДЫ ИНИЦИАЛИЗАЦИИ =======
        public void LoadModels(List<ModelSetting> models)
        {
            var commonDir = GetCommonDirectory(models.Select(m => m.SourcePath));
            var prefixLen = commonDir.Length > 0 ? commonDir.Length + 1 : 0;

            foreach (ModelSetting setting in models)
            {
                var normalized = setting.SourcePath.Replace('/', '\\');
                var display = normalized.Length > prefixLen
                    ? normalized.Substring(prefixLen)
                    : normalized;

                Items.Add(new ModelSelectionItem(setting, display));            }
        }

        private static string GetCommonDirectory(IEnumerable<string> path)
        {
            var dirSegments = path
                .Select(p => p.Replace("/", "\\").Split('\\'))
                .Select(segments => segments.Take(segments.Length - 1).ToArray())
                .ToList();

            if (dirSegments.Count == 0) return "";

            var minLen = dirSegments.Min(s => s.Length);
            var common = new List<string>();

            for (int i = 0; i < minLen; i++)
            {
                var segment = dirSegments[0][i];
                if (dirSegments.All(s => string.Equals(s[i], segment, StringComparison.OrdinalIgnoreCase)))
                    common.Add(segment);
                else
                    break;
            }

            return string.Join("\\", common);
        }

        /// <summary>
        /// Устанавливает callback для Ok и Cancel.
        /// Когда пользователь нажимает на кнопку, вызывается соответствующий callback.
        /// </summary>
        public void SetCallbacks(Action<List< ModelSetting>> onOk, Action onCancel)
        {
            _onOkCallback = onOk;
            _onCancelCallback = onCancel;
        }

        // ======= КОМАНДЫ (привязки к кнопкам) =======
        /// <summary>
        /// CanExecute для Ok кнопки - включаем если только что-то выбрано.
        /// </summary>
        private bool CanExecuteOk()
        {
            if (Items ==  null)
                return false;
            foreach (var item  in Items)
            {
                if (item.IsSelected) return true;
            }
            return false;
        }

        /// <summary>
        /// Execute для Ok кнопки.
        /// </summary>
        private void ExecuteOk()
        {

            var selected  = Items
                .Where(i => i.IsSelected)
                .Select(i => i.Model)
                .ToList();

            if ( selected.Count == 0)
            {
                StatusMessage = "No models selected";
                return;
            }

            StatusMessage = "Processing selected models ...";

            // Вызываем callback в CopyModels.cs
            _onOkCallback?.Invoke(selected);
        }

        /// <summary>
        /// Execute для Cancel кнопки.
        /// </summary>
        private void ExecuteCancel()
        {
            _onCancelCallback?.Invoke();
        }
        private void ExecuteCheckAll()
        {
            foreach (var item in Items)
                item.IsSelected = true;
        }

        private void ExecuteUncheckAll()
        {
            foreach (var item in Items)
                item.IsSelected = false;
        }

        private void ExecuteToggleAll()
        {
            foreach (var item in Items)
                item.IsSelected = !item.IsSelected;
        }

        // ======= INotifyPropertyChanged =======
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
