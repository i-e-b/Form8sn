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
        public string BasePdfFile { get; set; } = "";
        
        /// <summary>
        /// URL from which the template definition index can be loaded
        /// </summary>
        public string ProjectLoadUrl { get; set; } = "";
        
        /// <summary>
        /// URL to which we can post an updated template definition index
        /// </summary>
        public string ProjectStoreUrl { get; set; } = "";
        
        /// <summary>
        /// ID of the document template being edited (see FileDatabaseStub)
        /// </summary>
        public string DocumentId { get; set; } = "";
        
        /// <summary>
        /// URL that provides a partial view for editing a single template box
        /// </summary>
        public string BoxEditPartialUrl { get; set; } = "";
        
        /// <summary>
        /// URL that provides a partial view for picking a data path
        /// </summary>
        public string DataPathPartialUrl { get; set; } = "";
        
        /// <summary>
        /// URL that provides a partial view for editing a template box's display format
        /// </summary>
        public string DisplayFormatPartialUrl { get; set; } = "";
        
        /// <summary>
        /// URL that provides a partial view for editing a document's setting and filters
        /// </summary>
        public string DocumentInfoPartialUrl { get; set; } = "";
        
        /// <summary>
        /// URL that provides a partial view for editing page specific setting and filters
        /// </summary>
        public string PageInfoPartialUrl { get; set; } = "";

        /// <summary>
        /// URL that provides a partial view for editing data filters
        /// </summary>
        public string FilterEditPartialUrl { get; set; } = "";
        
        /// <summary>
        /// URL that stores a new size and location for a template page box
        /// </summary>
        public string BoxMoveUrl { get; set; } = "";
        
        /// <summary>
        /// URL that adds a new filter to a document or page
        /// </summary>
        public string AddFilterUrl { get; set; } = "";

        /// <summary>
        /// URL that deletes an existing filter from a document or page
        /// </summary>
        public string DeleteFilterUrl { get; set; } = "";
        
        /// <summary>
        /// URL for loading general files (used for base PDFs and JPEG images)
        /// </summary>
        public string FileLoadUrl { get; set; } = "";

    }
}