namespace Portable.Drawing.Toolkit.Fonts
{
    public class Ttf_HorizontalHeaderTable
    {
        public long TableBase { get; set; }
        
        public uint Version; // 0x00010000 for Version 1.0.
        public short Ascender; // Typographic ascent. (Distance from baseline of highest Ascender) 
        public short Descender; // Typographic descent. (Distance from baseline of lowest Descender) 
        public short LineGap; // Typographic line gap. Negative LineGap values are treated as zero in Windows 3.1, System 6, and System 7.
        public ushort AdvanceWidthMax;
        public short MinLeftSideBearing;
        public short MinRightSideBearing;
        public short XMaxExtent;
        public short CaretSlopeRise;
        public short CaretSlopeRun;
        public short Reserved1;
        public short Reserved2;
        public short Reserved3;
        public short Reserved4;
        public short Reserved5;
        public short MetricDataFormat;
        public ushort NumberOfHMetrics;

        public Ttf_HorizontalHeaderTable(BinaryReader file, OffsetEntry table)
        {
            file.Seek(table.Offset);
            TableBase = table.Offset;
            
            // See https://docs.microsoft.com/en-us/typography/opentype/spec/hhea
            Version = file.GetUint32();
            Ascender = file.GetFWord();
            Descender = file.GetFWord();
            LineGap = file.GetFWord();
            AdvanceWidthMax = file.GetUint16();
            MinLeftSideBearing = file.GetFWord();
            MinRightSideBearing = file.GetFWord();
            XMaxExtent = file.GetFWord();
            CaretSlopeRise = file.GetInt16();
            CaretSlopeRun = file.GetInt16();
            Reserved1 = file.GetInt16();
            Reserved2 = file.GetInt16();
            Reserved3 = file.GetInt16();
            Reserved4 = file.GetInt16();
            Reserved5 = file.GetInt16();
            MetricDataFormat = file.GetInt16();
            NumberOfHMetrics = file.GetUint16();
        }
    }
}