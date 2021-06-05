namespace Portable.Drawing.Toolkit.Fonts
{
    /// <summary>
    /// Header offset table entry
    /// </summary>
    public struct OffsetEntry{
        public uint Checksum;
        public uint Offset;
        public uint Length;
    }
}