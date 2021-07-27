using System;
using System.Collections.Generic;
using System.Linq;
using Portable.Drawing.Drawing2D;
using Portable.Drawing.Imaging.ImageFormats;
using Portable.Drawing.Text;
using Portable.Drawing.Toolkit.Fonts;
using Portable.Drawing.Toolkit.Fonts.Rendering;

namespace Portable.Drawing.Toolkit.Portable
{
    internal class PortableGraphics : IToolkitGraphics
    {
        /// <summary>
        /// Create a dummy graphics that is attached to no drawing surface
        /// </summary>
        /// <param name="parent"></param>
        public PortableGraphics(IToolkit parent)
        {
            Target = new PortableImage();
            Toolkit = parent;
            DpiX = DpiY = 96.0f; // default Windows-like value
        }

        public PortableGraphics(PortableToolkit parent, IToolkitImage image)
        {
            Toolkit = parent;
            DpiX = DpiY = 96.0f; // default Windows-like value
            Target = image;
        }

        protected readonly IToolkitImage Target;
        private Graphics? _baseGraphics;

        public void Dispose()
        {
        }

        public IToolkit Toolkit { get; }
        public CompositingMode CompositingMode { get; set; }
        public CompositingQuality CompositingQuality { get; set; }
        public float DpiX { get; }
        public float DpiY { get; }
        public InterpolationMode InterpolationMode { get; set; }
        public PixelOffsetMode PixelOffsetMode { get; set; }
        public Point RenderingOrigin { get; set; }
        public SmoothingMode SmoothingMode { get; set; }
        public int TextContrast { get; set; }
        public TextRenderingHint TextRenderingHint { get; set; }
        
        public PortableFont? CurrentFont { get; set; }
        
        public IToolkitPen? Pen { get; set; }
        public IToolkitBrush? Brush { get; set; }
        public IToolkitFont? Font { get; set; }


        public void Clear(Color color)
        {
            var frame = Target.Frame();
            if (frame == null) return;
            var c = color.ToArgb();

            // draw the first scan the hard way, then copy it
            for (int i = 0; i < frame.width; i++) frame.SetPixel(i, 0, c);
            for (var i = 1; i < frame.height; i++) frame.SetScanLine(i, frame.data);
        }

        public void DrawLine(int x1, int y1, int x2, int y2)
        {
            if (Pen == null) return;
            var frame = Target.Frame();
            if (frame == null) return;
            
            var color = Pen.Pen.Color.ToArgb();
            
            // Very, very stupid line drawing
            var adx = (float)Math.Abs(x1 - x2);
            var ady = (float)Math.Abs(y1 - y2);
            var mad = Math.Max(adx, ady);

            if (adx == 0 && ady == 0)
            {
                frame.SetPixel(x1, y1, color);
                return;
            }
            
            // in one direction, we will step single pixels. In the other we step fractions
            var dx = (float)Math.Sign(x2 - x1);
            var dy = (float)Math.Sign(y2 - y1);

            if (adx < ady) dx *= adx / ady; // more vertical, sub-step x
            if (adx > ady) dy *= ady / adx; // more horizontal, sub-step y

            float x = x1;
            float y = y1;
            for (int i = 0; i <= mad; i++)
            {
                // world's jankiest anti-aliasing
                var fx = (byte)((x - (int)x) * 255);
                var fy = (byte)((y - (int)y) * 255);
                
                frame.BlendPixel((int)x, (int)y, color, (byte)(255-(fx+fy)));
                frame.BlendPixel((int)x+1, (int)y, color, fx);
                frame.BlendPixel((int)x, (int)y+1, color, fy);
                
                x += dx;
                y += dy;
            }
        }

        public void DrawLines(Point[] points)
        {
            if (points.Length < 2) return;
            var start = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                var end = points[i];
                DrawLine(start.X, start.Y, end.X, end.Y);
                start = end;
            }
        }
        
        public void DrawLinesF(PointF[] points)
        {
            if (points.Length < 2) return;
            var start = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                var end = points[i];
                DrawLine((int)start.X, (int)start.Y, (int)end.X, (int)end.Y);
                start = end;
            }
        }

        public void DrawPolygon(Point[] points)
        {
            DrawLines(points);
            if (points.Length > 2) // draw 'closing' stroke
            {
                var end = points[points.Length-1];
                DrawLine(points[0].X, points[0].Y, end.X, end.Y);
            }
        }
        
        public void FillPolygon(PointF[] points, FillMode fillMode)
        {
            if (Brush == null) return;
            var frame = Target.Frame();
            if (frame == null) return;
            
            var color = Brush.Color.ToArgb();
            
            var spans = fillMode == FillMode.Winding
                ? PortableRasteriser.GetNonZeroWindingSpans(points)
                : PortableRasteriser.GetEvenOddSpans(points);

            foreach (var span in spans)
            {
                frame.SetSpan(span.Y, span.Left, span.Right, color);
            }
        }

        public void FillPolygon(Point[] points, FillMode fillMode)
        {
            if (Brush == null) return;
            var frame = Target.Frame();
            if (frame == null) return;
            
            var color = Brush.Color.ToArgb();
            
            var spans = fillMode == FillMode.Winding ? PortableRasteriser.GetNonZeroWindingSpans(points) : PortableRasteriser.GetEvenOddSpans(points);

            foreach (var span in spans)
            {
                frame.SetSpan(span.Y, span.Left, span.Right, color);
            }
        }

        public void DrawBezier(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            throw new NotImplementedException();
        }

        public void FillBezier(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4, FillMode fillMode)
        {
            throw new NotImplementedException();
        }

        public void DrawArc(Point[] rect, float startAngle, float sweepAngle)
        {
            throw new NotImplementedException();
        }

        public void DrawPie(Point[] rect, float startAngle, float sweepAngle)
        {
            throw new NotImplementedException();
        }

        public void FillPie(Point[] rect, float startAngle, float sweepAngle)
        {
            throw new NotImplementedException();
        }

        public void DrawClosedCurve(Point[] points, float tension)
        {
            throw new NotImplementedException();
        }

        public void FillClosedCurve(Point[] points, float tension, FillMode fillMode)
        {
            throw new NotImplementedException();
        }

        public void DrawCurve(Point[] points, int offset, int numberOfSegments, float tension)
        {
            throw new NotImplementedException();
        }

        public void DrawString(string s, int x, int y, StringFormat format)
        {
            if (Font == null) throw new Exception("Tried to draw string with no font set");
            if (!(Font is PortableFont pf)) throw new Exception($"Need to draw using font def: {Font.GetType().FullName}");
            
            var frame = Target.Frame();
            if (frame == null) return;
            
            Pen = new PortablePen(new Pen(Color.Black, 1.0f));// TODO: pick from a real parameter
            var font = pf.BaseTrueTypeFont;
            
            // TODO: Render from the proper drawing library.
            
            var scale = pf.GetScale();
            
            double xOff = x;
            foreach (char c in s)
            {
                var gl = font.ReadGlyph(c);
                if (gl == null) continue;
                
                xOff += gl.xMin * scale;
                var yAdj = gl.yMin * scale;
                var outline = gl.NormalisedContours(); // break into simple lines
                
                // not doing fill, just trace outline for now
                //OutlineGlyph(y, outline, xOff, scale, yAdj);
                
                FillGlyph(y, outline, xOff, scale, yAdj);
                
                // TODO: apply transforms to this? Or allow passing of pre-converted contours?
                //GlyphRenderer.DrawGlyph(frame, (float)xOff, y, (float)scale, gl, 0);
                xOff += (gl.xMax - gl.xMin) * scale;
            }
        }
        
        private void FillGlyph(int y, List<GlyphPoint[]> outline, double xOff, double scale, double yAdj)
        {
            var points = new List<PointF>();
            foreach (var curve in outline)
            {
                points.AddRange(curve.Select(p => new PointF(
                    (float) (xOff + p.X * scale),
                    (float) (y + p.Y * scale + yAdj)
                )));
            }

            
            if (_baseGraphics != null) _baseGraphics?.FillPolygon(Pen.Pen.Brush, points.ToArray(), FillMode.Winding); // this applies the transform before dropping back here to draw
            else FillPolygon(points.ToArray(), FillMode.Winding);
        }

        private void OutlineGlyph(int y, List<GlyphPoint[]> outline, double xOff, double scale, double yAdj)
        {
            foreach (var curve in outline)
            {
                var points = curve.Select(p => new PointF(
                    (float) (xOff + p.X * scale),
                    (float) (y + p.Y * scale + yAdj)
                    //(int)(y + (0-p.Y)*scale - yAdj) // Y is flipped
                )).ToArray();

                if (_baseGraphics != null) _baseGraphics?.DrawLines(Pen.Pen, points); // this applies the transform before dropping back here to draw
                else DrawLinesF(points);

                //break; // just the first contour?
            }
        }

        public void DrawString(string s, Point[] layoutRectangle, StringFormat format)
        {
            throw new NotImplementedException();
        }

        public Size MeasureString(string s, Point[] layoutRectangle, StringFormat format, out int charactersFitted, out int linesFilled, bool ascentOnly)
        {
            if (Font == null) throw new Exception("Tried to measure string with no font set");
            if (!(Font is PortableFont pf)) throw new Exception($"Need to measure using font def: {Font.GetType().FullName}");
            
            var font = pf.BaseTrueTypeFont;
            charactersFitted = 0;
            linesFilled = 1;
            
            var scale = pf.GetScale();

            // Really dumb way to start -- not using layout rect, just count up the character sizes.
            // when we do implement it, make sure DrawString uses the same logic
            var maxHeight = 0.0;
            var width = 0.0;
            foreach (char c in s)
            {
                var gl = font.ReadGlyph(c);
                if (gl == null) continue;
                
                charactersFitted++;
                var h = (gl.yMax - gl.yMin) * scale;
                var w = (gl.xMax - gl.xMin) * scale;
                maxHeight = Math.Max(h, maxHeight);
                width += w;
            }
            return new Size((int)width, (int)maxHeight);
        }

        public void Flush(FlushIntention intention)
        {
            throw new NotImplementedException();
        }

        public Color GetNearestColor(Color color)
        {
            throw new NotImplementedException();
        }

        public void AddMetafileComment(byte[] data)
        {
            throw new NotImplementedException();
        }

        public IntPtr GetHdc()
        {
            throw new NotImplementedException();
        }

        public void ReleaseHdc(IntPtr hdc)
        {
            throw new NotImplementedException();
        }

        public void SetClipEmpty()
        {
            throw new NotImplementedException();
        }

        public void SetClipInfinite()
        {
            throw new NotImplementedException();
        }

        public void SetClipRect(int x, int y, int width, int height)
        {
            throw new NotImplementedException();
        }

        public void SetClipRects(Rectangle[] rects)
        {
            throw new NotImplementedException();
        }

        public void SetClipMask(object mask, int topx, int topy)
        {
            throw new NotImplementedException();
        }

        public int GetLineSpacing()
        {
            return Font is PortableFont pf ? pf.GetLineHeight() : 10;
        }

        public void DrawImage(IToolkitImage image, int x, int y)
        {
            throw new NotImplementedException();
        }

        public void DrawImage(IToolkitImage image, Point[] src, Point[] dest)
        {
            throw new NotImplementedException();
        }

        public void DrawGlyph(int x, int y, byte[] bits, int bitsWidth, int bitsHeight, Color color)
        {
            throw new NotImplementedException();
        }

        public void BindTo(Graphics graphics)
        {
            _baseGraphics = graphics;
        }
    }
}