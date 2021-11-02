using Form8snCore.FileFormats;

namespace WebFormFiller.Models
{
    public class TemplateEditViewModel
    {
        public IndexFile Document { get; set; } = new("");
        public string PdfUrl { get; set; } = "";
    }
}