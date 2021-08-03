using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Portable.Drawing.Drawing2D;
using Portable.Drawing.Imaging;

namespace Portable.Drawing.Toolkit.Portable.Rasteriser
{
    public static class SdfDraw
    {
        /// <summary>
        /// Distance based anti-aliased line with thickness.
        /// This gives a much nicer result than coverage-based, but is slower
        /// </summary>
        public static void DrawLine(Frame img, double thickness, double x1, double y1, double x2, double y2, int color)
        {
            // work out maximum rect we have to cover
            var p1 = new Vector2(x1,y1);
            var p2 = new Vector2(x2,y2);
            
            MinMaxRange(out var minX, out var minY, out var maxX, out var maxY);
            ExpandRange(p1, ref minX, ref minY, ref maxX, ref maxY);
            ExpandRange(p2, ref minX, ref minY, ref maxX, ref maxY);
            ReduceMinMaxToBounds(img, ref minX, ref maxX, ref minY, ref maxY);
            
            var realThickness = thickness / 2.0;
            
            // Line as a distance function
            double DistanceFunc(Vector2 p) => OrientedBox(p, p1, p2, realThickness);

            // Do a general rendering of the function
            RenderDistanceFunction(img, minY, maxY, minX, maxX, DistanceFunc, color);
        }

        /// <summary>
        /// Distance based fill of polygon
        /// </summary>
        public static void FillPolygon(Frame img, PointF[] points, int color, FillMode mode)
        {
            var vectors = points.Select(p=>new Vector2(p)).ToArray();
            FillPolygon(img, vectors, color, mode);
        }

        /// <summary>
        /// Distance based fill of polygon
        /// </summary>
        public static void FillPolygon(Frame img, Vector2[] vectors, int color, FillMode mode)
        {
            // Basic setup
            if (vectors.Length < 3) return; // area is empty
            
            // Get bounds, and cast points to vectors
            MinMaxRange(out var minX, out var minY, out var maxX, out var maxY);
            foreach (var vector in vectors)
            {
                ExpandRange(vector, ref minX, ref minY, ref maxX, ref maxY);
            }
            ReduceMinMaxToBounds(img, ref minX, ref maxX, ref minY, ref maxY);

            // Pick a distance function based on the fill rule
            Func<Vector2, double> distanceFunc = mode switch
            {
                FillMode.Alternate => p => PolygonDistanceAlternate(p, vectors),
                FillMode.Winding => p => PolygonDistanceWinding(p, vectors),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null!)
            };
            
            // Do a general rendering of the function
            RenderDistanceFunction(img, minY, maxY, minX, maxX, distanceFunc, color);
        }

        /// <summary>
        /// Distance based fill of polygon
        /// </summary>
        public static void FillPolygon(Frame img, RasterContour[] contours, int color, FillMode mode)
        {
            // basic setup
            if (contours.Length < 1) return; // area is empty
            
            // Get bounds, and extract point pairs
            MinMaxRange(out var minX, out var minY, out var maxX, out var maxY);
            var allPairs = new List<VecSegment2>();
            foreach (var contour in contours)
            {
                var pairCount = contour!.PairCount();
                for (int i = 0; i < pairCount; i++)
                {
                    var pair = contour.Pair(i);
                    allPairs.Add(pair);
                    
                    ExpandRange(pair!.A, ref minX, ref minY, ref maxX, ref maxY);
                    ExpandRange(pair!.B, ref minX, ref minY, ref maxX, ref maxY);
                }
            }
            ReduceMinMaxToBounds(img, ref minX, ref maxX, ref minY, ref maxY);
            var pairs = allPairs.ToArray();
            
            // Pick a distance function based on the fill rule
            Func<Vector2, double> distanceFunc = mode switch
            {
                FillMode.Alternate => p => PolygonDistanceAlternate(p, pairs),
                FillMode.Winding => p => PolygonDistanceWinding(p, pairs),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
            
            // Do a general rendering of the function
            RenderDistanceFunction(img, minY, maxY, minX, maxX, distanceFunc, color);
        }

        /// <summary>
        /// Draw a set of lines, using `z` co-ord as line radius
        /// </summary>
        public static void DrawPressureCurve(Frame img, int color, Vector3[] curve)
        {
            // Basic setup
            if (curve.Length < 2) return; // line is a point. TODO: handle this
            
            // Determine bounds
            MinMaxRange(out var minX, out var minY, out var maxX, out var maxY);
            foreach (var point in curve)
            {
                ExpandRange(point, ref minX, ref minY, ref maxX, ref maxY);
                minX = Math.Min(minX, (int)(point.X - point.Z));
                maxX = Math.Max(maxX, (int)(point.X + point.Z));
                minY = Math.Min(minY, (int)(point.Y - point.Z));
                maxY = Math.Max(maxY, (int)(point.Y + point.Z));
            }
            ReduceMinMaxToBounds(img, ref minX, ref maxX, ref minY, ref maxY);
            
            
            // Build a distance function (we do all the line segments
            // at once so the blending works correctly)
            double DistanceFunc(Vector2 p)
            {
                var d = double.MaxValue;
                var lim = curve.Length - 1;
                for (int i = 0; i < lim; i++)
                {
                    d = Math.Min(d, UnevenCapsule(p, curve[i], curve[i + 1])); // 'min' in sdf is equivalent to logical 'or' in bitmaps
                }

                return d;
            }

            // Do a general rendering of the function
            RenderDistanceFunction(img, minY, maxY, minX, maxX, DistanceFunc, color);
        }

        #region SDF rendering core
        private static void RenderDistanceFunction(Frame img, int minY, int maxY, int minX, int maxX, Func<Vector2, double> distanceFunc, int color)
        {
            // Scan through the bounds, skipping where possible
            // filling area based on distance to edge (internal in negative)
            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    var s = new Vector2(x, y);
                    var d = distanceFunc!(s) + 0.5; // the 0.5 prevents over-bolding in anti-aliasing

                    if (d >= 1) // outside the iso-surface
                    {
                        x += (int) (d - 1);// jump if distance is big enough to save calculations, but don't get too close or we break anti-aliasing.
                        continue;
                    }

                    if (d < 0) // Inside the iso-surface
                    {
                        var id = (int) -d;

                        if (id >= 2) // if we're deep inside the polygon, draw big spans of pixels without shading
                        {
                            id = Math.Min(maxX - x, id);
                            img!.SetSpan(y, x, x+id, color);

                            x+=id;
                            continue;
                        }

                        d = 0;
                    }

                    // Not outside or inside -- we're within 1 pixel of the edge
                    // so draw a single pixel with blending and advance a single pixel
                    
                    var f = 1 - d; // inverse of the distance (how much fill color to blend in)
                    byte blend = (byte)(f * 255.0);
                    
                    img!.BlendPixel(x,y, color, blend);
                }
            }
        }
        #endregion
        
        #region SDF geometry
        // square ends
        private static double OrientedBox(Vector2 samplePoint, Vector2 a, Vector2 b, double thickness)
        {
            var l = (b - a).Length();
            var d = (b - a) / l;
            var q = (samplePoint - (a + b) * 0.5);
            q = new Matrix2(d.X, -d.Y, d.Y, d.X) * q;
            q = q.Abs() - (new Vector2(l, thickness)) * 0.5;
            return q.Max(0.0).Length() + Math.Min(Math.Max(q.X, q.Y), 0.0);    
        }
        
        // line with a linearly varying line width
        private static double UnevenCapsule(Vector2 samplePoint, Vector3 va, Vector3 vb)
        {
            var pa = va.SplitXY_Z(out var ra);
            var pb = vb.SplitXY_Z(out var rb);
            
            // rotate to standard form
            samplePoint  -= pa;
            pb -= pa;
            var h = Vector2.Dot(pb,pb);
            var q = new Vector2(Vector2.Dot(samplePoint, new Vector2(pb.Y, -pb.X)), Vector2.Dot(samplePoint, pb)) / h;
    
            //-----------
    
            q.X = Math.Abs(q.X);
    
            var b = ra-rb;
            var c = new Vector2(Math.Sqrt(h-b*b),b);
    
            var k = Vector2.Cross(c,q);
            var m = Vector2.Dot(c,q);
            var n = Vector2.Dot(q,q);
    
            if( k < 0.0 )  return Math.Sqrt(h*(n             )) - ra;
            if( k > c.X ) return Math.Sqrt(h*(n+1.0-2.0*q.Y)) - rb;
            return                     m                        - ra;
        }

        // https://www.shadertoy.com/view/wdBXRW
        private static double PolygonDistanceAlternate(Vector2 p, Vector2[] v)
        {
            var num = v!.Length;
            var d = Vector2.Dot(p - v[0], p - v[0]);
            var s = 1.0;
            for (int i = 0, j = num - 1; i < num; j = i, i++)
            {
                // distance
                var e = v[j] - v[i];
                var w = p - v[i];
                var b = w - e * Clamp(Vector2.Dot(w, e) / Vector2.Dot(e, e), 0.0, 1.0);
                d = Math.Min(d, Vector2.Dot(b, b));

                // winding number
                var cond = new BoolVec3(p.Y >= v[i].Y,
                    p.Y < v[j].Y,
                    e.X * w.Y > e.Y * w.X);
                if (cond.All() || cond.None()) s = -s;
            }

            return s * Math.Sqrt(d);
        }
        
        // Same as above, but input is disconnected line segments
        private static double PolygonDistanceAlternate(Vector2 p, VecSegment2[] v)
        {
            var num = v!.Length;
            var d = Vector2.Dot(p - v[0]!.A, p - v[0].A);
            var s = 1.0;
            for (int i = 0; i < num; i++)
            {
                // distance
                var e = v[i]!.B - v[i].A;
                var w = p - v[i].A;
                var b = w - e * Clamp(Vector2.Dot(w, e) / Vector2.Dot(e, e), 0.0, 1.0);
                d = Math.Min(d, Vector2.Dot(b, b));

                // winding number
                var cond = new BoolVec3(p.Y >= v[i].A.Y,
                    p.Y < v[i].B.Y,
                    e.X * w.Y > e.Y * w.X);
                if (cond.All() || cond.None()) s = -s;
            }

            return s * Math.Sqrt(d);
        }
        
        // cached data to reduce allocations
        private static readonly FlexArray<Vector2> _e = new();
        private static readonly FlexArray<Vector2> _v = new();
        private static readonly FlexArray<Vector2> _v2 = new();
        private static readonly FlexArray<Vector2> _pq = new();

        // https://www.shadertoy.com/view/WdSGRd
        private static double PolygonDistanceWinding(Vector2 p, Vector2[] poly)
        {
            var length = poly!.Length;
            
            // data
            for (int i = 0; i < length; i++)
            {
                int i2 = (int) ((float) (i + 1) % length); //i+1
                _e[i] = poly[i2] - poly[i];
                _v[i] = p - poly[i];
                _pq[i] = _v[i] - _e[i] * Clamp(Vector2.Dot(_v[i], _e[i]) / Vector2.Dot(_e[i], _e[i]), 0.0, 1.0);
            }

            //distance
            var d = Vector2.Dot(_pq[0], _pq[0]);
            for (int i = 1; i < length; i++)
            {
                var dt=Vector2.Dot(_pq[i], _pq[i]);
                d = Math.Min(d, dt);
            }

            //winding number
            // from http://geomalgorithms.com/a03-_inclusion.html
            var wn = 0;
            for (var i = 0; i < length; i++)
            {
                var i2 = (int) ((float) (i + 1) % length);
                var cond1 = 0.0 <= _v[i].Y;
                var cond2 = 0.0 > _v[i2].Y;
                var val3 = Vector2.Cross(_e[i], _v[i]); //isLeft
                wn += cond1 && cond2 && val3 > 0.0 ? 1 : 0; // have  a valid up intersect
                wn -= !cond1 && !cond2 && val3 < 0.0 ? 1 : 0; // have  a valid down intersect
            }

            var s = wn == 0 ? 1.0 : -1.0; // flip distance if we're inside
            return Math.Sqrt(d) * s;
        }
        
        // Same as above, but input is disconnected line segments
        private static double PolygonDistanceWinding(Vector2 p, VecSegment2[] poly)
        {
            var length = poly!.Length;
            
            // data
            for (int i = 0; i < length; i++)
            {
                _e[i] = poly[i]!.B - poly[i].A;
                _v[i] = p - poly[i].A;
                _v2[i] = p - poly[i].B;
                _pq[i] = _v[i] - _e[i] * Clamp(Vector2.Dot(_v[i], _e[i]) / Vector2.Dot(_e[i], _e[i]), 0.0, 1.0);
            }

            //distance
            var d = Vector2.Dot(_pq[0], _pq[0]);
            for (int i = 1; i < length; i++)
            {
                d = Math.Min(d, Vector2.Dot(_pq[i], _pq[i]));
            }

            //winding number
            // from http://geomalgorithms.com/a03-_inclusion.html
            var wn = 0;
            for (int i = 0; i < length; i++)
            {
                var cond1 = 0.0 <= _v[i].Y;
                var cond2 = 0.0 > _v2[i].Y;
                var val3 = Vector2.Cross(_e[i], _v[i]); //isLeft
                wn += cond1 && cond2 && val3 > 0.0 ? 1 : 0; // have  a valid up intersect
                wn -= !cond1 && !cond2 && val3 < 0.0 ? 1 : 0; // have  a valid down intersect
            }

            var s = wn == 0 ? 1.0 : -1.0; // flip distance if we're inside
            return Math.Sqrt(d) * s;
        }
        #endregion
        
        #region Helpers
        private static void ReduceMinMaxToBounds(Frame img, ref int minX, ref int maxX, ref int minY, ref int maxY)
        {
            minX = Math.Max(0, minX);
            minY = Math.Max(0, minY);
            maxX = Math.Min(img.width, maxX);
            maxY = Math.Min(img.height, maxY);
        }
        

        private static void ExpandRange(PointF p, ref int minX, ref int minY, ref int maxX, ref int maxY)
        {
            minX = Math.Min(minX, (int) (p.X - 2));
            minY = Math.Min(minY, (int) (p.Y - 2));
            maxX = Math.Max(maxX, (int) (p.X + 2));
            maxY = Math.Max(maxY, (int) (p.Y + 2));
        }
        
        private static void ExpandRange(Vector2 p, ref int minX, ref int minY, ref int maxX, ref int maxY)
        {
            minX = Math.Min(minX, (int) (p.X - 2));
            minY = Math.Min(minY, (int) (p.Y - 2));
            maxX = Math.Max(maxX, (int) (p.X + 2));
            maxY = Math.Max(maxY, (int) (p.Y + 2));
        }
        
        private static void ExpandRange(Vector3 p, ref int minX, ref int minY, ref int maxX, ref int maxY)
        {
            minX = Math.Min(minX, (int)(p.X - p.Z)-2);
            maxX = Math.Max(maxX, (int)(p.X + p.Z)+2);
            minY = Math.Min(minY, (int)(p.Y - p.Z)-2);
            maxY = Math.Max(maxY, (int)(p.Y + p.Z)+2);
        }

        private static void MinMaxRange(out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = int.MaxValue;
            minY = int.MaxValue;
            maxX = int.MinValue;
            maxY = int.MinValue;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Clamp(double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
        #endregion
    }
}