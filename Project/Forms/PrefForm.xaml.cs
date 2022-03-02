using System;
using System.Collections.Generic;
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
    public partial class PrefForm : Window
    {
        public PrefForm()
        {
            
            InitializeComponent();
            GridLength grl = new GridLength(0);
            this.Height = 210;
            Hiden.Height = grl;
            Hidbutton.Content = "Расширеные настройки";
           
            ar.Text = Properties.Settings.Default.AR;
            kr.Text = Properties.Settings.Default.KR;
            ov.Text = Properties.Settings.Default.OV;
            vk.Text = Properties.Settings.Default.VK;
            eom.Text = Properties.Settings.Default.EOM;
            ss.Text = Properties.Settings.Default.SS;

            opnFam.Text = Properties.Settings.Default.OpnFamilyName;
            delFam.Text = Properties.Settings.Default.DelFamilyName;
            prefix.Text = Properties.Settings.Default.Prefix;
            var mouse = GetMousePosition();
            Left = mouse.X - ActualWidth;
            Top = mouse.Y - ActualHeight;
            if (Properties.Settings.Default.Updater_On)
            {
                Checkbox1.IsChecked = true;
            }
            else
                Checkbox1.IsChecked = false;
        }

        private void Ok(object sender, RoutedEventArgs e)
        {
            Root.Close();
        }

        private void Load(object sender, RoutedEventArgs e)
        {
            Func.DownLoad(Prefences.Cdata);
            Autodesk.Revit.UI.TaskDialog.Show("Успех", "Смейство «ATL_Отверстие.rfa» в стене загружено в категорию «Окна»\n" +
                "Смейство «ATL_Марка отверстия в стене.rfa» в стене загружено в категорию «Марки окон»");
        }

        private void Update(object sender, RoutedEventArgs e)
        {
            Func.UpdateParameters(Prefences.Cdata);
            Autodesk.Revit.UI.TaskDialog.Show("Успех", "Отверстия обновлены");
        }

        private void Reg(object sender, RoutedEventArgs e)
        {
            Func.UpdReg(Prefences.Cdata);


        }
        private void Unreg(object sender, RoutedEventArgs e)
        {
            Func.UpdUnReg();
        }

        private void Unhide(object sender, RoutedEventArgs e)
        {
            if (Hiden.Height.Value==0)
            {
                GridLength grl = new GridLength(340);
                Hiden.Height = grl;
                Hidbutton.Content = "Свернуть";
                this.Height = 550;
            }
            else
            {
                GridLength grl = new GridLength(0);
                this.Height = 210;
                Hiden.Height = grl;
                Hidbutton.Content = "Расширеные настройки";
            }
        }
        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AR = ar.Text;
            Properties.Settings.Default.KR = kr.Text;
            Properties.Settings.Default.OV = ov.Text;
            Properties.Settings.Default.VK = vk.Text;
            Properties.Settings.Default.EOM = eom.Text;
            Properties.Settings.Default.SS = ss.Text;

            Properties.Settings.Default.OpnFamilyName = opnFam.Text;
            Properties.Settings.Default.DelFamilyName = delFam.Text;
            Properties.Settings.Default.Prefix = prefix.Text;
            Properties.Settings.Default.Save();
            MessageBox.Show("Настройки сохранены");
        }
        private void ExportSettings(object sender, RoutedEventArgs e)
        {
            List<string> param = new List<string>
            {
                ar.Text,
                kr.Text,
                ov.Text,
                vk.Text,
                eom.Text,
                ss.Text,
                opnFam.Text,
                delFam.Text,
                prefix.Text
            };

            string final_string = string.Join("\n", param);
            WriteFile();

            void WriteFile()
            {
                var dialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "Файлы параметров(*.opnpars)| *.opnpars"
                };
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string filepath = dialog.FileName;
                    File.WriteAllText(filepath, final_string);
                }
            }
        }

        private void ImportSettings(object sender, RoutedEventArgs e)
        {
            List<string> param = new List<string>();
            GetFile();

            ar.Text = param[0];
            kr.Text = param[1];
            ov.Text = param[2]; 
            vk.Text = param[3];  
            eom.Text = param[4]; 
            ss.Text = param[5];
            opnFam.Text = param[6];
            delFam.Text = param[7];
            prefix.Text = param[8];
            void GetFile()
            {
                var dialog = new System.Windows.Forms.OpenFileDialog
                {
                    Filter = "Файлы параметров(*.opnpars)| *.opnpars"
                };
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string filepath = dialog.FileName;
                    param = File.ReadAllLines(filepath).ToList();
                }
            }
        }


        public System.Windows.Point GetMousePosition()
        {
            System.Drawing.Point point = System.Windows.Forms.Control.MousePosition;
            return new System.Windows.Point(point.X, point.Y);
        }
    }
}
