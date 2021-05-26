using Form8snCore.FileFormats;

namespace Form8snCore.Rendering
{
    /// <summary>
    /// A box definition plus specific data to be rendered
    /// </summary>
    public class DocumentBox
    {
        public DocumentBox(TemplateBox src, string? dataString)
        {
            Definition = src;
            RenderContent = dataString;
        }

        public DocumentBox(TemplateBox src)
        {
            Definition = src;
            RenderContent = null;
        }
        
        public TemplateBox Definition { get; set; }
        public string? RenderContent { get; set; }
    }
}