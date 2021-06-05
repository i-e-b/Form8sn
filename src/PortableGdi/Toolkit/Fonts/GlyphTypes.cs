namespace Portable.Drawing.Toolkit.Fonts
{
    public enum GlyphTypes
    {
        /// <summary>
        /// Contains a set of curves and points to
        /// draw a glyph
        /// </summary>
        Simple,

        /// <summary>
        /// Contains a set of transformed and combined glyphs
        /// </summary>
        Compound,

        /// <summary>
        /// Is an empty glyph. This may have sizing effects
        /// but no graphic, like whitespace.
        /// </summary>
        Empty
    }
}