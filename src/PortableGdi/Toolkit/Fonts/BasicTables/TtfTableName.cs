using System;
using System.Collections.Generic;

namespace Portable.Drawing.Toolkit.Fonts
{
    public class TtfTableName
    {
        public TtfTableName(BinaryReader file, OffsetEntry table)
        {
            file.Seek(table.Offset);
            TableBase = table.Offset;
            
            // See https://docs.microsoft.com/en-gb/typography/opentype/spec/name
            Format = file.GetUint16();

            switch (Format)
            {
                case 0:
                case 1: // format 1 has extra info, but starts the same as zero
                    ReadFormatZero(file);
                    break;
                default:
                    break;
            }
        }


        private void ReadFormatZero(BinaryReader file)
        {
            Count = file.GetUint16();
            StringOffset = file.GetUint16();
            
            if (Count < 1) return;
            if (Count > 100) Count = 100; // safety valve

            Names = new List<NameRecord>();
            for (int i = 0; i < Count; i++)
            {
                Names.Add(new NameRecord(file, TableBase, StringOffset));
            }
        }

        public List<NameRecord> Names { get; set; }

        public long TableBase { get; set; }
        public long StringOffset { get; set; }
        public ushort Count { get; set; }
        public int Format { get; set; }
    }
}