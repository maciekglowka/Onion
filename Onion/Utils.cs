using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;

namespace Onion
{
    class Utils
    {

        public static bool isPointOnLine(XYZ p, XYZ a, XYZ b)
        {
            if (!(p - a).CrossProduct(b - a).IsAlmostEqualTo(XYZ.Zero)) return false;

            if (a.X != b.X)
            {
                if (a.X < p.X && p.X < b.X) return true;
                if (a.X > p.X && p.X > b.X) return true;
            } else
            {
                if (a.Y < p.Y && p.Y < b.Y) return true;
                if (a.Y > p.Y && p.Y > b.Y) return true;
            }

            return false;
        }

        public static List<Autodesk.Revit.DB.Solid> getSolids(Autodesk.Revit.DB.Element e, Autodesk.Revit.DB.Options options)
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

        public static bool solidIntersect(Autodesk.Revit.DB.Solid sA, Autodesk.Revit.DB.Solid sB)
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

        public static bool elementIntersect(Autodesk.Revit.DB.Element a, Autodesk.Revit.DB.Element b, Autodesk.Revit.DB.Options options)
        {
            List<Autodesk.Revit.DB.Solid> solidsA = getSolids(a, options);
            List<Autodesk.Revit.DB.Solid> solidsB = getSolids(b, options);

            foreach (Autodesk.Revit.DB.Solid sA in solidsA)
            {
                foreach (Autodesk.Revit.DB.Solid sB in solidsB)
                {
                    if (solidIntersect(sA, sB)) return true;
                }
            }

            return false;
        }
    }
}
