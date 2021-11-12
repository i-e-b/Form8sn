using System.Collections.Generic;
using Form8snCore.FileFormats;

namespace Form8snCore.Rendering
{
    /// <summary>
    /// Represents a page that is ready to add to a PDF
    /// </summary>
    public class DocumentPage
    {
        public DocumentPage(TemplatePage src, int sourcePageIndex)
        {
            Definition = src;
            SourcePageIndex = sourcePageIndex;
            DocumentBoxes = new Dictionary<string, DocumentBox>();
        }
        
        public TemplatePage Definition { get; set; }
        public Dictionary<string, DocumentBox> DocumentBoxes { get; set; }
        
        public int SourcePageIndex { get; set; }
        public int RepeatIndex { get; set; }
        public int RepeatCount { get; set; }
    }
}