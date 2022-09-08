using System.Collections;
using System.Collections.Generic;

namespace WebFormFiller.Models
{
    public class TemplateListViewModel
    {
        /// <summary>
        /// Document template ID => Name
        /// </summary>
        public IDictionary<int, string> Templates { get; set; } = new Dictionary<int, string>();

        /// <summary>
        /// List of image names that are stored for use in template boxes
        /// </summary>
        public IList<string> ImageStamps { get; set; } = new List<string>();
    }
}