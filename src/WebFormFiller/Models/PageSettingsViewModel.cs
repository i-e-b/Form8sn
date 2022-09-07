using System;
using System.Collections.Generic;
using Form8snCore.FileFormats;

namespace WebFormFiller.Models
{
    public class PageSettingsViewModel
    {
        
        /// <summary>
        /// Generate a view model from a project file
        /// </summary>
        public static PageSettingsViewModel From(TemplateProject project, int docId, int pageIndex)
        {
            var thePage = project.Pages[pageIndex];
            return new PageSettingsViewModel{
                DocumentId = docId,
                Version = project.Version ?? 0,
                PageIndex = pageIndex,
                
                Name = thePage.Name,
                Notes = thePage.Notes,
                RenderBackground = thePage.RenderBackground,
                PageFontSize = thePage.PageFontSize,
                PageWidthMillimetres = Math.Round(thePage.WidthMillimetres, 1),
                PageHeightMillimetres = Math.Round(thePage.HeightMillimetres, 1),
                
                PageRepeats = thePage.RepeatMode.Repeats,
                RepeatDataPath = string.Join('.', thePage.RepeatMode.DataPath??Array.Empty<string>()),
                Filters = thePage.PageDataFilters
            };
        }

        /// <summary>
        /// Copy view model values into an existing index file (to perform an update)
        /// </summary>
        public void CopyTo(TemplateProject target)
        {
            var thePage = target.Pages[PageIndex];
            
            thePage.Name = Name ?? $"Page {PageIndex+1}";
            thePage.Notes = Notes ?? "";
            thePage.RenderBackground = RenderBackground;
            thePage.PageFontSize = PageFontSize;
            thePage.WidthMillimetres = PageWidthMillimetres;
            thePage.HeightMillimetres = PageHeightMillimetres;
            
            thePage.RepeatMode.Repeats = PageRepeats;
            thePage.RepeatMode.DataPath = RepeatDataPath?.Split('.');
        }

        public double PageHeightMillimetres { get; set; }
        public double PageWidthMillimetres { get; set; }

        public string? RepeatDataPath { get; set; }

        public bool PageRepeats { get; set; }

        public int? PageFontSize { get; set; }

        public bool RenderBackground { get; set; }

        public int DocumentId { get; set; }
        public int PageIndex { get; set; }
        public int Version { get; set; }
        public string? Name { get; set; } = "";
        public string? Notes { get; set; }
        public IDictionary<string, MappingInfo> Filters { get; set; } = new Dictionary<string, MappingInfo>();


        private static int? ParseOrNull(string? baseFontSize)
        {
            if (baseFontSize is null) return null;
            if (!int.TryParse(baseFontSize , out var size)) return null;
            return size;
        }
    }
}