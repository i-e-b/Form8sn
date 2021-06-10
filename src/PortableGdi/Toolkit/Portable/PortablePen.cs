namespace Portable.Drawing.Toolkit.Portable
{
    internal class PortablePen : IToolkitPen
    {
        public Pen Pen { get; }

        public PortablePen(Pen pen)
        {
            Pen = pen;
        }

        public void Dispose() { Pen.Dispose(); }

        public void Select(IToolkitGraphics graphics)
        {
            graphics.Pen = this;
        }

        public void Select(IToolkitGraphics graphics, IToolkitBrush brush)
        {
            graphics.Pen = this;
        }
    }
}