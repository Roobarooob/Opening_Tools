using Autodesk.Revit.DB;
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
    public partial class InsertForm : Window
    {
        private readonly ExternalEvent _exEvent;
        public static ICollection<Element> links = new List<Element>();
        public Dictionary<string, RevitLinkInstance> link_dict = new Dictionary<string, RevitLinkInstance>();
        public static RevitLinkInstance Selectedlink = null;
        public InsertForm(EventInsertHandler ExHandler, ExternalEvent ExEvent)
        {
            InitializeComponent();
            _exEvent = ExEvent;
            LinkBox.Items.Clear();
            foreach (RevitLinkInstance link in links)
            {
                link_dict.Add(link.Name.Split(':')[0], link);
                LinkBox.Items.Add(link.Name.Split(':')[0]);
                Closing += WinClosing;
            }
        }
        private void RunInsertion(object sender, RoutedEventArgs e)
        {
            Selectedlink = link_dict[LinkBox.SelectedItem.ToString()];
            if (_exEvent != null)
            {
                _exEvent.Raise();
            }
            else
                MessageBox.Show("external event handler is null");
        }
        private void WinClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Insert.root = null;
        }
    }
}
