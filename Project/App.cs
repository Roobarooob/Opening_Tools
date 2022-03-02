#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;

#endregion

namespace Opening_Tools
{
    class App : IExternalApplication
    {
        public static Updater1 Updater_1 = null;
        public static Updater2 Updater_2 = null;
        public static App thisApp = null;
        public static PushButton Pref_button { get; set; }
        public static PushButton Place_button { get; set; }
        public static PushButton Union_buttton { get; set; }
        public static PushButton Coord_buttton { get; set; }
        public static PushButton Ins_buttton { get; set; }
        public static PushButton Cut_buttton { get; set; }
        public static PushButton Filter_buttton { get; set; }

        private void Application_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
        {
            
        }

        public RibbonPanel RibbonPanel(UIControlledApplication a, string tab, string ribbonPanelText)
        {
            // Empty ribbon panel 
            RibbonPanel ribbonPanel = null;
            // Try to create ribbon tab. 
            try
            {
                a.CreateRibbonTab(tab);
            }
            catch { }
            // Try to create ribbon panel.
            try
            {
                RibbonPanel panel = a.CreateRibbonPanel(tab, ribbonPanelText);
            }
            catch { }
            // Search existing tab for your panel.
            List<RibbonPanel> panels = a.GetRibbonPanels(tab);
            foreach (RibbonPanel p in panels)
            {
                if (p.Name == ribbonPanelText)
                {
                    ribbonPanel = p;
                }
            }
            //return panel 
            return ribbonPanel;
        }
        private void OnButtonCreate(UIControlledApplication application)
        {

            string assemblyPath = typeof(App).Assembly.Location;
            var pan1 = RibbonPanel(application,"ATLANT BIM", "Расстановка отверстий");
            var pan2 = RibbonPanel(application, "ATLANT BIM", "Перенос отверстий");

            // Create push buttons
            PushButton CreateBtn(RibbonPanel panel,string name, string text, string command, string image_uri) 
            {
                PushButtonData buttondata = new PushButtonData(name, text, assemblyPath, command);
                BitmapImage pb1Image = new BitmapImage(new Uri(image_uri));
                buttondata.LargeImage = pb1Image;
                PushButton button = panel.AddItem(buttondata) as PushButton;
                return button;
            }


            /// PushButtonData buttondata1 = new PushButtonData("Opening_Tools", "Расстановка отверстий\nв стенах", assemblyPath, "Opening_Tools.Command");
            /// BitmapImage pb1Image1 = new BitmapImage(new Uri("pack://application:,,,/Opening_Tools;component/Resources/PlaceIcon.png"));
            /// buttondata1.LargeImage = pb1Image1;
            Pref_button = CreateBtn(pan1, "Pref", "Параметры", "Opening_Tools.Prefences", "pack://application:,,,/Opening_Tools;component/Resources/pref_icon.png");
            Pref_button.ToolTip = "Расстановка отверстий по уровням";
            Pref_button.LongDescription = "Да да так и есть";
            
            Place_button = CreateBtn(pan1, "Place", "Расставить\nотверстия", "Opening_Tools.Command", "pack://application:,,,/Opening_Tools;component/Resources/place_icon.png");
            Place_button.ToolTip = "Расстановка отверстий по уровням";
            Place_button.LongDescription = "Да да так и есть";

            Union_buttton = CreateBtn(pan1, "Union", "Объединить\nотверстия", "Opening_Tools.Union", "pack://application:,,,/Opening_Tools;component/Resources/union_icon.png");
            Union_buttton.ToolTip = "Расстановка отверстий по уровням";
            Union_buttton.LongDescription = "Да да так и есть";

            Coord_buttton = CreateBtn(pan1, "Coord", "Координация\nотверстий",  "Opening_Tools.Coordination", "pack://application:,,,/Opening_Tools;component/Resources/coord_icon.png");
            Coord_buttton.ToolTip = "Расстановка отверстий по уровням";
            Coord_buttton.LongDescription = "Да да так и есть";

            Ins_buttton = CreateBtn(pan2, "Ins", "Отверстия\nотверстия из связи", "Opening_Tools.Insert", "pack://application:,,,/Opening_Tools;component/Resources/insert_icon.png");
            Ins_buttton.ToolTip = "Расстановка отверстий по уровням";
            Ins_buttton.LongDescription = "Да да так и есть";

            Cut_buttton = CreateBtn(pan2, "Cut", "Вырезать\nотверстия", "Opening_Tools.Cut", "pack://application:,,,/Opening_Tools;component/Resources/cut_icon.png");
            Cut_buttton.ToolTip = "Расстановка отверстий по уровням";
            Cut_buttton.LongDescription = "Да да так и есть";
            
            Cut_buttton = CreateBtn(pan2, "Filter", "Проверить\nотверстия", "Opening_Tools.Filter", "pack://application:,,,/Opening_Tools;component/Resources/filt_icon.png");
            Cut_buttton.ToolTip = "Расстановка отверстий по уровням";
            Cut_buttton.LongDescription = "Да да так и есть";
        }

        public Result OnStartup(UIControlledApplication application)
        {
            OnButtonCreate(application);
            application.ViewActivated += Application_ViewActivated;
            thisApp = this;
            Updater_On(application);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            Pref_button.Enabled = true;
            Place_button.Enabled = true;
            Union_buttton.Enabled = true;
            Updater_Off();
            return Result.Succeeded;
           
        }

        void Updater_On(UIControlledApplication application)
        {
            Updater1 updater1 = new Updater1(application.ActiveAddInId);
            Updater2 updater2 = new Updater2(application.ActiveAddInId);

            if (Properties.Settings.Default.Updater_On)
            {
                if (Updater_1 == null)
                {
                    UpdaterRegistry.RegisterUpdater(updater1);
                    UpdaterRegistry.RegisterUpdater(updater2);

                    ElementCategoryFilter f = new ElementCategoryFilter(BuiltInCategory.OST_Windows);

                    UpdaterRegistry.AddTrigger(updater1.GetUpdaterId(), f, Element.GetChangeTypeElementAddition());
                    UpdaterRegistry.AddTrigger(updater2.GetUpdaterId(), f, Element.GetChangeTypeAny());
                    Updater_1 = updater1;
                    Updater_2 = updater2;
                    ///Dialog dialog = new Dialog();
                    ///dialog.Show();
                    ///Thread.Sleep(5000);
                    ///dialog.Close();
                }
            }
        }
        void Updater_Off()
        {

            if (Updater_1 != null)
            {
                UpdaterRegistry.UnregisterUpdater(Updater_1.GetUpdaterId());
                UpdaterRegistry.UnregisterUpdater(Updater_2.GetUpdaterId());
                Updater_1 = null;
                Updater_2 = null;
            }
        }
    }
}
