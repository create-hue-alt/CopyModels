using CopyModels.Core.Models;
using CopyModels.Settings;
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
    /// Первое окно выбора: выбор проекта и заданий из JSON конфига.
    /// 
    /// Логика:
    /// 1. Загружаем все ProjectSettings из JSON
    /// 2. Порльзователь выбирает несколько (multiselect)
    /// 3. Проверяем {REQUEST} пути (пользователь указывает в ручную)
    /// 4. Возвращаем выбранные ProjectSettings для обработки
    /// </summary>
    internal class SelectionViewModel : INotifyPropertyChanged
    {
        // ======= СВОЙСТВА ДЛЯ UI =======

        // Все доступные задания (из JSON)
        private ObservableCollection<ProjectSettings> _allSettings;
        public ObservableCollection<ProjectSettings> AllSettings
        {
            get => _allSettings;
            set { _allSettings = value; OnPropertyChanged(); }
        }

        // Выбранные пользователем задания
        private ObservableCollection<ProjectSettings> _selectedSettings;
        public ObservableCollection<ProjectSettings> SelectedSettings
        {
            get => _selectedSettings;
            set { _selectedSettings = value; OnPropertyChanged(); }
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

        // ======= СВОЙСТВА ОКНА =======
        private Action<ObservableCollection<ProjectSettings>> _onOkCallback;
        private Action _onCancelCallback;

        // ======= КОНСТРУКТОР =======
        public SelectionViewModel()
        {
            AllSettings = new ObservableCollection<ProjectSettings>();
            SelectedSettings = new ObservableCollection<ProjectSettings>();

            OkCommand = new RelayCommand(ExecuteOk, CanExecuteOk);
            CancelCommand = new RelayCommand(ExecuteCancel);

            StatusMessage = "Ready";
        }

        // ======= МЕТОДЫ ИНИЦИАЛИЗАЦИИ =======
        /// <summary>
        /// Загружает все задания из JSON файла
        /// </summary>
        public void LoadSettings(string folderPath)
        {
            try
            {
                StatusMessage = "Loading settings...";

                // Читаем JSON (используем SettingReader из Core)
                var allSettings = new SettingsReader(folderPath).ReadAll()["ALL"];
                AllSettings = new ObservableCollection<ProjectSettings>(allSettings);

                StatusMessage = $"Loaded {AllSettings.Count} project groups";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Устанавливает callbacks для OK и Cancel.
        /// Когда пользователь нажмет кнопку, вызовеся соответсвующий callback.
        /// </summary>
        public void SetCallbacks(Action<ObservableCollection<ProjectSettings>> onOk, Action onCancel)
        {
            _onOkCallback = onOk;
            _onCancelCallback = onCancel;
        }

        // ======= КОМАНДЫ (привязки к кнопкам) =======

        /// <summary>
        /// CanExecute для OK кнопки - включаем только если что-то выбрано. 
        /// </summary>
        private bool CanExecuteOk() => SelectedSettings.Count > 0;       
        
        /// <summary>
        /// Execute для OK кнопки.
        /// </summary>
        private void ExecuteOk()
        {
            if (SelectedSettings.Count == 0)
            {
                StatusMessage = "No settings selected";
                return;
            }

            StatusMessage = "Processing selected settings...";

            // Вызываем callback в CopyModelsCommand.cs
            _onOkCallback?.Invoke(SelectedSettings);
        }

        /// <summary>
        /// Execute для Cancel кнопки.
        /// </summary>        
        private void ExecuteCancel()
        {
            _onCancelCallback?.Invoke();
        }



        // ======= INotifyPropertyChanged =======
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
