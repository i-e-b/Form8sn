using System;

// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable InconsistentNaming

namespace Portable.Drawing.Toolkit.Fonts
{
    public class TtfTableOS2
    {
        public UInt16 Version { get; set; }
        
        public TtfTableOS2(BinaryReader file, OffsetEntry table)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            file.Seek(table.Offset);
            
            // See https://docs.microsoft.com/en-gb/typography/opentype/spec/os2
            // See https://github.com/fontforge/fontforge/blob/master/fontforge/ttf.h#L467
            Version = file.GetUint16();

            switch (Version)
            {
                case 5:
                    ReadVersion5(file);
                    break;
                
                case 2:
                    case 3:
                    case 4:
                    ReadVersion4(file);
                    break;

                default: throw new Exception("OS/2 version not supported: " + Version);
            }
        }

        private void ReadVersion5(BinaryReader file)
        {
            ReadVersion4(file);
            usLowerOpticalPointSize = file.GetUint16();
            usUpperOpticalPointSize = file.GetUint16();
        }

        private void ReadVersion4(BinaryReader file)
        {
            xAvgCharWidth = file.GetInt16();
            usWeightClass = file.GetUint16();
            usWidthClass = file.GetUint16();
            fsType = file.GetUint16();
            ySubscriptXSize = file.GetInt16();
            ySubscriptYSize = file.GetInt16();
            ySubscriptXOffset = file.GetInt16();
            ySubscriptYOffset = file.GetInt16();
            ySuperscriptXSize = file.GetInt16();
            ySuperscriptYSize = file.GetInt16();
            ySuperscriptXOffset = file.GetInt16();
            ySuperscriptYOffset = file.GetInt16();
            yStrikeoutSize = file.GetInt16();
            yStrikeoutPosition = file.GetInt16();
            sFamilyClass = file.GetInt16();
            panose = file.GetPanose();
            ulUnicodeRange1 = file.GetUint32();
            ulUnicodeRange2 = file.GetUint32();
            ulUnicodeRange3 = file.GetUint32();
            ulUnicodeRange4 = file.GetUint32();
            achVendID = file.GetTag();
            fsSelection = file.GetUint16();
            usFirstCharIndex = file.GetUint16();
            usLastCharIndex = file.GetUint16();
            sTypoAscender = file.GetInt16();
            sTypoDescender = file.GetInt16();
            sTypoLineGap = file.GetInt16();
            usWinAscent = file.GetUint16();
            usWinDescent = file.GetUint16();
            ulCodePageRange1 = file.GetUint32();
            ulCodePageRange2 = file.GetUint32();
            sxHeight = file.GetInt16();
            sCapHeight = file.GetInt16();
            usDefaultChar = file.GetUint16();
            usBreakChar = file.GetUint16();
            usMaxContext = file.GetUint16();
        }

        public ushort usUpperOpticalPointSize { get; set; }

        public ushort usLowerOpticalPointSize { get; set; }

        public ushort usMaxContext { get; set; }

        public ushort usBreakChar { get; set; }

        public ushort usDefaultChar { get; set; }

        public short sCapHeight { get; set; }

        public short sxHeight { get; set; }

        public uint ulCodePageRange2 { get; set; }
        public uint ulCodePageRange1 { get; set; }

        public ushort usWinDescent { get; set; }

        public ushort usWinAscent { get; set; }

        public short sTypoLineGap { get; set; }

        public short sTypoDescender { get; set; }

        public short sTypoAscender { get; set; }

        public ushort usLastCharIndex { get; set; }

        public ushort usFirstCharIndex { get; set; }

        public ushort fsSelection { get; set; }

        public Tag achVendID { get; set; }

        public uint ulUnicodeRange1 { get; set; }
        public uint ulUnicodeRange2 { get; set; }
        public uint ulUnicodeRange3 { get; set; }
        public uint ulUnicodeRange4 { get; set; }

        public PanoseClassification panose { get; set; }

        public short sFamilyClass { get; set; }

        public short yStrikeoutPosition { get; set; }

        public short yStrikeoutSize { get; set; }

        public short ySuperscriptYOffset { get; set; }

        public short ySuperscriptXOffset { get; set; }

        public short ySuperscriptYSize { get; set; }

        public short ySuperscriptXSize { get; set; }

        public short ySubscriptYOffset { get; set; }

        public short ySubscriptXOffset { get; set; }

        public short ySubscriptYSize { get; set; }

        public short ySubscriptXSize { get; set; }

        public ushort fsType { get; set; }

        public ushort usWidthClass { get; set; }

        public ushort usWeightClass { get; set; }

        public short xAvgCharWidth { get; set; }
    }
}