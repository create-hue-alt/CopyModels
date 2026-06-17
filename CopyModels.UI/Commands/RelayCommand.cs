using System;
using System.Windows.Input;

namespace CopyModels.UI.Commands
{
    /// <summary>
    /// Позволяет привязать кнопку к методу ViewModel'a
    /// 
    /// Использование в XAML:
    /// &lt;Button Command = "{Binding SelectProjectCommand}" Content="OK"/&gt;
    /// 
    /// Использование в ViewModel:
    /// public RelayCommand SelectProjectCommand { get; }
    /// public MyViewModel()
    /// {
    ///     SelectProjectCommand = new RelayCommand(ExecuteSelectProject);
    /// }
    /// private void ExecuteSelectProject() {...}
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();
    }

    /// <summary>
    /// Версия с параметром (когда нужно передать значение в метод)
    /// 
    /// Использование в XAML:
    /// &lt;Button = Command = "{Binding SelectModelCommand}"
    ///     CommandParameter = "{Binding SelectedItem}" Content = "OK"/&gt;
    ///
    /// Использование в ViewModel:
    /// public RelayCommand&lt; ModelSetting&lt; SelectModelCommand { get; }
    /// public MyViewModel()
    /// {
    ///     SelectModelCommand = new RelayCommand&lt; ModelSetting&lt; (ExecuteSelectModel);
    /// }
    /// private void ExecuteSelectModel(ModelSetting model) {...}/// 
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            if (parameter is T t)
            {
                if (_canExecute != null)
                {
                    return _canExecute(t);
                }
                else { return true; }
            }
            else { return false; }
        }

        public void Execute(object parameter)
        {
            if (parameter is T t)
                _execute(t);
        }
    }
}
