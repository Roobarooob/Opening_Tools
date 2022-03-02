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

#endregion

namespace Opening_Tools
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Coordination : IExternalCommand
    {
        public static ExternalCommandData Cdata = null;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Cdata = commandData;
            CoordinationForm CrdForm = new CoordinationForm();
            CrdForm.ShowDialog();
            return Result.Succeeded;
        }
    }
    public class CrdFunc
    {
        public static void CompareToCSV(ExternalCommandData commandData, string filepath)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Application app = uiapp.Application;
                Document doc = uidoc.Document;
                string username = app.Username;
                string title = doc.Title.Replace("_" + username, "");

                List<string> list = new List<string> { };
                FilteredElementCollector typeIdCollector = new FilteredElementCollector(doc);
                FilteredElementCollector Elementcollector = new FilteredElementCollector(doc);
                FilteredElementCollector Levelcollector = new FilteredElementCollector(doc);


                ICollection<ElementId> typeIds = typeIdCollector.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsElementType().ToElementIds();
                IEnumerable<ElementId> ids = from i in typeIds where doc.GetElement(i).Name == Properties.Settings.Default.DelFamilyName select i;
                FamilySymbol type = null;
                try
                {
                    type = doc.GetElement(ids.First()) as FamilySymbol;
                }
                catch
                {
                    TaskDialog.Show("Ошибка", "Нет семейства для удалённого отверстия");
                }
                Autodesk.Revit.DB.Structure.StructuralType noStr = Autodesk.Revit.DB.Structure.StructuralType.NonStructural;
                var levels = Levelcollector.OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElements();


                ICollection<Element> elements = Elementcollector.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().ToElements();

                var dels = (from i in elements where i.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == Properties.Settings.Default.DelFamilyName select i).ToList();
                var opns = (from i in elements where i.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == Properties.Settings.Default.OpnFamilyName select i).ToList();
                string[] csvtext = File.ReadAllLines(filepath);
                                
                string[] modelStrings = (from line in csvtext where line.Split(';')[0].Contains(title + ".rvt") select line).ToArray();
                if (modelStrings.Count() == 0)
                {
                    Compare(csvtext);
                }
                else 
                {
                    Compare(modelStrings);
                }
                void Compare(string[] lines)
                {
                    using (Transaction trans = new Transaction(doc))
                    {
                        trans.Start("Сравнение отверстий");

                        foreach (Element d in dels)
                        {
                            doc.Delete(d.Id);
                        }
                        foreach (FamilyInstance opn in opns)
                        {
                            opn.LookupParameter("Комментарии").Set("");
                            string line = GetParameters(doc, opn);
                            if (!csvtext.Contains(line))
                            {
                                opn.LookupParameter("Комментарии").Set("Новое");
                            }
                            list.Add(GetParameters(doc, opn));
                            string final_string = string.Join("\n", list);
                            ///File.WriteAllText("C:/Test/1.txt", final_string);
                        }
                        foreach (string line in lines)
                        {
                            double b = 0, h = 0;
                            Level lev = null;
                            string[] vals = line.Split(';');
                            if (!list.Contains(line))
                            {
                                ///  type, elevation, X, Y, Z, Angle, gab;

                                double x = Convert.ToDouble(vals[2]), y = Convert.ToDouble(vals[3]), z = Convert.ToDouble(vals[4]), angle = Convert.ToDouble(vals[5]);

                                foreach (Level level in levels)
                                {
                                    if (level.Elevation == Convert.ToDouble(vals[1]))
                                    {
                                        lev = level;
                                    }
                                }

                                if (vals[6].Contains('d'))
                                {
                                    string[] gab = vals[6].Split('d');
                                    b = Convert.ToDouble(gab[1]);
                                    h = Convert.ToDouble(gab[1]);
                                }
                                else
                                {
                                    string[] gab = vals[6].Split('x');
                                    b = Convert.ToDouble(gab[0]);
                                    h = Convert.ToDouble(gab[1]);
                                }
                                XYZ p = new XYZ(x, y, z);
                                XYZ p1 = new XYZ(x, y, z + 10);
                                Line axis = Line.CreateBound(p, p1);
                                if (!type.IsActive)
                                {
                                    type.Activate();
                                }
                                FamilyInstance delCut = doc.Create.NewFamilyInstance(p, type, lev, lev, noStr);
                                delCut.Location.Rotate(axis, angle);
                                delCut.LookupParameter("Комментарии").Set("Удаленное");
                                delCut.LookupParameter("ЗАДАНИЕ_ВЫСОТА").Set(h);
                                delCut.LookupParameter("ЗАДАНИЕ_ШИРИНА").Set(b);
                            }
                        }
                        trans.Commit();
                    }
                }
            }
            catch
            {
                TaskDialog.Show("Ошибка", "Не удалось сравнить документы");
            }
        }
        public static void SaveCSV(ExternalCommandData commandData, string folderpath)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Application app = uiapp.Application;
                Document doc = uidoc.Document;
                List<string> list = new List<string> { };
                string username = app.Username;
                string title = doc.Title.Replace("_" + username, "");

                FilteredElementCollector collector = new FilteredElementCollector(doc);
                ICollection<Element> elements = collector.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().ToElements();
                var opns = from i in elements where i.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == Properties.Settings.Default.OpnFamilyName select i;
                foreach (FamilyInstance opn in opns)
                {
                    list.Add(GetParameters(doc, opn));
                }
                string final_string = string.Join("\n", list);
                string filepath = folderpath + "/" + DateTime.Today.ToString("yyyy-MM-dd")+ "_" + title + "_" + Properties.Settings.Default.Prefix +".csv";
                File.WriteAllText(filepath, final_string);
                TaskDialog.Show("Успех", "Файл записан");
            }
            catch
            {
                TaskDialog.Show("Ошибка", "Не удалось записать файл");
            }
        }
        public static string GetParameters(Document doc, FamilyInstance opn)
        {
            string table_string = "";
            if (opn != null)
            {
                try
                {
                    LocationPoint Lp = opn.Location as LocationPoint;
                    Level level = doc.GetElement(opn.LevelId) as Level;
                    double el = level.Elevation;
                    bool problem = false;
                    try
                    {
                        ElementId up_level_id = level.get_Parameter(BuiltInParameter.LEVEL_UP_TO_LEVEL).AsElementId();
                        Level up_level = doc.GetElement(up_level_id) as Level;
                    }
                    catch (Exception)
                    {
                        problem = true;
                    }
                   /// string type = "Отверстие в стене";
                    string model = opn.LookupParameter("Файл модели стен").AsString();
                    string elevation = el.ToString();
                    string X = Math.Round(Lp.Point.X, 13).ToString();
                    string Y = Math.Round(Lp.Point.Y, 13).ToString();
                    string Z = Math.Round(Lp.Point.Z, 13).ToString();
                    string Angle = Math.Round(Lp.Rotation,13).ToString();
                    int round = opn.LookupParameter("Круглое").AsInteger();
                    double d = Math.Round(opn.LookupParameter("ADSK_Размер_Диаметр").AsDouble(),13);
                    double b = Math.Round(opn.LookupParameter("ADSK_Размер_Ширина").AsDouble(),13);
                    double h = Math.Round(opn.LookupParameter("ADSK_Размер_Высота").AsDouble(), 13);
                    string gab = "";
                    if (round != 1)
                    {
                        gab = b.ToString("0") + "x" + h.ToString();
                    }
                    else
                    {
                        gab = "d" + d.ToString();
                    }
                    string g = Math.Round(opn.LookupParameter("ADSK_Размер_Глубина").AsDouble(),13).ToString();
                    if (problem == false)
                    {
                        List<string> s_list = new List<string> { model, elevation, X, Y, Z, Angle, gab };
                        table_string = string.Join(";", s_list);
                    }
                }
                catch (Exception)
                {
                    //Другие обобщенные модели
                }

            }
            return table_string;
        }

    }

}