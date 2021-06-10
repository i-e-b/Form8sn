using System;
using Portable.Drawing.Drawing2D;
using Portable.Drawing.Imaging.ImageFormats;
using Portable.Drawing.Text;

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
        public PortableFont CurrentFont { get; set; }
        
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
                frame.SetPixel((int)x, (int)y, color);
                x += dx;
                y += dy;
            }
        }

        public void DrawLines(Point[] points)
        {
            throw new NotImplementedException();
        }

        public void DrawPolygon(Point[] points)
        {
            throw new NotImplementedException();
        }

        public void FillPolygon(Point[] points, FillMode fillMode)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void DrawString(string s, Point[] layoutRectangle, StringFormat format)
        {
            throw new NotImplementedException();
        }

        public Size MeasureString(string s, Point[] layoutRectangle, StringFormat format, out int charactersFitted, out int linesFilled, bool ascentOnly)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
    }
}