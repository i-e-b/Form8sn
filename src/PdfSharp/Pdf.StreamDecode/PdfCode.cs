using System.Collections.Generic;

namespace PdfSharp.Pdf.StreamDecode
{

    /// <summary>
    /// NOTE: IEB: in progress.
    ///
    /// This is a class to hold decoded PDF code streams.
    /// The idea is to be able to reconstruct drawing commands from these,
    /// so we can fully edit or render existing PDF documents.
    /// </summary>
    public class PdfCode
    {
        public PdfTokenType TokenType { get; set; }

        public Stack<string> Arguments { get; set;}
    }
}