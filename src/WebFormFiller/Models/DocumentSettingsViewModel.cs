using System;
using System.Collections.Generic;
using System.Linq;
using Form8snCore.FileFormats;
using Microsoft.AspNetCore.Mvc.Rendering;
using Portable.Drawing.Toolkit.Portable;

namespace WebFormFiller.Models
{
    public class DocumentSettingsViewModel
    {
        /// <summary>
        /// Generate a view model from a project file
        /// </summary>
        public static DocumentSettingsViewModel From(TemplateProject project, int docId)
        {
            return new DocumentSettingsViewModel
            {
                DocumentId = docId,
                Version = project.Version ?? 0,
                Name = project.Name,
                SampleDocumentName = project.SampleFileName ?? "<none>",

                BaseFontSize = project.BaseFontSize?.ToString(),
                FontFamily = project.FontName,
                Notes = project.Notes,
                Filters = project.DataFilters,
                KnownFontList = ListFontsOnServer(project.FontName)
            };
        }

        /// <summary>
        /// Copy view model values into an existing index file (to perform an update)
        /// </summary>
        public void CopyTo(TemplateProject target)
        {
            target.BaseFontSize = ParseOrNull(BaseFontSize);
            target.FontName = FontFamily;
            target.Name = Name ?? "Untitled";
            target.Notes = Notes ?? "";
        }

        public int DocumentId { get; set; }
        public int Version { get; set; }
        public string? Name { get; set; } = "";
        public string? BaseFontSize { get; set; }
        public string? FontFamily { get; set; }
        public string? Notes { get; set; }
        public string? SampleDocumentName { get; set; }
        public IDictionary<string, MappingInfo> Filters { get; set; } = new Dictionary<string, MappingInfo>();

        // To see how this is populated: WebFormFiller/wwwroot/js/formSubmit.js
        public string? FileUpload { get; set; }
        public string? FileUploadName { get; set; }

        #region Inner workings

        public IEnumerable<SelectListItem> KnownFontList { get; set; } = Array.Empty<SelectListItem>();

        private static IEnumerable<SelectListItem> ListFontsOnServer(string? selected)
        {
            return PortableFont.ListKnownFonts().Select(name => new SelectListItem(name, name, name == selected));
        }

        private static int? ParseOrNull(string? baseFontSize)
        {
            if (baseFontSize is null) return null;
            if (!int.TryParse(baseFontSize, out var size)) return null;
            return size;
        }

        #endregion
    }
}