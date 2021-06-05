namespace Portable.Drawing.Toolkit.Fonts
{
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
    }
}