using System.Collections.Generic;

namespace Form8snCore.FileFormats
{
    public class DisplayFormatFilter
    {
        public DisplayFormatFilter() { FormatParameters = new Dictionary<string, string>(); }
        
        /// <summary>
        /// How the data should be formatted
        /// </summary>
        public DisplayFormatType Type { get; set; }
        
        /// <summary>
        /// Params for the mapping (if any)
        /// </summary>
        public Dictionary<string,string> FormatParameters { get; set; }
    }
}