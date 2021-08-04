using System.Collections.Generic;
using Portable.Drawing.Toolkit.Portable.Rasteriser;

namespace Portable.Drawing.Toolkit.Fonts
{
    /// <summary>
    /// Data for a point on a glyph contour
    /// </summary>
    public class GlyphPoint
    {
        public bool OnCurve;
        public double X;
        public double Y;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public PointF ToGdiPoint()
        {
            return new PointF((float)X, (float)Y);
        }
    }
}