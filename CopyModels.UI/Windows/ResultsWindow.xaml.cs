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
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : Window
    {
        private ResultsViewModel _vm;
        public ResultsWindow()
        {
            InitializeComponent();
            _vm = new ResultsViewModel();
            DataContext = _vm;
        }

        public ResultsViewModel ViewModel => _vm;
    }
}
