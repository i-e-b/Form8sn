using System;
using System.Collections.Generic;
using System.IO;
using PdfSharp.Pdf;
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
            var pageCount = 1;
            foreach (var pdfPage in pdf.Pages)
            {
                var page = new TemplatePage
                {
                    Boxes = new Dictionary<string, TemplateBox>(),
                    Name = $"Page {pageCount} of {pdf.PageCount}",
                    Notes = null,
                    BackgroundImage = null,
                    HeightMillimetres = pdfPage.Height.Millimeter,
                    WidthMillimetres = pdfPage.Width.Millimeter,
                    RenderBackground = false,
                    RepeatMode = new RepeatMode{Repeats = false, DataPath = null},
                    PageFontSize = null,
                    PageDataFilters = new Dictionary<string, MappingInfo>()
                };
                // TODO: try to read pre-existing boxes?
                //pdfPage.
                ReadExistingBoxes(pdf, pdfPage, page);
                pages.Add(page);
                pageCount++;
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
        ///  See PDF reference, section 8.6;Table 8.49; pdf-page 551
        /// </summary>
        /// <param name="pdf"></param>
        /// <param name="pdfPage"></param>
        /// <param name="page"></param>
        private static void ReadExistingBoxes(PdfDocument pdf, PdfPage pdfPage, TemplatePage page)
        {
            var form = pdf.AcroForm;
            if (form == null) Console.WriteLine("No form found");
            else
            {
                Console.WriteLine($"Found {form.Fields.Count} form fields");
                PdfObject thing;
                int i = 1;
                foreach (var field in form.Fields)
                {
                    if (field is PdfReference refr) { thing = refr.Value; }
                    else { thing = (PdfObject)field; }

                    if (thing is PdfDictionary dict)
                    {
                        var rect = dict.Elements["/Rect"] as PdfArray; // or PdfRectangle?
                        if (rect is null)
                        {
                            Console.WriteLine("Expected PdfArray");
                            continue;
                        }

                        var a = rect.Elements[0] as PdfReal;
                        var b = rect.Elements[1] as PdfReal;
                        var c = rect.Elements[2] as PdfReal;
                        var d = rect.Elements[3] as PdfReal;
                        if (a is null || b is null || c is null || d is null)
                        {
                            continue;
                        }

                        page.Boxes.Add("Auto_"+i, new TemplateBox{
                            Alignment = TextAlignment.BottomLeft,
                            Height = d.Value, Width = c.Value, Left = a.Value, Top = b.Value,
                            BoxOrder = 1, DependsOn = "otherBoxName",
                            DisplayFormat = new DisplayFormatFilter{Type = DisplayFormatType.DateFormat, FormatParameters = new Dictionary<string, string>()},
                            MappingPath = new string[]{"path","is","here"},
                            WrapText = true, ShrinkToFit = true, BoxFontSize = 16
                        });
                        i++;
                    }

                    Console.WriteLine(thing.Internals.TypeID);
                    Console.WriteLine("Field def:");
                    //LogPdfItem(field);
                    
                    /*page.Boxes.Add("Sample", new TemplateBox{
                        Alignment = TextAlignment.BottomLeft,
                        Height = 10, Width = 10, Left = 10, Top = 10,
                        BoxOrder = 1, DependsOn = "otherBoxName",
                        DisplayFormat = new DisplayFormatFilter{Type = DisplayFormatType.DateFormat, FormatParameters = new Dictionary<string, string>()},
                        MappingPath = new string[]{"path","is","here"},
                        WrapText = true, ShrinkToFit = true, BoxFontSize = 16
                    });*/
                }
            }
        }
    }
}