namespace Portable.Drawing.Toolkit.Fonts.Rendering
{
    /// <summary>
    /// Meta-data of edge definitions produced by the edge rasteriser.
    /// This is consumed by some of the renderers
    /// </summary>
    public class EdgeWorkspace
    {
        public byte[]? Data;
        public int Width;
        public int Height;
        public float Baseline;
        public float Shift;

        public static EdgeWorkspace Empty()
        {
            return new EdgeWorkspace{
                Data = new byte[0],
                Height = 0,
                Width = 0
            };
        }
    }
}