using System;

// ReSharper disable InconsistentNaming

namespace Portable.Drawing.Toolkit.Fonts
{
    /// <summary>
    /// Details from the TTF header block
    /// </summary>
    public struct FontHeader {
        public double Version;
        public double Revision;
        public uint ChecksumAdjustment;
        public uint MagicNumber;
        public ushort Flags;
        public ushort UnitsPerEm;
        public DateTime Created;
        public DateTime Modified;

        public short xMin;
        public short yMin;
        public short xMax;
        public short yMax;

        public ushort MacStyle;
        public ushort LowestRecPPEM;
        public short FontDirectionHint;
        public short IndexToLocFormat;
        public short GlyphDataFormat;
    }
}