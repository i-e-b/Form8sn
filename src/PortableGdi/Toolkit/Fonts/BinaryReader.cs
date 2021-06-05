using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// http://stevehanov.ca/blog/TrueType.js
namespace Portable.Drawing.Toolkit.Fonts
{
    /// <summary>
    /// Byte reader for TTF / OpenType files
    /// </summary>
    public class BinaryReader {
        private readonly byte[] _data;
        private long _pos;
        private readonly Stack<long> _offsets = new Stack<long>();

        /// <summary>
        /// Load from a file
        /// </summary>
        public BinaryReader(string filename)
        {
            if (filename == null || !File.Exists(filename)) throw new Exception("Could not read font file");
            _data = File.ReadAllBytes(filename);
            if (_data == null) throw new Exception("Failed to read file");

            _pos = 0;
        }

        /// <summary>
        /// Seek to absolute position. Returns previous position
        /// </summary>
        public long Seek(long newPos)
        {
            var oldPos = _pos;
            _pos = newPos;
            return oldPos;
        }

        /// <summary>
        /// Return current position
        /// </summary>
        public long Position() => _pos;

        /// <summary> Read an unsigned byte from current position and advance </summary>
        public int GetUint8(){ return _data[_pos++]; } 
        
        /// <summary> Read an unsigned 16bit word from current position and advance </summary>
        public ushort GetUint16(){ return (ushort) ((GetUint8() << 8) + GetUint8()); }

        /// <summary> Read an unsigned 32bit word from current position and advance </summary>
        public uint GetUint32() { return (uint)GetInt32(); }
        
        /// <summary> Read a signed 16bit word from current position and advance </summary>
        public short GetInt16() {
            return (short)GetUint16();
        }

        
        /// <summary> Read an unsigned 24bit word from current position and advance </summary>
        public long GetUint24()
        {
            return (GetUint8() << 16) +
                   (GetUint8() << 8) +
                   GetUint8();
        }

        /// <summary> Read a signed 32bit word from current position and advance </summary>
        public long GetInt32()
        {
            return (GetUint8() << 24) +
                   (GetUint8() << 16) +
                   (GetUint8() << 8) +
                   GetUint8();
        }

        public Tag GetTag()
        {
            return new Tag(GetString(4));
        }

        /// <summary>
        /// Get signed fixword size
        /// </summary>
        public short GetFWord() { return GetInt16(); }

        /// <summary> Get float from a 16 bit fixed point </summary>
        public double Get2Dot14() {
            return (double)GetInt16() / (1 << 14);
        }

        /// <summary> Get float from 32 bit fixed point </summary>
        public double GetFixed() {
            return (double)GetInt32() / (1 << 16);
        }

        /// <summary>
        /// Read an ASCII string from current position and advance
        /// </summary>
        public string GetString(int length) {
            var result = new StringBuilder();
            for(var i = 0; i < length; i++) {
                result.Append( (char)GetUint8() );
            }
            return result.ToString();
        }
        
        /// <summary>
        /// Read a string of bytes from current position and advance
        /// </summary>
        public byte[] GetByteString(int length) {
            var result = new byte[length];
            for(var i = 0; i < length; i++) {
                result[i] = (byte)GetUint8();
            }
            return result;
        }

        /// <summary> Read a TTF / Opentype date </summary>
        public DateTime GetDate() {
            var macTimeSeconds = GetUint32() * 0x100000000UL + GetUint32();
            var utcBase = new DateTime(1904, 1, 1, 0, 0, 0);

            try
            {
                return utcBase.AddSeconds(macTimeSeconds);
            }
            catch
            {
                return utcBase;
            }
        }

        /// <summary>
        /// Skip forward from current position
        /// </summary>
        public void Skip(int dist)
        {
            _pos += dist;
        }

        /// <summary>
        /// Pick a UInt16 from an array at `baseAddr` with 0-based `index`.
        /// This does NOT use or update the current position
        /// </summary>
        public ushort PickUint16(long baseAddr, int index)
        {
            var i = index*2;
            var a = _data[baseAddr+i];
            var b = _data[baseAddr+i+1];
            return (ushort)((a << 8) + b);
        }
        
        /// <summary>
        /// Pick a signed Int16 from an array at `baseAddr` with 0-based `index`.
        /// This does NOT use or update the current position
        /// </summary>
        public short PickInt16(long baseAddr, int index)
        {
            var i = index*2;
            int a = _data[baseAddr+i];
            var b = _data[baseAddr+i+1];
            return (short)((a << 8) + b);
        }

        public PanoseClassification GetPanose()
        {
            return new PanoseClassification{
                bFamilyType = GetUint8(),
                bSerifStyle = GetUint8(),
                bWeight = GetUint8(),
                bProportion = GetUint8(),
                bContrast = GetUint8(),
                bStrokeVersion = GetUint8(),
                bArmStyle = GetUint8(),
                bLetterform = GetUint8(),
                bMidline = GetUint8(),
                bXHeight = GetUint8()
            };
        }

        public void PushPosition()
        {
            _offsets.Push(Position());
        }

        public void PopPosition()
        {
            Seek(_offsets.Pop());
        }
    }
}