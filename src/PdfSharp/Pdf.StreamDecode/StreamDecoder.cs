using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using Portable.Drawing;
using Portable.Drawing.Drawing2D;
using Portable.Drawing.Toolkit.Portable;

namespace PdfSharp.Pdf.StreamDecode
{
    public static class DecodeExtensions { public static StreamDecoder RecoverInstructions(this PdfPage page) => new(page); }
    
    public class StreamDecoder
    {
        private readonly PdfPage _page;
        private readonly CSequence _content;

        public StreamDecoder(PdfPage page)
        {
            _page = page;
            // https://www.adobe.com/content/dam/acom/en/devnet/pdf/pdfs/pdf_reference_archives/PDFReference.pdf
            // Chapters 4, 5, 9. Start at page 134 (pdf154), table 4.1 - operator categories; 156 (table 4.7) and page 163 (table 4.9 - paths)
            // https://stackoverflow.com/questions/29467539/encoding-of-pdf-text-string
            _content = ContentReader.ReadContent(page);
            
            // Uncomment to spew the page into stdout
            /*foreach (var c in _content)
            {
                if (c is not COperator op) Console.WriteLine($"####???{c.GetType().Name} - {c}");
                else Console.WriteLine($"{op.GetType().FullName} => {op.Name} ({op.OpCode.Description}) - {ShortDesc(op.Operands)}");
            }*/
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

                default: return $"=>{op.GetType().FullName}: {op}";
            }
        }

        public void RenderToImage(Bitmap bmp)
        {
            var targetWidth = bmp.Width;
            var targetHeight = bmp.Height;
            var sourceWidth = _page.Width.Point;
            var sourceHeight = _page.Height.Point;
            
            if (Min(targetWidth, targetHeight, sourceWidth, sourceHeight) < 1) return; // Nothing to draw
            
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            
            var sx = targetWidth / sourceWidth;
            var sy = targetHeight / sourceHeight;
            // These will need wrapped in a state object...
            PointF textPoint = new PointF();
            Font defaultFont = new("Arial", 8);
            var pf = new PortableFont(defaultFont, g.DpiX);
            
            g.RotateTransform(-10.0f); // Just for testing -- demo that we affect fonts

            foreach (var c in _content)
            {
                if (c is not COperator op)
                {
                    Console.WriteLine($"Unknown root element: {c.GetType().Name} - {c}");
                    continue;
                }

                switch (op.OpCode.OpCodeName)
                {
                    case OpCodeName.Dictionary:
                        break;
                    case OpCodeName.b://Close, fill, and stroke path using nonzero winding number rule.
                        // TODO: this
                        break;
                    case OpCodeName.B:// Fill and stroke path using nonzero winding number rule.
                        // TODO: this
                        break;
                    case OpCodeName.bx:// Close, fill, and stroke path using even-odd rule.
                        // TODO: this
                        break;
                    case OpCodeName.Bx:// Fill and stroke path using even-odd rule.
                        // TODO: this
                        break;
                    case OpCodeName.BDC:// (PDF 1.2) Begin marked-content sequence with property list.
                        break;
                    case OpCodeName.BI:// Begin inline image object.
                        break;
                    case OpCodeName.BMC:// (PDF 1.2) Begin marked-content sequence.
                        break;
                    case OpCodeName.BT:// Begin text object.
                        // TODO: need the position
                        //textPoint = new PointF(0,0); // ?
                        break;
                    case OpCodeName.BX:// (PDF 1.1) Begin compatibility section.
                        break;
                    case OpCodeName.c:// append curved segment to path (3 control points)
                        // TODO: this
                        break;
                    case OpCodeName.cm:
                        break;
                    case OpCodeName.CS:
                        break;
                    case OpCodeName.cs:
                        break;
                    case OpCodeName.d:
                        break;
                    case OpCodeName.d0:
                        break;
                    case OpCodeName.d1:
                        break;
                    case OpCodeName.Do:
                        break;
                    case OpCodeName.DP:
                        break;
                    case OpCodeName.EI:
                        break;
                    case OpCodeName.EMC:
                        break;
                    case OpCodeName.ET:
                        break;
                    case OpCodeName.EX:
                        break;
                    case OpCodeName.f:// Fill path using non-zero winding rule
                    case OpCodeName.F:
                        // TODO: this
                        break;
                    case OpCodeName.fx:
                        break;
                    case OpCodeName.G:
                        break;
                    case OpCodeName.g:
                        break;
                    case OpCodeName.gs:
                        break;
                    case OpCodeName.h:// Close sub-path
                        // TODO: this
                        break;
                    case OpCodeName.i:
                        break;
                    case OpCodeName.ID:
                        break;
                    case OpCodeName.j:
                        break;
                    case OpCodeName.J:
                        break;
                    case OpCodeName.K:// Set CMYK color for stroking operations
                        // TODO: this
                        break;
                    case OpCodeName.k:// set CMYK color for non-stroking operations
                        // TODO: this
                        break;
                    case OpCodeName.l:// line-to - append straight line segment to path
                        // TODO: this
                        break;
                    case OpCodeName.m:// move-to - begin new sub path
                        // TODO: this
                        break;
                    case OpCodeName.M:
                        break;
                    case OpCodeName.MP:
                        break;
                    case OpCodeName.n:
                        break;
                    case OpCodeName.q:// Push graphics state
                        Console.Write("q; ");
                        break;
                    case OpCodeName.Q:// Pop graphics state
                        Console.Write("Q; ");
                        break;
                    case OpCodeName.re:// Add rectangle to path
                        // TODO: this
                        break;
                    case OpCodeName.RG:// set RGB color for stroking operations
                        // TODO: this
                        break;
                    case OpCodeName.rg:// set RGB color for non-stroking operations
                        // TODO: this
                        break;
                    case OpCodeName.ri:
                        break;
                    case OpCodeName.s:// Close path and stroke
                        // TODO: this
                        break;
                    case OpCodeName.S:// Stroke path
                        // TODO: this
                        break;
                    case OpCodeName.SC:
                        break;
                    case OpCodeName.sc:
                        break;
                    case OpCodeName.SCN:
                        break;
                    case OpCodeName.scn:
                        break;
                    case OpCodeName.sh:
                        break;
                    case OpCodeName.Tx:
                        break;
                    case OpCodeName.Tc:
                        break;
                    case OpCodeName.Td:// Move text position: Move to the start of the next line, offset by [0],[1]
                        Console.Write("Td; ");
                        if (op.Operands.Count != 2) throw new Exception("Unexpected op length in Td");
                        var x = GetNumber(op, 0);
                        var y = GetNumber(op, 1);
                        textPoint = new PointF((float)x, textPoint.Y + (float)y + pf.GetLineHeight());
                        break;
                    case OpCodeName.TD:// Move text position and set leading
                        Console.Write("TD; ");
                        break;
                    case OpCodeName.Tf:
                        break;
                    case OpCodeName.Tj:// Show text
                        if (op.Operands.Count != 1) throw new Exception("Unexpected op length in Tj");
                        var str = GetString(op, 0);
                        var adv = g.MeasureString(str, defaultFont);
                        g.DrawString(str, defaultFont, Brushes.Black, textPoint);
                        textPoint = new PointF(textPoint.X + adv.Width, textPoint.Y);
                        break;
                    case OpCodeName.TJ:// Show text with glyph positioning
                        
                        var m = GetFloatArray(op, 6);
                        /*
                         [ [0] [1] 0
                           [2] [3] 0
                           [4] [5] 1 ]
                         */
                        //This replaces (rather that concatenating) the existing matrix
                        
                        //g.MultiplyTransform(new Matrix(m[0],m[1], m[2],m[3], m[4],m[5])); // pretty sure this is wrong
                        textPoint = new PointF(textPoint.X + m[0], textPoint.Y + m[1]); // this is just for testing
                        Console.WriteLine($"TJ - {string.Join(",",m)} would be x={m[0]}, y={m[1]} ? ; ");
                        break;
                    case OpCodeName.TL:
                        break;
                    case OpCodeName.Tm:// Set text matrix and text line matrix
                        Console.Write("Tm; ");
                        break;
                    case OpCodeName.Tr:
                        break;
                    case OpCodeName.Ts:
                        break;
                    case OpCodeName.Tw:
                        break;
                    case OpCodeName.Tz:
                        break;
                    case OpCodeName.v:
                        break;
                    case OpCodeName.w:
                        break;
                    case OpCodeName.W:
                        break;
                    case OpCodeName.Wx:
                        break;
                    case OpCodeName.y:
                        break;
                    case OpCodeName.QuoteSingle:
                        break;
                    case OpCodeName.QuoteDbl:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            g.DrawLine(Pens.Black, 0,0, 100,150);
            g.DrawLine(Pens.Red, 0,0, 150,100);
            g.DrawLine(Pens.DarkCyan, 0,0, 150,150);
            
        }

        private float[] GetFloatArray(COperator op, int count)
        {
            var outp = new float[count];
            
            if (op.Operands[0] is CArray arr && arr.Count != 1)
            {
                var least = Math.Min(arr.Count, count); // anything not supplied should be zero?
                for (int i = 0; i < least; i++)
                {
                    if (arr[i] is CReal real) outp[i] = (float)real.Value;
                    else if (arr[i] is CInteger integer) outp[i] = integer.Value;
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    outp[i] = (float)GetNumber(op, i);
                }
            }

            return outp;
        }

        private double[] GetNumberArray(COperator op, int count)
        {
            var outp = new double[count];
            
            if (op.Operands[0] is CArray arr && arr.Count != 1)
            {
                var least = Math.Min(arr.Count, count); // anything not supplied should be zero?
                for (int i = 0; i < least; i++)
                {
                    if (arr[i] is CReal real) outp[i] = real.Value;
                    else if (arr[i] is CInteger integer) outp[i] = integer.Value;
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    outp[i] = GetNumber(op, i);
                }
            }

            return outp;
        }

        private string GetString(COperator op, int i)
        {
            if (op.Operands[i] is CString str) return str.Value; // TODO: needs decoding
            throw new Exception($"Unhandled string type: {op.Operands[i].GetType().FullName}");
        }

        private double GetNumber(COperator op, int i)
        {
            if (op.Operands[i] is CReal real) return real.Value;
            if (op.Operands[i] is CInteger integer) return integer.Value;
            if (op.Operands[i] is CArray arr)
            {
                if (arr.Count < 0) throw new Exception("Unexpected empty array when looking for a number");
                if (arr.Count > 1) throw new Exception("Unexpected long array when looking for a number");
                var v = arr[0];
                if (v is CReal vReal) return vReal.Value;
                if (v is CInteger vInteger) return vInteger.Value;
            }

            throw new Exception($"Expected numeric, got {op.Operands[i].GetType().FullName}");
        }

        private double Min(params double[] x)
        {
            if (x.Length<1) return 0.0;
            var m = x[0];
            for (int i = 1; i < x.Length; i++) { m = Math.Min(m, x[i]); }
            return m;
        }
    }
}