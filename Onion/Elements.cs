using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using RevitServices.Persistence;
using RevitServices.Transactions;
using Revit.Elements;

using System.Collections;
using Autodesk.DesignScript.Runtime;
using Revit.GeometryConversion;
using Autodesk.DesignScript.Geometry;

namespace Onion
{
    public static class Elements
    {

        [MultiReturn(new[] { "element A", "element B"})]
        public static Dictionary<string, object> LinearSplit(List<Revit.Elements.Element> elements, List<Autodesk.DesignScript.Geometry.Point> points)
        {

            IList outputA = new List<object>();
            IList outputB = new List<object>();

            Document doc = DocumentManager.Instance.CurrentDBDocument;
            TransactionManager.Instance.EnsureInTransaction(doc);
            Dictionary<ElementId, List<Autodesk.Revit.DB.Element>> children = new Dictionary<ElementId, List<Autodesk.Revit.DB.Element>>();

            int max_i = Math.Max(elements.Count, points.Count);
            for(int i=0; i < max_i; i++)
            {
                List<Autodesk.Revit.DB.Element> toCheck = new List<Autodesk.Revit.DB.Element>() { elements[i].InternalElement };
                ElementId eId = elements[i].InternalElement.Id;
                if (children.ContainsKey(eId))
                {
                    toCheck.AddRange(children[eId]);
                }

                foreach(Autodesk.Revit.DB.Element e in toCheck)
                {
                    List<Autodesk.Revit.DB.Element> splitResult = splitElement(e, points[i].ToXyz(),doc);
                    if (splitResult != null)
                    {
                        outputA.Add(splitResult[0]);
                        outputB.Add(splitResult[1]);
                        if (children.ContainsKey(eId))
                        {
                            children[eId].Add(splitResult[1]);
                        } else
                        {
                            children.Add(eId, new List<Autodesk.Revit.DB.Element>() { splitResult[1] });
                        }
                        break;
                    }
                }
            }

            TransactionManager.Instance.TransactionTaskDone();

            return new Dictionary<string, object>
            {
                {"element A", outputA },
                {"element B", outputB }
            };
        }

        static List<Autodesk.Revit.DB.Element> splitElement(Autodesk.Revit.DB.Element e, XYZ p, Document doc)
        {
            LocationCurve lc = e.Location as LocationCurve;
            if (lc==null)
            {
                return null;
            }

            XYZ v0 = lc.Curve.GetEndPoint(0);
            XYZ v1 = lc.Curve.GetEndPoint(1);

            if ((p-v0).IsAlmostEqualTo(XYZ.Zero) || (p - v1).IsAlmostEqualTo(XYZ.Zero))
            {
                return null;
            }

            if (!Utils.isPointOnLine(p, v0, v1)) return null;

            bool join_1=false;
            if (e is Autodesk.Revit.DB.Wall)
            {
                join_1 = WallUtils.IsWallJoinAllowedAtEnd(e as Autodesk.Revit.DB.Wall, 1);
                WallUtils.DisallowWallJoinAtEnd(e as Autodesk.Revit.DB.Wall, 1);
            }

            Autodesk.Revit.DB.Element e1 = doc.GetElement(ElementTransformUtils.CopyElement(doc, e.Id, XYZ.Zero).FirstOrDefault());
            if (e is Autodesk.Revit.DB.Wall)
            {
                WallUtils.DisallowWallJoinAtEnd(e1 as Autodesk.Revit.DB.Wall, 0);
            }

            Autodesk.Revit.DB.Line nc0 = Autodesk.Revit.DB.Line.CreateBound(v0, p);
            Autodesk.Revit.DB.Line nc1 = Autodesk.Revit.DB.Line.CreateBound(v1, p);

            lc.Curve = nc0;

            LocationCurve lc1 = e1.Location as LocationCurve;
            lc1.Curve = nc1;

            if (e is Autodesk.Revit.DB.Wall)
            {
                if (join_1) WallUtils.DisallowWallJoinAtEnd(e1 as Autodesk.Revit.DB.Wall, 0);
                WallUtils.AllowWallJoinAtEnd(e as Autodesk.Revit.DB.Wall, 1);
                WallUtils.AllowWallJoinAtEnd(e1 as Autodesk.Revit.DB.Wall, 0);
            }

            return new List<Autodesk.Revit.DB.Element>() { e, e1 };

        }

        [MultiReturn(new[] { "element A", "element B", "point" })]
        public static Dictionary<string, object> LinearIntersection(List<Revit.Elements.Element> elementsA, List<Revit.Elements.Element> elementsB, bool checkSolids = false)
        {

            IList outputA = new List<object>();
            IList outputB = new List<object>();
            IList outputPoint = new List<object>();

            List<Tuple<Revit.Elements.Element, Revit.Elements.Element>> checkedPairs = new List<Tuple<Revit.Elements.Element, Revit.Elements.Element>>();

            Document doc = DocumentManager.Instance.CurrentDBDocument;
            Autodesk.Revit.DB.Options options = doc.Application.Create.NewGeometryOptions();

            foreach (Revit.Elements.Element a in elementsA)
            {
                foreach (Revit.Elements.Element b in elementsB)
                {
                    if (a == b) continue;

                    if (checkedPairs.Any(x => x.Item1 == b && x.Item2 == a)) { continue; }
                    else { checkedPairs.Add(new Tuple<Revit.Elements.Element, Revit.Elements.Element>(a, b)); }

                    LocationCurve lcA = a.InternalElement.Location as LocationCurve;
                    LocationCurve lcB = b.InternalElement.Location as LocationCurve;

                    if (lcA == null || lcB == null)
                    {
                        throw new InvalidOperationException("Elements are note curve based!");
                    }

                    Autodesk.Revit.DB.Curve cA = lcA.Curve;
                    Autodesk.Revit.DB.Curve cB = lcB.Curve;

                    if (checkSolids)
                    {
                        XYZ a0 = cA.GetEndPoint(0);
                        XYZ a1 = cA.GetEndPoint(1);
                        cA = Autodesk.Revit.DB.Line.CreateBound(new XYZ(a0.X, a0.Y, 0), new XYZ(a1.X, a1.Y, 0));

                        XYZ b0 = cB.GetEndPoint(0);
                        XYZ b1 = cB.GetEndPoint(1);
                        cB = Autodesk.Revit.DB.Line.CreateBound(new XYZ(b0.X, b0.Y, 0), new XYZ(b1.X, b1.Y, 0));
                    }

                    IntersectionResultArray ra = new IntersectionResultArray();
                    SetComparisonResult r = cA.Intersect(cB, out ra);

                    if (r == SetComparisonResult.Overlap)
                    {
                        if (!checkSolids)
                        {
                            outputA.Add(a);
                            outputB.Add(b);
                            outputPoint.Add(ra.get_Item(0).XYZPoint.ToPoint());
                        }
                        else
                        {
                            if (Utils.elementIntersect(a.InternalElement, b.InternalElement, options))
                            {
                                XYZ ip = ra.get_Item(0).XYZPoint;
                                IntersectionResult result = lcA.Curve.Project(ip);
                                outputA.Add(a);
                                outputB.Add(b);
                                outputPoint.Add(result.XYZPoint.ToPoint());
                            }

                        }
                    }

                    if(checkSolids)
                    {
                        cA.Dispose();
                        cB.Dispose();
                    }
                }
            }

            return new Dictionary<string, object>
            {
                {"element A", outputA },
                {"element B", outputB },
                {"point", outputPoint }
            };

        }
    }
}
