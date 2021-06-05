namespace Portable.Drawing.Toolkit.Fonts
{
    public struct CompoundComponent
    {
        public int GlyphIndex;
        public double[] Matrix;  // variable sized
        public int? DestPointIndex;
        public int? SrcPointIndex;
    }
}