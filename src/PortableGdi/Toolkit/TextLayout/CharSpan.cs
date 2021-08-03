namespace Portable.Drawing.Toolkit.TextLayout
{
    internal sealed class CharSpan
    {
        // Publicly-accessible state.
        public int start;
        public int length;
        public int pixelWidth;
        public bool newline;


        // Constructor.
        public CharSpan()
        {
            start = 0;
            length = 0;
            pixelWidth = -1;
            newline = false;
        }


        // Copy the values of this character span to the given span.
        public void CopyTo(CharSpan span)
        {
            span.start = start;
            span.length = length;
            span.pixelWidth = pixelWidth;
            span.newline = newline;
        }

        // Set the values of this character span.
        public void Set(int start, int length, bool newline)
        {
            this.start = start;
            this.length = length;
            pixelWidth = -1;
            this.newline = newline;
        }
    };
}