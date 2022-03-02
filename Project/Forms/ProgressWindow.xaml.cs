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
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }
        public void UpdateProgress(string message, int current, int total)
        {
            this.Dispatcher.Invoke(new Action<string, int, int>(

            delegate (string m, int v, int t)
            {
                progressBar1.Maximum = System.Convert.ToDouble(t);
                progressBar1.Value = System.Convert.ToDouble(v);
                Labl1.Content = m;
                Tb.Text = (v * 100 / t).ToString()+"%";
            }),
            System.Windows.Threading.DispatcherPriority.Background,
            message, current, total);
        }
    }
}
