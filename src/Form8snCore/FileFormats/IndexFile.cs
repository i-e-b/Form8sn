using System.Collections.Generic;

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
        /// Optional: internal revision number. Used to guard against data loss with async UIs
        /// </summary>
        public int? Version { get; set; }
        
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
    }
}