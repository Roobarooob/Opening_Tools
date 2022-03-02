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
    public class Union : IExternalCommand
    {

        public static UnionForm uform = null;
        public Autodesk.Revit.ApplicationServices.Application RevitApp;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UnionHandler unionHander = new UnionHandler();
            ExternalEvent unionEvent = ExternalEvent.Create(unionHander);

            if (uform == null)
            {
                uform = new UnionForm(unionHander, unionEvent);
                uform.Show();
            }
            else
            {
                uform.Activate();
            }
            return Result.Succeeded;

        }

    }



    public class UnionHandler : IExternalEventHandler
    {

        public void Execute(UIApplication uiapp)
        {
            UnionOpns(uiapp);
        }

        public static double SignedDistanceTo(Plane plane, XYZ p)
        {
            XYZ v = p - plane.Origin;
            return plane.Normal.DotProduct(v);
        }

        public static XYZ ProjectOnto(Plane plane, XYZ p)
        {
            double d = SignedDistanceTo(plane, p);
            XYZ q = p - d * plane.Normal;
            return q;
        }
        public static UV ProjectInto(Plane plane, XYZ p)
        {
            XYZ q = ProjectOnto(plane, p);
            XYZ o = plane.Origin;
            XYZ d = q - o;
            double u = d.DotProduct(plane.XVec);
            double v = d.DotProduct(plane.YVec);
            return new UV(u, v);
        }

        void UnionOpns(UIApplication uiapp)
        {

            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            Autodesk.Revit.DB.Structure.StructuralType noStr = Autodesk.Revit.DB.Structure.StructuralType.NonStructural;

            FilteredElementCollector typeIdCollector = new FilteredElementCollector(doc);
            ICollection<ElementId> typeIds = typeIdCollector.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsElementType().ToElementIds();
            IEnumerable<ElementId> ids = from i in typeIds where doc.GetElement(i).Name == Properties.Settings.Default.OpnFamilyName select i;
            FamilySymbol type = doc.GetElement(ids.First()) as FamilySymbol;

            OpnFilter filter = new OpnFilter();
            ICollection<ElementId> lsId = uidoc.Selection.GetElementIds();
            FilteredElementCollector opn_collector = new FilteredElementCollector(doc);
            ICollection<Element> elements = opn_collector.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().ToElements();
            var opns = (from i in elements where i.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == Properties.Settings.Default.OpnFamilyName select i.Id).ToList();
            List<Element> opns_inwork = new List<Element> { };
            foreach (ElementId id in lsId)
            {
                if (opns.Contains(id))
                {
                    opns_inwork.Add(doc.GetElement(id));
                }
            }

            Element ref_opn = opns_inwork.First();
            LocationPoint ref_lp = ref_opn.Location as LocationPoint;
            XYZ ref_p = ref_lp.Point;
            double ref_angle = ref_lp.Rotation;

            XYZ ref_x = new XYZ(ref_p.X + 10, ref_p.Y, ref_p.Z);
            XYZ ref_y = new XYZ(ref_p.X, ref_p.Y + 10, ref_p.Z);
            XYZ ref_z = new XYZ(ref_p.X, ref_p.Y, ref_p.Z + 10);
            
            Transform ref_rot = Transform.CreateRotationAtPoint(XYZ.BasisZ, ref_angle, ref_p);

            //создание проекционной плоскости ширины
            Line ref_line_w = Line.CreateBound(ref_p, ref_x);
            Curve plane_curve_w = ref_line_w.CreateTransformed(ref_rot);
            Plane ref_plane_w = Plane.CreateByThreePoints(plane_curve_w.GetEndPoint(0), plane_curve_w.GetEndPoint(1), ref_z);

            //создание проекционной плоскости глубины
            Line ref_line_d = Line.CreateBound(ref_p, ref_y);
            Curve plane_curve_d = ref_line_d.CreateTransformed(ref_rot);
            Plane ref_plane_d = Plane.CreateByThreePoints(plane_curve_d.GetEndPoint(0), plane_curve_d.GetEndPoint(1), ref_z);

            Level lev = doc.GetElement(ref_opn.LevelId) as Level;

            List<string> razd = new List<string>();
            List<string> wall_model = new List<string>();
            List<string> comm_model = new List<string>();
            ///Dictionary<double,XYZ> w_pointdict = new Dictionary<double, XYZ>();
            List<XYZ> w_list = new List<XYZ>();
            List<XYZ> d_list = new List<XYZ>();
            List<XYZ> h_list = new List<XYZ>();

            List<double> x_list = new List<double>();
            List<double> y_list = new List<double>();
            List<double> z_list = new List<double>();

            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Объединение отверстий");
                foreach (Element opn in opns_inwork)
                {
                    double width = 0;
                    double height = 0;
                    
                    razd.Add(opn.LookupParameter("ATL_Раздел инженерной сети").AsString());
                    wall_model.Add(opn.LookupParameter("Файл модели стен").AsString());
                    comm_model.Add(opn.LookupParameter("Файл модели инженерной сети").AsString());

                    int round = opn.LookupParameter("Круглое").AsInteger();
                    if (round == 1)
                    {
                        width = opn.LookupParameter("ADSK_Размер_Диаметр").AsDouble();
                        height = opn.LookupParameter("ADSK_Размер_Диаметр").AsDouble();
                    }
                    else
                    { 
                        width = opn.LookupParameter("ADSK_Размер_Ширина").AsDouble();
                        height = opn.LookupParameter("ADSK_Размер_Высота").AsDouble();
                    }
                    double deep = opn.LookupParameter("ADSK_Размер_Глубина").AsDouble();
                    
                    LocationPoint lp = opn.Location as LocationPoint;
                    XYZ p = lp.Point;
                    double x1 = p.X + width / 2, y1 = p.Y + deep / 2, z1 = p.Z + height / 2;
                    double x2 = p.X - width / 2, y2 = p.Y - deep / 2, z2 = p.Z - height / 2;
                    double p_angle = lp.Rotation;
                    Transform p_rot = Transform.CreateRotationAtPoint(XYZ.BasisZ, p_angle, p);

                    //Собираем точки
                    XYZ w1 = new XYZ(x1, p.Y, 0);
                    XYZ w2 = new XYZ(x2, p.Y, 0);
                    XYZ d1 = new XYZ(p.X, y1, 0);
                    XYZ d2 = new XYZ(p.X, y2, 0);
                    XYZ h1 = new XYZ(0, 0, z1);
                    XYZ h2 = new XYZ(0, 0, z2);

                    //Поворот точек по ширине
                    Curve w_line = Line.CreateBound(w1, w2).CreateTransformed(p_rot);

                    XYZ w_point1 = w_line.GetEndPoint(0);
                    XYZ w_point2 = w_line.GetEndPoint(1);
                    
                    XYZ onplane_w1 = ProjectOnto(ref_plane_w, w_point1);
                    XYZ onplane_w2 = ProjectOnto(ref_plane_w, w_point2);
                    w_list.Add(onplane_w1);
                    w_list.Add(onplane_w2);

                    //Поворот точек по глубине
                    Curve d_line = Line.CreateBound(d1, d2).CreateTransformed(p_rot);

                    XYZ d_point1 = d_line.GetEndPoint(0);
                    XYZ d_point2 = d_line.GetEndPoint(1);

                    XYZ onplane_d1 = ProjectOnto(ref_plane_d, d_point1);
                    XYZ onplane_d2 = ProjectOnto(ref_plane_d, d_point2);
                    d_list.Add(onplane_d1);
                    d_list.Add(onplane_d2);

                    //точки высоты
                    h_list.Add(h1);
                    h_list.Add(h2);

                    //списки координат
                    x_list.Add(w_point1.X); 
                    x_list.Add(w_point2.X); 
                    y_list.Add(w_point1.Y);
                    y_list.Add(w_point2.Y);
                    z_list.Add(z1);
                    z_list.Add(z2);
                }
                ///XYZ w_max = w_pointdict[w_pointdict.Keys.Max()];
                ///XYZ w_min = w_pointdict[w_pointdict.Keys.Min()];
                //Определение ширины
                List<double> w_dist_list = new List<double>();
                foreach (XYZ point in w_list)
                {
                    foreach (XYZ p in w_list)
                    {
                        double ww = point.DistanceTo(p);
                        w_dist_list.Add(ww);
                    }
                }
                double w = w_dist_list.Max();

                //Определение глубины
                List<double> d_dist_list = new List<double>();
                foreach (XYZ point in d_list)
                {
                    foreach (XYZ p in d_list)
                    {
                        double dd = point.DistanceTo(p);
                        d_dist_list.Add(dd);
                    }
                }
                double d = d_dist_list.Max();

                //Определение высоты
                List<double> h_dist_list = new List<double>();
                foreach (XYZ point in h_list)
                {
                    foreach (XYZ p in h_list)
                    {
                        double hh = point.DistanceTo(p);
                        h_dist_list.Add(hh);
                    }
                }
                double h = h_dist_list.Max();

                //определение точек центра
                XYZ cp1 = new XYZ(x_list.Max(), y_list.Max(), z_list.Max());
                XYZ cp2 = new XYZ(x_list.Min(), y_list.Min(), z_list.Min());
                Line centerline = Line.CreateBound(cp1, cp2);
                XYZ centerpoint = centerline.Evaluate(0.5, true);
                XYZ fin_point = new XYZ(centerpoint.X, centerpoint.Y, centerpoint.Z - lev.Elevation);
                XYZ point_forAxis = new XYZ(centerpoint.X, centerpoint.Y, centerpoint.Z - lev.Elevation+10);
                
                Line axis = Line.CreateBound(fin_point, point_forAxis);


                FamilyInstance newopn = doc.Create.NewFamilyInstance(fin_point, type, lev, lev, noStr);
                newopn.Location.Rotate(axis, ref_angle);

                newopn.LookupParameter("ЗАДАНИЕ_ШИРИНА").Set(Math.Ceiling(w * 304.8 / 10) * 10 / 304.8);
                newopn.LookupParameter("ЗАДАНИЕ_ВЫСОТА").Set(Math.Ceiling(h * 304.8 / 10) * 10 / 304.8);
                newopn.LookupParameter("ADSK_Размер_Глубина").Set(Math.Ceiling(d * 304.8 / 10) * 10 / 304.8);
                newopn.LookupParameter("Отступ").Set(0);
                newopn.LookupParameter("Круглое").Set(0);

                string[] razd_str = razd.Distinct().ToArray();
                string razd_value = string.Join(", ", razd_str);
                newopn.LookupParameter("ATL_Раздел инженерной сети").Set(razd_value);

                string[] w_mod_str = wall_model.Distinct().ToArray();
                string w_mod_value = string.Join(", ", w_mod_str);

                string[] c_mod_str = comm_model.Distinct().ToArray();
                string c_mod_value = string.Join(", ", c_mod_str);

                newopn.LookupParameter("Файл модели стен").Set(w_mod_value);
                newopn.LookupParameter("Файл модели инженерной сети").Set(c_mod_value);

                foreach (Element old in opns_inwork)
                {
                    doc.Delete(old.Id);
                }
                trans.Commit();
            }
        }

        public string GetName()
        {
            return "UnionHandler";
        }

        public class OpnFilter : ISelectionFilter
        {
            public bool AllowElement(Element element)
            {
                if (element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == Properties.Settings.Default.OpnFamilyName)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference refer, XYZ point)
            {
                return false;
            }
        }
    }

}