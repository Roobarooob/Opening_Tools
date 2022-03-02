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
    public class Cut : IExternalCommand
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

        public static InsertForm root = null;
        public Autodesk.Revit.ApplicationServices.Application RevitApp;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            CutOpns(uiapp);
            return Result.Succeeded;
        }
        void CutOpns(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            FilteredElementCollector Elementcollector = new FilteredElementCollector(doc);
            ICollection<Element> elements = Elementcollector.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().ToElements();
            var opns = (from i in elements where i.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == Properties.Settings.Default.OpnFamilyName select i).ToList();
            
            Options opt = app.Create.NewGeometryOptions(); ;

            int countOpns = opns.Count();
            int currentOpn = 0;

            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Вырезание отверстий");
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
                    Cuting(opn);
                    progWindow.UpdateProgress("Подождите, отверстия вырезаются" + countOpns.ToString() + "/" + countOpns.ToString(), currentOpn, countOpns);
                }
                progWindow.Dispatcher.Invoke(new Action(progWindow.Close));
                trans.Commit();
            }
            void Cuting(Element opn)
            {
                GeometryElement opn_geo = opn.get_Geometry(opt);
                Solid opn_solid = null;
                foreach (GeometryInstance i in opn_geo)
                {
                    GeometryElement opn_solids = i.GetInstanceGeometry();
                    foreach (Solid solid in opn_solids)
                    {
                        if (solid.Volume != 0)
                        {
                            opn_solid = solid;
                        }
                    }        
                }
                
                ElementIntersectsSolidFilter filter = new ElementIntersectsSolidFilter(opn_solid);
                FilteredElementCollector wallcollector = new FilteredElementCollector(doc);
                var walls = wallcollector.OfCategory(BuiltInCategory.OST_Walls).WherePasses(filter).ToElements().ToList();
                foreach (Element wall in walls)
                {
                    try
                    {
                        SolidSolidCutUtils.AddCutBetweenSolids(doc, wall, opn);
                    }
                    catch
                    {
                        InstanceVoidCutUtils.AddInstanceVoidCut(doc, wall, opn);
                    }
                }
            }
        }
    }
}
