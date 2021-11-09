using Form8snCore.FileFormats;

namespace WebFormFiller.Models
{
    public class TemplateEditViewModel
    {
        /// <summary>
        /// The definition of this document template
        /// </summary>
        public IndexFile Document { get; set; } = new("");
        
        /// <summary>
        /// A URL from which the base PDF file can be loaded
        /// </summary>
        public string PdfUrl { get; set; } = "";
        
        /// <summary>
        /// URL from which the template definition index can be loaded
        /// </summary>
        public string ProjectLoadUrl { get; set; } = "";
        
        /// <summary>
        /// ID of the document template being edited (see FileDatabaseStub)
        /// </summary>
        public string DocumentId { get; set; } = "";
        
        /// <summary>
        /// URL that provides a partial view for editing a single template box
        /// </summary>
        public string BoxEditPartialUrl { get; set; } = "";
    }
}