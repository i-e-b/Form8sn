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
                AdditionalBackgroundDataPath = thePage.BackgroundImage,
                
                PageFontSize = thePage.PageFontSize,
                PageWidthMillimetres = Math.Round(thePage.WidthMillimetres, 1),
                PageHeightMillimetres = Math.Round(thePage.HeightMillimetres, 1),
                
                PageRepeats = thePage.RepeatMode.Repeats,
                RepeatDataPath = string.Join('.', thePage.RepeatMode.DataPath??Array.Empty<string>()),
                
                MergeToRoll = thePage.MergeToRoll,
                RollWidthMillimetres = thePage.RollWidthMillimetres ?? 0.0,
                RollHeightMillimetres = thePage.RollHeightMillimetres ?? 0.0,
                RollMarginMillimetres = thePage.RollMarginMillimetres ?? 0.0,
                
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
            thePage.BackgroundImage = AdditionalBackgroundDataPath;
            
            thePage.PageFontSize = PageFontSize;
            thePage.WidthMillimetres = PageWidthMillimetres;
            thePage.HeightMillimetres = PageHeightMillimetres;
            
            thePage.RepeatMode.Repeats = PageRepeats;
            thePage.RepeatMode.DataPath = RepeatDataPath?.Split('.');
            
            thePage.MergeToRoll = MergeToRoll;
            thePage.RollHeightMillimetres = RollHeightMillimetres;
            thePage.RollWidthMillimetres = RollWidthMillimetres;
            thePage.RollMarginMillimetres = RollMarginMillimetres;
        }

        public double PageHeightMillimetres { get; set; }
        public double PageWidthMillimetres { get; set; }

        public string? RepeatDataPath { get; set; }

        public bool PageRepeats { get; set; }

        public int? PageFontSize { get; set; }

        public bool RenderBackground { get; set; }
        public string? AdditionalBackgroundDataPath { get; set; }
        
        public bool MergeToRoll { get; set; }
        public double RollHeightMillimetres { get; set; }
        public double RollWidthMillimetres { get; set; }
        public double RollMarginMillimetres { get; set; }

        public int DocumentId { get; set; }
        public int PageIndex { get; set; }
        public int Version { get; set; }
        public string? Name { get; set; } = "";
        public string? Notes { get; set; }
        public IDictionary<string, MappingInfo> Filters { get; set; } = new Dictionary<string, MappingInfo>();
    }
}