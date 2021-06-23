using System;
using System.Collections.Generic;
using System.Linq;

namespace Portable.Drawing.Toolkit.Portable
{
    public class PortableRasteriser
    {
        public static IEnumerable<PixelSpan> GetNonZeroWindingSpans(Point[] points)
        {
            var segments = ToLineSegments(points);
            var spans = new List<PixelSpan>();
            if (segments.Count < 2) return spans;
            
            var top = (int)segments.Min(s=>s.Top);
            var bottom = (int)segments.Max(s=>s.Bottom);

            for (var y = top; y < bottom; y++) // each scan line
            {
                // find segments that affect this line
                var rowSegments = segments.Where(s => s.Bottom >= y && s.Top <= y).Select(s => s.PositionedAtY(y) ).OrderBy(s=>s.Pos).ToArray();
                if (rowSegments.Length < 1) continue; // nothing on this line
                
                var windingCount = 0;
                double left = 0;
                foreach (var span in rowSegments) // run across the scan line, find left and right edges of drawn area
                {
                    if (windingCount == 0) left = span.Pos;
                    if (span.Clockwise) windingCount--;
                    else windingCount++;

                    if (windingCount == 0)
                    {
                        spans.Add(new PixelSpan{Y = y, Left = (int)left, Right = (int)span.Pos});
                    }
                }
                
            }
            return spans;
        }

        public static IEnumerable<PixelSpan> GetEvenOddSpans(Point[] points)
        {
            throw new NotImplementedException();
        }
        
        
        
        private static List<Segment> ToLineSegments(Point[] points)
        {
            var outp = new List<Segment>();
            if (points.Length < 2) return outp;
            for (int i = 0; i < points.Length; i++)
            {
                var j = (i + 1) % points.Length;
                
                if (Horizontal(points[i], points[j])) continue;
                outp.Add(new Segment(points[i], points[j]));
            }
            return outp;
        }

        private static bool Horizontal(PointF a, PointF b)
        {
            return Math.Abs(a.Y - b.Y) < 0.00001;
        }
    }
    
    
    internal struct Segment
    {
        public readonly double Top;
        public readonly double Bottom;
        public readonly double TopX;
        public readonly double BottomX;
        public readonly double Dy;
        public readonly double Dx;
        
        public double Pos;

        public Segment(Point a, Point b)
        {
            if (a.Y == b.Y) Clockwise = a.X < b.X;
            else Clockwise = a.Y > b.Y;

            if (a.Y < b.Y)
            {
                Top  = a.Y; Bottom  = b.Y;
                TopX = a.X; BottomX = b.X;
            }
            else
            {
                Top  = b.Y; Bottom  = a.Y;
                TopX = b.X; BottomX = a.X;
            }
            Dy = Bottom-Top;
            Dx = (BottomX - TopX) / Dy;
            
            Pos = int.MinValue;
        }

        public bool Clockwise { get; set; }

        public Segment PositionedAtY(double y)
        {
            var n = (Segment)MemberwiseClone();
            var dy = y - Top;
            n.Pos = (dy * Dx) + TopX;
            return n;
        }
    }


    public class PixelSpan
    {
        public int Right;
        public int Left;
        public int Y;
    }
}