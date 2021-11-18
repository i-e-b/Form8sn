using System.Collections.Generic;
using System.Linq;
using Form8snCore.FileFormats;
using Form8snCore.HelpersAndConverters;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebFormFiller.Models
{
    public class DisplayFormatViewModel
    {
        #region Read-Only Properties
        /// <summary>
        /// DocumentId of the template project we are editing
        /// </summary>
        public int DocumentId { get; set; }
        /// <summary>
        /// Index in the document's page list that contains the box to be edited
        /// </summary>
        public int PageIndex { get; set; }
        /// <summary>
        /// Name of the box. This is used as the key into the project index file, and
        /// should NOT be edited
        /// </summary>
        public string? BoxKey { get; set; }
        
        /// <summary>
        /// Internal revision number. Used to guard against data loss if an old save command comes in very late.
        /// </summary>
        public int Version { get; set; }

        #endregion
        
        /// <summary>
        /// The display format to be edited
        /// </summary>
        public DisplayFormatFilter? DisplayFormat { get; set; }
        
        /// <summary>
        /// UI-side selected filter type
        /// </summary>
        public string? FormatFilterType { get; set; }
        
        #region Parameter values
        
        // these must be named the same as parameters in Form8snCore/FileFormats/DisplayFormatType.cs
        public string? FormatString { get; set; }
        public string? DecimalPlaces { get; set; }
        public string? DecimalSeparator { get; set; }
        public string? Prefix { get; set; }
        public string? Postfix { get; set; }
        public string? ThousandsSeparator { get; set; }
        #endregion

        public DisplayFormatViewModel()
        {
            AvailableFilterTypes = EnumOptions.AllDisplayFormatTypes().Select(SelectorItemForEnum).ToList();
        }

        public IEnumerable<SelectListItem> AvailableFilterTypes { get; set; }

        public static DisplayFormatViewModel From(TemplateProject project, int docId, int pageIndex, string boxKey)
        {
            var thePage = project.Pages[pageIndex];
            
            var theBox = thePage.Boxes[boxKey];
            
            
            var model = new DisplayFormatViewModel{
                // Keys, not to be edited
                PageIndex = pageIndex,
                Version = (project.Version ?? 0) + 1,
                DocumentId = docId,
                BoxKey = boxKey,
                
                // Editable values
                DisplayFormat = theBox.DisplayFormat,
                FormatFilterType = theBox.DisplayFormat?.Type.ToString(),
                
                FormatString = ReadParam(theBox.DisplayFormat?.FormatParameters, nameof(DateDisplayParams.FormatString)),
                
                DecimalPlaces = ReadParam(theBox.DisplayFormat?.FormatParameters, nameof(FractionalDisplayParams.DecimalPlaces))
                             ?? ReadParam(theBox.DisplayFormat?.FormatParameters, nameof(NumberDisplayParams.DecimalPlaces)),
                
                Postfix = ReadParam(theBox.DisplayFormat?.FormatParameters, nameof(NumberDisplayParams.Postfix)),
                Prefix = ReadParam(theBox.DisplayFormat?.FormatParameters, nameof(NumberDisplayParams.Prefix)),
                DecimalSeparator = ReadParam(theBox.DisplayFormat?.FormatParameters, nameof(NumberDisplayParams.DecimalSeparator)),
                ThousandsSeparator = ReadParam(theBox.DisplayFormat?.FormatParameters, nameof(NumberDisplayParams.ThousandsSeparator))
            };
            return model;
        }


        private static SelectListItem SelectorItemForEnum(EnumOption e)
        {
            if (string.IsNullOrWhiteSpace(e.Description)) return new SelectListItem(e.Name, e.Name);
            return new SelectListItem(e.Name + " - " + e.Description, e.Name);
        }
        
        private static string? ReadParam(Dictionary<string, string>? paramSet, string name)
        {
            if (paramSet is null) return null;
            if (paramSet.ContainsKey(name)) return paramSet[name];
            return null;
        }
    }
}