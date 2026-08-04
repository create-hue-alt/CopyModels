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

namespace CopyModels.UI.ViewModels
{
    /// <summary>
    /// Окно итогов выполнения задания - список результатов по каждой содели
    /// (успех/ошибка) плюс сводные счетчики.
    /// </summary>
    public class ResultsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ResultItemViewModel> _items;
        public ObservableCollection<ResultItemViewModel> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(); }
        }

        public int SuccessCount => Items?.Count(i => i.Success) ?? 0;
        public int FailureCount => Items?.Count(i => !i.Success) ?? 0;

        public RelayCommand CloseCommand { get; }
        private Action _onCloseCallback;

        public ResultsViewModel()
        {
            Items = new ObservableCollection<ResultItemViewModel>();
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        public void LoadResults(List<ExportResultItem> results)
        {
            Items.Clear();
            foreach (var r in results)
                Items.Add(new ResultItemViewModel(r));

            OnPropertyChanged(nameof(SuccessCount));
            OnPropertyChanged(nameof(FailureCount));
        }

        public void SetCallbacks(Action onClose)
        {
            _onCloseCallback = onClose;
        }

        private void ExecuteClose() => _onCloseCallback?.Invoke();

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
