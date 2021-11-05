using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace Form8snCore.FileFormats
{
    public class IndexFile
    {
        public IndexFile(string name)
        {
            Name = name;
            Notes = "";
            Pages = new List<TemplatePage>();
            DataFilters = new Dictionary<string, MappingInfo>();
        }

        /// <summary>
        /// Optional: file path for a sample input file
        /// </summary>
        public string? SampleFileName { get; set; }

        /// <summary>
        /// Optional: Path for the source PDF to be filled in.
        /// If this is not supplied, each rendered page will use a background image.
        /// </summary>
        public string? BasePdfFile { get; set; }
        
        /// <summary>
        /// Notes for the template maintainer. Not used for generating files
        /// </summary>
        public string Notes { get; set; }
        
        /// <summary>
        /// Title embedded into the PDF file
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Optional: set a default font size across the document
        /// </summary>
        public int? BaseFontSize { get; set; }

        /// <summary>
        /// Optional: set the font family name.
        /// </summary>
        public string? FontName { get; set; }
        
        /// <summary>
        /// List of page templates
        /// </summary>
        public List<TemplatePage> Pages { get; set; }
        
        /// <summary>
        /// Mapping configuration for this template
        /// </summary>
        public Dictionary<string, MappingInfo> DataFilters { get; set; }

        /// <summary>
        /// Generate a new index file from an existing PDF
        /// </summary>
        /// <param name="pdfSource">Stream holding a PDF file</param>
        /// <param name="pdfReloadUrl">URL from which the PDF can be reloaded later, for use by the renderer and clients</param>
        /// <param name="templateName"></param>
        public static IndexFile FromExistingPdf(Stream pdfSource, string pdfReloadUrl, string templateName)
        {
            using var pdf = PdfReader.Open(pdfSource);
            
            var pages = new List<TemplatePage>();
            var pageNumber = 1;
            foreach (var pdfPage in pdf.Pages)
            {
                var page = new TemplatePage
                {
                    Boxes = new Dictionary<string, TemplateBox>(),
                    Name = $"Page {pageNumber} of {pdf.PageCount}",
                    Notes = null,
                    BackgroundImage = null,
                    HeightMillimetres = pdfPage.Height.Millimeter,
                    WidthMillimetres = pdfPage.Width.Millimeter,
                    RenderBackground = false,
                    RepeatMode = new RepeatMode{Repeats = false, DataPath = null},
                    PageFontSize = null,
                    PageDataFilters = new Dictionary<string, MappingInfo>()
                };
                
                // Try to read pre-existing boxes, if there is AcroForms data in this PDF
                ReadExistingBoxes(pdf, pdfPage, pageNumber, page);
                pages.Add(page);
                pageNumber++;
            }
            
            return new IndexFile(templateName){
                Notes = "",
                Pages = pages,
                DataFilters = new Dictionary<string, MappingInfo>(),
                FontName = null,
                BaseFontSize = null,
                BasePdfFile = pdfReloadUrl,
                SampleFileName = null
            };
        }

        /// <summary>
        /// See PDF reference, section 8.6;Table 8.49; pdf-page 551
        /// </summary>
        private static void ReadExistingBoxes(PdfDocument pdf, PdfPage pdfPage, int pdfPageNumber, TemplatePage page)
        {
            var form = pdf.AcroForm;
            if (form == null) Console.WriteLine("No form found");
            else
            {
                for (var fieldNumber = 0; fieldNumber < form.Fields.Count; fieldNumber++)
                {
                    var field = form.Fields[fieldNumber];
                    field.GetPage(out var pageNum);
                    
                    if (pageNum != pdfPageNumber) continue;

                    switch (field)
                    {
                        // the fields that make sense to import
                        case PdfTextField _:
                        case PdfCheckBoxField _:
                        case PdfChoiceField _:
                        case PdfGenericField _:
                        case PdfSignatureField _:
                            AddBoxForAcroField(page, pdfPage, field, fieldNumber);
                            break;
                        
                        // ignore everything else
                        default:
                            Console.WriteLine("Ignoring a field");
                            break;
                    }
                }
            }
        }

        private static void AddBoxForAcroField(TemplatePage page, PdfPage pdfPage, PdfAcroField field, int fieldNumber)
        {
            
            // TODO: convert right&bottom to width & height
            // TODO: convert from std units to millimetres.
            if (!field.Elements.ContainsKey("/Rect")) return;
            var rect = field.Elements["/Rect"] as PdfArray; // or PdfRectangle?
            if (rect is null) return;

            var name = $"Pdf field {fieldNumber}";
            if (field.Elements.ContainsKey("/TU"))
            {
                if (field.Elements["/TU"] is PdfString description)
                {
                    name = "Pdf field " + description.Value;
                }
            }

            AddTemplateBox(page, pdfPage, rect, name);
        }

        private static void AddTemplateBox(TemplatePage page, PdfPage pdfPage, PdfArray rect, string name)
        {
            var a = rect.Elements[0] as PdfReal;
            var b = rect.Elements[1] as PdfReal;
            var c = rect.Elements[2] as PdfReal;
            var d = rect.Elements[3] as PdfReal;
            if (a is null || b is null || c is null || d is null) return;

            // Convert rect to millimeters,
            // and flip vertically (PDF 0,0 is bottom-left; XGraphics, images, and template 0,0 is top-left)
            var x1 = XUnit.FromPoint(a.Value).Millimeter;
            var x2 = XUnit.FromPoint(c.Value).Millimeter;
            var y1 = pdfPage.Height.Millimeter - XUnit.FromPoint(b.Value).Millimeter;
            var y2 = pdfPage.Height.Millimeter - XUnit.FromPoint(d.Value).Millimeter;
            
            // normalise rectangle (PDF allows for any opposite corners)
            var left = Math.Min(x1,x2);
            var right = Math.Max(x1,x2);
            var top = Math.Min(y1,y2);
            var bottom = Math.Max(y1,y2);
            
            var boxName = name;
            for (var i = 0; i < 100; i++)
            {
                if (!page.Boxes.ContainsKey(boxName)) break;
                boxName = name+"_"+i;
            }

            page.Boxes.Add(boxName, new TemplateBox
            {
                Alignment = TextAlignment.BottomLeft,
                
                Left = left, Top = top,
                Width = right-left,
                Height = bottom-top,
                
                BoxOrder = 1, DependsOn = "otherBoxName",
                DisplayFormat = new DisplayFormatFilter { Type = DisplayFormatType.DateFormat, FormatParameters = new Dictionary<string, string>() },
                MappingPath = new string[] { "path", "is", "here" },
                WrapText = true, ShrinkToFit = true, BoxFontSize = 16
            });
        }
    }
}