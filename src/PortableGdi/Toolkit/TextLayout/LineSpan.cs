namespace Portable.Drawing.Toolkit.TextLayout
{
    /// <summary>
    /// Each span of a full line for center and bottom line justified text.
    /// </summary>
    internal struct LineSpan
    {
        // Publicly-accessible state.
        public int start;
        public int length;
        public int pixelWidth;


        // Constructor.
        public LineSpan(int start, int length, int pixelWidth)
        {
            this.start = start;
            this.length = length;
            this.pixelWidth = pixelWidth;
        }
    }
}