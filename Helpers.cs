using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace Polymetal.Pdms.Design.DrawListManager
{
    class Helpers
    {
        private static void QuickHull(ref List<Point> points)
        {
            var convexHull = new List<Point>();
            if (points.Count <= 3) return;
            var countBefore = points.Count;

            //foreach (var p in points)
            //{
            //    p.X = Math.Round(p.X, 2);
            //    p.Y = Math.Round(p.Y, 2);
            //    //p.Z = Math.Round(p.Z, 2);
            //}

            int minPoint = -1, maxPoint = -1;
            var minX = Double.MaxValue;
            var maxX = Double.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].X < minX)
                {
                    minX = points[i].X;
                    minPoint = i;
                }
                if (points[i].X > maxX)
                {
                    maxX = points[i].X;
                    maxPoint = i;
                }
            }
            var A = points[minPoint];
            var B = points[maxPoint];
            convexHull.Add(A);
            convexHull.Add(B);
            points.Remove(A);
            points.Remove(B);

            var leftSet = new List<Point>();
            var rightSet = new List<Point>();

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (PointLocation(A, B, p) == -1)
                {
                    leftSet.Add(p);
                }
                else
                {
                    rightSet.Add(p);
                }
            }
            HullSet(A, B, rightSet, convexHull);
            HullSet(B, A, leftSet, convexHull);

            points = convexHull;
            if (points.Count != countBefore)
                QuickHull(ref points);
        }

        private static int PointLocation(Point A, Point B, Point P)
        {
            var cp1 = (B.X - A.X) * (P.Y - A.Y) - (B.Y - A.Y) * (P.X - A.X);
            return (cp1 > 0) ? 1 : -1;
        }

        private static double PointDistance(Point A, Point B, Point C)
        {
            var ABx = B.X - A.X;
            var ABy = B.Y - A.Y;
            var num = ABx * (A.Y - C.Y) - ABy * (A.X - C.X);
            if (num < 0) num = -num;
            return num;
        }
        
        private static void HullSet(Point A, Point B, List<Point> set, List<Point> hull)
        {
            var insertPosition = hull.IndexOf(B);
            if (set.Count == 0) return;
            if (set.Count == 1)
            {
                var p = set[0];
                set.Remove(p);
                hull.Insert(insertPosition, p);
                return;
            }

            var dist = double.MinValue;
            var furthestPoint = -1;
            for (int i = 0; i < set.Count; i++)
            {
                var p = set[i];
                var distance = PointDistance(A, B, p);
                if (distance > dist)
                {
                    dist = distance;
                    furthestPoint = i;
                }
            }
            var P = set[furthestPoint];
            set.RemoveAt(furthestPoint);
            hull.Insert(insertPosition, P);

            var leftSetAP = new List<Point>();
            for (int i = 0; i < set.Count; i++)
            {
                var M = set[i];
                if (PointLocation(A, P, M) == 1)
                {
                    leftSetAP.Add(M);
                }
            }

            var leftSetPB = new List<Point>();
            for (int i = 0; i < set.Count; i++)
            {
                var M = set[i];
                if (PointLocation(P, B, M) == 1)
                {
                    leftSetPB.Add(M);
                }
            }
            HullSet(A, P, leftSetAP, hull);
            HullSet(P, B, leftSetPB, hull);
        }

    }
}
