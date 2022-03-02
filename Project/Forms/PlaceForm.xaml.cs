using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
    public partial class PlaceForm : Window
    {
        ///private EventRegisterHandler _exeventHander;
        ///public static List<string> wallstrings = new List<string>();
        public static List<string> wallstrings = new List<string>();
        public static List<string> commstrings = new List<string>();
        private readonly ExternalEvent _exEvent;
        public static ICollection<Element> levels = new List<Element>();
        public static ICollection<Element> links = new List<Element>();
        public static Level level_Name = null;
        public static RevitLinkInstance wall_Link_Name = null;
        public static RevitLinkInstance comm_Link_Name = null;
        public Dictionary<string, Level> lev_dict = new Dictionary<string, Level>();
        public Dictionary<string, RevitLinkInstance> link_dict = new Dictionary<string, RevitLinkInstance>();
        
        public PlaceForm (EventRegisterHandler ExHandler, ExternalEvent ExEvent)
        {
            GetSorting();
            InitializeComponent();
            Walls.Items.Clear();
            Comms.Items.Clear();
            _exEvent = ExEvent;
            foreach (Level lev in levels)
            {
                lev_dict.Add(lev.Name, lev);
                Level.Items.Add(lev.Name);
            }
            foreach(RevitLinkInstance w_link in links)
            {
                link_dict.Add(w_link.Name.Split(':')[0], w_link);
                foreach (string i in wallstrings)
                {
                    if (w_link.Name.Contains(i))
                    {
                        Walls.Items.Add(w_link.Name.Split(':')[0]);
                    }
                }
                foreach (string i in commstrings)
                {
                    if (w_link.Name.Contains(i))
                    {
                        Comms.Items.Add(w_link.Name.Split(':')[0]);
                    }
                }
            } 
        }

        private void Run_Placing(object sender, RoutedEventArgs e)
        {
            bool check = false;
            try
            {
                wall_Link_Name = link_dict[Walls.SelectedItem.ToString()];
                comm_Link_Name = link_dict[Comms.SelectedItem.ToString()];
                level_Name = lev_dict[Level.SelectedItem.ToString()];
                check = true;
            }
            catch
            {
                MessageBox.Show("Выберите разделы и уровень");
            }
            if (_exEvent != null)
            {
                if (check) _exEvent.Raise();
            }
            else
            {
                MessageBox.Show("external event handler is null");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

            private void Close(object sender, RoutedEventArgs e)
        {
            root.Close();
            Command.root = null;
        }

        void GetSorting()
        {
            wallstrings.Clear();
            commstrings.Clear();
            Getkeys(Properties.Settings.Default.AR, wallstrings);
            Getkeys(Properties.Settings.Default.KR, wallstrings);
            Getkeys(Properties.Settings.Default.OV, commstrings);
            Getkeys(Properties.Settings.Default.VK, commstrings);
            Getkeys(Properties.Settings.Default.EOM, commstrings);
            Getkeys(Properties.Settings.Default.SS, commstrings);

            void Getkeys(string parameter, List<string> list)
            {
                if (parameter.Contains(","))
                {
                    string[] all = parameter.Split(',');
                    foreach (string i in all)
                    {
                        list.Add(i.Trim());
                    }
                }
                else
                {
                    list.Add(parameter.Trim());
                }
            }
        }
    }
}
