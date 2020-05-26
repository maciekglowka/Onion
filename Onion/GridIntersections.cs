using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Revit.Elements;
using System.Collections;
using Revit.GeometryConversion;

using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;

namespace Onion
{
    public static class GridIntersections
    {
        [MultiReturn(new[] { "grid A", "grid B", "point" })]
        public static Dictionary<string, object> FindClosestIntersection(List<Autodesk.DesignScript.Geometry.Point> points, List<Revit.Elements.Element> grids)
        {
            List<Intersection> intersections = new List<Intersection>();

            foreach(Revit.Elements.Grid g0 in grids)
            {
                foreach(Revit.Elements.Grid g1 in grids)
                {
                    if(g0!=g1)
                    {
                        IntersectionResultArray ra = new IntersectionResultArray();
                        Autodesk.Revit.DB.Grid revit_g0 = g0.InternalElement as Autodesk.Revit.DB.Grid;
                        Autodesk.Revit.DB.Grid revit_g1 = g1.InternalElement as Autodesk.Revit.DB.Grid;
                        SetComparisonResult r = revit_g0.Curve.Intersect(revit_g1.Curve, out ra);
                        if (r == SetComparisonResult.Overlap) intersections.Add(new Intersection(g0, g1, ra.get_Item(0).XYZPoint.ToPoint()));
                    }
                }
            }

            //List<Intersection> closestIntersections = new List<Intersection>();
            IList gridsA = new List<object>();
            IList gridsB = new List<object>();
            IList int_points = new List<object>();

            foreach (Autodesk.DesignScript.Geometry.Point p in points)
            {
                Intersection closest = intersections.OrderBy(a => distance2D(p, a.p)).FirstOrDefault();
                gridsA.Add(closest.g0);
                gridsB.Add(closest.g1);
                int_points.Add(closest.p);
            }

            return new Dictionary<string, object>
            {
                {"grid A", gridsA },
                {"grid B", gridsB },
                {"point", int_points }
            };
        }
        
        static double distance2D(Autodesk.DesignScript.Geometry.Point a, Autodesk.DesignScript.Geometry.Point b)
        {
            return Math.Sqrt(Math.Pow(b.X-a.X,2)+Math.Pow(b.Y-a.Y,2));
        }

        struct Intersection {
            public Revit.Elements.Grid g0;
            public Revit.Elements.Grid g1;
            public Autodesk.DesignScript.Geometry.Point p;

            public Intersection(Revit.Elements.Grid _g0, Revit.Elements.Grid _g1, Autodesk.DesignScript.Geometry.Point _p)
            {
                g0 = _g0;
                g1 = _g1;
                p = _p;
            }
        }
    }
}
