using System;
using System.Drawing;
using Portable.Drawing.Drawing2D;
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
            Toolkit = parent;
            DpiX = DpiY = 96.0f; // default Windows-like value
        }
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

        public void Clear(Color color)
        {
            throw new NotImplementedException();
        }

        public void DrawLine(int x1, int y1, int x2, int y2)
        {
            throw new NotImplementedException();
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