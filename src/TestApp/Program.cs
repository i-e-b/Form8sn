using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.StreamDecode;

namespace TestApp
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Numbers test");
            Console.WriteLine((123_456.000_1).ToString("0,000.000"));
            Console.WriteLine((123_456.000_1).ToString("0,000.#"));
            Console.WriteLine((123_456.000_1).ToString("0,000."));
            Console.WriteLine((123_456.000_1).ToString("#,##0.000"));
            Console.WriteLine((789_123_456.000_1).ToString("#,##0.000"));
            
            return;
            Console.WriteLine("Creating a PDF from scratch...");
            var document = new PdfDocument();

            document.Info.Title = "Created with PDFsharp";
            // Create an empty page
            var page = document.AddPage();

            // Get an XGraphics object for drawing
            var gfx = XGraphics.FromPdfPage(page);

            // Create a font
            var font = new XFont("Verdana", 20, XFontStyle.BoldItalic);

            // Draw the text
            gfx.DrawString("Hello, World!", font, XBrushes.Black,
                new XRect(0, 0, page.Width, page.Height),
                XStringFormats.Center);

            Console.WriteLine("Saving...");
            // Save the document...
            const string filename = "HelloWorld.pdf";
            document.Save(filename);
            
            Console.WriteLine("Trying to open an existing PDF");

            var sw = new Stopwatch();
            sw.Start();
            using (var existing = PdfReader.Open("big_test.pdf"))
            {
                Console.WriteLine($"Found {existing.FullPath}: {existing.Pages.Count} pages");
                
                int pageNumber = 0;
                foreach (var ePage in existing.Pages)
                {
                    pageNumber++;
    
                    ExtractImages(ePage);
                    
                    var bits = ePage!.Contents.ToList();
                    foreach (var item in bits)
                    {
                        Console.WriteLine("------------");
                        
                        LogPdfItem(item);
                    }
                    Console.WriteLine("==================================================");

                    using (var g = XGraphics.FromPdfPage(ePage))
                    {
                        g.DrawString("Hello, World!", font, XBrushes.Black,
                            new XRect(0, 0, page.Width, page.Height),
                            XStringFormats.Center);
                    }
                }
                
                existing.Save("big_test_edited.pdf");
            }
            sw.Stop();
            Console.WriteLine($"Opening editing and writing PDF took {sw.Elapsed}");

            Console.WriteLine("Done.");
        }

        
        private static int imageCount = 1;
        private static void ExtractImages(PdfDictionary page)
        {
            var resources = page.Elements.GetDictionary("/Resources");
            if (resources == null) return;
            
            var xObjects = resources.Elements.GetDictionary("/XObject");
            if (xObjects == null) return;
            
            foreach (var key in xObjects.Elements.Keys)
            {
                var item = xObjects.Elements[key];
                var reference = item as PdfReference;
                if (reference == null) continue;

                if (reference.Value is PdfDictionary xObject
                    && xObject.Elements.GetString("/Subtype") == "/Image")
                {
                    ExportImage(xObject, $"{key}", ref imageCount);
                }
            }
        }
        
        static void ExportImage(PdfDictionary image, string name, ref int count)
        {
            var filter = image?.Elements.GetName("/Filter");
            switch (filter)
            {
                case "/DCTDecode":
                    ExportJpegImage(image, name, ref count);
                    break;
 
                case "/FlateDecode":
                    ExportAsPngImage(image, name, ref count);
                    break;
            }
        }
        static void ExportJpegImage(PdfDictionary image, string name, ref int count)
        {
            // Fortunately JPEG has native support in PDF and exporting an image is just writing the stream to a file.
            byte[] stream = image.Stream.Value;
            FileStream fs = new FileStream($"{CleanName(name)}_{count++}.jpeg", FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(stream);
            bw.Close();
        }

        private static string CleanName(string name)
        {
            var sb = new StringBuilder();
            bool lost = false;
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c)) { sb.Append(c); lost = false; }
                else if (!lost) { sb.Append('_'); lost = true; }
            }
            return sb.ToString();
        }

        static void ExportAsPngImage(PdfDictionary image, string name, ref int count)
        {
            int width = image.Elements.GetInteger(PdfImage.Keys.Width);
            int height = image.Elements.GetInteger(PdfImage.Keys.Height);
            int bitsPerComponent = image.Elements.GetInteger(PdfImage.Keys.BitsPerComponent);
 
            Console.WriteLine($"Unhandled image, not exported: w{width} h{height} {bitsPerComponent}bpp");
            // TODO: You can put the code here that converts vom PDF internal image format to a Windows bitmap
            // and use GDI+ to save it in PNG format.
            // It is the work of a day or two for the most important formats. Take a look at the file
            // PdfSharp.Pdf.Advanced/PdfImage.cs to see how we create the PDF image formats.
            // We don't need that feature at the moment and therefore will not implement it.
            // If you write the code for exporting images I would be pleased to publish it in a future release
            // of PDFsharp.
        }
        

        private static void LogPdfItem(PdfItem item)
        {
            Console.Write("####" + item!.GetType().Name);

            if (item is not PdfReference reference) return;
            
            Console.Write(" -> " + reference.Value.GetType());
            if (reference.Value is not PdfContent cont) return;

            foreach (var element in cont.Elements)
            {
                Console.WriteLine("    " + element);
            }
            Console.WriteLine("..................................................");
            Console.WriteLine(cont.Stream.ToString());
            
            // TODO: implement this to get re-rendering
            // https://www.adobe.com/content/dam/acom/en/devnet/pdf/pdfs/pdf_reference_archives/PDFReference.pdf
            // Chapters 4, 5, 9. Start at page 134 (pdf154), table 4.1 - operator categories; 156 (table 4.7) and page 163 (table 4.9 - paths)
            var instructionList = cont.Stream.RecoverInstructions();
            foreach (var instruction in instructionList)
            {
                Console.Write(instruction.ToString());
            }
            /*
0.1 w                                   ; line width = 0.1
q 56.645 56.733 728.509 481.939 re      ; push state, add rectangle to current path
W* n                                    ; W* - Intersect clip with current path (* = using even-odd rule), n = End the path object without filling or stroking it.
q 728.4 0 0 481.8 -1400.1 538.653 cm    ; push state, concat transform matrix
/Im20 Do Q                              ; Named object 'Im20' (this is a reference in Resources->XObject->/Im20) Draws the image (Do = "Paint the specified XObject"), Pop state
q 728.4 0 0 481.8 -671.7 538.653 cm     ; Set transform
/Im20 Do Q                              ; draw the image in new location, pop state

Q
q 0 0 0 rg
BT
740.3 77.403 Td /F1 8 Tf<25>Tj
ET

*/
            
            
            // https://stackoverflow.com/questions/29467539/encoding-of-pdf-text-string
            /*             
BT
56.8 721.3 Td 
/F2 12 Tf
[<01>2<0203>2<04>-10<0503>2<04>-2<0506070809>2<0A>1<0B>]TJ
ET
             BT and ET indicate the beginning and end of a text showing section
             
             56.8 721.3 Td positions the current point to coordinates "56.8 points in horizontal, 721.3 points in vertical direction".
             
             12 Tf sets the font size to 12 points.
             
             /F1 sets the font to be use to one that is defined elsewhere in the PDF document. That font also somewhere sets a font encoding (and possibly a /ToUnicode table). The font encoding will determine which glyph shape should be drawn when a specific character code is seen in the text strings.
             
[<01>2<0203>2<04>-10<0503>2<04>-2<0506070809>2<0A>1<0B>]TJ
    
            <01>2 : <01> is the first character code. 2 is a parameter for the "individual glyph positioning" allowed when using the text show operator TJ.
            <0203>2 : <0203> are two more character codes. 2 again is a parameter for the "individual glyph positioning" for TJ.
            <04>-10 : <04> is the fourth character code. -10 again for the "individual glyph positioning" with TJ.
            <0503>2 : <05> is the fifth character code, <03> is the third character code (used before). 2 is for "individual glyph positioning"...
             
             */
        }
    }
}