using CopyModels.Core.Models;
using CopyModels.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CopyModels.UI.Windows
{
    /// <summary>
    /// Interaction logic for SelectionWindow.xaml
    /// </summary>
    public partial class SelectionWindow : Window
    {
        private SelectionViewModel _vm;
        public SelectionWindow()
        {
            InitializeComponent();
            _vm = new SelectionViewModel();
            DataContext = _vm;
        }

        private void TaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _vm.SelectedSettings.Clear();
            foreach (ProjectSettings item in TaskList.SelectedItems)
                _vm.SelectedSettings.Add(item);
        }
    }
}
