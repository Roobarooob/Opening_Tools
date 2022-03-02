#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Application = Autodesk.Revit.ApplicationServices.Application;

#endregion

namespace Opening_Tools
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Insert : IExternalCommand
    {

        public static InsertForm root = null;
        public Autodesk.Revit.ApplicationServices.Application RevitApp;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            EventInsertHandler _exeventHander = new EventInsertHandler();
            ExternalEvent _exEvent = ExternalEvent.Create(_exeventHander);
            GetData();
            if (root == null)
            {
                root = new InsertForm(_exeventHander, _exEvent);
                root.Show();
            }
            else
            {
                root.Activate();
            }
            return Result.Succeeded;




            void GetData()
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                FilteredElementCollector linkcollector = new FilteredElementCollector(doc);
                InsertForm.links = linkcollector.OfCategory(BuiltInCategory.OST_RvtLinks).WhereElementIsNotElementType().ToElements();
            }

        }
    }
    public class EventInsertHandler : IExternalEventHandler
    {
        private ProgressWindow progWindow;
        internal static EventWaitHandle _progressWindowWaitHandle;

        private void ShowProgWindow()
        {
            //creates and shows the progress window
            progWindow = new ProgressWindow();
            progWindow.Show();

            //makes sure dispatcher is shut down when the window is closed

            //Notifies command thread the window has been created
            _progressWindowWaitHandle.Set();

            //Starts window dispatcher
            System.Windows.Threading.Dispatcher.Run();
        }

        public void Execute(UIApplication uiapp)
        {
            PlaceOpns(uiapp);
        }
        void PlaceOpns(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            Autodesk.Revit.DB.Structure.StructuralType noStr = Autodesk.Revit.DB.Structure.StructuralType.NonStructural;

            RevitLinkInstance opn_link = InsertForm.Selectedlink;
            Document ol = opn_link.GetLinkDocument();
            FilteredElementCollector Elementcollector = new FilteredElementCollector(ol);
            ICollection<Element> elements = Elementcollector.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().ToElements();
            var opns = (from i in elements where i.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == Properties.Settings.Default.OpnFamilyName select i).ToList();

            FilteredElementCollector typeIdCollector = new FilteredElementCollector(doc);
            ICollection<ElementId> typeIds = typeIdCollector.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsElementType().ToElementIds();
            IEnumerable<ElementId> ids = from i in typeIds where doc.GetElement(i).Name == Properties.Settings.Default.OpnFamilyName select i;
            if(ids.Count()==0)
            {
                MessageBox.Show("Отсутствуют семейства отверстий\n Загрузите семейства из раздела «Параметры»");
            }
            
            FamilySymbol type = doc.GetElement(ids.First()) as FamilySymbol;

            FilteredElementCollector Levelcollector = new FilteredElementCollector(doc);
            var Thisfile_Levels = Levelcollector.OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElements();

            int countOpns = opns.Count();
            int currentOpn = 0;
            using (Transaction trans = new Transaction(doc))
            { 
                trans.Start("Копирование отверстий");
                using (_progressWindowWaitHandle = new AutoResetEvent(false))
                {

                    //Starts the progress window thread
                    Thread newprogWindowThread = new Thread(new ThreadStart(ShowProgWindow));
                    newprogWindowThread.SetApartmentState(ApartmentState.STA);
                    newprogWindowThread.IsBackground = true;
                    newprogWindowThread.Start();

                    //Wait for thread to notify that it has created the window
                    _progressWindowWaitHandle.WaitOne();
                }
                foreach (FamilyInstance opn in opns)
                {
                    ++currentOpn;
                    InsertOpens(opn);
                    progWindow.UpdateProgress("Подождите, копирование отверстий" + countOpns.ToString() + "/" + countOpns.ToString(), currentOpn, countOpns);
                }
                progWindow.Dispatcher.Invoke(new Action(progWindow.Close));
                trans.Commit();
            }

            void InsertOpens(FamilyInstance opn)
            {
                string model = opn.LookupParameter("Файл модели стен").AsString();
                string username = app.Username;
                string title = doc.Title.Replace("_" + username, "");
                if (opn != null && model == title + ".rvt")
                {
                    double X = 0;
                    double Y = 0;
                    double Z = 0;
                    double d = 0;
                    double b = 0;
                    double h = 0;
                    double g = 0;
                    double otst = 0;
                    double angle = 0;
                    double el = 0;
                    int round = 0;
                    string razd = "";
                    Level level = null;
                    type.Activate();

                    LocationPoint Lp = opn.Location as LocationPoint;
                    level = ol.GetElement(opn.LevelId) as Level;
                    el = level.Elevation;
                    angle = Lp.Rotation;
                    X = Lp.Point.X;
                    Y = Lp.Point.Y;
                    Z = Lp.Point.Z;
                    d = opn.LookupParameter("ЗАДАНИЕ_ДИАМЕТР").AsDouble();
                    b = opn.LookupParameter("ЗАДАНИЕ_ШИРИНА").AsDouble();
                    h = opn.LookupParameter("ЗАДАНИЕ_ВЫСОТА").AsDouble();
                    g = opn.LookupParameter("ADSK_Размер_Глубина").AsDouble();
                    round = opn.LookupParameter("Круглое").AsInteger();
                    otst = opn.LookupParameter("Отступ").AsDouble();
                    model = opn.LookupParameter("Файл модели стен").AsString();
                    razd = opn.LookupParameter("ATL_Раздел инженерной сети").AsString();

                    Level lev = (from i in Thisfile_Levels where (i as Level).Elevation == el select i).First() as Level;
                    XYZ point = new XYZ(X, Y, Z - el);
                    XYZ p1 = new XYZ(X, Y, Z - el +10);
                    Line axis = Line.CreateBound(point, p1);
                    FamilyInstance newOpn = doc.Create.NewFamilyInstance(point, type, lev, lev, noStr);
                    newOpn.Location.Rotate(axis, angle);
                    newOpn.LookupParameter("ЗАДАНИЕ_ДИАМЕТР").Set(d);
                    newOpn.LookupParameter("ЗАДАНИЕ_ШИРИНА").Set(b);
                    newOpn.LookupParameter("ЗАДАНИЕ_ВЫСОТА").Set(h);
                    newOpn.LookupParameter("Круглое").Set(round);
                    newOpn.LookupParameter("ADSK_Размер_Глубина").Set(g);
                    newOpn.LookupParameter("Отступ").Set(otst);
                    newOpn.LookupParameter("Файл модели стен").Set(model);
                    newOpn.LookupParameter("ATL_Раздел инженерной сети").Set(razd);
                }   
            }
        }

        public string GetName()
        {
            return "EventInsertHandler";
        }

    }

}
