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

        public string? SampleFileName { get; set; }
        public string Notes { get; set; }
        public string Name { get; set; }
        
        /// <summary>
        /// Optional: set a default font size across the document
        /// </summary>
        public int? BaseFontSize { get; set; }

        /// <summary>
        /// Optional: set the font family name.
        /// </summary>
        public string? FontName { get; set; }
        
        public List<TemplatePage> Pages { get; set; }
        public Dictionary<string, MappingInfo> DataFilters { get; set; }
    }
}