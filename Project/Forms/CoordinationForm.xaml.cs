using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public partial class CoordinationForm : Window
    {
        public string folderpath = Properties.Settings.Default.FolderPath;

        public CoordinationForm()
        {
            if (folderpath=="")
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    ShowNewFolderButton = true,
                    Description = "Выберете папку для хранения файлов csv"
                };
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    folderpath = dialog.SelectedPath;
                    Properties.Settings.Default.FolderPath = folderpath;
                    Properties.Settings.Default.Save();
                }
            }
            InitializeComponent();
            var mouse = GetMousePosition();
            this.Left = mouse.X - ActualWidth;
            this.Top = mouse.Y - ActualHeight;
            Folderbutton.Content = "Путь к папке csv:\n" + folderpath;
            Set_filelist();
        }

        public void Set_filelist()
        {
            Files.Items.Clear();
            var files = Directory.GetFiles(folderpath, "*.csv");
            foreach (string file in files)
            {
                string name = System.IO.Path.GetFileName(file);
                Files.Items.Add(name);
            }
        }
        private void Ok(object sender, RoutedEventArgs e)
        {
            Root.Close();
        }

        private void GetFolderPath(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                Description = "Выберете папку для хранения файлов csv"
            };
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {

                folderpath = dialog.SelectedPath;
                Folderbutton.Content = "Путь к папке csv:\n" + folderpath;
                Properties.Settings.Default.FolderPath = folderpath;
                Properties.Settings.Default.Save();
                Set_filelist();
            }

        }

        private void Export(object sender, RoutedEventArgs e)
        {
            CrdFunc.SaveCSV(Coordination.Cdata, folderpath);
            Set_filelist();
        }

        private void Compare(object sender, RoutedEventArgs e)
        {
            string filepath = System.IO.Path.Combine(folderpath, Files.SelectedItem.ToString());
            CrdFunc.CompareToCSV(Coordination.Cdata, filepath);
        }

        private void FileOpen(object sender, RoutedEventArgs e)
        {
            string filepath = System.IO.Path.Combine(folderpath, Files.SelectedItem.ToString());
            if (!File.Exists(filepath))
            {
                return;
            }

            // combine the arguments together
            // it doesn't matter if there is a space after ','
            string argument = "/select, \"" + filepath + "\"";

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        public System.Windows.Point GetMousePosition()
        {
            System.Drawing.Point point = System.Windows.Forms.Control.MousePosition;
            return new System.Windows.Point(point.X, point.Y);
        }
    }
}
    