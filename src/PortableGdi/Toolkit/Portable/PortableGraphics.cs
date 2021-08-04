using System;
using System.Collections.Generic;
using System.Linq;
using Portable.Drawing.Drawing2D;
using Portable.Drawing.Imaging.ImageFormats;
using Portable.Drawing.Text;
using Portable.Drawing.Toolkit.Fonts;
using Portable.Drawing.Toolkit.Portable.Rasteriser;

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
            
            var thickness = Pen.Pen.Width;
            var color = Pen.Pen.Color.ToArgb();
            
            // Very, very stupid line drawing
            SdfDraw.DrawLine(frame, thickness, x1, y1, x2, y2, color);
        }
        
        public void DrawLine(float x1, float y1, float x2, float y2)
        {
            if (Pen == null) return;
            var frame = Target.Frame();
            if (frame == null) return;
            
            var thickness = Pen.Pen.Width;
            var color = Pen.Pen.Color.ToArgb();
            
            // Very, very stupid line drawing
            SdfDraw.DrawLine(frame, thickness, x1, y1, x2, y2, color);
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
                DrawLine(start.X, start.Y, end.X, end.Y);
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
            
            SdfDraw.FillPolygon(frame, points, color, fillMode);
        }

        public void FillPolygon(Point[] points, FillMode fillMode)
        {
            FillPolygon(points.Select(p=>(PointF)p).ToArray(), fillMode);
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
            throw new NotImplementedException($"{nameof(Graphics)} should be calling more specific call: {nameof(FillGlyphs)}");
            /*
            if (Font == null) throw new Exception("Tried to draw string with no font set");
            if (!(Font is PortableFont pf)) throw new Exception($"Need to draw using font def: {Font.GetType().FullName}");
            
            var frame = Target.Frame();
            if (frame == null) return;
            
            var font = pf.BaseTrueTypeFont;
            
            var scale = pf.GetScale();
            
            // adjustment to have (x,y) be the top-left, rather than baseline-left
            //var ry = y + font.Height() * scale;
            var ry = y + pf.GetLineHeight();
            
            // TODO: need to move measurements up, so that the transform matrix is correctly applied
            double xOff = x; // advance for each character (not correct yet)
            foreach (char c in s)
            {
                var gl = font.ReadGlyph(c);
                if (gl == null) continue;
                
                var bearing = gl.LeftBearing * scale;
                var yAdj = gl.yMin * scale;
                var outline = gl.NormalisedContours(); // break into simple lines

                FillGlyph(ry, outline, xOff + bearing, scale, yAdj);
                
                xOff += gl.Advance * scale;
            }*/
        }
        
        public void FillGlyphs(IEnumerable<RenderableGlyph> positionedGlyphs, Brush brush)
        {
            if (Brush == null) return;
            var frame = Target.Frame();
            if (frame == null) return;
            var color = brush.GetBrush(Toolkit).Color.ToArgb();
            
            foreach (var glyph in positionedGlyphs)
            {
                SdfDraw.FillPolygon(frame, glyph.ToArray(), color, FillMode.Winding);
            }
        }
        
        public void DrawString(string s, Point[] layoutRectangle, StringFormat format)
        {
            throw new NotImplementedException();
        }

        public Size MeasureString(string s, Point[]? layoutRectangle, StringFormat? format, out int charactersFitted, out int linesFilled, bool ascentOnly)
        {
            // TODO: IEB: Merge this with `TextLayoutManager`?
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
                
                // TODO: need vertical advance in Glyph
                
                charactersFitted++;
                var h = (gl.yMax - gl.yMin) * scale;
                var w = (gl.xMax - gl.xMin) * scale;
                maxHeight = Math.Max(h, maxHeight);
                width += w;
            }
            return new Size((int)width, (int)(maxHeight*1.8)); // 1.8 is a fudge until we have vertical metrics
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