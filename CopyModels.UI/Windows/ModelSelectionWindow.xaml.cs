using CopyModels.UI.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CopyModels.UI.Windows
{
    /// <summary>
    /// Interaction logic for ModelSelectionWindow.xaml
    /// </summary>
    public partial class ModelSelectionWindow : Window
    {
        private ModelSelectionViewModel _vm;
        public ModelSelectionWindow()
        {
            InitializeComponent();
            _vm = new ModelSelectionViewModel();
            DataContext = _vm;
        }

        public ModelSelectionViewModel ViewModel => _vm;

        private int _anchorIndex = -1;

        private void Row_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsCheckBoxClick(e.OriginalSource))
                return;

            if (!(sender is FrameworkElement fe) || !(fe.DataContext is ModelSelectionItem item))
                return;

            var items = _vm.Items;
            var index = items.IndexOf(item);

            bool shiftHeld = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            bool ctrlHeld = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            if (shiftHeld && _anchorIndex >=0)
            {
                var start = Math.Min(_anchorIndex, index);
                var end = Math.Max(_anchorIndex, index);
                for (int i = 0; i < items.Count; i++)
                    items[i].IsHighlighted = i >= start && i <= end;
            }
            else if (ctrlHeld)
            {
                item.IsHighlighted = !item.IsHighlighted;
                _anchorIndex = index;
            }
            else
            {
                foreach (var it in items)
                    it.IsHighlighted = false;
                item.IsHighlighted = true;
                _anchorIndex = index;
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox) || !(checkBox.DataContext is ModelSelectionItem item))
                return;

            var state = checkBox.IsChecked == true;
            var items = _vm.Items;

            if (item.IsHighlighted && items.Count(i => i.IsHighlighted) > 1)
            {
                foreach (var it in items.Where(i => i.IsHighlighted))
                    it.IsSelected = state;
            }
            else
            {
                foreach (var it in items)
                    it.IsSelected = false;
                item.IsSelected = state;
            }
        }

        private static bool IsCheckBoxClick(object originalSource)
        {
            var d = originalSource as DependencyObject;
            while (d != null)
            {
                if (d is CheckBox) return true;
                d = VisualTreeHelper.GetParent(d);
            }
            return false;
        }
    }
}
