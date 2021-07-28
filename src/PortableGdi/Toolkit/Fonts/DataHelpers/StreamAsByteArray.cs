using System.IO;

namespace Portable.Drawing.Toolkit.Fonts
{
    /// <summary>
    /// Helper to treat a file stream like a byte array
    /// </summary>
    internal class StreamAsByteArray
    {
        private readonly FileStream _stream;
        private readonly object _lock = new object();

        public StreamAsByteArray(FileStream stream)
        {
            _stream = stream;
        }

        public int this[long pos]
        {
            get {
                lock (_lock)
                {
                    if (pos == _stream.Position) return _stream.ReadByte();

                    _stream.Seek(pos, SeekOrigin.Begin);
                    return _stream.ReadByte();
                }
            }
        }
    }
}