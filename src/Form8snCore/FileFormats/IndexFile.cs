using System.Collections.Generic;
using System.IO;
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
                page.Boxes.Add("Sample", new TemplateBox{
                    Alignment = TextAlignment.BottomLeft,
                    Height = 10, Width = 10, Left = 10, Top = 10,
                    BoxOrder = 1, DependsOn = "otherBoxName",
                    DisplayFormat = new DisplayFormatFilter{Type = DisplayFormatType.DateFormat, FormatParameters = new Dictionary<string, string>()},
                    MappingPath = new string[]{"path","is","here"},
                    WrapText = true, ShrinkToFit = true, BoxFontSize = 16
                });
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
    }
}