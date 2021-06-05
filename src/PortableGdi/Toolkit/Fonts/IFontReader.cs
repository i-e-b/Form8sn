namespace Portable.Drawing.Toolkit.Fonts
{
    public interface IFontReader
    {
        /// <summary>
        /// https://docs.microsoft.com/en-us/typography/opentype/spec/cmap
        /// </summary>
        Glyph ReadGlyph(char c);
    }
}