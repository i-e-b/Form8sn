using System;
using System.Collections.Generic;
using System.Linq;

namespace Portable.Drawing.Toolkit.Fonts
{
    public class Contour
    {
        /// <summary>
        /// Points in the curve
        /// </summary>
        public List<GlyphPoint> Points { get; }

        /// <summary>
        /// For outline drawing. If true, the segment between the last and first
        /// point should be drawn. No effect on filled shapes.
        /// </summary>
        public bool Closed { get; set; }

        /// <summary>
        /// Pass only the points on a single contour
        /// </summary>
        public Contour(IEnumerable<GlyphPoint>? points)
        {
            Points = points?.ToList() ?? throw new ArgumentNullException(nameof(points));
        }
        
        /// <summary>
        /// Pass only the points on a single contour
        /// </summary>
        public Contour()
        {
            Points = new List<GlyphPoint>();
        }
        
        /// <summary>
        /// Curve reduces to straight-line components for drawing
        /// </summary>
        public List<GlyphPoint> Render()
        {
            return NormaliseContour(Points, Closed, 2.0).ToList();
        }
        
        
        /// <summary>
        /// Break curves into segments where needed.
        /// Any zero-length segments should be removed by this function.
        /// </summary>
        public static GlyphPoint[] NormaliseContour(IReadOnlyList<GlyphPoint> contour, bool closeForm, double resolution)
        {
            var len = contour.Count;
            if (len < 2) return new GlyphPoint[0];
            var final = new List<GlyphPoint>(len * 4);
            var offs = len - 1;

            // If we get more than one 'off-curve' point in a row,
            // then we add a new virtual point between the two off-curve
            // points, and interpolate a bezier curve with 1 control point
            for (int i = 0; i <= len; i++)
            {
                var current = contour[i % len];
                var prev = contour[(i+offs) % len];
                var next = contour[(i+1   ) % len];
                
                if (current == null || prev == null || next == null) throw new Exception($"Invalid contour at point {i}");

                // if current is on-curve, just add it.
                // if current is off-curve, but next is on-curve, do a simple interpolate
                // if current AND next are off-curve, create a virtual point and interpolate

                if (current.OnCurve) // simple corner point
                {
                    AddIfNonZeroDistance(final, current);
                }
                else if (next.OnCurve && prev.OnCurve) // simple curve
                {
                    var set = InterpolateCurve(resolution, prev, current, next);
                    foreach (var point in set) { AddIfNonZeroDistance(final, point); }
                }
                else if (prev.OnCurve) // single virtual curve forward
                {
                    var virt = new GlyphPoint
                    {
                        X = (current.X + next.X) / 2.0,
                        Y = (current.Y + next.Y) / 2.0
                    };
                    var set = InterpolateCurve(resolution, prev, current, virt);
                    foreach (var point in set) { AddIfNonZeroDistance(final, point); }
                }
                else if (next.OnCurve) // single virtual curve behind
                {
                    var virt = new GlyphPoint
                    {
                        X = (current.X + prev.X) / 2.0,
                        Y = (current.Y + prev.Y) / 2.0
                    };
                    var set = InterpolateCurve(resolution, virt, current, next);
                    foreach (var point in set) { AddIfNonZeroDistance(final, point); }
                }
                else // double virtual curve
                {
                    var virtPrev = new GlyphPoint
                    {
                        X = (current.X + prev.X) / 2.0,
                        Y = (current.Y + prev.Y) / 2.0
                    };
                    var virtNext = new GlyphPoint
                    {
                        X = (current.X + next.X) / 2.0,
                        Y = (current.Y + next.Y) / 2.0
                    };
                    var set = InterpolateCurve(resolution, virtPrev, current, virtNext);
                    foreach (var point in set) { AddIfNonZeroDistance(final, point); }
                }
            }

            // Final check: if start and end points are the same, remove the end
            if (final.Count > 1)
            {
                var start = final[0];
                var end = final[final.Count-1];
                var dx = end.X - start.X;
                var dy = end.Y - start.Y;
                var sqrDist = (dx*dx)+(dy*dy);
            
                if (sqrDist < 0.0001) final.RemoveAt(final.Count-1);
            }

            return final.ToArray();
        }

        private static void AddIfNonZeroDistance(List<GlyphPoint> target, GlyphPoint point)
        {
            if (target.Count < 1)
            {
                target.Add(point);
                return;
            }

            var prev = target[target.Count-1];
            var dx = point.X - prev.X;
            var dy = point.Y - prev.Y;
            var sqrDist = (dx*dx)+(dy*dy);
            
            if (sqrDist < 0.0001) return; // too small, ignore
            
            target.Add(point);
        }

        /// <summary>
        /// A more refined curve breaker for larger sizes
        /// </summary>
        private static IEnumerable<GlyphPoint> InterpolateCurve(double resolution, GlyphPoint start, GlyphPoint ctrl, GlyphPoint end)
        {
            // Estimate a step size
            var dx1 = start.X - ctrl.X;
            var dy1 = start.Y - ctrl.Y;
            var dx2 = ctrl.X - end.X;
            var dy2 = ctrl.Y - end.Y;
            var dist = Math.Sqrt((dx1 * dx1) + (dy1 * dy1) + (dx2 * dx2) + (dy2 * dy2));
            if (dist <= 1) {
                yield return start;
                yield break;
            }

            var minStep = resolution;   // larger = less refined curve, but faster
            var inv = minStep / dist; // estimated step size. Refined by 'minStep' checks in the main loop

            for (double t = 0; t < 1; t+= inv)
            {
                yield return InterpolatePoints(start, ctrl, end, t, 1.0 - t);
            }
        }

        /// <summary>
        /// de Casteljau's algorithm for exactly 3 points
        /// </summary>
        private static GlyphPoint InterpolatePoints(GlyphPoint start, GlyphPoint ctrl, GlyphPoint end, double t, double it)
        {
            var aX = it * start.X + t * ctrl.X;
            var aY = it * start.Y + t * ctrl.Y;
            
            var bX = it * ctrl.X + t * end.X;
            var bY = it * ctrl.Y + t * end.Y;

            return new GlyphPoint{
                X = it * aX + t * bX,
                Y = it * aY + t * bY
            };
        }
    }
}