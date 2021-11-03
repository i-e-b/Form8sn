using Form8snCore.FileFormats;

namespace WebFormFiller.Models
{
    public class TemplateEditViewModel
    {
        public IndexFile Document { get; set; } = new("");
        public string PdfUrl { get; set; } = "";
        public string ProjectLoadUrl { get; set; } = "";
        public string DocumentId { get; set; } = "";
    }
}