using Autodesk.Revit.UI;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Opening_Tools
{
    /// <summary>
    /// Логика взаимодействия для UserControl1.xaml
    /// </summary>

    public partial class UnionForm : Window
    {
        private readonly ExternalEvent _exEvent;
        public UnionForm(UnionHandler ExHandler, ExternalEvent ExEvent)
        {
            InitializeComponent();
            _exEvent = ExEvent;
            Closing += WinClosing;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_exEvent != null)
            {
                _exEvent.Raise();
            }
            else
                MessageBox.Show("external event handler is null");
        }
          private void WinClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Union.uform = null;
        }
    }

}
