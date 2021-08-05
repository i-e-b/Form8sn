/*
 * Graphics.cs - Implementation of the "System.Drawing.Graphics" class.
 *
 * Copyright (C) 2003  Southern Storm Software, Pty Ltd.
 * Copyright (C) 2004  Free Software Foundation, Inc.
 * Contributions from Thomas Fritzsche <tf@noto.de>.
 * Contributions from Neil Cawse.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Portable.Drawing.Drawing2D;
using Portable.Drawing.Imaging;
using Portable.Drawing.Text;
using Portable.Drawing.Toolkit;
using Portable.Drawing.Toolkit.Portable.Rasteriser;
using Portable.Drawing.Toolkit.TextLayout;

namespace Portable.Drawing
{
    internal enum Transforms
    {
        None = 0,
        Apply = 1
    }

    [ComVisible(false)]
    public sealed class Graphics : MarshalByRefObject, IDisposable
    {
        // Internal state.
        private IToolkitGraphics? _graphics;
        private Region? _clip;
        internal Matrix? transform;
        internal GraphicsContainer? stackTop;

        internal static Graphics defaultGraphics;

        // The window this graphics represents overlying the IToolkitGraphics
        internal Rectangle baseWindow;

        // The current clip in device coordinates - useful when deciding if something
        // is out of bounds, limiting the need to call the toolkit.
        private Rectangle deviceClipExtent;

        // Default DPI for the screen.
        internal const float DefaultScreenDpi = 96.0f;

        // Constructor.
        internal Graphics(IToolkitGraphics graphics)
        {
            _graphics = graphics;
            _graphics.BindTo(this);
            _clip = null;
            transform = null;
            PageScale = 1.0f;
            PageUnit = GraphicsUnit.World;
            stackTop = null;
            baseWindow = Rectangle.Empty;
        }

        // Window Constructor. Copies the existing Graphics and creates a new
        // Graphics that has an origin of baseWindow.Location and is always clipped
        // to baseWindow

        internal Graphics(IToolkitGraphics graphics, Rectangle baseWindow)
            : this(graphics)
        {
            this.baseWindow = baseWindow;
            _graphics = graphics;
            _graphics.BindTo(this);
            
            _clip = new Region(baseWindow);
            _clip.Translate(-baseWindow.X, -baseWindow.Y);
            Clip = _clip;
        }

        // Create a Graphics that is internally offset to baseWindow
        internal Graphics(Graphics graphics, Rectangle baseWindow)
        {
            // Use the same toolkit
            _graphics = graphics._graphics;
            _graphics?.BindTo(this);
            if (graphics._clip != null)
            {
                _clip = graphics._clip.Clone();
                _clip.Intersect(baseWindow);
            }
            else
                _clip = new Region(baseWindow);

            // Find out what the clip is with our new Origin
            _clip.Translate(-baseWindow.X, -baseWindow.Y);
            this.baseWindow = baseWindow;
            if (graphics.transform != null)
                transform = new Matrix(graphics.transform);
            PageScale = graphics.PageScale;
            PageUnit = graphics.PageUnit;
            stackTop = null;
            Clip = _clip;
        }


        // Destructor.
        ~Graphics()
        {
            Dispose(false);
        }

        // Dispose of this object.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            lock (this)
            {
                if (_graphics != null)
                {
                    _graphics.Dispose();
                    _graphics = null;
                }

                if (_clip != null)
                {
                    _clip.Dispose();
                    _clip = null;
                }
            }
        }

        // Get or set this object's properties.
        public Region Clip
        {
            get => _clip ??= new Region();
            set => SetClip(value, CombineMode.Replace);
        }

        public RectangleF ClipBounds => Clip.GetBounds(this);

        public CompositingMode CompositingMode
        {
            get
            {
                lock (this)
                {
                    return _graphics?.CompositingMode ?? CompositingMode.SourceOver;
                }
            }
            set
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        _graphics.CompositingMode = value;
                    }
                }
            }
        }

        public CompositingQuality CompositingQuality
        {
            get
            {
                lock (this)
                {
                    return _graphics?.CompositingQuality ?? CompositingQuality.Default;
                }
            }
            set
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        _graphics.CompositingQuality = value;
                    }
                }
            }
        }

        public float DpiX
        {
            get
            {
                lock (this)
                {
                    return _graphics?.DpiX ?? DefaultScreenDpi;
                }
            }
        }

        public float DpiY
        {
            get
            {
                lock (this)
                {
                    return _graphics?.DpiY ?? DefaultScreenDpi;
                }
            }
        }

        public InterpolationMode InterpolationMode
        {
            get
            {
                lock (this)
                {
                    return _graphics?.InterpolationMode ?? InterpolationMode.Default;
                }
            }
            set
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        _graphics.InterpolationMode = value;
                    }
                }
            }
        }

        public bool IsClipEmpty => (_clip != null && _clip.IsEmpty(this));

        public bool IsVisibleClipEmpty
        {
            get
            {
                var clip = VisibleClipBounds;
                return (clip.Width <= 0.0f && clip.Height <= 0.0f);
            }
        }

        public float PageScale { get; set; }

        public GraphicsUnit PageUnit { get; set; }

        public PixelOffsetMode PixelOffsetMode
        {
            get
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        return _graphics.PixelOffsetMode;
                    }
                    else
                    {
                        return PixelOffsetMode.Default;
                    }
                }
            }
            set
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        _graphics.PixelOffsetMode = value;
                    }
                }
            }
        }

        public Point RenderingOrigin
        {
            get
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        return _graphics.RenderingOrigin;
                    }
                    else
                    {
                        return new Point(0, 0);
                    }
                }
            }
            set
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        _graphics.RenderingOrigin = value;
                    }
                }
            }
        }

        public SmoothingMode SmoothingMode
        {
            get
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        return _graphics.SmoothingMode;
                    }
                    else
                    {
                        return SmoothingMode.Default;
                    }
                }
            }
            set
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        _graphics.SmoothingMode = value;
                    }
                }
            }
        }

        public int TextContrast
        {
            get
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        return _graphics.TextContrast;
                    }
                    else
                    {
                        return 4;
                    }
                }
            }
            set
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        _graphics.TextContrast = value;
                    }
                }
            }
        }

        public TextRenderingHint TextRenderingHint
        {
            get
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        return _graphics.TextRenderingHint;
                    }
                    else
                    {
                        return TextRenderingHint.SystemDefault;
                    }
                }
            }
            set
            {
                lock (this)
                {
                    if (_graphics != null)
                    {
                        _graphics.TextRenderingHint = value;
                    }
                }
            }
        }

        public Matrix Transform
        {
            get
            {
                lock (this)
                {
                    if (transform == null)
                    {
                        transform = new Matrix();
                    }

                    // return a copy instead of the original
                    return new Matrix(transform);
                }
            }
            set
            {
                lock (this)
                {
                    if (value == null || value.IsIdentity)
                    {
                        transform = null;
                    }
                    else
                    {
                        // Copy the given Matrix, do not work on it directly
                        transform = new Matrix(value);
                    }
                }
            }
        }

        public RectangleF VisibleClipBounds
        {
            get
            {
                if (_graphics != null)
                {
                    PointF bottomRight = new PointF(baseWindow.Width, baseWindow.Height);
                    PointF[] coords = new PointF[] {PointF.Empty, bottomRight};
                    TransformPoints(CoordinateSpace.World, CoordinateSpace.Device, coords);
                    return RectangleF.FromLTRB(coords[0].X, coords[0].Y, coords[1].X, coords[1].Y);
                }
                else
                {
                    return RectangleF.Empty;
                }
            }
        }

        // Add a metafile comment.
        public void AddMetafileComment(byte[] data)
        {
            lock (this)
            {
                if (_graphics != null)
                {
                    _graphics.AddMetafileComment(data);
                }
            }
        }

        // Save the current contents of the graphics context in a container.
        public GraphicsContainer BeginContainer()
        {
            lock (this)
            {
                return new GraphicsContainer(this);
            }
        }

        public GraphicsContainer BeginContainer(Rectangle dstRect,
            Rectangle srcRect,
            GraphicsUnit unit)
        {
            return BeginContainer((RectangleF) dstRect,
                (RectangleF) srcRect, unit);
        }

        public GraphicsContainer BeginContainer(RectangleF dstRect,
            RectangleF srcRect,
            GraphicsUnit unit)
        {
            // Save the current state of the context.
            GraphicsContainer container = BeginContainer();

            // Modify the context information appropriately.
            // TODO

            // Return the saved information to the caller.
            return container;
        }

        // Clear the entire drawing surface.
        public void Clear(Color color)
        {
            ToolkitGraphics.Clear(color);
        }

        // Draw an arc.
        public void DrawArc(Pen pen, Rectangle rect,
            float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height,
                startAngle, sweepAngle);
        }

        public void DrawArc(Pen pen, RectangleF rect,
            float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height,
                startAngle, sweepAngle);
        }

        public void DrawArc(Pen pen, int x, int y, int width, int height,
            float startAngle, float sweepAngle)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            // if transform is set use graphics path to draw or fill, since the transformation works for this then.
            if (null != transform)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddArc(x, y, width, height, startAngle, sweepAngle);
                DrawPath(pen, path);
            }
            else
            {
                Point[] rect = ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit);
                lock (this)
                {
                    SelectPen(pen);
                    ToolkitGraphics.DrawArc(rect, startAngle, sweepAngle);
                }
            }
        }

        public void DrawArc(Pen pen, float x, float y, float width, float height,
            float startAngle, float sweepAngle)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            // if transform is set use graphics path to draw or fill, since the transformation works for this then.
            if (null != transform)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddArc(x, y, width, height, startAngle, sweepAngle);
                DrawPath(pen, path);
            }
            else
            {
                Point[] rect = ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit);
                lock (this)
                {
                    SelectPen(pen);
                    ToolkitGraphics.DrawArc(rect, startAngle, sweepAngle);
                }
            }
        }

        // Draw a Bezier spline.
        public void DrawBezier(Pen pen, Point pt1, Point pt2,
            Point pt3, Point pt4)
        {
            DrawBezier(pen, (float) (pt1.X), (float) (pt1.Y),
                (float) (pt2.X), (float) (pt2.Y),
                (float) (pt3.X), (float) (pt3.Y),
                (float) (pt4.X), (float) (pt4.Y));
        }

        public void DrawBezier(Pen pen, PointF pt1, PointF pt2,
            PointF pt3, PointF pt4)
        {
            DrawBezier(pen, pt1.X, pt1.Y, pt2.X, pt2.Y,
                pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        public void DrawBezier(Pen pen, float x1, float y1, float x2, float y2,
            float x3, float y3, float x4, float y4)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            int dx1, dy1, dx2, dy2;
            int dx3, dy3, dx4, dy4;
            ConvertPoint(x1 + baseWindow.X, y1 + baseWindow.Y, out dx1, out dy1, PageUnit);
            ConvertPoint(x2 + baseWindow.X, y2 + baseWindow.Y, out dx2, out dy2, PageUnit);
            ConvertPoint(x3 + baseWindow.X, y3 + baseWindow.Y, out dx3, out dy3, PageUnit);
            ConvertPoint(x4 + baseWindow.X, y4 + baseWindow.Y, out dx4, out dy4, PageUnit);
            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawBezier(dx1, dy1, dx2, dy2,
                    dx3, dy3, dx4, dy4);
            }
        }

        // Draw a Bezier spline.
        public void FillBezier(Brush brush, Point pt1, Point pt2,
            Point pt3, Point pt4, FillMode fillMode)
        {
            FillBezier(brush, (float) (pt1.X), (float) (pt1.Y),
                (float) (pt2.X), (float) (pt2.Y),
                (float) (pt3.X), (float) (pt3.Y),
                (float) (pt4.X), (float) (pt4.Y), fillMode);
        }

        public void FillBezier(Brush brush, PointF pt1, PointF pt2,
            PointF pt3, PointF pt4, FillMode fillMode)
        {
            FillBezier(brush, pt1.X, pt1.Y, pt2.X, pt2.Y,
                pt3.X, pt3.Y, pt4.X, pt4.Y, fillMode);
        }

        public void FillBezier(Brush brush, float x1, float y1, float x2, float y2,
            float x3, float y3, float x4, float y4, FillMode fillMode)
        {
            int dx1, dy1, dx2, dy2;
            int dx3, dy3, dx4, dy4;
            ConvertPoint(x1 + baseWindow.X, y1 + baseWindow.Y, out dx1, out dy1, PageUnit);
            ConvertPoint(x2 + baseWindow.X, y2 + baseWindow.Y, out dx2, out dy2, PageUnit);
            ConvertPoint(x3 + baseWindow.X, y3 + baseWindow.Y, out dx3, out dy3, PageUnit);
            ConvertPoint(x4 + baseWindow.X, y4 + baseWindow.Y, out dx4, out dy4, PageUnit);
            lock (this)
            {
                SelectBrush(brush);
                ToolkitGraphics.DrawBezier(dx1, dy1, dx2, dy2,
                    dx3, dy3, dx4, dy4);
                ToolkitGraphics.FillBezier(dx1, dy1, dx2, dy2,
                    dx3, dy3, dx4, dy4, fillMode);
            }
        }


        // Draw a series of Bezier splines.
        public void DrawBeziers(Pen pen, Point[] points)
        {
            if (pen == null)
            {
                throw new ArgumentNullException("pen");
            }

            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            int posn = 0;
            while ((posn + 4) <= points.Length)
            {
                DrawBezier(pen, points[posn], points[posn + 1],
                    points[posn + 2], points[posn + 3]);
                posn += 3;
            }
        }

        public void DrawBeziers(Pen pen, PointF[] points)
        {
            if (pen == null)
            {
                throw new ArgumentNullException("pen");
            }

            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            int posn = 0;
            while ((posn + 4) <= points.Length)
            {
                DrawBezier(pen, points[posn], points[posn + 1],
                    points[posn + 2], points[posn + 3]);
                posn += 3;
            }
        }

        // Draw a closed cardinal spline.
        public void DrawClosedCurve(Pen pen, Point[] points)
        {
            DrawClosedCurve(pen, points, 0.5f, FillMode.Alternate);
        }

        public void DrawClosedCurve(Pen pen, PointF[] points)
        {
            DrawClosedCurve(pen, points, 0.5f, FillMode.Alternate);
        }

        public void DrawClosedCurve(Pen pen, Point[] points,
            float tension, FillMode fillMode)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            points = ConvertPoints(points, 4, PageUnit);
            BaseOffsetPoints(points);
            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawClosedCurve(points, tension);
            }
        }

        public void DrawClosedCurve(Pen pen, PointF[] points,
            float tension, FillMode fillMode)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            Point[] dpoints = ConvertPoints(points, 4, PageUnit);
            BaseOffsetPoints(dpoints);
            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawClosedCurve(dpoints, tension);
            }
        }

        // Draw a cardinal spline.
        public void DrawCurve(Pen pen, Point[] points)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            DrawCurve(pen, points, 0, points.Length - 1, 0.5f);
        }

        public void DrawCurve(Pen pen, PointF[] points)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            DrawCurve(pen, points, 0, points.Length - 1, 0.5f);
        }

        public void DrawCurve(Pen pen, Point[] points, float tension)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            DrawCurve(pen, points, 0, points.Length - 1, tension);
        }

        public void DrawCurve(Pen pen, PointF[] points, float tension)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            DrawCurve(pen, points, 0, points.Length - 1, tension);
        }

        public void DrawCurve(Pen pen, Point[] points,
            int offset, int numberOfSegments)
        {
            DrawCurve(pen, points, offset, numberOfSegments, 0.5f);
        }

        public void DrawCurve(Pen pen, PointF[] points,
            int offset, int numberOfSegments)
        {
            DrawCurve(pen, points, offset, numberOfSegments, 0.5f);
        }

        public void DrawCurve(Pen pen, Point[] points,
            int offset, int numberOfSegments, float tension)
        {
            points = ConvertPoints(points, 4, PageUnit);
            BaseOffsetPoints(points);
            if (offset < 0 || offset >= (points.Length - 1))
            {
                throw new ArgumentOutOfRangeException
                    (nameof(offset), "Arg_InvalidCurveOffset");
            }

            if (numberOfSegments < 1 ||
                (offset + numberOfSegments) >= points.Length)
            {
                throw new ArgumentOutOfRangeException
                    (nameof(numberOfSegments), "Arg_InvalidCurveSegments");
            }

            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }


            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawCurve
                    (points, offset, numberOfSegments, tension);
            }
        }

        public void DrawCurve(Pen pen, PointF[] points,
            int offset, int numberOfSegments, float tension)
        {
            Point[] dpoints = ConvertPoints(points, 4, PageUnit);
            BaseOffsetPoints(dpoints);
            if (offset < 0 || offset >= (points.Length - 1))
            {
                throw new ArgumentOutOfRangeException
                    (nameof(offset), "Arg_InvalidCurveOffset");
            }

            if (numberOfSegments < 1 ||
                (offset + numberOfSegments) >= points.Length)
            {
                throw new ArgumentOutOfRangeException
                    (nameof(numberOfSegments), "Arg_InvalidCurveSegments");
            }

            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawCurve
                    (dpoints, offset, numberOfSegments, tension);
            }
        }

        // Draw an ellipse.
        public void DrawEllipse(Pen pen, Rectangle rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawEllipse(Pen pen, RectangleF rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawEllipse(Pen pen, int x, int y, int width, int height)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            // if transform is set use graphics path to draw or fill, since the transformation works for this then.
            if (null != transform)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(x, y, width, height);
                DrawPath(pen, path);
            }
            else
            {
                Point[] rect = ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit);
                lock (this)
                {
                    SelectPen(pen);
                    ToolkitGraphics.DrawArc(rect, 0.0f, 360.0f);
                }
            }
        }

        public void DrawEllipse(Pen pen, float x, float y,
            float width, float height)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            // if transform is set use graphics path to draw or fill, since the transformation works for this then.
            if (null != transform)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(x, y, width, height);
                DrawPath(pen, path);
            }
            else
            {
                Point[] rect = ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit);
                lock (this)
                {
                    SelectPen(pen);
                    ToolkitGraphics.DrawArc(rect, 0.0f, 360.0f);
                }
            }
        }

        // Draw an unstretched complete icon via the toolkit.
        private void ToolkitDrawIcon(Icon icon, int x, int y)
        {
            lock (this)
            {
                IToolkitImage toolkitImage = icon.GetToolkitImage(this);
                if (toolkitImage != null)
                {
                    ConvertPoint(ref x, ref y, PageUnit);
                    ToolkitGraphics.DrawImage
                    (toolkitImage, x + baseWindow.X,
                        y + baseWindow.Y);
                }
            }
        }

        // Draw an icon.
        public void DrawIcon(Icon icon, Rectangle targetRect)
        {
            if (icon == null)
            {
                throw new ArgumentNullException("icon");
            }

            if (targetRect.Width == icon.Width &&
                targetRect.Height == icon.Height)
            {
                // This is the easy case.
                ToolkitDrawIcon(icon, targetRect.X, targetRect.Y);
            }
            else
            {
                // Stretch and draw the image.
                Point[] src = ConvertUnits
                    (0, 0, icon.Width, icon.Height, GraphicsUnit.Pixel);
                Point[] dest = ConvertRectangle3
                (targetRect.X, targetRect.Y,
                    targetRect.Width, targetRect.Height, PageUnit);
                BaseOffsetPoints(dest);
                lock (this)
                {
                    IToolkitImage toolkitImage = icon.GetToolkitImage(this);
                    if (toolkitImage != null)
                    {
                        ToolkitGraphics.DrawImage
                            (toolkitImage, src, dest);
                    }
                }
            }
        }

        public void DrawIconUnstretched(Icon icon, Rectangle targetRect)
        {
            if (icon == null)
            {
                throw new ArgumentNullException("icon");
            }

            int width = icon.Width;
            int height = icon.Height;
            if (width <= targetRect.Width && height <= targetRect.Height)
            {
                // No truncation is necessary, so use the easy case.
                ToolkitDrawIcon(icon, targetRect.X, targetRect.Y);
            }
            else
            {
                // Only draw as much as will fit in the target rectangle.
                if (width > targetRect.Width)
                {
                    width = targetRect.Width;
                }

                if (height > targetRect.Height)
                {
                    height = targetRect.Height;
                }

                Point[] src = ConvertUnits
                    (0, 0, width, height, GraphicsUnit.Pixel);
                Point[] dest = ConvertRectangle3
                    (targetRect.X, targetRect.Y, width, height, PageUnit);
                BaseOffsetPoints(dest);
                lock (this)
                {
                    IToolkitImage toolkitImage = icon.GetToolkitImage(this);
                    if (toolkitImage != null)
                    {
                        ToolkitGraphics.DrawImage
                            (toolkitImage, src, dest);
                    }
                }
            }
        }

        public void DrawIcon(Icon icon, int x, int y)
        {
            if (icon == null)
            {
                throw new ArgumentNullException("icon");
            }

            ToolkitDrawIcon(icon, x, y);
        }

        // Used to call the toolkit DrawImage methods
        private void ToolkitDrawImage(Image image, Point[] src, Point[] dest)
        {
            if (image.toolkitImage == null)
                image.toolkitImage = ToolkitGraphics.Toolkit.CreateImage(image.dgImage, 0);
            BaseOffsetPoints(dest);
            ToolkitGraphics.DrawImage(image.toolkitImage, src, dest);
        }

        private void ToolkitDrawImage(Image image, int x, int y)
        {
            if (image.toolkitImage == null)
                image.toolkitImage = ToolkitGraphics.Toolkit.CreateImage(image.dgImage, 0);
            ToolkitGraphics.DrawImage(image.toolkitImage, x + baseWindow.X, y + baseWindow.Y);
        }

        // Draw an image.
        public void DrawImage(Image image, Point point)
        {
            DrawImage(image, point.X, point.Y);
        }

        public void DrawImage(Image image, Point[] destPoints)
        {
            Point[] src = ConvertUnits(0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
            Point[] dest = ConvertPoints(destPoints, 3, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, PointF point)
        {
            DrawImage(image, point.X, point.Y);
        }

        public void DrawImage(Image image, PointF[] destPoints)
        {
            Point[] src = ConvertUnits(0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
            Point[] dest = ConvertPoints(destPoints, 3, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, Rectangle rect)
        {
            Point[] src = ConvertUnits(0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
            Point[] dest = ConvertRectangle3(rect.X, rect.Y, rect.Width, rect.Height, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, RectangleF rect)
        {
            Point[] src = ConvertUnits(0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
            Point[] dest = ConvertRectangle3(rect.X, rect.Y, rect.Width, rect.Height, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, int x, int y)
        {
            lock (this)
            {
                ConvertPoint(ref x, ref y, PageUnit);
                ToolkitDrawImage(image, x, y);
            }
        }

        public void DrawImage(Image image, float x, float y)
        {
            int dx, dy;
            ConvertPoint(x, y, out dx, out dy, PageUnit);
            ToolkitDrawImage(image, dx, dy);
        }

        public void DrawImage(Image image, Point[] destPoints,
            Rectangle srcRect, GraphicsUnit srcUnit)
        {
            Point[] src = ConvertUnits(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, srcUnit);
            Point[] dest = ConvertPoints(destPoints, 3, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, PointF[] destPoints,
            RectangleF srcRect, GraphicsUnit srcUnit)
        {
            Point[] src = ConvertUnits(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, srcUnit);
            Point[] dest = ConvertPoints(destPoints, 3, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, Rectangle destRect,
            Rectangle srcRect, GraphicsUnit srcUnit)
        {
            Point[] src = ConvertUnits(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, srcUnit);
            Point[] dest = ConvertRectangle3(destRect.X, destRect.Y, destRect.Width, destRect.Height, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, RectangleF destRect,
            RectangleF srcRect, GraphicsUnit srcUnit)
        {
            Point[] src = ConvertUnits(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, srcUnit);
            Point[] dest = ConvertRectangle3(destRect.X, destRect.Y, destRect.Width, destRect.Height, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, int x, int y, int width, int height)
        {
            Point[] src = ConvertUnits(0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
            Point[] dest = ConvertRectangle3(x, y, width, height, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, int x, int y,
            Rectangle srcRect, GraphicsUnit srcUnit)
        {
            Point[] src = ConvertUnits(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, srcUnit);
            Point[] dest = ConvertRectangle3(x, y, 0, 0, PageUnit);
            dest[1].X = image.Width;
            dest[2].Y = image.Height;
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, float x, float y,
            RectangleF srcRect, GraphicsUnit srcUnit)
        {
            Point[] src = ConvertUnits(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, srcUnit);
            Point[] dest = ConvertRectangle3(x, y, srcRect.Width, srcRect.Height, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, float x, float y,
            float width, float height)
        {
            Point[] src = ConvertUnits(0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
            Point[] dest = ConvertRectangle3(x, y, width, height, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, Rectangle destRect,
            int srcX, int srcY, int srcWidth, int srcHeight,
            GraphicsUnit srcUnit)
        {
            Point[] src = ConvertUnits(srcX, srcY, srcWidth, srcHeight, srcUnit);
            Point[] dest = ConvertRectangle3(destRect.X, destRect.Y, destRect.Width, destRect.Height, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        public void DrawImage(Image image, Rectangle destRect,
            float srcX, float srcY,
            float srcWidth, float srcHeight,
            GraphicsUnit srcUnit)
        {
            Point[] src = ConvertUnits(srcX, srcY, srcWidth, srcHeight, srcUnit);
            Point[] dest = ConvertRectangle3(destRect.X, destRect.Y, destRect.Width, destRect.Height, PageUnit);
            ToolkitDrawImage(image, src, dest);
        }

        // We don't use image attributes or image abort procedures in this
        // implementation, so stub out the remaining DrawImage methods.
        public void DrawImage(Image image, Point[] destPoints,
            Rectangle srcRect, GraphicsUnit srcUnit,
            ImageAttributes imageAttr)
        {
            DrawImage(image, destPoints, srcRect, srcUnit);
        }

        public void DrawImage(Image image, PointF[] destPoints,
            RectangleF srcRect, GraphicsUnit srcUnit,
            ImageAttributes imageAttr)
        {
            DrawImage(image, destPoints, srcRect, srcUnit);
        }

        public void DrawImage(Image image, Point[] destPoints,
            Rectangle srcRect, GraphicsUnit srcUnit,
            ImageAttributes imageAttr,
            DrawImageAbort callback)
        {
            DrawImage(image, destPoints, srcRect, srcUnit);
        }

        public void DrawImage(Image image, PointF[] destPoints,
            RectangleF srcRect, GraphicsUnit srcUnit,
            ImageAttributes imageAttr,
            DrawImageAbort callback)
        {
            DrawImage(image, destPoints, srcRect, srcUnit);
        }

        public void DrawImage(Image image, Point[] destPoints,
            Rectangle srcRect, GraphicsUnit srcUnit,
            ImageAttributes imageAttr,
            DrawImageAbort callback, int callbackdata)
        {
            DrawImage(image, destPoints, srcRect, srcUnit);
        }

        public void DrawImage(Image image, PointF[] destPoints,
            RectangleF srcRect, GraphicsUnit srcUnit,
            ImageAttributes imageAttr,
            DrawImageAbort callback, int callbackdata)
        {
            DrawImage(image, destPoints, srcRect, srcUnit);
        }

        public void DrawImage(Image image, Rectangle destRect,
            int srcX, int srcY, int srcWidth, int srcHeight,
            GraphicsUnit srcUnit, ImageAttributes imageAttr)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth,
                srcHeight, srcUnit);
        }

        public void DrawImage(Image image, Rectangle destRect,
            float srcX, float srcY,
            float srcWidth, float srcHeight,
            GraphicsUnit srcUnit, ImageAttributes imageAttr)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth,
                srcHeight, srcUnit);
        }

        public void DrawImage(Image image, Rectangle destRect,
            int srcX, int srcY, int srcWidth, int srcHeight,
            GraphicsUnit srcUnit, ImageAttributes imageAttr,
            DrawImageAbort callback)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth,
                srcHeight, srcUnit);
        }

        public void DrawImage(Image image, Rectangle destRect,
            float srcX, float srcY,
            float srcWidth, float srcHeight,
            GraphicsUnit srcUnit, ImageAttributes imageAttr,
            DrawImageAbort callback)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth,
                srcHeight, srcUnit);
        }

        public void DrawImage(Image image, Rectangle destRect,
            int srcX, int srcY, int srcWidth, int srcHeight,
            GraphicsUnit srcUnit, ImageAttributes imageAttr,
            DrawImageAbort callback, IntPtr callbackData)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth,
                srcHeight, srcUnit);
        }

        public void DrawImage(Image image, Rectangle destRect,
            float srcX, float srcY,
            float srcWidth, float srcHeight,
            GraphicsUnit srcUnit, ImageAttributes imageAttr,
            DrawImageAbort callback, IntPtr callbackData)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth,
                srcHeight, srcUnit);
        }

        // Draw an unscaled image.
        public void DrawImageUnscaled(Image image, Point point)
        {
            DrawImageUnscaled(image, point.X, point.Y);
        }

        public void DrawImageUnscaled(Image image, Rectangle rect)
        {
            DrawImageUnscaled(image, rect.X, rect.Y);
        }

        public void DrawImageUnscaled(Image image, int x, int y,
            int width, int height)
        {
            DrawImageUnscaled(image, x, y);
        }

        public void DrawImageUnscaled(Image image, int x, int y)
        {
            DrawImage(image, x, y);
        }

        // Draw a line between two points.
        public void DrawLine(Pen pen, Point pt1, Point pt2)
        {
            DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y);
        }

        public void DrawLine(Pen pen, PointF pt1, PointF pt2)
        {
            DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y);
        }

        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            ConvertPoint(ref x1, ref y1, PageUnit);
            ConvertPoint(ref x2, ref y2, PageUnit);
            if (x1 == x2 && y1 == y2)
                return;
            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawLine(x1 + baseWindow.X, y1 + baseWindow.Y,
                    x2 + baseWindow.X, y2 + baseWindow.Y);
            }
        }

        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            int dx1, dy1, dx2, dy2;
            ConvertPoint(x1, y1, out dx1, out dy1, PageUnit);
            ConvertPoint(x2, y2, out dx2, out dy2, PageUnit);
            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawLine(dx1 + baseWindow.X, dy1 + baseWindow.Y,
                    dx2 + baseWindow.X, dy2 + baseWindow.Y);
            }
        }

        // Draw a series of connected line segments.
        public void DrawLines(Pen pen, Point[] points)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0) return;
            if (points.Length < 2) return;

            points = ConvertPoints(points, 2, PageUnit);
            BaseOffsetPoints(points);
            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawLines(points);
            }
        }

        public void DrawLines(Pen pen, PointF[] points)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            Point[] dpoints = ConvertPoints(points, 2, PageUnit);
            BaseOffsetPoints(dpoints);
            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawLines(dpoints);
            }
        }

        // Draw a path object.
        public void DrawPath(Pen pen, GraphicsPath path)
        {
            if (pen == null)
            {
                throw new ArgumentNullException("pen");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            path.Draw(this, pen);
        }

        // Draw a pie shape.
        public void DrawPie(Pen pen, Rectangle rect,
            float startAngle, float sweepAngle)
        {
            if (((float) (int) startAngle) == startAngle &&
                ((float) (int) sweepAngle) == sweepAngle)
            {
                DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height,
                    (int) startAngle, (int) sweepAngle);
            }
            else
            {
                DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height,
                    startAngle, sweepAngle);
            }
        }

        public void DrawPie(Pen pen, RectangleF rect,
            float startAngle, float sweepAngle)
        {
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height,
                startAngle, sweepAngle);
        }

        public void DrawPie(Pen pen, int x, int y, int width, int height,
            int startAngle, int sweepAngle)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            Point[] rect = ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit);
            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawPie(rect, startAngle, sweepAngle);
            }
        }

        public void DrawPie(Pen pen, float x, float y, float width, float height,
            float startAngle, float sweepAngle)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            Point[] rect = ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit);
            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawPie(rect, startAngle, sweepAngle);
            }
        }

        // Draw a polygon.
        public void DrawPolygon(Pen pen, Point[] points)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            points = ConvertPoints(points, 2, PageUnit);
            BaseOffsetPoints(points);
            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawPolygon(points);
            }
        }

        public void DrawPolygon(Pen pen, PointF[] points)
        {
            // Bail out now if there's nothing to draw.
            if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
            {
                return;
            }

            Point[] dpoints = ConvertPoints(points, 2, PageUnit);
            BaseOffsetPoints(dpoints);
            lock (this)
            {
                SelectPen(pen);
                ToolkitGraphics.DrawPolygon(dpoints);
            }
        }

        // Draw a rectangle.
        public void DrawRectangle(Pen pen, Rectangle rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawRectangle(Pen pen, int x, int y, int width, int height)
        {
            if (width > 0 && height > 0)
            {
                // Bail out now if there's nothing to draw.
                if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
                {
                    return;
                }

                lock (this)
                {
                    SelectPen(pen);
                    ToolkitGraphics.DrawPolygon(ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit));
                }
            }
        }

        public void DrawRectangle(Pen pen, float x, float y,
            float width, float height)
        {
            if (width > 0 && height > 0)
            {
                // Bail out now if there's nothing to draw.
                if (pen.PenType == PenType.SolidColor && pen.Color.A == 0)
                {
                    return;
                }

                lock (this)
                {
                    SelectPen(pen);
                    ToolkitGraphics.DrawPolygon(ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit));
                }
            }
        }

        // Draw a series of rectangles.
        public void DrawRectangles(Pen pen, Rectangle[] rects)
        {
            if (rects == null)
            {
                throw new ArgumentNullException("rects");
            }

            int posn;
            for (posn = 0; posn < rects.Length; ++posn)
            {
                DrawRectangle(pen, rects[posn].X, rects[posn].Y,
                    rects[posn].Width, rects[posn].Height);
            }
        }

        public void DrawRectangles(Pen pen, RectangleF[] rects)
        {
            if (rects == null)
            {
                throw new ArgumentNullException("rects");
            }

            int posn;
            for (posn = 0; posn < rects.Length; ++posn)
            {
                DrawRectangle(pen, rects[posn].X, rects[posn].Y,
                    rects[posn].Width, rects[posn].Height);
            }
        }

        // Draw a string.
        public void DrawString(string s, Font font, Brush brush, PointF point)
        {
            DrawString(s, font, brush, point.X, point.Y, null);
        }

        public void DrawString(string s, Font font, Brush brush,
            RectangleF layoutRectangle)
        {
            DrawString(s, font, brush, layoutRectangle, null);
        }

        public void DrawString(string s, Font font, Brush brush,
            PointF point, StringFormat format)
        {
            DrawString(s, font, brush, point.X, point.Y, format);
        }

        public void DrawString(string? s, Font font, Brush brush,
            RectangleF layoutRectangle, StringFormat? format)
        {
            // bail out now if there's nothing to draw
            if (brush is SolidBrush {Color: {A: 0}}) return;
            if (string.IsNullOrEmpty(s!)) return;

            var layout = new TextLayoutManager();
            var positionedGlyphs = PrepareStringForDrawOrMeasure(layout, s, font, layoutRectangle, format);
            if (positionedGlyphs == null || positionedGlyphs.Count < 1) return; // nothing to draw
            
            // Our 'position' is a bit off here when the points are setup
            foreach (var glyph in positionedGlyphs)
            {
                glyph.ApplyTransform(transform);
            }

            // set the default temporary clip
            Region? clipTemp = null;

            // draw the text
            lock (this)
            {
                // get the clipping region, if needed
                if (format == null || (format.FormatFlags & StringFormatFlags.NoClip) == 0)
                {
                    // get the clipping region, if there is one
                    if (_clip != null)
                    {
                        // get a copy of the current clipping region
                        clipTemp = _clip.Clone();

                        // intersect the clipping region with the layout
                        SetClip(layoutRectangle, CombineMode.Intersect);
                    }
                }

                // attempt to draw the text
                try
                {
                    ToolkitGraphics.FillGlyphs(positionedGlyphs, brush);
                }
                finally
                {
                    // reset the clip
                    if (clipTemp != null)
                    {
                        Clip = clipTemp;
                    }
                }
            }
        }

        private static void BoundsOfStringGraphics(List<RenderableGlyph>? positionedGlyphs, out double minX, out double maxX, out double minY, out double maxY)
        {
            minX = double.MaxValue;
            maxX = double.MinValue;
            minY = double.MaxValue;
            maxY = double.MinValue;
            var c = 0;
            foreach (var glyph in positionedGlyphs)
            {
                foreach (var contour in glyph)
                {
                    c++;
                    if (!contour.Bounds(out var mx, out var Mx, out var my, out var My)) continue;
                    minX = Math.Min(mx, minX);
                    minY = Math.Min(my, minY);
                    maxX = Math.Max(Mx, maxX);
                    maxY = Math.Max(My, maxY);
                }
            }

            if (c < 1)
            {
                minX = 0;
                maxX = 0;
                minY = 0;
                maxY = 0;
            }
        }

        private int FontLayoutInflation(Font font)
        {
            return (int) (-font.SizeInPoints * DpiX / 369.7184);
        }

        private List<RenderableGlyph>? PrepareStringForDrawOrMeasure(TextLayoutManager layout, string? s, Font font, RectangleF layoutRectangle, StringFormat? format)
        {
            // make a little inset around the text dependent of the font size
            layoutRectangle.Inflate(FontLayoutInflation(font), 0);

            // convert the layout into device coordinates
            var rect = ConvertRectangle(layoutRectangle.X + baseWindow.X, layoutRectangle.Y + baseWindow.Y, layoutRectangle.Width - 1, layoutRectangle.Height - 1, PageUnit);

            // create a layout rectangle from the device coordinates
            var deviceLayout = new Rectangle(rect[0].X, rect[0].Y, (rect[1].X - rect[0].X + 1), (rect[2].Y - rect[0].Y + 1));

            // bail out now if there's nothing to draw
            if (_clip != null && !deviceClipExtent.IntersectsWith(deviceLayout)) return null;

            // Prepare all the glyphs ready for transform and render
            var positionedGlyphs = layout.LayoutText(this, s, font, layoutRectangle, format);
            return positionedGlyphs;
        }

        public void DrawString(string s, Font font, Brush brush, float x, float y)
        {
            DrawString(s, font, brush, x, y, null);
        }

        public void DrawString(string s, Font font, Brush brush,
            float x, float y, StringFormat? format)
        {
            DrawString(s, font, brush, new RectangleF(x, y, 999999.0f, 999999.0f), format);
        }

        // Reset the graphics state back to a previous container level.
        public void EndContainer(GraphicsContainer container)
        {
            if (container != null)
            {
                lock (this)
                {
                    container.Restore(this);
                }
            }
        }

        // Enumerate the contents of a metafile.
        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point destPoint,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point destPoint,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point destPoint,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point destPoint,
            Rectangle srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints,
            Rectangle srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint,
            RectangleF srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints,
            RectangleF srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect,
            Rectangle srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect,
            RectangleF srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point destPoint,
            Rectangle srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints,
            Rectangle srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint,
            RectangleF srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints,
            RectangleF srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect,
            Rectangle srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect,
            RectangleF srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point destPoint,
            Rectangle srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints,
            Rectangle srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint,
            RectangleF srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints,
            RectangleF srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect,
            Rectangle srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        [TODO]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect,
            RectangleF srcRect, GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            // TODO
        }

        // Update the clipping region to exclude a particular rectangle.
        public void ExcludeClip(Rectangle rect)
        {
            Clip.Exclude(rect);
            UpdateClip();
        }

        public void ExcludeClip(Region region)
        {
            Clip.Exclude(region);
            UpdateClip();
        }

        // Fill a closed cardinal spline.
        public void FillClosedCurve(Brush brush, Point[] points)
        {
            FillClosedCurve(brush, points, FillMode.Alternate, 0.5f);
        }

        public void FillClosedCurve(Brush brush, PointF[] points)
        {
            FillClosedCurve(brush, points, FillMode.Alternate, 0.5f);
        }

        public void FillClosedCurve(Brush brush, Point[] points,
            FillMode fillMode)
        {
            FillClosedCurve(brush, points, fillMode, 0.5f);
        }

        public void FillClosedCurve(Brush brush, PointF[] points,
            FillMode fillMode)
        {
            FillClosedCurve(brush, points, fillMode, 0.5f);
        }

        public void FillClosedCurve(Brush brush, Point[] points,
            FillMode fillMode, float tension)
        {
            // Bail out now if there's nothing to draw.
            if ((brush is SolidBrush) && ((SolidBrush) brush).Color.A == 0)
            {
                return;
            }

            points = ConvertPoints(points, 4, PageUnit);
            BaseOffsetPoints(points);
            lock (this)
            {
                SelectBrush(brush);
                ToolkitGraphics.FillClosedCurve
                    (points, tension, fillMode);
            }
        }

        public void FillClosedCurve(Brush brush, PointF[] points,
            FillMode fillMode, float tension)
        {
            // Bail out now if there's nothing to draw.
            if ((brush is SolidBrush) && ((SolidBrush) brush).Color.A == 0)
            {
                return;
            }

            Point[] dpoints = ConvertPoints(points, 4, PageUnit);
            BaseOffsetPoints(dpoints);
            lock (this)
            {
                SelectBrush(brush);
                ToolkitGraphics.FillClosedCurve
                    (dpoints, tension, fillMode);
            }
        }

        // Fill an ellipse.
        public void FillEllipse(Brush brush, Rectangle rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillEllipse(Brush brush, RectangleF rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillEllipse(Brush brush, int x, int y, int width, int height)
        {
            // Bail out now if there's nothing to draw.
            if ((brush is SolidBrush) && ((SolidBrush) brush).Color.A == 0)
            {
                return;
            }

            // if transform is set use graphics path to draw or fill, since the transformation works for this then.
            if (null != transform)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(x, y, width, height);
                FillPath(brush, path);
            }
            else
            {
                Point[] rect = ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit);
                lock (this)
                {
                    SelectBrush(brush);
                    ToolkitGraphics.FillPie(rect, 0.0f, 360.0f);
                }
            }
        }

        public void FillEllipse(Brush brush, float x, float y,
            float width, float height)
        {
            // Bail out now if there's nothing to draw.
            if ((brush is SolidBrush) && ((SolidBrush) brush).Color.A == 0)
            {
                return;
            }

            // if transform is set use graphics path to draw or fill, since the transformation works for this then.
            if (null != transform)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(x, y, width, height);
                FillPath(brush, path);
            }
            else
            {
                Point[] rect = ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit);
                lock (this)
                {
                    SelectBrush(brush);
                    ToolkitGraphics.FillPie(rect, 0.0f, 360.0f);
                }
            }
        }

        // Fill the interior of a path.
        public void FillPath(Brush brush, GraphicsPath path)
        {
            if (brush == null)
            {
                throw new ArgumentNullException("brush");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            // Bail out now if there's nothing to draw.
            if ((brush is SolidBrush) && ((SolidBrush) brush).Color.A == 0)
            {
                return;
            }

            path.Fill(this, brush);
        }

        // Fill a pie shape.
        public void FillPie(Brush brush, Rectangle rect,
            float startAngle, float sweepAngle)
        {
            if (((float) (int) startAngle) == startAngle &&
                ((float) (int) sweepAngle) == sweepAngle)
            {
                FillPie(brush, rect.X, rect.Y, rect.Width, rect.Height,
                    (int) startAngle, (int) sweepAngle);
            }
            else
            {
                FillPie(brush, rect.X, rect.Y, rect.Width, rect.Height,
                    startAngle, sweepAngle);
            }
        }

        public void FillPie(Brush brush, int x, int y, int width, int height,
            int startAngle, int sweepAngle)
        {
            // Bail out now if there's nothing to draw.
            if ((brush is SolidBrush) && ((SolidBrush) brush).Color.A == 0)
            {
                return;
            }

            // if transform is set use graphics path to draw or fill, since the transformation works for this then.
            if (null != transform)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddPie(x, y, width, height, startAngle, sweepAngle);
                FillPath(brush, path);
            }
            else
            {
                Point[] rect = ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit);
                lock (this)
                {
                    SelectBrush(brush);
                    ToolkitGraphics.FillPie(rect, startAngle, sweepAngle);
                }
            }
        }

        public void FillPie(Brush brush, float x, float y,
            float width, float height,
            float startAngle, float sweepAngle)
        {
            // Bail out now if there's nothing to draw.
            if ((brush is SolidBrush) && ((SolidBrush) brush).Color.A == 0)
            {
                return;
            }

            // if transform is set use graphics path to draw or fill, since the transformation works for this then.
            if (null != transform)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddPie(x, y, width, height, startAngle, sweepAngle);
                FillPath(brush, path);
            }
            else
            {
                Point[] rect = ConvertRectangle(x + baseWindow.X, y + baseWindow.Y, width, height, PageUnit);
                lock (this)
                {
                    SelectBrush(brush);
                    ToolkitGraphics.FillPie(rect, startAngle, sweepAngle);
                }
            }
        }

        // Fill a polygon.
        public void FillPolygon(Brush brush, Point[] points)
        {
            FillPolygon(brush, points, FillMode.Alternate);
        }

        public void FillPolygon(Brush brush, PointF[] points)
        {
            FillPolygon(brush, points, FillMode.Alternate);
        }

        public void FillPolygon(Brush brush, Point[] points, FillMode fillMode)
        {
            // Bail out now if there's nothing to draw.
            if (brush is SolidBrush {Color: {A: 0}}) return;

            points = ConvertPoints(points, 2, PageUnit);
            BaseOffsetPoints(points);
            lock (this)
            {
                SelectBrush(brush);
                ToolkitGraphics.FillPolygon(points, fillMode);
            }
        }

        public void FillPolygon(Brush brush, PointF[] points, FillMode fillMode)
        {
            // Bail out now if there's nothing to draw.
            if (brush is SolidBrush {Color: {A: 0}}) return;

            Point[] dpoints = ConvertPoints(points, 2, PageUnit);
            BaseOffsetPoints(dpoints);
            lock (this)
            {
                SelectBrush(brush);
                ToolkitGraphics.FillPolygon(dpoints, fillMode);
            }
        }

        // Fill a rectangle.
        public void FillRectangle(Brush brush, Rectangle rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillRectangle(Brush brush, RectangleF rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillRectangle(Brush brush, int x, int y, int width, int height)
        {
            // Bail out now if there's nothing to draw.
            if ((brush is SolidBrush) && ((SolidBrush) brush).Color.A == 0)
            {
                return;
            }

            lock (this)
            {
                SelectBrush(brush);
                ToolkitGraphics.FillPolygon(ConvertRectangle(x + baseWindow.X,
                    y + baseWindow.Y, width, height, PageUnit), FillMode.Alternate);
            }
        }

        public void FillRectangle(Brush brush, float x, float y,
            float width, float height)
        {
            // Bail out now if there's nothing to draw.
            if ((brush is SolidBrush) && ((SolidBrush) brush).Color.A == 0)
            {
                return;
            }

            lock (this)
            {
                SelectBrush(brush);
                ToolkitGraphics.FillPolygon(ConvertRectangle(x + baseWindow.X,
                    y + baseWindow.Y, width, height, PageUnit), FillMode.Alternate);
            }
        }

        // Fill a series of rectangles.
        public void FillRectangles(Brush brush, Rectangle[] rects)
        {
            if (rects == null)
            {
                throw new ArgumentNullException("rects");
            }

            int posn;
            for (posn = 0; posn < rects.Length; ++posn)
            {
                FillRectangle(brush, rects[posn].X, rects[posn].Y,
                    rects[posn].Width, rects[posn].Height);
            }
        }

        public void FillRectangles(Brush brush, RectangleF[] rects)
        {
            if (rects == null)
            {
                throw new ArgumentNullException("rects");
            }

            int posn;
            for (posn = 0; posn < rects.Length; ++posn)
            {
                FillRectangle(brush, rects[posn].X, rects[posn].Y,
                    rects[posn].Width, rects[posn].Height);
            }
        }

        // Fill a region.
        public void FillRegion(Brush brush, Region region)
        {
            // Bail out now if there's nothing to draw.
            if ((brush is SolidBrush) && ((SolidBrush) brush).Color.A == 0)
            {
                return;
            }

            RectangleF[] rs = region.GetRegionScans(new Matrix());
            for (int i = 0; i < rs.Length; i++)
            {
                Rectangle b = Rectangle.Truncate(rs[i]);
                FillRectangle(brush, rs[i]);
            }
        }

        // Flush graphics operations to the display device.
        public void Flush()
        {
            Flush(FlushIntention.Flush);
        }

        public void Flush(FlushIntention intention)
        {
            lock (this)
            {
                if (_graphics != null)
                {
                    _graphics.Flush(intention);
                }
            }
        }

        // Create a Graphics object from a HDC.
        public static Graphics FromHdc(IntPtr hdc)
        {
            return new Graphics
                (ToolkitManager.Toolkit.CreateFromHdc(hdc, IntPtr.Zero));
        }

        public static Graphics FromHdc(IntPtr hdc, IntPtr hdevice)
        {
            return new Graphics
                (ToolkitManager.Toolkit.CreateFromHdc(hdc, hdevice));
        }

        public static Graphics FromHdcInternal(IntPtr hdc)
        {
            return FromHdc(hdc);
        }

        // Create a Graphics object from a HWND.
        public static Graphics FromHwnd(IntPtr hwnd)
        {
            return new Graphics
                (ToolkitManager.Toolkit.CreateFromHwnd(hwnd));
        }

        public static Graphics FromHwndInternal(IntPtr hwnd)
        {
            return FromHwnd(hwnd);
        }

        // Create a graphics object for drawing into an image.
        public static Graphics FromImage(Image image)
        {
            if (image.toolkitImage == null && image.dgImage == null) throw new Exception("Invalid image");
            image.toolkitImage ??= ToolkitManager.Toolkit.CreateImage(image.dgImage!, 0);
            Graphics g = new Graphics(ToolkitManager.Toolkit.CreateFromImage(image.toolkitImage));
            return g;
        }

        // Get the handle for the Windows halftone palette.  Not used here.
        public static IntPtr GetHalftonePalette()
        {
            return ToolkitManager.Toolkit.GetHalftonePalette();
        }

        // Get the HDC associated with this graphics object.
        public IntPtr GetHdc()
        {
            lock (this)
            {
                if (_graphics != null)
                {
                    return _graphics.GetHdc();
                }
                else
                {
                    return IntPtr.Zero;
                }
            }
        }

        // Get the nearest color that is supported by this graphics object.
        public Color GetNearestColor(Color color)
        {
            lock (this)
            {
                if (_graphics != null)
                {
                    return _graphics.GetNearestColor(color);
                }
                else
                {
                    return color;
                }
            }
        }

        // Intersect a region with the current clipping region.
        public void IntersectClip(Rectangle rect)
        {
            Clip.Intersect(rect);
            UpdateClip();
        }

        public void IntersectClip(RectangleF rect)
        {
            Clip.Intersect(rect);
            UpdateClip();
        }

        public void IntersectClip(Region region)
        {
            Clip.Intersect(region);
            UpdateClip();
        }

        // Determine if a point is within the visible clip region.
        public bool IsVisible(Point point)
        {
            return Clip.IsVisible(point, this);
        }

        public bool IsVisible(PointF point)
        {
            return Clip.IsVisible(point, this);
        }

        public bool IsVisible(Rectangle rect)
        {
            return Clip.IsVisible(rect, this);
        }

        public bool IsVisible(RectangleF rect)
        {
            return Clip.IsVisible(rect, this);
        }

        public bool IsVisible(int x, int y)
        {
            return Clip.IsVisible(x, y, this);
        }

        public bool IsVisible(float x, float y)
        {
            return Clip.IsVisible(x, y, this);
        }

        public bool IsVisible(int x, int y, int width, int height)
        {
            return Clip.IsVisible(x, y, width, height, this);
        }

        public bool IsVisible(float x, float y, float width, float height)
        {
            return Clip.IsVisible(x, y, width, height, this);
        }

        // First attempt as a way to measure strings and draw strings, taking into account a string format.
        // The string measuring happens a word at a time.
        private class StringDrawPositionCalculator
        {
            private SplitWord[] words;
            private int wordCount;
            private string text;
            private Point[] linePositions;
            private Graphics graphics;
            private Font font;
            private RectangleF layout;
            private StringFormat format;
            private int HotKeyIdx;

            // Details for each word
            private struct SplitWord
            {
                // Publicly-accessible state.
                public int start;
                public int length;
                public int width;
                public int line;

                // Constructor.
                public SplitWord(int start, int length)
                {
                    this.start = start;
                    this.length = length;
                    width = 0;
                    line = -1;
                }
            }; // struct SplitWord


            public StringDrawPositionCalculator(string text, Graphics graphics, Font font, RectangleF layout, StringFormat format)
            {
                this.text = text;
                this.graphics = graphics;
                this.font = font;
                this.layout = layout;
                this.format = format;
                HotKeyIdx = -1;

                if (format.HotkeyPrefix == HotkeyPrefix.Show)
                {
                    HotKeyIdx = text.IndexOf('&');
                    if (HotKeyIdx != -1)
                    {
                        if (HotKeyIdx >= text.Length - 1 ||
                            char.IsControl(text[HotKeyIdx]))
                        {
                            HotKeyIdx = -1;
                        }
                        else
                        {
                            this.text = text.Substring(0, HotKeyIdx) +
                                        text.Substring(HotKeyIdx + 1);
                        }
                    }
                }
            }

            public void LayoutByWords()
            {
                // Break the string into "words". Each word has a start pos, end pos and measured size.
                // Each new line group is treated as a "word" as is each whitespace.
                wordCount = 0;
                words = new SplitWord[16];
                int start = 0;
                int len = text.Length;
                for (int i = 0; i < len;)
                {
                    start = i;
                    char c = text[i];
                    // Look for \r on its own, \n on its own or \r\n.
                    if (c == '\r')
                    {
                        if (i < (len - 1) && text[i + 1] == '\n')
                        {
                            i += 2;
                        }
                        else
                        {
                            i++;
                        }
                    }
                    else if (c == '\n')
                    {
                        i++;
                    }
                    else
                    {
                        // Skip over the whitespace.
                        while (i < len && char.IsWhiteSpace(text[i]))
                        {
                            i++;
                        }

                        // We are at the start of text so skip over the text.
                        if (i == start)
                        {
                            while (i < len)
                            {
                                c = text[i];
                                if (char.IsWhiteSpace(c) ||
                                    c == '\n' || c == '\r')
                                {
                                    break;
                                }

                                i++;
                            }
                        }
                    }

                    // Dynamically allocate the array if we need more space.
                    if (wordCount >= words.Length)
                    {
                        SplitWord[] newWords = new SplitWord[words.Length * 2];
                        Array.Copy(words, newWords, words.Length);
                        words = newWords;
                    }

                    // Add the word.
                    words[wordCount++] = new SplitWord(start, i - start);
                }

                Layout();
            }

            private void Layout()
            {
                MeasureStrings();
                WrapLines(layout.Width - 1);
                linePositions = GetPositions(layout, format);
            }

            // Draw each line
            public void Draw(Brush brush)
            {
                graphics.SelectBrush(brush);
                if (linePositions.Length == 0)
                    return;
                int currentLine = 0;
                int textStart = 0;
                int textLength = 0;

                SplitWord word = new SplitWord(0, 0);
                for (int i = 0; i <= wordCount; i++)
                {
                    if (i != wordCount)
                    {
                        word = words[i];
                        if (word.line == -1)
                            continue;
                        if (word.line == currentLine)
                        {
                            textLength += word.length;
                            continue;
                        }
                    }

                    Point linePosition = linePositions[currentLine];
                    // Draw if some of it is within layout.
                    if (linePosition.X <= layout.Right && linePosition.Y <= layout.Bottom)
                    {
                        if (HotKeyIdx >= textStart && HotKeyIdx < textStart + textLength)
                        {
                            string startString = text.Substring(textStart,
                                HotKeyIdx - textStart);
                            string endString = text.Substring(HotKeyIdx + 1,
                                textLength - (HotKeyIdx - textStart) - 1);
                            graphics.ToolkitGraphics.DrawString(startString, linePosition.X, linePosition.Y, null);
                            Font underlineFont = new Font(font, font.Style | FontStyle.Underline);
                            float startWidth = graphics.MeasureString(startString, font).Width;
                            float hotkeyWidth = graphics.MeasureString(text.Substring(HotKeyIdx, 1), underlineFont).Width;
                            graphics.SelectFont(font);
                            graphics.ToolkitGraphics.DrawString(endString,
                                linePosition.X + (int) (startWidth + hotkeyWidth), linePosition.Y, null);
                            graphics.SelectFont(underlineFont);
                            graphics.ToolkitGraphics.DrawString(text.Substring(HotKeyIdx, 1),
                                linePosition.X + (int) startWidth, linePosition.Y, null);
                            graphics.SelectFont(font);
                        }
                        else
                        {
                            string lineText = text.Substring(textStart, textLength);
                            graphics.ToolkitGraphics.DrawString(lineText, linePosition.X, linePosition.Y, null);
                        }
                    }

                    textStart = word.start;
                    textLength = word.length;
                    currentLine = word.line;
                }
            }

            // Calculate the bounds of the measured strings, the number of characters fitted and the number of lines.
            public Size GetBounds(out int charactersFitted, out int linesFilled)
            {
                linesFilled = 0;
                charactersFitted = 0;
                if (linePositions.Length == 0)
                {
                    return Size.Empty;
                }

                bool noPartialLines = (format.FormatFlags & StringFormatFlags.LineLimit) != 0;
                int h = font.Height;
                // Find number of lines filled.
                for (int i = 0; i < linePositions.Length; i++)
                {
                    if (noPartialLines)
                    {
                        if (linePositions[i].Y + h > layout.Height)
                        {
                            break;
                        }
                    }
                    else if (linePositions[i].Y > layout.Height)
                    {
                        break;
                    }

                    linesFilled++;
                }

                int maxWidth = 0;
                int currentWidth = 0;
                int currentLine = 0;
                // Find the maximum width of a line and the number of characters fitted.
                for (int i = 0; i < wordCount; i++)
                {
                    SplitWord word = words[i];
                    charactersFitted += word.length;
                    if (word.line == currentLine)
                    {
                        currentWidth += word.width;
                    }
                    else
                    {
                        if (currentWidth > maxWidth)
                        {
                            maxWidth = currentWidth;
                        }

                        currentWidth = word.width;
                        currentLine++;
                    }
                }

                if (currentWidth > maxWidth)
                {
                    maxWidth = currentWidth;
                }

                return new Size(maxWidth, h * linesFilled);
            }

            // Use the toolkit to measure all the words and spaces.
            private void MeasureStrings()
            {
                graphics.SelectFont(font);
                int spaceWidth = -1;
                Point[] rect = new Point[0];
                int charactersFitted;
                int linesFilled;
                for (int i = 0; i < wordCount; i++)
                {
                    SplitWord word = words[i];
                    char c = text[word.start];
                    if (c != '\n' && c != '\r')
                    {
                        if (char.IsWhiteSpace(c))
                        {
                            if (spaceWidth == -1)
                            {
                                spaceWidth = graphics.ToolkitGraphics.MeasureString(" ", rect, null, out charactersFitted, out linesFilled, false).Width;
                            }

                            word.width = spaceWidth;
                            words[i] = word;
                        }
                        else
                        {
                            word.width = graphics.ToolkitGraphics.MeasureString(text.Substring(word.start, word.length), rect, null, out charactersFitted, out linesFilled, false).Width;
                            words[i] = word;
                        }
                    }
                }
            }

            // Calculate the word wrap from the words.
            private void WrapLines(float lineSize)
            {
                float currSize = 0;
                int currLine = 0;

                for (int i = 0; i < wordCount; i++)
                {
                    SplitWord word = words[i];
                    char c = text[word.start];
                    // Wrap when \r\n
                    if (c == '\r' || c == '\n')
                    {
                        currLine++;
                        currSize = 0;
                        continue;
                    }

                    if (char.IsWhiteSpace(c))
                    {
                        if (i < wordCount - 1)
                        {
                            // Check that we are not at the end of the line.
                            SplitWord nextWord = words[i + 1];
                            char c1 = text[nextWord.start];
                            if (c1 != '\r' && c1 != '\n')
                            {
                                // If we have space for the next word in the line then set the lines.
                                if (currSize + word.width + nextWord.width <= lineSize)
                                {
                                    currSize += word.width + nextWord.width;
                                    word.line = nextWord.line = currLine;
                                    words[i] = word;
                                    words[i + 1] = nextWord;
                                    i += 1;
                                }
                                else
                                {
                                    // No space left in the line.
                                    currLine++;
                                    currSize = 0;
                                }
                            }
                        }
                    }
                    else
                    {
                        // we have no whitespace in line -> append / wrap line
                        if (currSize + word.width < lineSize || currLine == 0)
                        {
                            word.line = currLine;
                            words[i] = word;
                            currSize += word.width;
                        }
                        else
                        {
                            word.line = ++currLine;
                            words[i] = word;
                            currSize = 0;
                        }
                    }
                }
            }

            // Calculate the Position of the wrapped lines
            private Point[] GetPositions(RectangleF layout, StringFormat format)
            {
                // Get the total number of lines by checking from the back.
                int numLines = 0;
                for (int i = wordCount - 1; i >= 0; i--)
                {
                    if (words[i].line != -1)
                    {
                        numLines = words[i].line + 1;
                        break;
                    }
                }

                Point[] linePositions = new Point[numLines];

                int h = font.Height;

                int yOffset = 0;
                // Set the offset based on the  LineAlignment.
                if (format.LineAlignment == StringAlignment.Far)
                {
                    yOffset = (int) layout.Height - numLines * h;
                }
                else if (format.LineAlignment == StringAlignment.Center)
                {
                    yOffset = ((int) layout.Height - numLines * h) / 2;
                }

                for (int line = 0; line < numLines; line++)
                {
                    int xOffset = 0;
                    if (format.Alignment != StringAlignment.Near)
                    {
                        for (int i = 0; i < wordCount; i++)
                        {
                            SplitWord word = words[i];
                            if (word.line == line)
                            {
                                xOffset += word.width;
                            }

                            if (word.line > line)
                            {
                                break;
                            }
                        }

                        // Set the offset based on the Alignment.
                        if (format.Alignment == StringAlignment.Far)
                        {
                            xOffset = (int) layout.Width - 1 - xOffset;
                        }
                        else if (format.Alignment == StringAlignment.Center)
                        {
                            xOffset = ((int) layout.Width - 1 - xOffset) / 2;
                        }
                    }

                    linePositions[line].Y = (int) layout.Y + yOffset + line * h;
                    linePositions[line].X = (int) layout.X + xOffset;
                }

                return linePositions;
            }
        }; // class StringDrawPositionCalculator

        private class StringMeasurePositionCalculator
        {
            // Internal state.
            private Graphics graphics;
            private string text;
            private Font font;
            private Rectangle layout;
            private StringFormat format;
            private int[] lines = new int[16];
            private int count = 0;
            private int currentLine = 0;

            // NOTE: this needs to be rewritten as stateless in order to avoid
            //       excess allocations... this also needs to be fixed so that
            //       it actually handles horizontal text layout (currently only
            //       handles vertical) for character range measurements


            // Constructor.
            public StringMeasurePositionCalculator
            (Graphics graphics, string text, Font font,
                Rectangle layout, StringFormat format)
            {
                this.graphics = graphics;
                this.text = text;
                this.font = font;
                this.layout = layout;
                this.format = format;
            }


            // Get the regions for the character ranges of the string format.
            public Region[] GetRegions()
            {
                Rectangle[] bounds = GetCharBounds();
                // Now consolidate positions based on character ranges
                Region[] regions = new Region[format.ranges.Length];
                for (int i = 0; i < format.ranges.Length; i++)
                {
                    CharacterRange range = format.ranges[i];
                    Region region = null;
                    for (int j = range.First; j < range.First + range.Length; j++)
                    {
                        if (region == null)
                            region = new Region(bounds[j]);
                        else
                            region.Union(bounds[j]);
                    }

                    regions[i] = region;
                }

                return regions;
            }

            // Get the rectangles bounding each character.
            public Rectangle[] GetCharBounds()
            {
                // create the bounds
                Rectangle[] bounds = new Rectangle[text.Length];

                // set the default maximum x
                int xMax = 0;

                // set the default maximum y
                int yMax = 0;

                // get the font height
                int fontHeight = (int) font.GetHeight(graphics);

                // get the vertical flag
                bool vertical = ((format.FormatFlags &
                                  StringFormatFlags.DirectionVertical) != 0);

                // set the maximum x and y based on the vertical flag
                if (vertical)
                {
                    // set the maximum x to the maximum height
                    xMax = (layout.Height - 1);

                    // set the maximum y to the maximum width
                    yMax = (layout.Width - 1);
                }
                else
                {
                    // set the maximum x to the maximum width
                    xMax = (layout.Width - 1 - (int) (font.SizeInPoints * graphics.DpiX / 369.7184));

                    // set the maximum y to the maximum height
                    yMax = (layout.Height - 1);
                }

                // get the wrapping flag
                bool noWrap = ((format.FormatFlags &
                                StringFormatFlags.NoWrap) != 0);

                // set the line size to the maximum y
                int lineSizeRemaining = yMax;

                // add the first line
                AddLine(0);

                // set the current position
                int currentPos = 0;

                // add the lines of text
                do
                {
                    // measure the line
                    MeasureLine
                    (ref bounds, ref currentPos, ref text, xMax,
                        graphics, font, vertical, noWrap);

                    // add the current line
                    AddLine(currentPos);

                    // update the remaining line height
                    lineSizeRemaining -= fontHeight;
                } while (currentPos < text.Length && lineSizeRemaining >= 0 && !noWrap);

                // set the default y offset
                int yOffset = 0;

                // adjust the y offset for the line alignment
                if (format.LineAlignment == StringAlignment.Center)
                {
                    yOffset = (lineSizeRemaining / 2);
                }
                else if (format.LineAlignment == StringAlignment.Far)
                {
                    yOffset = lineSizeRemaining;
                }

                // set the default x offset
                int xOffset = 0;

                // ??
                for (int i = 0; i < text.Length;)
                {
                    // ??
                    if (CurrentLine == i)
                    {
                        // ??
                        currentLine++;

                        // ??
                        xOffset = 0;

                        // ??
                        if (format.Alignment == StringAlignment.Far)
                        {
                            // Go back and find the right point of the last visible character

                            // ??
                            int back = CurrentLine - 1;

                            // ??
                            if (back > -1)
                            {
                                // ??
                                for (; back >= 0; back--)
                                {
                                    // ??
                                    if (bounds[back] != Rectangle.Empty)
                                    {
                                        break;
                                    }
                                }

                                // ??
                                xOffset = xMax + 1 - bounds[back].Right;
                            }
                            else
                            {
                                // ??
                                xOffset = xMax + 1;
                            }
                        }
                        else if (format.Alignment == StringAlignment.Center)
                        {
                            // ??
                            for (int j = i; j < CurrentLine; j++)
                            {
                                // ??
                                xOffset += bounds[j].Width;
                            }

                            // ??
                            xOffset = (xMax + 1 - xOffset) / 2;
                        }
                    }
                    else
                    {
                        // ??
                        if (bounds[i] != Rectangle.Empty)
                        {
                            // ??
                            Rectangle rect = bounds[i];

                            // ??
                            bounds[i] = new Rectangle
                            ((rect.Left + xOffset + layout.Left),
                                (rect.Top + ((currentLine - 1) *
                                             fontHeight) + layout.Top + 1),
                                rect.Width, rect.Height);
                        }

                        // ??
                        i++;
                    }
                }

                // ??
                return bounds;
            }

            private void AddLine(int value)
            {
                // ensure the capacity of the lines array
                if (count == lines.Length)
                {
                    // create the new lines array
                    int[] newLines = new int[lines.Length * 2];

                    // copy the line data to the new array
                    Array.Copy(lines, newLines, lines.Length);

                    // reset the lines array to the new array
                    lines = newLines;
                }

                // add the line
                lines[count++] = value;
            }

            private int CurrentLine
            {
                get
                {
                    // return minus one if past the last line
                    if (currentLine > count)
                    {
                        return -1;
                    }

                    // return the current line
                    return lines[currentLine];
                }
            }

            // Measures one full line. Updates the positions of the characters in that line relative to 0,0
            private void MeasureLine
            (ref Rectangle[] bounds, ref int currentPos,
                ref string text, float maxX, Graphics g, Font f,
                bool vertical, bool noWrap)
            {
                // set the initial position
                int initialPos = currentPos;

                // set the default x position
                int x = (int) (f.SizeInPoints * g.DpiX / 369.7184);

                // set the default y position
                int y = 0;

                // ??
                do
                {
                    // get the current character
                    char c = text[currentPos];

                    // check for the end of the line
                    if (c == '\n')
                    {
                        // move past the end of the line
                        currentPos++;

                        // we're done
                        return;
                    }

                    // Ignore returns
                    if (c != '\r')
                    {
                        //TODO use Platform specific measure function & take into account kerning

                        // get the size of the current character
                        g.SelectFont(f);
                        int charactersFitted, linesFilled;
                        Size s = g.ToolkitGraphics.MeasureString(c.ToString(), null, null, out charactersFitted, out linesFilled, false);
                        s.Height = f.Height;

                        // ??
                        int newX = x;

                        // ??
                        int newY = y;

                        // ??
                        if (vertical)
                        {
                            newX += s.Height;
                        }
                        else
                        {
                            newX += s.Width;
                        }

                        // ??
                        if (newX > maxX)
                        {
                            // ??
                            if (noWrap)
                            {
                                return;
                            }

                            // Backtrack to wrap the word

                            // ??
                            for (int i = currentPos; i > initialPos; i--)
                            {
                                // ??
                                if (text[i] == ' ')
                                {
                                    // Swallow the space

                                    // ??
                                    bounds[i++] = Rectangle.Empty;

                                    // ??
                                    currentPos = i;

                                    // ??
                                    return;
                                }
                            }

                            // ??
                            return;
                        }
                        else
                        {
                            // ??
                            if (vertical)
                            {
                                bounds[currentPos] = new Rectangle
                                    (y, x, s.Height, s.Width - 1);
                            }
                            else
                            {
                                bounds[currentPos] = new Rectangle
                                    (x, y, s.Width, s.Height - 1);
                            }
                        }

                        // ??
                        x = newX;
                    }

                    // ??
                    currentPos++;
                } while (currentPos < text.Length);
            }
        }; // class StringMeasurePositionCalculator

        // Measure the character ranges for a string.
        public Region[] MeasureCharacterRanges(string text, Font font, RectangleF layoutRect, StringFormat stringFormat)
        {
            var calculator = new StringMeasurePositionCalculator(this, text, font, Rectangle.Truncate(layoutRect), stringFormat);
            return calculator.GetRegions();
        }

        // Non Microsoft
        public Rectangle[] MeasureCharacters(string text, Font font, RectangleF layoutRect, StringFormat stringFormat)
        {
            var calculator = new StringMeasurePositionCalculator(this, text, font, Rectangle.Truncate(layoutRect), stringFormat);
            return calculator.GetCharBounds();
        }

        // Measure the size of a string.
        public SizeF MeasureString(string text, Font font)
        {
            return MeasureString(text, font, new SizeF(999999.0f, 999999.0f), null);
        }

        public SizeF MeasureString(string text, Font font, int width)
        {
            return MeasureString
                (text, font, new SizeF(width, 999999.0f), null);
        }

        public SizeF MeasureString(string text, Font font, SizeF layoutArea)
        {
            return MeasureString(text, font, layoutArea, null);
        }

        public SizeF MeasureString(string text, Font font,
            int width, StringFormat format)
        {
            return MeasureString
                (text, font, new SizeF(width, 999999.0f), format);
        }

        public SizeF MeasureString(string text, Font font,
            PointF origin, StringFormat format)
        {
            return MeasureString
                (text, font, new SizeF(0.0f, 0.0f), format);
        }

        public SizeF MeasureString(string text, Font font,
            SizeF layoutArea, StringFormat format)
        {
            int charactersFitted, linesFilled;
            return MeasureString
            (text, font, layoutArea, format, out charactersFitted,
                out linesFilled);
        }

        public SizeF MeasureString(string? text, Font font, SizeF layoutArea,
            StringFormat? format, out int charactersFitted,
            out int linesFilled)
        {
            // bail out now if there's nothing to measure
            if (text is null || text.Length == 0)
            {
                charactersFitted = 0;
                linesFilled = 0;
                return new SizeF(0.0f, 0.0f);
            }
            
            var layoutMgr = new TextLayoutManager();
            var layoutRectangle = new RectangleF(0,0, layoutArea.Width, layoutArea.Height);
            var glyphs = PrepareStringForDrawOrMeasure(layoutMgr, text, font, layoutRectangle, format);
            linesFilled = layoutMgr.LinesFitted;
            charactersFitted = layoutMgr.CharactersFitted;
            
            if (glyphs is null) return SizeF.Empty;
            
            var measured = FindBoundSize(glyphs);
            
            measured.Width -= FontLayoutInflation(font)*2;
            measured.Width += (int)layoutMgr.LastAdvance;
            
            measured.Height = Math.Max(measured.Height, GetLineSpacing(font));

            return measured;
        }

        private SizeF FindBoundSize(List<RenderableGlyph>? glyphs)
        {
            if (glyphs == null) return SizeF.Empty;

            bool ever = false;
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;
            
            foreach (var glyph in glyphs)
            {
                foreach (var contour in glyph)
                {
                    ever = true;
                    if (!contour.Bounds(out var gMinX, out var gMaxX, out var gMinY, out var gMaxY)) continue;
                    
                    minX = Math.Min(gMinX, minX);
                    minY = Math.Min(gMinY, minY);
                    maxX = Math.Max(gMaxX, maxX);
                    maxY = Math.Max(gMaxY, maxY);
                }
            }
            
            if (!ever) return SizeF.Empty;
            return new SizeF((float)(maxX - minX), (float)(maxY - minY));
        }

        // Multiply the transformation matrix by a specific amount.
        public void MultiplyTransform(Matrix matrix)
        {
            // Do not use the property Transform directly, because Transform should return a Copy
            transform = new Matrix(Transform);
            transform.Multiply(matrix);
            if (transform.IsIdentity)
            {
                transform = null;
            }
        }

        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            // Do not use the property Transform directly, because Transform should return a Copy
            transform = new Matrix(Transform);
            transform.Multiply(matrix, order);
            if (transform.IsIdentity)
            {
                transform = null;
            }
        }

        // Release a HDC that was obtained via a previous call to "GetHdc".
        public void ReleaseHdc(IntPtr hdc)
        {
            lock (this)
            {
                if (_graphics != null)
                {
                    _graphics.ReleaseHdc(hdc);
                }
            }
        }

        public void ReleaseHdcInternal(IntPtr hdc)
        {
            ReleaseHdc(hdc);
        }

        // Reset the clipping region.
        public void ResetClip()
        {
            Clip = new Region();
            UpdateClip();
        }

        // Reset the transformation matrix to identity.
        public void ResetTransform()
        {
            transform = null;
            Clip = _clip;
        }

        // Restore to a previous save point.
        public void Restore(GraphicsState gstate)
        {
            if (gstate != null)
            {
                lock (this)
                {
                    gstate.Restore(this);
                }
            }
        }

        // Apply a rotation to the transformation matrix.
        public void RotateTransform(float angle)
        {
            transform ??= new Matrix();
            transform.Rotate(angle, MatrixOrder.Prepend);
            if (transform.IsIdentity) { transform = null; }
        }

        public void RotateTransform(float angle, MatrixOrder order)
        {
            transform ??= new Matrix();
            transform.Rotate(angle, order);
            if (transform.IsIdentity) { transform = null; }
        }

        // Save the current graphics state.
        public GraphicsState Save()
        {
            lock (this)
            {
                return new GraphicsState(this);
            }
        }

        // Apply a scaling factor to the transformation matrix.
        public void ScaleTransform(float sx, float sy)
        {
            // Do not use the property Transform directly, because Transform should return a Copy
            transform = new Matrix(Transform);
            transform.Scale(sx, sy);
            if (transform.IsIdentity)
            {
                transform = null;
            }
        }

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            // Do not use the property Transform directly, because Transform should return a Copy
            transform = new Matrix(Transform);
            transform.Scale(sx, sy, order);
            if (transform.IsIdentity)
            {
                transform = null;
            }
        }

        // Set the clipping region of this graphics object.
        public void SetClip(Graphics g)
        {
            if (g == null)
            {
                throw new ArgumentNullException("g");
            }

            _clip = g.Clip.Clone();
            UpdateClip();
        }

        public void SetClip(Graphics g, CombineMode combineMode)
        {
            if (g == null)
            {
                throw new ArgumentNullException("g");
            }

            Region other = g.Clip;
            switch (combineMode)
            {
                case CombineMode.Replace:
                    _clip = other.Clone();
                    break;

                case CombineMode.Intersect:
                {
                    Clip.Intersect(other);
                }
                    break;

                case CombineMode.Union:
                {
                    Clip.Union(other);
                }
                    break;

                case CombineMode.Xor:
                {
                    Clip.Xor(other);
                }
                    break;

                case CombineMode.Exclude:
                {
                    Clip.Exclude(other);
                }
                    break;

                case CombineMode.Complement:
                {
                    Clip.Complement(other);
                }
                    break;

                default: return;
            }

            UpdateClip();
        }

        public void SetClip(GraphicsPath path)
        {
            _clip = new Region(path);
            UpdateClip();
        }

        public void SetClip(GraphicsPath path, CombineMode combineMode)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            switch (combineMode)
            {
                case CombineMode.Replace:
                {
                    _clip = new Region(path);
                }
                    break;

                case CombineMode.Intersect:
                {
                    Clip.Intersect(path);
                }
                    break;

                case CombineMode.Union:
                {
                    Clip.Union(path);
                }
                    break;

                case CombineMode.Xor:
                {
                    Clip.Xor(path);
                }
                    break;

                case CombineMode.Exclude:
                {
                    Clip.Exclude(path);
                }
                    break;

                case CombineMode.Complement:
                {
                    Clip.Complement(path);
                }
                    break;

                default: return;
            }

            UpdateClip();
        }

        public void SetClip(Rectangle rect)
        {
            _clip = new Region(rect);
            UpdateClip();
        }

        public void SetClip(Rectangle rect, CombineMode combineMode)
        {
            switch (combineMode)
            {
                case CombineMode.Replace:
                {
                    _clip = new Region(rect);
                }
                    break;

                case CombineMode.Intersect:
                {
                    Clip.Intersect(rect);
                }
                    break;

                case CombineMode.Union:
                {
                    Clip.Union(rect);
                }
                    break;

                case CombineMode.Xor:
                {
                    Clip.Xor(rect);
                }
                    break;

                case CombineMode.Exclude:
                {
                    Clip.Exclude(rect);
                }
                    break;

                case CombineMode.Complement:
                {
                    Clip.Complement(rect);
                }
                    break;

                default: return;
            }

            UpdateClip();
        }

        public void SetClip(RectangleF rect)
        {
            _clip = new Region(rect);
            UpdateClip();
        }

        public void SetClip(RectangleF rect, CombineMode combineMode)
        {
            switch (combineMode)
            {
                case CombineMode.Replace:
                {
                    _clip = new Region(rect);
                }
                    break;

                case CombineMode.Intersect:
                {
                    Clip.Intersect(rect);
                }
                    break;

                case CombineMode.Union:
                {
                    Clip.Union(rect);
                }
                    break;

                case CombineMode.Xor:
                {
                    Clip.Xor(rect);
                }
                    break;

                case CombineMode.Exclude:
                {
                    Clip.Exclude(rect);
                }
                    break;

                case CombineMode.Complement:
                {
                    Clip.Complement(rect);
                }
                    break;

                default: return;
            }

            UpdateClip();
        }

        public void SetClip(Region? region, CombineMode combineMode)
        {
            if (region == null)
            {
                if (combineMode == CombineMode.Replace)
                {
                    // Special case: clear the clip region
                    _clip?.MakeInfinite();
                    return;
                }

                throw new ArgumentNullException(nameof(region));
            }

            switch (combineMode)
            {
                case CombineMode.Replace:
                {
                    _clip = region.Clone();
                }
                    break;

                case CombineMode.Intersect:
                {
                    Clip.Intersect(region);
                }
                    break;

                case CombineMode.Union:
                {
                    Clip.Union(region);
                }
                    break;

                case CombineMode.Xor:
                {
                    Clip.Xor(region);
                }
                    break;

                case CombineMode.Exclude:
                {
                    Clip.Exclude(region);
                }
                    break;

                case CombineMode.Complement:
                {
                    Clip.Complement(region);
                }
                    break;

                default: return;
            }

            UpdateClip();
        }

        internal void SetClipInternal(Region region)
        {
            _clip = region;
        }

        // Transform points from one co-ordinate space to another.
        public void TransformPoints(CoordinateSpace destSpace,
            CoordinateSpace srcSpace,
            Point[] pts)
        {
            float x;
            float y;

            if (srcSpace == CoordinateSpace.Device)
            {
                if (destSpace != CoordinateSpace.Device)
                {
                    // Convert from Device to Page.
                    for (int i = 0; i < pts.Length; i++)
                    {
                        if (PageUnit == GraphicsUnit.Pixel)
                            continue;
                        x = pts[i].X;
                        y = pts[i].Y;
                        // Apply the page unit to get page co-ordinates.
                        switch (PageUnit)
                        {
                            case GraphicsUnit.Display:
                            {
                                x /= DpiX / 75.0f;
                                y /= DpiY / 75.0f;
                            }
                                break;

                            case GraphicsUnit.Point:
                            {
                                x /= DpiX / 72.0f;
                                y /= DpiY / 72.0f;
                            }
                                break;

                            case GraphicsUnit.Inch:
                            {
                                x /= DpiX;
                                y /= DpiY;
                            }
                                break;

                            case GraphicsUnit.Document:
                            {
                                x /= DpiX / 300.0f;
                                y /= DpiY / 300.0f;
                            }
                                break;

                            case GraphicsUnit.Millimeter:
                            {
                                x /= DpiX / 25.4f;
                                y /= DpiY / 25.4f;
                            }
                                break;
                            default:
                                break;
                        }

                        // Apply the inverse of the page scale factor.
                        if (PageScale != 1.0f)
                        {
                            x /= PageScale;
                            y /= PageScale;
                        }

                        pts[i] = new Point((int) x, (int) y);
                    }
                } // destSpace != CoordinateSpace.Device

                srcSpace = CoordinateSpace.Page;
            }

            if (srcSpace == CoordinateSpace.World)
            {
                if (destSpace == CoordinateSpace.World)
                    return;
                // Convert from World to Page.
                if (transform != null)
                    transform.TransformPoints(pts);

                srcSpace = CoordinateSpace.Page;
            }

            if (srcSpace == CoordinateSpace.Page)
            {
                if (destSpace == CoordinateSpace.World && transform != null)
                {
                    // Convert from Page to World.
                    // Apply the inverse of the world transform.
                    Matrix invert = new Matrix(transform);
                    invert.Invert();
                    invert.TransformPoints(pts);
                }

                if (destSpace == CoordinateSpace.Device)
                {
                    // Convert from Page to Device.
                    // Apply the page scale factor.
                    for (int i = 0; i < pts.Length; i++)
                    {
                        x = pts[i].X;
                        y = pts[i].Y;
                        if (PageScale != 1.0f)
                        {
                            x *= PageScale;
                            y *= PageScale;
                        }

                        // Apply the page unit to get device co-ordinates.
                        switch (PageUnit)
                        {
                            case GraphicsUnit.Display:
                            {
                                x *= DpiX / 75.0f;
                                y *= DpiY / 75.0f;
                            }
                                break;

                            case GraphicsUnit.Point:
                            {
                                x *= DpiX / 72.0f;
                                y *= DpiY / 72.0f;
                            }
                                break;

                            case GraphicsUnit.Inch:
                            {
                                x *= DpiX;
                                y *= DpiY;
                            }
                                break;

                            case GraphicsUnit.Document:
                            {
                                x *= DpiX / 300.0f;
                                y *= DpiY / 300.0f;
                            }
                                break;

                            case GraphicsUnit.Millimeter:
                            {
                                x *= DpiX / 25.4f;
                                y *= DpiY / 25.4f;
                            }
                                break;
                            case GraphicsUnit.World:
                            case GraphicsUnit.Pixel:
                            default:
                                break;
                        }

                        pts[i] = new Point((int) x, (int) y);
                    }
                }
            }
        }

        public void TransformPoints(CoordinateSpace destSpace,
            CoordinateSpace srcSpace,
            PointF[] pts)
        {
            float x;
            float y;

            if (srcSpace == CoordinateSpace.Device)
            {
                if (destSpace != CoordinateSpace.Device)
                {
                    // Convert from Device to Page.
                    for (int i = 0; i < pts.Length; i++)
                    {
                        if (PageUnit == GraphicsUnit.Pixel)
                            continue;
                        x = pts[i].X;
                        y = pts[i].Y;
                        // Apply the page unit to get page co-ordinates.
                        switch (PageUnit)
                        {
                            case GraphicsUnit.Display:
                            {
                                x /= DpiX / 75.0f;
                                y /= DpiY / 75.0f;
                            }
                                break;

                            case GraphicsUnit.Point:
                            {
                                x /= DpiX / 72.0f;
                                y /= DpiY / 72.0f;
                            }
                                break;

                            case GraphicsUnit.Inch:
                            {
                                x /= DpiX;
                                y /= DpiY;
                            }
                                break;

                            case GraphicsUnit.Document:
                            {
                                x /= DpiX / 300.0f;
                                y /= DpiY / 300.0f;
                            }
                                break;

                            case GraphicsUnit.Millimeter:
                            {
                                x /= DpiX / 25.4f;
                                y /= DpiY / 25.4f;
                            }
                                break;
                            default:
                                break;
                        }

                        // Apply the inverse of the page scale factor.
                        if (PageScale != 1.0f)
                        {
                            x /= PageScale;
                            y /= PageScale;
                        }

                        pts[i] = new PointF(x, y);
                    }
                } // destSpace != CoordinateSpace.Device

                srcSpace = CoordinateSpace.Page;
            }

            if (srcSpace == CoordinateSpace.World)
            {
                if (destSpace == CoordinateSpace.World)
                    return;
                // Convert from World to Page.
                if (transform != null)
                    transform.TransformPoints(pts);

                srcSpace = CoordinateSpace.Page;
            }

            if (srcSpace == CoordinateSpace.Page)
            {
                if (destSpace == CoordinateSpace.World && transform != null)
                {
                    // Convert from Page to World.
                    // Apply the inverse of the world transform.
                    Matrix invert = new Matrix(transform);
                    invert.Invert();
                    invert.TransformPoints(pts);
                }

                if (destSpace == CoordinateSpace.Device)
                {
                    // Convert from Page to Device.
                    // Apply the page scale factor.
                    for (int i = 0; i < pts.Length; i++)
                    {
                        x = pts[i].X;
                        y = pts[i].Y;
                        if (PageScale != 1.0f)
                        {
                            x *= PageScale;
                            y *= PageScale;
                        }

                        // Apply the page unit to get device co-ordinates.
                        switch (PageUnit)
                        {
                            case GraphicsUnit.Display:
                            {
                                x *= DpiX / 75.0f;
                                y *= DpiY / 75.0f;
                            }
                                break;

                            case GraphicsUnit.Point:
                            {
                                x *= DpiX / 72.0f;
                                y *= DpiY / 72.0f;
                            }
                                break;

                            case GraphicsUnit.Inch:
                            {
                                x *= DpiX;
                                y *= DpiY;
                            }
                                break;

                            case GraphicsUnit.Document:
                            {
                                x *= DpiX / 300.0f;
                                y *= DpiY / 300.0f;
                            }
                                break;

                            case GraphicsUnit.Millimeter:
                            {
                                x *= DpiX / 25.4f;
                                y *= DpiY / 25.4f;
                            }
                                break;
                            case GraphicsUnit.World:
                            case GraphicsUnit.Pixel:
                            default:
                                break;
                        }

                        pts[i] = new PointF(x, y);
                    }
                }
            }
        }

        // Translate the clipping region by a specified amount.
        public void TranslateClip(int dx, int dy)
        {
            Region clip = Clip;
            clip.Translate(dx, dy);
            Clip = clip;
        }

        public void TranslateClip(float dx, float dy)
        {
            Region clip = Clip;
            clip.Translate(dx, dy);
            Clip = clip;
        }

        // Apply a translation to the transformation matrix.
        public void TranslateTransform(float dx, float dy)
        {
            // Do not use the property Transform directly, because Transform should return a Copy
            transform = new Matrix(Transform);
            transform.Translate(dx, dy);
            if (transform.IsIdentity)
            {
                transform = null;
            }
        }

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            // Do not use the property Transform directly, because Transform should return a Copy
            transform = new Matrix(Transform);
            transform.Translate(dx, dy, order);
            if (transform.IsIdentity)
            {
                transform = null;
            }
        }

        // Delegate that is used to handle abort callbacks on "DrawImage".
#if !ECMA_COMPAT
        [Serializable]
        [ComVisible(false)]
#endif
        public delegate bool DrawImageAbort(IntPtr callbackdata);

        // Delegate that is used to enumerate metafile contents.
#if !ECMA_COMPAT
        [Serializable]
        [ComVisible(false)]
#endif
        public delegate bool EnumerateMetafileProc
        (EmfPlusRecordType recordType, int flags, int dataSize,
            IntPtr data, PlayRecordCallback callbackData);

        private void BaseOffsetPoints(Point[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Point p = points[i];
                p.X += baseWindow.X;
                p.Y += baseWindow.Y;
                points[i] = p;
            }
        }

        // Convert a point into device pixels.
        private void ConvertPoint(ref int x, ref int y, GraphicsUnit graphicsUnit, Transforms transforms = Transforms.Apply)
        {
            float newX, newY;
            float adjustX, adjustY;

            // Apply the world transform first.
            if (transform != null && transforms == Transforms.Apply)
            {
                transform.TransformPoint(x, y, out newX, out newY);
            }
            else
            {
                newX = (float) x;
                newY = (float) y;
            }

            // Apply the page scale factor.
            if (PageScale != 1.0f)
            {
                newX *= PageScale;
                newY *= PageScale;
            }

            // Apply the page unit to get device co-ordinates.
            switch (graphicsUnit)
            {
                case GraphicsUnit.World:
                case GraphicsUnit.Pixel:
                default:
                {
                    // We are finished - no more adjustments are necessary.
                    x = (int) newX;
                    y = (int) newY;
                    return;
                }
                // Not reached.

                case GraphicsUnit.Display:
                {
                    adjustX = DpiX / 75.0f;
                    adjustY = DpiY / 75.0f;
                }
                    break;

                case GraphicsUnit.Point:
                {
                    adjustX = DpiX / 72.0f;
                    adjustY = DpiY / 72.0f;
                }
                    break;

                case GraphicsUnit.Inch:
                {
                    adjustX = DpiX;
                    adjustY = DpiY;
                }
                    break;

                case GraphicsUnit.Document:
                {
                    adjustX = DpiX / 300.0f;
                    adjustY = DpiY / 300.0f;
                }
                    break;

                case GraphicsUnit.Millimeter:
                {
                    adjustX = DpiX / 25.4f;
                    adjustY = DpiY / 25.4f;
                }
                    break;
            }

            x = (int) (newX * adjustX);
            y = (int) (newY * adjustY);
        }

        private void ConvertPoint(float x, float y, out int dx, out int dy, GraphicsUnit graphicsUnit, Transforms transforms = Transforms.Apply)
        {
            float newX, newY;
            float adjustX, adjustY;

            // Apply the world transform first.
            if (transform != null && transforms == Transforms.Apply)
            {
                transform.TransformPoint(x, y, out newX, out newY);
            }
            else
            {
                newX = x;
                newY = y;
            }

            // Apply the page scale factor.
            if (PageScale != 1.0f)
            {
                newX *= PageScale;
                newY *= PageScale;
            }

            // Apply the page unit to get device co-ordinates.
            switch (graphicsUnit)
            {
                case GraphicsUnit.World:
                case GraphicsUnit.Pixel:
                default:
                {
                    // We are finished - no more adjustments are necessary.
                    dx = (int) newX;
                    dy = (int) newY;
                    return;
                }
                // Not reached.

                case GraphicsUnit.Display:
                {
                    adjustX = DpiX / 75.0f;
                    adjustY = DpiY / 75.0f;
                }
                    break;

                case GraphicsUnit.Point:
                {
                    adjustX = DpiX / 72.0f;
                    adjustY = DpiY / 72.0f;
                }
                    break;

                case GraphicsUnit.Inch:
                {
                    adjustX = DpiX;
                    adjustY = DpiY;
                }
                    break;

                case GraphicsUnit.Document:
                {
                    adjustX = DpiX / 300.0f;
                    adjustY = DpiY / 300.0f;
                }
                    break;

                case GraphicsUnit.Millimeter:
                {
                    adjustX = DpiX / 25.4f;
                    adjustY = DpiY / 25.4f;
                }
                    break;
            }

            dx = (int) (newX * adjustX);
            dy = (int) (newY * adjustY);
        }

        // Convert a list of points into device pixels.
        private Point[] ConvertPoints(Point[] points, int minPoints, GraphicsUnit unit, Transforms transforms = Transforms.Apply)
        {
            // Validate the parameter.
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Length < minPoints) throw new ArgumentException("Arg_NeedsAtLeastNPoints");

            // Convert the "points" array.
            Point[] newPoints = new Point [points.Length];
            int x, y;
            int posn;
            for (posn = 0; posn < points.Length; ++posn)
            {
                x = points[posn].X;
                y = points[posn].Y;
                ConvertPoint(ref x, ref y, unit, transforms);
                newPoints[posn] = new Point(x, y);
            }

            return newPoints;
        }

        private Point[] ConvertPoints(PointF[] points, int minPoints, GraphicsUnit unit, Transforms transforms = Transforms.Apply)
        {
            // Validate the parameter.
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Length < minPoints) return Array.Empty<Point>();

            // Convert the "points" array.
            Point[] newPoints = new Point [points.Length];
            int x, y;
            int posn;
            for (posn = 0; posn < points.Length; ++posn)
            {
                ConvertPoint(points[posn].X, points[posn].Y, out x, out y, unit, transforms);
                newPoints[posn] = new Point(x, y);
            }

            return newPoints;
        }

        // Convert a rectangle into a set of 3 device co-ordinates.
        // The result may be a parallelogram, not a rectangle.
        private Point[] ConvertRectangle3(int x, int y, int width, int height, GraphicsUnit unit)
        {
            Point[] points = new Point[3];
            int pt1x, pt1y, pt2x, pt2y, pt3x, pt3y;
            pt1x = x;
            pt1y = y;
            pt2x = x + width;
            pt2y = y;
            pt3x = x;
            pt3y = y + height;
            if (transform != null || PageScale != 1.0f ||
                (unit != GraphicsUnit.Pixel && unit != GraphicsUnit.World))
            {
                ConvertPoint(ref pt1x, ref pt1y, unit);
                ConvertPoint(ref pt2x, ref pt2y, unit);
                ConvertPoint(ref pt3x, ref pt3y, unit);
            }

            points[0] = new Point(pt1x, pt1y);
            points[1] = new Point(pt2x, pt2y);
            points[2] = new Point(pt3x, pt3y);
            return points;
        }

        private Point[] ConvertRectangle3(float x, float y, float width, float height, GraphicsUnit unit)
        {
            Point[] points = new Point[3];
            int pt1x, pt1y, pt2x, pt2y, pt3x, pt3y;
            ConvertPoint(x, y, out pt1x, out pt1y, unit);
            ConvertPoint(x + width, y, out pt2x, out pt2y, unit);
            ConvertPoint(x, y + height, out pt3x, out pt3y, unit);
            points[0] = new Point(pt1x, pt1y);
            points[1] = new Point(pt2x, pt2y);
            points[2] = new Point(pt3x, pt3y);
            return points;
        }

        // Convert a rectangle into a set of 4 device co-ordinates.
        // The result may be a parallelogram, not a rectangle.
        private Point[] ConvertRectangle(int x, int y,
            int width, int height, GraphicsUnit unit,
            Transforms transforms = Transforms.Apply)
        {
            Point[] points = new Point[4];
            points[0] = new Point(x, y);
            points[1] = new Point(x + width, y);
            points[2] = new Point(x + width, y + height);
            points[3] = new Point(x, y + height);
            return ConvertPoints(points, 4, unit, transforms);
        }

        private Point[] ConvertRectangle(float x, float y,
            float width, float height, GraphicsUnit unit,
            Transforms transforms = Transforms.Apply)
        {
            PointF[] points = new PointF[4];
            points[0] = new PointF(x, y);
            points[1] = new PointF(x + width, y);
            points[2] = new PointF(x + width, y + height);
            points[3] = new PointF(x, y + height);
            return ConvertPoints(points, 4, unit, transforms);
        }

        // Convert a size value from device co-ordinates to graphics units.
        private SizeF ConvertSizeBack(int width, int height)
        {
            float newX, newY, adjustX, adjustY;

            // Bail out early if the context is using pixel units.
            if (IsPixelUnits())
            {
                return new SizeF((float) width, (float) height);
            }

            // Apply the page unit to get page co-ordinates.
            newX = (float) width;
            newY = (float) height;
            switch (PageUnit)
            {
                case GraphicsUnit.World:
                case GraphicsUnit.Pixel:
                default:
                {
                    adjustX = 1.0f;
                    adjustY = 1.0f;
                }
                    break;

                case GraphicsUnit.Display:
                {
                    adjustX = DpiX / 75.0f;
                    adjustY = DpiY / 75.0f;
                }
                    break;

                case GraphicsUnit.Point:
                {
                    adjustX = DpiX / 72.0f;
                    adjustY = DpiY / 72.0f;
                }
                    break;

                case GraphicsUnit.Inch:
                {
                    adjustX = DpiX;
                    adjustY = DpiY;
                }
                    break;

                case GraphicsUnit.Document:
                {
                    adjustX = DpiX / 300.0f;
                    adjustY = DpiY / 300.0f;
                }
                    break;

                case GraphicsUnit.Millimeter:
                {
                    adjustX = DpiX / 25.4f;
                    adjustY = DpiY / 25.4f;
                }
                    break;
            }

            newX /= adjustX;
            newY /= adjustY;

            // Apply the inverse of the page scale factor.
            if (PageScale != 1.0f)
            {
                newX /= PageScale;
                newY /= PageScale;
            }

            // Apply the inverse of the world transform.
            if (transform != null)
            {
                transform.TransformSizeBack
                    (newX, newY, out adjustX, out adjustY);
                return new SizeF(adjustX, adjustY);
            }
            else
            {
                return new SizeF(newX, newY);
            }
        }

        // Convert a rectangle into a set of 3 device unit co-ordinates.
        private Point[] ConvertUnits(int x, int y, int width, int height,
            GraphicsUnit unit)
        {
            // get the unit adjustment vector
            PointF adjust = ConvertUnitsAdjustVector(unit);

            // calculate the unit adjusted rectangle bounds
            int x0 = (int) ((x + 0) * adjust.X);
            int x1 = (int) ((x + width) * adjust.X);
            int y0 = (int) ((y + 0) * adjust.Y);
            int y1 = (int) ((y + height) * adjust.Y);

            // create the unit adjusted rectangle points
            Point[] points = new Point[]
            {
                new Point(x0, y0),
                new Point(x1, y0),
                new Point(x0, y1)
            };

            // return the unit adjusted rectangle points
            return points;
        }

        private Point[] ConvertUnits(float x, float y, float width, float height,
            GraphicsUnit unit)
        {
            // get the unit adjustment vector
            PointF adjust = ConvertUnitsAdjustVector(unit);

            // calculate the unit adjusted rectangle bounds
            int x0 = (int) ((x + 0) * adjust.X);
            int x1 = (int) ((x + width) * adjust.X);
            int y0 = (int) ((y + 0) * adjust.Y);
            int y1 = (int) ((y + height) * adjust.Y);

            // create the unit adjusted rectangle points
            Point[] points = new Point[]
            {
                new Point(x0, y0),
                new Point(x1, y0),
                new Point(x0, y1)
            };

            // return the unit adjusted rectangle points
            return points;
        }

        // Get the unit adjustment vector for the ConvertUnits methods.
        private PointF ConvertUnitsAdjustVector(GraphicsUnit unit)
        {
            // create and return the unit adjustment vector
            switch (unit)
            {
                case GraphicsUnit.World:
                case GraphicsUnit.Pixel:
                default:
                {
                    return new PointF(1.0f, 1.0f);
                }

                case GraphicsUnit.Display:
                {
                    return new PointF((DpiX / 75.0f), (DpiY / 75.0f));
                }

                case GraphicsUnit.Point:
                {
                    return new PointF((DpiX / 72.0f), (DpiY / 72.0f));
                }

                case GraphicsUnit.Inch:
                {
                    return new PointF(DpiX, DpiY);
                }

                case GraphicsUnit.Document:
                {
                    return new PointF((DpiX / 300.0f), (DpiY / 300.0f));
                }

                case GraphicsUnit.Millimeter:
                {
                    return new PointF((DpiX / 25.4f), (DpiY / 25.4f));
                }
            }
        }


        // Get the toolkit graphics object underlying this object.
        internal IToolkitGraphics ToolkitGraphics
        {
            get
            {
                if (_graphics != null)
                {
                    return _graphics;
                }

                throw new ObjectDisposedException("graphics");
            }
        }

        // Get the default graphics object for the current toolkit.
        internal static Graphics DefaultGraphics
        {
            get
            {
                lock (typeof(Graphics))
                {
                    if (defaultGraphics == null)
                    {
                        defaultGraphics = new Graphics
                            (ToolkitManager.Toolkit.GetDefaultGraphics());
                        defaultGraphics.PageUnit = GraphicsUnit.Pixel;
                    }

                    return defaultGraphics;
                }
            }
        }

        // Select a pen into the toolkit graphics object.
        private void SelectPen(Pen pen)
        {
            if (pen == null)
            {
                throw new ArgumentNullException("pen");
            }

            if (_graphics == null)
            {
                throw new ObjectDisposedException("graphics");
            }

            Pen penNew = pen;
            if (transform != null)
            {
                // calculation new pen size, if a transformation is set
                // using the workaround for Font scaling
                float penWidth = pen.Width;
                float newWidth = transform.TransformFontSize(penWidth);
                if (penWidth != newWidth)
                {
                    penNew = (Pen) pen.Clone();
                    penNew.Width = newWidth;
                }
            }

            IToolkitPen tpen = penNew.GetPen(_graphics.Toolkit);
            if (penNew.PenType == PenType.SolidColor)
            {
                tpen.Select(_graphics);
            }
            else
            {
                IToolkitBrush tbrush = penNew.Brush.GetBrush(_graphics.Toolkit);
                tpen.Select(_graphics, tbrush);
            }
        }

        // Select a brush into the toolkit graphics object.
        internal void SelectBrush(Brush brush)
        {
            if (brush == null) throw new ArgumentNullException(nameof(brush));

            if (_graphics == null) throw new ObjectDisposedException("graphics");

            brush.GetBrush(_graphics.Toolkit).Select(_graphics);
        }

        // Select a font into the toolkit graphics object.
        internal void SelectFont(Font font)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));

            if (_graphics == null) throw new ObjectDisposedException("graphics");

            font.GetFont(_graphics.Toolkit, DpiY).Select(_graphics);
        }

        // Update the clipping region within the IToolkitGraphics object.
        private void UpdateClip()
        {
            RectangleF[] rectsF;
            if (transform == null && PageScale == 1.0f && PageUnit == GraphicsUnit.World)
            {
                rectsF = _clip.GetRegionScansIdentity();
            }
            else
            {
                rectsF = _clip.GetRegionScans(Transform);
            }

            int left = int.MaxValue;
            int top = int.MaxValue;
            int right = int.MinValue;
            int bottom = int.MinValue;
            Rectangle[] rects = new Rectangle[rectsF.Length];
            for (int i = 0; i < rectsF.Length; i++)
            {
                Rectangle r = Rectangle.Truncate(rectsF[i]);
                if (baseWindow != Rectangle.Empty)
                {
                    r.Offset(baseWindow.Location);
                    r.Intersect(baseWindow);
                }

                rects[i] = r;
                if (left > r.Left)
                    left = r.Left;
                if (right < r.Right)
                    right = r.Right;
                if (top > r.Top)
                    top = r.Top;
                if (bottom < r.Bottom)
                    bottom = r.Bottom;
            }

            _graphics?.SetClipRects(rects);
            deviceClipExtent = Rectangle.FromLTRB(left, top, right, bottom);
        }

        // Determine if this graphics object is using 1-to-1 pixel mappings.
        internal bool IsPixelUnits()
        {
            if ((PageUnit == GraphicsUnit.Pixel ||
                 PageUnit == GraphicsUnit.World) &&
                transform == null && PageScale == 1.0f)
            {
                return true;
            }

            return false;
        }

        // Get the line spacing of a font, in pixels.
        internal int GetLineSpacing(Font font)
        {
            lock (this)
            {
                SelectFont(font);
                return ToolkitGraphics.GetLineSpacing();
            }
        }

        // Draw a bitmap-based glyph to a "Graphics" object.  "bits" must be
        // in the form of an xbm bitmap.
        internal void DrawGlyph(int x, int y,
            byte[] bits, int bitsWidth, int bitsHeight,
            Color color)
        {
            // Bail out now if there's nothing to draw.
            if (color.A == 0)
            {
                return;
            }

            int dx, dy;
            ConvertPoint(x, y, out dx, out dy, PageUnit);
            lock (this)
            {
                ToolkitGraphics.DrawGlyph
                (dx + baseWindow.X, dy + baseWindow.Y,
                    bits, bitsWidth, bitsHeight, color);
            }
        }
    }; // class Graphics
}; // namespace System.Drawing