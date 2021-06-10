using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using Portable.Drawing;

namespace PdfSharp.Pdf.StreamDecode
{
    public static class DecodeExtensions { public static StreamDecoder RecoverInstructions(this PdfPage page) => new(page); }
    
    public class StreamDecoder
    {
        public StreamDecoder(PdfPage page)
        {
            // https://www.adobe.com/content/dam/acom/en/devnet/pdf/pdfs/pdf_reference_archives/PDFReference.pdf
            // Chapters 4, 5, 9. Start at page 134 (pdf154), table 4.1 - operator categories; 156 (table 4.7) and page 163 (table 4.9 - paths)
            // https://stackoverflow.com/questions/29467539/encoding-of-pdf-text-string
            var cx = ContentReader.ReadContent(page);
            
            foreach (var c in cx)
            {
                if (c is not COperator op) System.Console.WriteLine($"####???{c.GetType().Name} - {c}");
                else System.Console.WriteLine($"{op.Name} - {op.OpCode.Description} - {ShortDesc(op.Operands)}");
            }
        }

        private string ShortDesc(CSequence opOperands)
        {
            return string.Join("; ", opOperands.Select(Describe));
        }

        private string Describe(CObject op)
        {
            switch (op)
            {
                case CName name:
                    return '"'+name.Name+'"';
                case CString str:
                    return str.CStringType + " -> " + str; // needs decoding?
                case CReal real:
                    return real.ToString();
                case CInteger intg:
                    return intg.ToString();
                
                default: return op.GetType().Name + ": " + op.ToString();
            }
        }

        public void RenderToImage(Bitmap bmp)
        {
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            g.DrawLine(Pens.Black, 0,0, 100,150);
            g.DrawLine(Pens.Red, 0,0, 150,100);
            g.DrawLine(Pens.DarkCyan, 0,0, 150,150);
            
        }
    }
}