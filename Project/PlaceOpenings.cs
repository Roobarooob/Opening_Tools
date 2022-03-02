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
    public class Command : IExternalCommand
    {

        public static PlaceForm root = null;
        public static ExternalCommandData Cdata = null;
        public Autodesk.Revit.ApplicationServices.Application RevitApp;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            EventRegisterHandler _exeventHander = new EventRegisterHandler();
            ExternalEvent _exEvent = ExternalEvent.Create(_exeventHander);
            Cdata = commandData;
            GetData();
            if (root == null)
            {
                root = new PlaceForm(_exeventHander, _exEvent);
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

                FilteredElementCollector Levelcollector = new FilteredElementCollector(doc);
                PlaceForm.levels = Levelcollector.OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElements();

                FilteredElementCollector linkcollector = new FilteredElementCollector(doc);
                PlaceForm.links = linkcollector.OfCategory(BuiltInCategory.OST_RvtLinks).WhereElementIsNotElementType().ToElements();
            }

        }
    }
    public class EventRegisterHandler : IExternalEventHandler
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


            List<string> old_opnStrings = new List<string>();
            FilteredElementCollector Elementcollector = new FilteredElementCollector(doc);
            ICollection<Element> elements = Elementcollector.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().ToElements();
            var opns = (from i in elements where i.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == Properties.Settings.Default.OpnFamilyName select i).ToList();
            foreach (FamilyInstance opn in opns)
            {
                old_opnStrings.Add(GetParameters(doc, opn));
            }
               
            FilteredElementCollector typeIdCollector = new FilteredElementCollector(doc);
            ICollection<ElementId> typeIds = typeIdCollector.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsElementType().ToElementIds();
            IEnumerable<ElementId> ids = from i in typeIds where doc.GetElement(i).Name == Properties.Settings.Default.OpnFamilyName select i;
            FamilySymbol type = doc.GetElement(ids.First()) as FamilySymbol;

            RevitLinkInstance wallLink = PlaceForm.wall_Link_Name;
            Document wd = wallLink.GetLinkDocument();
            FilteredElementCollector WallCollector = new FilteredElementCollector(wd);
            ICollection<Element> walls = WallCollector.OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType().ToElements();

            RevitLinkInstance sysLink = PlaceForm.comm_Link_Name;
            Document sd = sysLink.GetLinkDocument();
            FilteredElementCollector commCollector = new FilteredElementCollector(sd);
            ICollection<BuiltInCategory> categories = new List<BuiltInCategory>() { BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_CableTray };
            ElementMulticategoryFilter MCFilter = new ElementMulticategoryFilter(categories);
            ICollection<Element> all_communications = commCollector.WherePasses(MCFilter).WhereElementIsNotElementType().ToElements();
            IEnumerable<Element> communications = (from i in all_communications where Math.Round((sd.GetElement(i.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId()) as Level).Elevation, 3) == Math.Round(PlaceForm.level_Name.Elevation, 3) select i).ToList();

            Options opt = new Options();
            SolidCurveIntersectionOptions optS = new SolidCurveIntersectionOptions();
            Solid wall_solid = null;
            int countComms = communications.Count();
            int currentComm = 0;
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Расстановка отверстий," + PlaceForm.level_Name.Name);
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
                foreach (Element comm in communications)
                {
                    ++currentComm;
                    MainCycle(comm);
                    progWindow.UpdateProgress("Подождите, выполняется обработка пересечений элемента" + currentComm.ToString() + "/" + countComms.ToString(), currentComm, countComms);
                }
                progWindow.Dispatcher.Invoke(new Action(progWindow.Close));
                void MainCycle(Element comm)
                {
                    ElementId levelId = comm.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
                    double elevation = (sd.GetElement(levelId) as Level).Elevation;
                    LocationCurve clc = comm.Location as LocationCurve;
                    Curve comm_curve = clc.Curve;
                    foreach (Element wall in walls)
                    {
                        LocationCurve wlc = wall.Location as LocationCurve;
                        Curve wall_curve = wlc.Curve;
                        GeometryElement wall_geo = wall.get_Geometry(opt);
                        foreach (GeometryObject i in wall_geo)
                        {
                            wall_solid = i as Solid;
                        }
                        SolidCurveIntersection intersection = wall_solid.IntersectWithCurve(comm_curve, optS);
                        if (intersection.SegmentCount > 0)
                        {
                            double b = (wall as Wall).WallType.Width;
                            double xy_Angle = 0;
                            double z_Angle = 0;

                            double l = 0;
                            double h = 0;
                            double d = 0;
                            int r = 0;

                            XYZ wall_direction = (wall_curve as Line).Direction;
                            double wx = wall_direction.X, wy = wall_direction.Y;
                            XYZ comm_direction = (comm_curve as Line).Direction;
                            double cx = comm_direction.X, cy = comm_direction.Y;
                            XYZ wdir = new XYZ(wx, wy, 0), cdir = new XYZ(cx, cy, 0);

                            double wall_comm_Angle = wdir.AngleTo(cdir);
                            if (wall_comm_Angle > Math.PI / 2)
                            {
                                xy_Angle = Math.PI - wall_comm_Angle;
                            }
                            else
                            {
                                xy_Angle = wall_comm_Angle;
                            }
                            double xy_sin = Math.Sin(xy_Angle);
                            double xy_tan = Math.Tan(xy_Angle);

                            double z_comm_Angle = comm_direction.AngleTo(XYZ.BasisZ);
                            if (z_comm_Angle > Math.PI / 2)
                            {
                                z_Angle = Math.PI - z_comm_Angle;
                            }
                            else
                            {
                                z_Angle = z_comm_Angle;
                            }
                            double z_sin = Math.Sin(z_Angle);
                            double z_tan = Math.Tan(z_Angle);

                            bool checkround = (Math.Abs(Math.Round(xy_Angle, 3) - Math.Round(Math.PI / 2, 3)) > 0.1) || (Math.Abs(Math.Round(z_Angle, 3) - Math.Round(Math.PI / 2, 3)) > 0.1);
                            string cat = comm.Category.Name;
                            if (cat == "Трубы")
                            {
                                l = comm.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble();
                                h = comm.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble();
                                d = comm.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble();
                                if (!checkround)
                                {
                                    r = 1;
                                }


                            }
                            if (cat == "Воздуховоды")
                            {
                                string sect = comm.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString();
                                if (sect.Contains("круг"))
                                {
                                    l = comm.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble();
                                    h = comm.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble();
                                    d = comm.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble();
                                    if (!checkround)
                                    {
                                        r = 1;
                                    }
                                }
                                else
                                {
                                    l = comm.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
                                    h = comm.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();
                                    r = 0;
                                }
                            }
                            if (cat == "Кабельные лотки")
                            {
                                l = comm.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
                                h = comm.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble();
                                r = 0;
                            }

                            double length = Math.Ceiling((l / xy_sin + b / xy_tan) * 304.8 / 10) * 10 / 304.8;
                            double height = Math.Ceiling((h / z_sin + b / z_tan) * 304.8 / 10) * 10 / 304.8;
                            double diam = Math.Ceiling((d) * 304.8 / 10) * 10 / 304.8;

                            Curve i_line = intersection.GetCurveSegment(0);
                            //XYZ i_dir = (i_line as Line).Direction;
                            XYZ p = i_line.Evaluate(0.5, true);
                            Level lev = PlaceForm.level_Name;
                            type.Activate();

                            double x = p.X, y = p.Y, z = p.Z - PlaceForm.level_Name.Elevation;
                            XYZ p1 = new XYZ(x, y, z + 10);
                            XYZ point = new XYZ(x, y, z);
                            Line axis = Line.CreateBound(point, p1);
                            string wall_model = PlaceForm.wall_Link_Name.Name.Split(':')[0].Trim();
                            string comm_model = PlaceForm.comm_Link_Name.Name.Split(':')[0].Trim();
                            string gab = "";
                            
                            if (r != 1)
                            {
                                gab = Math.Round(length,13).ToString("0") + "x" + Math.Round(height,13).ToString();
                            }
                            else
                            {
                                gab = "d" + Math.Round(diam,13).ToString();
                            }

                            List<string> s_list = new List<string> {Math.Round(p.X,13).ToString(), Math.Round(p.Y, 13).ToString(), Math.Round(p.Z, 13).ToString(), gab};
                            string opn_string = string.Join(";", s_list);
                            try
                            {
                                if (!old_opnStrings.Contains(opn_string) && length < 1000000 && height < 1000000 && diam < 1000000 && b < 1000000 )
                                {
                                    FamilyInstance opn = doc.Create.NewFamilyInstance(point, type, lev, lev, noStr);
                                    double angle = wall_direction.AngleTo(XYZ.BasisX);
                                    opn.Location.Rotate(axis, -angle);

                                    opn.LookupParameter("ЗАДАНИЕ_ШИРИНА").Set(length);
                                    opn.LookupParameter("ЗАДАНИЕ_ВЫСОТА").Set(height);
                                    opn.LookupParameter("ЗАДАНИЕ_ДИАМЕТР").Set(diam);
                                    opn.LookupParameter("Круглое").Set(r);
                                    opn.LookupParameter("ADSK_Размер_Глубина").Set(b);

                                    opn.LookupParameter("Файл модели стен").Set(wall_model);
                                    opn.LookupParameter("Файл модели инженерной сети").Set(comm_model);

                                    Getkeys(opn, Properties.Settings.Default.OV, "ОВ");
                                    Getkeys(opn, Properties.Settings.Default.VK, "ВК");
                                    Getkeys(opn, Properties.Settings.Default.EOM, "ЭОМ");
                                    Getkeys(opn, Properties.Settings.Default.SS, "СС");
                                }
                            }
                            catch
                            {

                            }
                            void Getkeys(Element opn, string parameter, string value)
                            {
                                List<string> list = new List<string>();
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
                                foreach (string i in list)
                                {
                                    if (PlaceForm.comm_Link_Name.Name.Split(':')[0].Trim().Contains(i))
                                    {
                                        opn.LookupParameter("ATL_Раздел инженерной сети").Set(value);
                                    }
                                }

                            }
                        }
                    }
                }
                trans.Commit();
            }
        }

        string GetParameters(Document doc, FamilyInstance opn)
        {
            string table_string = "";
            if (opn != null)
            {
                try
                {
                    LocationPoint Lp = opn.Location as LocationPoint;
                    Level level = doc.GetElement(opn.LevelId) as Level;
                    double el = level.Elevation;
                    /// string type = "Отверстие в стене";
                    string X = Math.Round(Lp.Point.X,13).ToString();
                    string Y = Math.Round(Lp.Point.Y,13).ToString();
                    string Z = Math.Round(Lp.Point.Z,13).ToString();
                    double d = Math.Round(opn.LookupParameter("ЗАДАНИЕ_ДИАМЕТР").AsDouble(),13);
                    double b = Math.Round(opn.LookupParameter("ЗАДАНИЕ_ШИРИНА").AsDouble(),13);
                    double h = Math.Round(opn.LookupParameter("ЗАДАНИЕ_ВЫСОТА").AsDouble(),13);
                    int round = opn.LookupParameter("Круглое").AsInteger();
                    string gab = "";
                    if (round != 1)
                    {
                        gab = b.ToString("0") + "x" + h.ToString();
                    }
                    else
                    {
                        gab = "d" + d.ToString();
                    }
                    List<string> s_list = new List<string> {X, Y, Z, gab};
                    table_string = string.Join(";", s_list);
                }
                catch (Exception)
                {
                    //Другие обобщенные модели
                }

            }
            return table_string;
        }

        public string GetName()
        {
            return "EventRegisterHandler";
        }

    }

}
