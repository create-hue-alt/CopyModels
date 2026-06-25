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
    /// Interaction logic for ProjectSelectionWindow.xaml
    /// </summary>
    public partial class ProjectSelectionWindow : Window
    {
        private ProjectSelectionViewModel _vm;
        public ProjectSelectionWindow()
        {
            InitializeComponent();
            _vm = new ProjectSelectionViewModel();
            DataContext = _vm;
        }      

        public ProjectSelectionViewModel ViewModel => _vm;
    }
}
