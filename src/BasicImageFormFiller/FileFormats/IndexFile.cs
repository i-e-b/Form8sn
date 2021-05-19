using System.Collections.Generic;

namespace BasicImageFormFiller.FileFormats
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

        public IndexFile() {
            Name = "Untitled";
            Notes = "";
            Pages = new List<TemplatePage>();
            DataFilters = new Dictionary<string, MappingInfo>();
        }

        public string? SampleFileName { get; set; }
        public string Notes { get; set; }
        public string Name { get; set; }
        public List<TemplatePage> Pages { get; set; }
        public Dictionary<string, MappingInfo> DataFilters { get; set; }
    }
}