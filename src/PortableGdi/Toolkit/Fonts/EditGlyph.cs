using System;
using System.Collections.Generic;

namespace Portable.Drawing.Toolkit.Fonts
{
    /// <summary>
    /// An editable representation of `Glyph`.
    /// This does not cache any of its contours or components
    /// </summary>
    public class EditGlyph
    {
        public List<Contour> Curves { get; private set; }

        public EditGlyph() { Curves = new List<Contour>(); }

        public EditGlyph(Glyph source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.GlyphType != GlyphTypes.Simple) throw new Exception("Editing non-simple glyphs is not currently supported");
            
            Curves = new List<Contour>();
            
            if (source.Points == null || source.Points.Length < 1) return; // no points
            if (source.ContourEnds == null || source.ContourEnds.Length < 1) return; // no contours

            var p = 0;
            var c = 0;
            var contourPoints = new List<GlyphPoint>();
            var pLen = source.Points.Length;

            while (p < pLen)
            {
                var point = source.Points[p];
                if (point == null) throw new Exception($"Invalid point in glyph at {p}");

                contourPoints.Add(new GlyphPoint { X = point.X, Y = point.Y, OnCurve = point.OnCurve });

                if (p == source.ContourEnds[c])
                {
                    Curves.Add(new Contour(contourPoints));
                    contourPoints.Clear();
                    c++;
                }

                p++;
            }
            
        }

        public void ClosePath()
        {
            if (Curves.Count < 1) return;
            Curves[Curves.Count - 1]!.Closed = true;
        }

        public void Clear()
        {
            Curves = new List<Contour>();
        }

        public void AddPoint(float x, float y, bool onCurve)
        {
            if (Curves.Count < 1) Curves.Add(new Contour());
            var head = Curves[Curves.Count - 1];
            head!.Points.Add(new GlyphPoint{X = x, Y=y, OnCurve = onCurve});
        }

        public void AddPoint(double x, double y, bool onCurve)
        {
            if (Curves.Count < 1) Curves.Add(new Contour());
            var head = Curves[Curves.Count - 1];
            head!.Points.Add(new GlyphPoint{X = x, Y=y, OnCurve = onCurve});
        }

        public void StartCurve()
        {
            Curves.Add(new Contour());
        }

        public void GetOffset(out double x, out double y)
        {
            x=0;y=0;
            if (Curves.Count < 1) return;
            var head = Curves[Curves.Count - 1];
            var c = head!.Points.Count;
            if (c < 1) return;
            
            var p = head.Points[c-1];
            x = p.X;
            y = p.Y;
        }
    }
}