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
        public static DocumentSettingsViewModel From(IndexFile project, int docId)
        {
            return new DocumentSettingsViewModel{
                DocumentId = docId,
                Version = project.Version ?? 0,
                Name = project.Name,
                BaseFontSize = project.BaseFontSize?.ToString(),
                FontFamily = project.FontName,
                Notes = project.Notes,
                KnownFontList = ListFontsOnServer(project.FontName)
            };
        }
        
        /// <summary>
        /// Copy view model values into an existing index file (to perform an update)
        /// </summary>
        public void CopyTo(IndexFile target)
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
        
        
        #region Inner workings
        public string? LoadDataPickerUrl { get; set; }
        public IEnumerable<SelectListItem> KnownFontList { get; set; } = Array.Empty<SelectListItem>();

        private static IEnumerable<SelectListItem> ListFontsOnServer(string? selected)
        {
            return PortableFont.ListKnownFonts().Select(name => new SelectListItem(name, name, name==selected));
        }

        private static int? ParseOrNull(string? baseFontSize)
        {
            if (baseFontSize is null) return null;
            if (!int.TryParse(baseFontSize , out var size)) return null;
            return size;
        }
        #endregion
    }
}