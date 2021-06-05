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
        /// Pass only the points on a single contour
        /// </summary>
        public Contour(IEnumerable<GlyphPoint> points)
        {
            Points = points?.ToList() ?? throw new ArgumentNullException(nameof(points));
        }
        
        /// <summary>
        /// Curve reduces to straight-line components for drawing
        /// </summary>
        public List<GlyphPoint> Render()
        {
            return NormaliseContour(Points).ToList();
        }
        
        
        /// <summary>
        /// break curves into segments where needed
        /// </summary>
        public static GlyphPoint[] NormaliseContour(IReadOnlyList<GlyphPoint> contour)
        {
            var len = contour.Count;
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
                    final.Add(current);
                }
                else if (next.OnCurve && prev.OnCurve) // simple curve
                {
                    final.AddRange(InterpolateCurve(prev, current, next));
                }
                else if (prev.OnCurve) // single virtual curve forward
                {
                    var virt = new GlyphPoint
                    {
                        X = (current.X + next.X) / 2.0,
                        Y = (current.Y + next.Y) / 2.0
                    };
                    final.AddRange(InterpolateCurve(prev, current, virt));
                }
                else if (next.OnCurve) // single virtual curve behind
                {
                    var virt = new GlyphPoint
                    {
                        X = (current.X + prev.X) / 2.0,
                        Y = (current.Y + prev.Y) / 2.0
                    };
                    final.AddRange(InterpolateCurve(virt, current, next));
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
                    final.AddRange(InterpolateCurve(virtPrev, current, virtNext));
                }
            }

            return final.ToArray();
        }

        /// <summary>
        /// A more refined curve breaker for larger sizes
        /// </summary>
        private static IEnumerable<GlyphPoint> InterpolateCurve(GlyphPoint start, GlyphPoint ctrl, GlyphPoint end)
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

            var minStep = 20.0d;   // larger = less refined curve, but faster
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