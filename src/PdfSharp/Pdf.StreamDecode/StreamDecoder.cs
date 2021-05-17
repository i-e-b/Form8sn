using System.Collections;
using System.Collections.Generic;

namespace PdfSharp.Pdf.StreamDecode
{
    public static class DecodeExtensions
    {
        public static IEnumerable<PdfCode> RecoverInstructions(this PdfDictionary.PdfStream stream)
        {
            var str = stream.ToString(); // string with filters un-applied (deflate etc)
            return new StreamDecoder(str);
        }
    }
    
    public class StreamDecoder : IEnumerable<PdfCode>
    {
        private readonly string _str;

        public StreamDecoder(string unfilteredStream)
        {
            _str = unfilteredStream;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<PdfCode> GetEnumerator()
        {
            yield break; // TODO: implement decode
        }

        /// <summary>
        /// Does this character split the token stream?
        /// </summary>
        private bool IsSplit(char c)
        {
            return c switch
            {
                // Whitespace
                ' ' => true,
                '\t' => true,
                '\x0C' => true,
                '\r' => true,
                '\n' => true,
                // array and dictionary
                '<' => true,
                '>' => true,
                '[' => true,
                ']' => true,
                '{' => true,
                '}' => true,
                // string literal
                '(' => true,
                ')' => true,
                
                // anything else is part of the tokens. (note, we don't consider '/' here)
                _ => false
            };
        }
    }
}