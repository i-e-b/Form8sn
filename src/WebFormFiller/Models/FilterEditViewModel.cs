using System.Collections.Generic;
using System.Linq;
using Form8snCore.HelpersAndConverters;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebFormFiller.Models
{
    public class FilterEditViewModel
    {
        #region Read-Only Properties
        /// <summary>
        /// DocumentId of the template project we are editing
        /// </summary>
        public int DocumentId { get; set; }
        /// <summary>
        /// Index in the document's page list that contains the filter to be edited. If null, edit document filters
        /// </summary>
        public int? PageIndex { get; set; }
        /// <summary>
        /// Name of the filter. This is used as the key into the project index file, and should NOT be edited
        /// </summary>
        public string? FilterKey { get; set; }
        
        /// <summary>
        /// Internal revision number. Used to guard against data loss if an old save command comes in very late.
        /// </summary>
        public int Version { get; set; }

        #endregion
        
        public FilterEditViewModel()
        {
            AvailableFilterTypes = EnumOptions.AllDataFilterTypes().Select(SelectorItemForEnum).ToList();
        }
        
        /// <summary>
        /// UI-side selected filter type
        /// </summary>
        public string? FormatFilterType { get; set; }

        public IEnumerable<SelectListItem> AvailableFilterTypes { get; set; }
        
        
        private static SelectListItem SelectorItemForEnum(EnumOption e)
        {
            if (string.IsNullOrWhiteSpace(e.Description)) return new SelectListItem(e.Name, e.Name);
            return new SelectListItem(e.Name + " - " + e.Description, e.Name);
        }
    }
}