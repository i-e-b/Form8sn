using System.Collections.Generic;

namespace Portable.Drawing.Toolkit.Fonts
{
    /// <summary>
    /// Horizontal metrics are quite complex. See
    /// https://docs.microsoft.com/en-us/typography/opentype/spec/hmtx
    /// and
    /// https://www.freetype.org/freetype2/docs/glyphs/glyphs-3.html
    /// </summary>
    public class Ttf_HorizontalMetricsTable
    {
        public long TableBase { get; set; }
        
        public List<ushort> _advanceWidths;
        public List<short> _leftSideBearings;

        public Ttf_HorizontalMetricsTable(BinaryReader file, OffsetEntry table, Ttf_HorizontalHeaderTable header, Ttf_MaximumProfileTable profile)
        {
            file.Seek(table.Offset);
            TableBase = table.Offset;
            
            int numMetrics = header.NumberOfHMetrics;
            int extraLeftSideBearings = profile.NumGlyphs - numMetrics;

            _advanceWidths = new List<ushort>(numMetrics);
            _leftSideBearings = new List<short>(numMetrics+extraLeftSideBearings);

            for (int idx = 0; idx < numMetrics; idx++)
            {
                _advanceWidths.Add(file.GetUint16());
                _leftSideBearings.Add(file.GetInt16());
            }

            for (int idx = 0; idx < extraLeftSideBearings; idx++)
            {
                _leftSideBearings.Add(file.GetInt16());
            }
        }

        public int GlyphWidth(int glyphIndex)
        {
            // glyphIndex >= numberOfHMetrics means the font is mono-spaced and all glyphs have the same width
            if (glyphIndex >= _advanceWidths.Count) glyphIndex = _advanceWidths.Count - 1;

            return _advanceWidths[glyphIndex];
        }
        
        public int GlyphLeftSideBearing(int glyphIndex)
        {
            if (glyphIndex >= _leftSideBearings.Count) glyphIndex = _leftSideBearings.Count - 1;

            return _leftSideBearings[glyphIndex];
        }
    }
}