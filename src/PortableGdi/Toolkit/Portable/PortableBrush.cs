namespace Portable.Drawing.Toolkit.Portable
{
    internal class PortableBrush : IToolkitBrush
    {
        public Color Color { get; }

        public PortableBrush(Color color)
        {
            Color = color;
        }

        public void Dispose() { }

        public void Select(IToolkitGraphics graphics)
        {
            graphics.Brush = this;
        }
    }
}