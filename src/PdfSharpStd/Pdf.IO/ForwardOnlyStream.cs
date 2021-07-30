using System;
using System.IO;

namespace PdfSharp.Pdf.IO
{
    /// <summary>
    /// Diagnostic helper for stream
    /// </summary>
    internal class ForwardOnlyStream : IDisposable
    {
        private readonly Stream _baseStream;
        private readonly object _lock = new();
        
        private long _hardPosition;

        public ForwardOnlyStream(Stream baseStream)
        {
            _baseStream = baseStream;
            _hardPosition = _baseStream.Length;
        }

        public long Position => _hardPosition;

        public void Dispose() {
            try
            {
                _baseStream.Dispose();
            }
            catch
            {
                //ignore
            }
        }

        public void Close()
        {
            _baseStream.Flush();
            _baseStream.Close();
        }

        public void Write(byte[] bytes, int i, int bytesLength)
        {
            lock (_lock)
            {
                _baseStream.Write(bytes, i, bytesLength);
                _hardPosition += bytesLength;
                if (_hardPosition != _baseStream.Length) throw new Exception("Stream writer bug!");
            }
        }

        private readonly byte[] oneBuf = new byte[1];
        public void WriteByte(byte ch)
        {
            oneBuf[0]=ch;
            Write(oneBuf, 0, 1);
        }

        public void Flush() => _baseStream.Flush();
    }
}