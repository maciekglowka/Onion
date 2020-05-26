using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using RevitServices.Transactions;
using RevitServices.Persistence;
using Revit.Elements;

using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

using System.Collections;
using Autodesk.DesignScript.Runtime;

namespace Onion
{
    public static class AutoJoin
    {
        private delegate void ProgressBarDelegate();
        private static ProgressBarForm pbForm;
        private static int progress;
        private static Options options;

        [MultiReturn(new[] { "element A", "element B" })]
        public static Dictionary<string, object> JoinList(List<Revit.Elements.Element> elementsA, List<Revit.Elements.Element> elementsB)
        {
            IList outputA = new List<object>();
            IList outputB = new List<object>();

            Document doc = DocumentManager.Instance.CurrentDBDocument;
            TransactionManager.Instance.EnsureInTransaction(doc);
            options = doc.Application.Create.NewGeometryOptions();

            using (pbForm = new ProgressBarForm())
            {
                pbForm.Show();

                double maxCount = elementsA.Count * elementsB.Count;
                progress = 0;

                for (int i = 0; i < elementsA.Count; i++)
                {
                    Autodesk.Revit.DB.Element eA = elementsA[i].InternalElement;
                    BoundingBoxXYZ bbA = eA.get_BoundingBox(null);
                    if (bbA != null)
                    {
                        for (int j = 0; j < elementsB.Count; j++)
                        {
                            progress = (int)(100 * (i * elementsB.Count + j) / maxCount);
                            pbForm.progressBar.Invoke(new ProgressBarDelegate(updateProgress));
                            Autodesk.Revit.DB.Element eB = elementsB[j].InternalElement;
                            BoundingBoxXYZ bbB = eB.get_BoundingBox(null);
                            if (eA != eB && bbB != null)
                            {
                                if (bbIntersect(bbA, bbB))
                                {
                                    if (Utils.elementIntersect(eA, eB, options))
                                    {
                                        try
                                        {
                                            JoinGeometryUtils.JoinGeometry(doc, eA, eB);
                                            //output.Add(new List<Revit.Elements.Element>() { elementsA[i], elementsB[j] });
                                            outputA.Add(elementsA[i]);
                                            outputB.Add(elementsB[j]);
                                        }
                                        catch (Autodesk.Revit.Exceptions.ApplicationException)
                                        {

                                        }
                                    }
                                }
                            }
                        }
                    }
                }


                TransactionManager.Instance.TransactionTaskDone();
                pbForm.Close();
            }

            return new Dictionary<string, object>
            {
                {"element A", outputA },
                {"element B", outputB }
            };
        }

        private static void updateProgress()
        {
            pbForm.progressBar.Value = progress;
            pbForm.Text = progress.ToString() + "%";
        }

        private static bool overlap1D(double a1, double a2, double b1, double b2)
        {
            if (a2 >= b1 && b2 >= a1) return true;
            return false;
        }
        private static bool bbIntersect(BoundingBoxXYZ A, BoundingBoxXYZ B)
        {

            return overlap1D(A.Min.X, A.Max.X, B.Min.X, B.Max.X) &&
              overlap1D(A.Min.Y, A.Max.Y, B.Min.Y, B.Max.Y) &&
              overlap1D(A.Min.Z, A.Max.Z, B.Min.Z, B.Max.Z);

        }
        /*
        private static List<Autodesk.Revit.DB.Solid> getSolids(Autodesk.Revit.DB.Element e)
        {
            List<Autodesk.Revit.DB.Solid> solids = new List<Autodesk.Revit.DB.Solid>();
            GeometryElement gE = e.get_Geometry(options);

            foreach (GeometryObject gO in gE)
            {
                Autodesk.Revit.DB.Solid s = gO as Autodesk.Revit.DB.Solid;
                if (s != null)
                {
                    solids.Add(s);
                }
                GeometryInstance gI = gO as GeometryInstance;
                if (gI != null)
                {
                    GeometryElement instanceElement = gI.GetInstanceGeometry();
                    foreach (GeometryObject instanceObject in instanceElement)
                    {
                        Autodesk.Revit.DB.Solid instancesolid = instanceObject as Autodesk.Revit.DB.Solid;
                        if (instancesolid != null)
                        {
                            solids.Add(instancesolid);
                        }
                    }
                }
            }

            return solids;
        }

        private static bool solidIntersect(Autodesk.Revit.DB.Solid sA, Autodesk.Revit.DB.Solid sB)
        {
            try
            {
                Autodesk.Revit.DB.Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(sA, sB, BooleanOperationsType.Intersect);
                if (Math.Abs(intersection.Volume) > 0.000001)
                {
                    return true;
                }

                Autodesk.Revit.DB.Solid union = BooleanOperationsUtils.ExecuteBooleanOperation(sA, sB, BooleanOperationsType.Union);
                double area = Math.Abs(sA.SurfaceArea + sB.SurfaceArea - union.SurfaceArea);
                if (area > 0.00001)
                {
                    return true;
                }
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {

            }

            return false;
        }

        private static bool elementIntersect(Autodesk.Revit.DB.Element a, Autodesk.Revit.DB.Element b)
        {
            List<Autodesk.Revit.DB.Solid> solidsA = getSolids(a);
            List<Autodesk.Revit.DB.Solid> solidsB = getSolids(b);

            foreach (Autodesk.Revit.DB.Solid sA in solidsA)
            {
                foreach (Autodesk.Revit.DB.Solid sB in solidsB)
                {
                    if (solidIntersect(sA, sB)) return true;
                }
            }

            return false;
        }
        */

        private class ProgressBarForm : System.Windows.Forms.Form
        {
            public ProgressBar progressBar;
            public ProgressBarForm()
            {
                this.Width = 260;
                this.Height = 120;
                this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

                progressBar = new ProgressBar();
                progressBar.Location = new System.Drawing.Point(20, 20);
                progressBar.Width = 200;
                progressBar.Height = 30;
                progressBar.Name = "Join Progress";
                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Value = 0;

                this.Controls.Add(progressBar);

            }

        }

    }
}
