using System;
using System.Collections.Generic;
using System.Linq;

namespace Portable.Drawing.Toolkit.Fonts
{
    public class TrueTypeFont : IFontReader
    {
        private readonly string _filename;
        public const uint HeaderMagic = 0x5f0f3cf5;

        private readonly BinaryReader _file;
        private readonly Dictionary<string, OffsetEntry> _tables;
        private readonly Dictionary<char, int> _unicodeIndexes;
        private readonly Dictionary<int, Glyph> _glyphCache;

        private readonly FontHeader _header;

        private uint _scalarType;
        private ushort _searchRange;
        private ushort _entrySelector;
        private ushort _rangeShift;
        private int _length;
        
        public TrueTypeFont(string filename)
        {
            _filename = filename;
            _file = new BinaryReader(filename);
            _unicodeIndexes = new Dictionary<char, int>();
            _glyphCache = new Dictionary<int, Glyph>();

            // The order that things are read below is important
            // DO NOT REARRANGE CALLS!
            _tables = ReadOffsetTables();
            _header = ReadHeadTable();
            _length = GlyphCount();

            if ( ! _tables.ContainsKey("glyf")) throw new Exception("Bad font: glyf table missing");
            if ( ! _tables.ContainsKey("loca")) throw new Exception("Bad font: loca table missing");
        }

        /// <summary>
        /// Read a glyph based on a Unicode character. This will be cached.
        /// </summary>
        public Glyph ReadGlyph(char wantedChar)
        {
            if ( ! _unicodeIndexes.ContainsKey(wantedChar)) {
                _unicodeIndexes.Add(wantedChar,  GlyphIndexForChar(wantedChar));
            }

            var offset = _unicodeIndexes[wantedChar]; // we do it this way, because multiple characters could map to the same glyph

            if ( ! _glyphCache.ContainsKey(offset) ) {
                var g = ReadGlyphByIndex(_unicodeIndexes[wantedChar], char.IsWhiteSpace(wantedChar));
                g.SourceCharacter = wantedChar;
                g.SourceFont = _filename;
                _glyphCache.Add(offset, g);
            }

            return _glyphCache[offset];
        }
        
        /// <summary>
        /// Get the reported overall height of the font
        /// </summary>
        public float Height()
        {
            // Use what the font headers say:
            //return (float)_header.xMax + _header.xMin;

            // Ignoring font declaration, guess based on a character height
            ReadGlyph('x').GetPointBounds(out _, out _, out _, out var yMax);
            return (float)yMax * 2.0f;
        }
        
        private Glyph ReadGlyphByIndex(int index, bool forceEmpty)
        {
            var offset = GetGlyphOffset(index);

            if (offset >= _tables["glyf"].Offset + _tables["glyf"].Length) throw new Exception("Bad font: Invalid glyph offset (too high) at index " + index);
            if (offset < _tables["glyf"].Offset) throw new Exception("Bad font: Invalid glyph offset (too low) at index" + index);

            _file.Seek(offset);
            var glyph = new Glyph{
                NumberOfContours = _file.GetInt16(),
                xMin = _file.GetFWord(),
                yMin = _file.GetFWord(),
                xMax = _file.GetFWord(),
                yMax = _file.GetFWord()
            };

            if (glyph.NumberOfContours < -1) throw new Exception("Bad font: Invalid contour count at index " + index);

            var baseOffset = _file.Position();
            if (forceEmpty || glyph.NumberOfContours == 0) return EmptyGlyph(glyph);
            if (glyph.NumberOfContours == -1) return ReadCompoundGlyph(glyph, baseOffset);
            return ReadSimpleGlyph(glyph);
        }
        

        private int GlyphIndexForChar(char wantedChar)
        {
            // read cmap table if possible,
            // then call down to the index based ReadGlyph.

            if ( ! _tables.ContainsKey("cmap")) throw new Exception("Can't translate character: cmap table missing");
            _file.Seek(_tables["cmap"].Offset);

            var vers = _file.GetUint16();
            var numTables = _file.GetUint16();
            var offset = 0u;
            var found = false;

            for (int i = 0; i < numTables; i++)
            {
                var platform = _file.GetUint16();
                var encoding = _file.GetUint16();
                offset = _file.GetUint32();

                if (platform == 3 && encoding == 1) // Unicode 2 byte encoding for Basic Multilingual Plane
                {
                    found = true;
                    break;
                }
            }

            if (!found) {
                return 0; // the specific 'missing' glyph
            }
            
            // format 4 table
            if (offset < _file.Position()) {_file.Seek(_tables["cmap"].Offset + offset); } // guessing
            else { _file.Seek(offset); }

            var subtableFmt = _file.GetUint16();

            var byteLength = _file.GetUint16();
            var res1 = _file.GetUint16(); // should be 0
            var segCountX2 = _file.GetUint16();
            var searchRange = _file.GetUint16();
            var entrySelector = _file.GetUint16();
            var rangeShift = _file.GetUint16();

            if (subtableFmt != 4) throw new Exception("Invalid font: Unicode BMP table with non- format 4 subtable");
            
            // read the parallel arrays
            var segs = segCountX2 / 2;
            var endsBase = _file.Position();
            var startsBase = endsBase + segCountX2 + 2;
            var idDeltaBase = startsBase + segCountX2;
            var idRangeOffsetBase = idDeltaBase + segCountX2;

            var targetSegment = -1;

            var c = (int)wantedChar;

            for (int i = 0; i < segs; i++)
            {
                int end = _file.PickUint16(endsBase, i);
                int start = _file.PickUint16(startsBase, i);
                if (end >= c && start <= c) {
                    targetSegment = i;
                    break;
                }
            }
            
            if (targetSegment < 0) return 0; // the specific 'missing' glyph

            var rangeOffset = _file.PickUint16(idRangeOffsetBase, targetSegment);
            if (rangeOffset == 0) {
                // direct lookup:
                var lu = _file.PickInt16(idDeltaBase, targetSegment); // this can represent a negative by way of the modulo
                var glyphIdx = (lu + c) % 65536;
                return glyphIdx;
            }

            // Complex case. The TrueType spec expects us to have mapped the font into memory, then do some
            // nasty pointer arithmetic. "This obscure indexing trick works because glyphIdArray immediately follows idRangeOffset in the font file"
            //
            // https://docs.microsoft.com/en-us/typography/opentype/spec/cmap
            // https://github.com/LayoutFarm/Typography/wiki/How-To-Translate-Unicode-Character-Codes-to-TrueType-Glyph-Indices-in-Windows-95
            // https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6cmap.html

            var ros = _file.PickUint16(idRangeOffsetBase, targetSegment);
            var startc = _file.PickUint16(startsBase, targetSegment);
            var offsro = idRangeOffsetBase + (targetSegment * 2);
            var glyphIndexAddress = ros + 2 * (c - startc) + offsro;
            var res = _file.PickInt16(glyphIndexAddress, 0);

            return res;
        }


        private int GlyphCount()
        {
            if ( ! _tables.ContainsKey("maxp")) throw new Exception("Bad font: maxp table missing (no glyph count)");
            var old = _file.Seek(_tables["maxp"].Offset + 4);
            var count = _file.GetUint16();
            _file.Seek(old);
            return count;
        }

        private FontHeader ReadHeadTable()
        {
            if ( ! _tables.ContainsKey("head")) throw new BadImageFormatException("Bad font: Header table missing");
            _file.Seek(_tables["head"].Offset);

            var h = new FontHeader();
            
            // DO NOT REARRANGE CALLS!
            h.Version = _file.GetFixed();
            h.Revision = _file.GetFixed();
            h.ChecksumAdjustment = _file.GetUint32();
            h.MagicNumber = _file.GetUint32();

            if (h.MagicNumber != HeaderMagic) throw new Exception("Bad font: incorrect identifier in header table");

            h.Flags = _file.GetUint16();
            h.UnitsPerEm = _file.GetUint16();
            h.Created = _file.GetDate();
            h.Modified = _file.GetDate();

            h.xMin = _file.GetFWord();
            h.yMin = _file.GetFWord();
            h.xMax = _file.GetFWord();
            h.yMax = _file.GetFWord();

            h.MacStyle = _file.GetUint16();
            h.LowestRecPPEM = _file.GetUint16();
            h.FontDirectionHint = _file.GetInt16();
            h.IndexToLocFormat = _file.GetInt16();
            h.GlyphDataFormat = _file.GetInt16();

            return h;
        }

        private Dictionary<string, OffsetEntry> ReadOffsetTables()
        {
            var tables = new Dictionary<string, OffsetEntry>();

            // DO NOT REARRANGE CALLS!
            _scalarType = _file.GetUint32();
            var numTables = _file.GetUint16();

            _searchRange = _file.GetUint16();
            _entrySelector = _file.GetUint16();
            _rangeShift = _file.GetUint16();

            for (int i = 0; i < numTables; i++)
            {
                var tag = _file.GetString(4);
                if (string.IsNullOrEmpty(tag)) break; // malformatted? End of file?
                
                var entry = new OffsetEntry{
                    Checksum = _file.GetUint32(),
                    Offset = _file.GetUint32(),
                    Length = _file.GetUint32()
                };
                if (tables.ContainsKey(tag)) Console.WriteLine($"duplicate tag: {tag}");
                else tables.Add(tag, entry);

               /* if (tag != "head") {
                    if (CalculateTableChecksum(file, tables[tag].Offset, tables[tag].Length) != tables[tag].Checksum)
                        throw new Exception("Bad file format: checksum fail in offset tables");
                }*/
            }
            return tables;
        }

        private long CalculateTableChecksum(BinaryReader reader, uint offset, uint length)
        {
            var old = reader.Seek(offset);
            long sum = 0;
            var nlongs = (length + 3) / 4;
            while( nlongs > 0 ) {
                nlongs--;
                sum += reader.GetUint32() & 0xFFFFFFFFu;
            }

            _file.Seek(old);
            return sum;
        }

        private Glyph ReadCompoundGlyph(Glyph g, long baseOffset)
        {
            // http://stevehanov.ca/blog/TrueType.js    ->  readCompoundGlyph: function(file, glyph) {...}
            // Noto-sans "ǽ" \u01FD will trigger this
            g.GlyphType = GlyphTypes.Compound;
            return g;
            //throw new Exception("Compounds not yet supported");

            // A compound glyph brings together simple glyphs from elsewhere in the font
            // and combines them with transforms.
            // If this method gets implemented, it should reduce the components down to a simple glyph
        }

        private Glyph EmptyGlyph(Glyph g)
        {
            g.GlyphType = GlyphTypes.Empty;
            return g;
        }

        private Glyph ReadSimpleGlyph(Glyph g)
        {
            g.GlyphType = GlyphTypes.Simple;

            var ends = new List<int>();

            for (int i = 0; i < g.NumberOfContours; i++) { ends.Add(_file.GetUint16()); }

            // Skip past hinting instructions
            _file.Skip(_file.GetUint16());

            var numPoints = ends.Max() + 1;

            // Flags and points match up
            var flags = new List<SimpleGlyphFlags>();
            var points = new List<GlyphPoint>();

            // Read point flags, creating base entries
            for (int i = 0; i < numPoints; i++)
            {
                var flag = (SimpleGlyphFlags)_file.GetUint8();
                flags.Add(flag);
                points.Add(new GlyphPoint{ OnCurve = flag.HasFlag(SimpleGlyphFlags.ON_CURVE)});

                if (flag.HasFlag(SimpleGlyphFlags.REPEAT)) {
                    var repeatCount = _file.GetUint8();
                    i += repeatCount;
                    while (repeatCount-- > 0) {
                        flags.Add(flag);
                        points.Add(new GlyphPoint{ OnCurve = flag.HasFlag(SimpleGlyphFlags.ON_CURVE)});
                    }
                }
            }

            // Fill out point data
            ElaborateCoords(flags, points, (i, v) => points[i].X = v, SimpleGlyphFlags.X_IS_BYTE, SimpleGlyphFlags.X_DELTA);
            ElaborateCoords(flags, points, (i, v) => points[i].Y = v, SimpleGlyphFlags.Y_IS_BYTE, SimpleGlyphFlags.Y_DELTA);

            g.Points = points.ToArray();
            g.ContourEnds = ends.ToArray();

            return g;
        }

        private void ElaborateCoords(List<SimpleGlyphFlags> flags, List<GlyphPoint> points, Action<int, double> map, SimpleGlyphFlags byteFlag, SimpleGlyphFlags deltaFlag)
        {
            var value = 0.0d;

            for (int i = 0; i < points.Count; i++)
            {
                var flag = flags[i];
                if (flag.HasFlag(byteFlag)) {
                    if (flag.HasFlag(deltaFlag)) {
                        value += _file.GetUint8();
                    } else {
                        value -= _file.GetUint8();
                    }
                } else if (!flag.HasFlag(deltaFlag)) {
                    value += _file.GetInt16();
                } else {
                    // value not changed
                    // this is why X and Y are separate
                }

                map(i, value);
            }
        }

        private long GetGlyphOffset(int index)
        {
            var table = _tables["loca"];
            var size = table.Offset + table.Length;
            long offset, old;

            if (_header.IndexToLocFormat == 1) {
                var target = table.Offset + index * 4;
                if (target + 4 > size) throw new Exception("Glyph index out of range");
                old = _file.Seek(target);
                offset = _file.GetUint32();
            } else {
                var target = table.Offset + index * 2;
                if (target + 2 > size) throw new Exception("Glyph index out of range");
                old = _file.Seek(target);
                offset = _file.GetUint16() * 2;
            }

            _file.Seek(old);
            return offset + _tables["glyf"].Offset;
        }

        public List<string> ListTablesKeys()
        {
            return _tables.Keys.ToList();
        }

        public object GetTable(string name)
        {
            // References:
            // http://pfaedit.org/non-standard.html#FFTM
            //
            switch (name)
            {
                case "OS/2": return new TtfTableOS2(_file, _tables[name]);
                case "name": return new TtfTableName(_file, _tables[name]);

                default: return null;
            }
        }
    }
}