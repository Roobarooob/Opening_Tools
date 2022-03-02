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
    public class Prefences : IExternalCommand
    {
        public static ExternalCommandData Cdata = null;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Cdata = commandData;
            PrefForm pref = new PrefForm();
            pref.ShowDialog();
            return Result.Succeeded;
        }
    }
    public class Func
    {
        public static void DownLoad(ExternalCommandData commandData)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            Document doc = uidoc.Document;
            Opt opt = new Opt();
            string FamilyPath = Path.GetDirectoryName(typeof(App).Assembly.Location) + "/ATL_Отверстие в стене.rfa";
            string VoidPath = Path.GetDirectoryName(typeof(App).Assembly.Location) + "/ATL_Отверстие_Удалено.rfa";
            string TagPath = Path.GetDirectoryName(typeof(App).Assembly.Location) + "/ATL_Марка отверстия в стене.rfa";
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Загрузить семейство");
                doc.LoadFamily(FamilyPath, opt, out Family family);
                doc.LoadFamily(TagPath, opt, out Family tag);
                doc.LoadFamily(VoidPath, opt, out Family v);
                tx.Commit();
            }
        }
        public class Opt : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            {
                overwriteParameterValues = true;
                return true;
            }

            public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
            {
                source = FamilySource.Family;
                overwriteParameterValues = true;
                return true;
            }
        }
    


        public static void UpdReg(ExternalCommandData commandData)
        {
            // Register wall updater with Revit
            Updater1 updater1 = new Updater1(commandData.Application.ActiveAddInId);
            Updater2 updater2 = new Updater2(commandData.Application.ActiveAddInId);
            if (App.Updater_1 == null)
            {
                UpdaterRegistry.RegisterUpdater(updater1);
                UpdaterRegistry.RegisterUpdater(updater2);
                // Change Scope = any Wall element
                ElementCategoryFilter f = new ElementCategoryFilter(BuiltInCategory.OST_Windows);

                // Change type = element addition
                UpdaterRegistry.AddTrigger(updater1.GetUpdaterId(), f, Element.GetChangeTypeElementAddition());
                UpdaterRegistry.AddTrigger(updater2.GetUpdaterId(), f, Element.GetChangeTypeAny());
                App.Updater_1 = updater1;
                App.Updater_2 = updater2;
                Properties.Settings.Default.Updater_On = true;
                Properties.Settings.Default.Save();
                ///TaskDialog.Show("Запись параметров отверстий", "Отслеживание включено!");
            }
        }


        public static void UpdUnReg()
        {
            UpdaterRegistry.UnregisterUpdater(App.Updater_1.GetUpdaterId());
            UpdaterRegistry.UnregisterUpdater(App.Updater_2.GetUpdaterId());
            App.Updater_1 = null;
            App.Updater_2 = null;
            Properties.Settings.Default.Updater_On = false;
            Properties.Settings.Default.Save();
            ///TaskDialog.Show("Запись параметров отверстий", "Отслеживание отключено!");
        }
        public static void UpdateParameters(ExternalCommandData commandData)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Application app = uiapp.Application;
                Document doc = uidoc.Document;


                FilteredElementCollector collector = new FilteredElementCollector(doc);
                ICollection<Element> elements = collector.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().ToElements();
                var opns = from i in elements where i.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == Properties.Settings.Default.OpnFamilyName select i;
                Transaction trans = new Transaction(doc, "Обновление существующих");
                trans.Start();
                foreach (FamilyInstance opn in opns)
                {
                    SetParameters(doc, opn);
                }
                trans.Commit();
            }
            catch
            {

            }
        }
        public static void SetParameters(Document doc, FamilyInstance opn)
        {
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
                        double up_el = up_level.Elevation;
                        opn.LookupParameter("Отметка уровня выше").Set(up_el);
                    }
                    catch (Exception)
                    {
                        problem = true;
                    }
                    double Z = Lp.Point.Z;
                    int round = opn.LookupParameter("Круглое").AsInteger();
                    double d = opn.LookupParameter("ADSK_Размер_Диаметр").AsDouble();
                    double b = opn.LookupParameter("ADSK_Размер_Ширина").AsDouble();
                    double h = opn.LookupParameter("ADSK_Размер_Высота").AsDouble();
                    string rectgab = (b * 304.8).ToString("0") + "x" + (h * 304.8).ToString("0") + "(h)";
                    string roundgab = "⌀" + (d * 304.8).ToString("0");
                    opn.LookupParameter("Отметка уровня").Set(el);
                    opn.LookupParameter("Отметка").Set(Z);
                    if (problem == false)
                    {
                        if (round == 1)
                        {
                            opn.LookupParameter("ATL_Габариты").Set(roundgab);
                        }
                        else
                        {
                            opn.LookupParameter("ATL_Габариты").Set(rectgab);
                        }
                    }
                    else
                    {
                        opn.LookupParameter("ATL_Габариты").Set("Заполните параметр текущего уровня \n НА ЭТАЖ ВЫШЕ");
                    }
                }
                catch (Exception)
                {
                    //Другие обобщенные модели
                }

            }
        }
    }
    public class Updater1 : IUpdater
    {
        static AddInId m_appId1;
        static UpdaterId m_updaterId1;
        Element opn = null;

        // constructor takes the AddInId for the add-in associated with this updater
        public Updater1(AddInId id)
        {
            m_appId1 = id;
            m_updaterId1 = new UpdaterId(m_appId1, new Guid("5fc7f8be-1b52-4153-a13f-ff528d72c6c4"));
        }

        public void Execute(UpdaterData data)
        {
            Document doc = data.GetDocument();

            // Кэшируем существует ли отверстие в стене
            if (opn == null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(Family));
                var opns = from element in collector where element.Name== Properties.Settings.Default.OpnFamilyName select element;
                if (opns.Count<Element>() > 0)
                {
                    opn = opns.Cast<Element>().ElementAt<Element>(0);
                }
            }

            if (opn != null)
            {
                // Выбор семейства отверстия по имени.
                var BigData = data.GetAddedElementIds();
                var opns = from i in BigData where doc.GetElement(i).get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == Properties.Settings.Default.OpnFamilyName select i;
                foreach (ElementId addedElemId in opns)
                {
                    FamilyInstance opn = doc.GetElement(addedElemId) as FamilyInstance;
                    Func.SetParameters(doc, opn);
                }
            }
        }

        public string GetAdditionalInformation()
        {
            return "Заполняет параметры в созданых отверстиях";
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority.FloorsRoofsStructuralWalls;
        }

        public UpdaterId GetUpdaterId()
        {
            return m_updaterId1;
        }

        public string GetUpdaterName()
        {
            return "Отслеживание созданных отверстий";
        }
    }
    public class Updater2 : IUpdater
    {
        static AddInId m_appId2;
        static UpdaterId m_updaterId2;
        Element opn = null;

        // constructor takes the AddInId for the add-in associated with this updater
        public Updater2(AddInId id)
        {
            m_appId2 = id;
            m_updaterId2 = new UpdaterId(m_appId2, new Guid("a619330e-a2c8-4e90-82ba-bd2c2435add3"));
        }

        public void Execute(UpdaterData data)
        {
            Document doc = data.GetDocument();

            // Cache the wall type
            if (opn == null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(Family));
                var opns = from element in collector where element.Name == Properties.Settings.Default.OpnFamilyName select element;
                if (opns.Count<Element>() > 0)
                {
                    opn = opns.Cast<Element>().ElementAt<Element>(0);
                }
            }

            if (opn != null)
            {
                // Change the wall to the cached wall type.
                var BigData = data.GetModifiedElementIds();
                var opns = from i in BigData where doc.GetElement(i).get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == Properties.Settings.Default.OpnFamilyName select i;
                foreach (ElementId addedElemId in opns)
                {
                    FamilyInstance opn = doc.GetElement(addedElemId) as FamilyInstance;
                    Func.SetParameters(doc, opn);
                }
                
            }
        }

        public string GetAdditionalInformation()
        {
            return "Заполняет параметры измененных отверстиях";
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority.FloorsRoofsStructuralWalls;
        }

        public UpdaterId GetUpdaterId()
        {
            return m_updaterId2;
        }

        public string GetUpdaterName()
        {
            return "Отслеживание измененных отверстий";
        }
    }
}