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
using Portable.Drawing;
using Portable.Drawing.Imaging;

namespace TestApp
{
    class Program
    {
        static void Main()
        {
            
            #region Create PDF

            Console.WriteLine("Creating a PDF from scratch...");
            PdfPage page;
            XFont font;
            using (var document = new PdfDocument())
            {
                document.Info.Title = "Created with PDFsharp";
                // Create an empty page
                page = document.AddPage();

                // Get an XGraphics object for drawing
                var gfx = XGraphics.FromPdfPage(page);

                // Create a font
                //font = new XFont("Courier", 20, XFontStyle.BoldItalic);
                font = XFont.Default(20); // this should short-cut all the loading and caching, just let the PDF renderer guess

                // Draw the text
                gfx.DrawString("Hello, World! ©®£¥§", font, XBrushes.Black, // the end characters aren't in the fall-back font
                    new XRect(0, 0, page.Width, page.Height),
                    XStringFormats.Center);

                Console.WriteLine("Saving...");
                // Save the document...
                const string filename = "HelloWorld.pdf";
                document.Save(filename);
            }

            #endregion

            #region Open, interpret, and edit an existing PDF

            Console.WriteLine("Trying to open an existing PDF");

            var sw = new Stopwatch();
            sw.Start();
            var targetFile = "big_test";
            using (var existing = PdfReader.Open($"{targetFile}.pdf"))
            {
                var form = existing.AcroForm;
                if (form == null) Console.WriteLine("No form found");
                else
                {
                    Console.WriteLine($"Found {form.Fields.Count} form fields");
                    foreach (var field in form.Fields)
                    {
                        Console.WriteLine("Field def:");
                        LogPdfItem(field);
                    }
                }

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

                    // Try recovering some of the page to an image (proper PDF rendering)
                    AttemptToRenderPage(ePage, pageNumber); // TODO: implement properly. This is *REALLY* not working yet

                    using (var g = XGraphics.FromPdfPage(ePage))
                    {
                        g.DrawString("Hello, World!", font, XBrushes.Black,
                            new XRect(0, 0, page.Width, page.Height),
                            XStringFormats.Center);
                    }
                }

                existing.Save($"{targetFile}-edited.pdf");
            }

            sw.Stop();
            Console.WriteLine($"Opening editing and writing PDF took {sw.Elapsed}");

            #endregion

            #region Concatenate two existing PDFs into a new document

            Console.WriteLine("Trying to concat documents");

            for (int i = 1; i <= 3; i++)
            {
                MakeSmallDoc(font, $"_concat_src_{i}.pdf");
            }

            using (var concatOut = new PdfDocument())
            {
                concatOut.Info.Title = "Joined with PdfSharpStd";

                for (int i = 1; i <= 3; i++)
                {
                    Console.WriteLine($"    Reading document {i}");
                    using (var srcDoc = PdfReader.Open($"_concat_src_{i}.pdf", PdfDocumentOpenMode.Import)) // Must use import mode to copy pages across
                    {
                        var p = 0;
                        foreach (var pdfPage in srcDoc.Pages)
                        {
                            p++;
                            if (pdfPage is null) continue;
                            concatOut.AddPage(pdfPage);
                            Console.WriteLine($"        copied page {p}");
                        }
                    }
                }

                Console.WriteLine("    Saving result");
                concatOut.Save("Concatenated.pdf");
            }

            #endregion
         
            #region Read-back a file made from a concatenated set of sources

            Console.WriteLine("Trying to read previously merged pdf");
            using (var concatSrc = PdfReader.Open("Concatenated.pdf"))
            {
                Console.WriteLine($"Concat doc has {concatSrc.Pages.Count} pages");
            }

            #endregion

            Console.WriteLine("Done.");
        }

        private static void MakeSmallDoc(XFont font, string path)
        {
            using var source = new PdfDocument();
            var pg = source.AddPage();
            var gfx = XGraphics.FromPdfPage(pg);
            gfx.DrawString("This is a single page PDF generated by PdfSharp", font, XBrushes.Black, 10, 10);
            for (int i = 0; i < 60; i++)
            {
                gfx.DrawString(new string('x', 300), font, XBrushes.Black, 10, 20+ i*10);
            }
            
            source.Save(path!);
        }

        private static void AttemptToRenderPage(PdfPage ePage, int pageNumber)
        {
            var width = ePage!.Width.Presentation;
            var height = ePage.Height.Presentation;
            var bmp = new Bitmap((int) width, (int) height, PixelFormat.Format24bppRgb);

            var instructionList = ePage.RecoverInstructions();
            instructionList.RenderToImage(bmp);
            bmp.Save($"C:\\Temp\\Page{pageNumber}.png", ImageFormat.Png);
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
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lost = false;
                }
                else if (!lost)
                {
                    sb.Append('_');
                    lost = true;
                }
            }

            return sb.ToString();
        }

        static void ExportAsPngImage(PdfDictionary image, string name, ref int count)
        {
            int width = image.Elements.GetInteger(PdfImage.Keys.Width);
            int height = image.Elements.GetInteger(PdfImage.Keys.Height);
            int bitsPerComponent = image.Elements.GetInteger(PdfImage.Keys.BitsPerComponent);

            Console.WriteLine($"Deflate-decode image, not exported: w{width} h{height} {bitsPerComponent}bpp; {image.Stream.Length} bytes.");
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

            var x = item is PdfReference reference ? reference.Value : item;
            Console.Write(" -> " + x.GetType());

            if (x is PdfDictionary dict) LogPdfDict(dict);
            else Console.WriteLine($"Un-logged type {x.GetType().Name}");
        }

        private static void LogPdfDict(PdfDictionary dict)
        {
            Console.WriteLine($"Dictionary with {dict!.Elements.Count} elements");
            foreach (var element in dict.Elements)
            {
                if (element.Key == "/Rect" && element.Value is PdfArray arr)
                {
                    Console.Write($"Rect: {arr.Elements.Count} items [");
                    foreach (var ai in arr.Elements)
                    {
                        if (ai is PdfReal real)
                        {
                            Console.Write(real.Value + "; ");
                        }
                    }

                    Console.WriteLine("]");
                }
                else
                {
                    if (element.Value is PdfString str) Console.WriteLine($"Item: {element.Key} => String: '{str}'");
                    else if (element.Value is PdfName name) Console.WriteLine($"Item: {element.Key} => Name: {name.Value}");
                    else if (element.Value is PdfInteger integer) Console.WriteLine($"Item: {element.Key} => int: {integer.Value}");
                    else Console.WriteLine($"Item: {element.Key} => {element.Value.GetType().Name}");
                }
            }
        }
    }
}