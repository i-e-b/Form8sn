namespace Portable.Drawing.Toolkit.Fonts
{
    /// <summary>
    /// Ranges of some tables where not listed on the table itself.
    /// See https://docs.microsoft.com/en-us/typography/opentype/spec/maxp
    /// </summary>
    public class Ttf_MaximumProfileTable
    {
        public long TableBase { get; set; }
        
        public uint Version; // must be 0x00010000 for TrueType
        public ushort NumGlyphs;
        public ushort MaxPoints;
        public ushort MaxContours;
        public ushort MaxCompositePoints;
        public ushort MaxCompositeContours;
        public ushort MaxZones;
        public ushort MaxTwilightPoints;
        public ushort MaxStorage;
        public ushort MaxFunctionDefs;
        public ushort MaxInstructionDefs;
        public ushort MaxStackElements;
        public ushort MaxSizeOfInstructions;
        public ushort MaxComponentElements;
        public ushort MaxComponentDepth;

        public Ttf_MaximumProfileTable(BinaryReader file, OffsetEntry table)
        {
            file.Seek(table.Offset);
            TableBase = table.Offset;
            
            Version = file.GetUint32();
            NumGlyphs = file.GetUint16();
            MaxPoints = file.GetUint16();
            MaxContours = file.GetUint16();
            MaxCompositePoints = file.GetUint16();
            MaxCompositeContours = file.GetUint16();
            MaxZones = file.GetUint16();
            MaxTwilightPoints = file.GetUint16();
            MaxStorage = file.GetUint16();
            MaxFunctionDefs = file.GetUint16();
            MaxInstructionDefs = file.GetUint16();
            MaxStackElements = file.GetUint16();
            MaxSizeOfInstructions = file.GetUint16();
            MaxComponentElements = file.GetUint16();
            MaxComponentDepth = file.GetUint16();
        }
    }
}