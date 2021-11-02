using System.Collections.Generic;

namespace WebFormFiller.Models
{
    public class TemplateListViewModel
    {
        /// <summary>
        /// Document template ID => Name
        /// </summary>
        public IDictionary<int, string> Templates { get; set; } = new Dictionary<int, string>();
    }
}