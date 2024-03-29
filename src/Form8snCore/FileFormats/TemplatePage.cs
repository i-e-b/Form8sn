using System.Collections.Generic;
using System.IO;

namespace Form8snCore.FileFormats
{
    public class TemplatePage
    {
        public TemplatePage()
        {
            Boxes = new Dictionary<string, TemplateBox>();
            RepeatMode = new RepeatMode{Repeats = false};
            PageDataFilters = new Dictionary<string, MappingInfo>();
        }

        /// <summary> Width of the page, in mm </summary>
        public double WidthMillimetres { get; set; }
        /// <summary> Height of the page, in mm </summary>
        public double HeightMillimetres { get; set; }

        /// <summary>
        /// Repetitions of this template page should be merged onto larger 'roll' pages.
        /// Roll page size must be defined, and be larger than the page size for this to work.
        /// <p/>
        /// Merge-to-Roll pages don't support PDF background copying at present.
        /// <p/>
        /// This is mainly to support large-format sticker printing.
        /// </summary>
        public bool MergeToRoll { get; set; }

        /// <summary>
        /// Width of large-format page that individual copies of this page should be merged into.
        /// Pages are merged width-first. This should be the roll width (the smaller dimension) if using a roll printer.
        /// <p/>
        /// Ignored unless <see cref="MergeToRoll"/> is set to <c>true</c>
        /// </summary>
        public double? RollWidthMillimetres { get; set; }
        
        /// <summary>
        /// Size of large-format page that individual copies of this page should be merged into
        /// Pages are merged width-first. This should be the maximum roll length (the larger dimension) if using a roll printer.
        /// <p/>.
        /// Ignored unless <see cref="MergeToRoll"/> is set to <c>true</c>
        /// </summary> 
        public double? RollHeightMillimetres { get; set; }

        /// <summary>
        /// Minimum spacing between copies of the page on a roll.
        /// Ignored unless <see cref="MergeToRoll"/> is set to <c>true</c>
        /// </summary>
        public double? RollMarginMillimetres { get; set; }

        /// <summary>
        /// Optional: override the base font size for this page
        /// </summary>
        public int? PageFontSize { get; set; }
        
        /// <summary>
        /// Name for the page (not used in PDF)
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// Notes for the user (not used in PDF)
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// Optional: URL of image to use as background
        /// </summary>
        public string? BackgroundImage { get; set; }

        /// <summary>
        /// Is this a single page, or do we repeat over data sets?
        /// </summary>
        public RepeatMode RepeatMode { get; set; }
        
        /// <summary>
        /// Unique (on page) box name -> box definition
        /// </summary>
        public IDictionary<string, TemplateBox> Boxes { get; set; }
        
        /// <summary>
        /// Filters specifically for this page.
        /// These will have access to the data-repeater paths
        /// </summary>
        public Dictionary<string, MappingInfo> PageDataFilters { get; set; }
        
        /// <summary>
        /// If true: background is drawn when setting up the template, AND when creating PDFs.
        /// Use this for printing on blank paper.
        /// <para></para>
        /// If false: background is only shown when setting up template, NOT when creating PDFs.
        /// Use this for printing on pre-printed forms.
        /// </summary>
        public bool RenderBackground { get; set; } = true;

        public string GetBackgroundPath()
        {
            return BackgroundImage ?? "";
        }

        /// <summary>
        /// If a 'boxes' preview has been written, return a URL to that.
        /// Otherwise if there is a background, return the URL to that.
        /// Otherwise, return empty string.
        /// </summary>
        public string GetBackgroundPreviewUrl(FileSystemProject project)
        {
            if (BackgroundImage == null) return "";
            
            var preview = Path.Combine(project.BasePath, "Form8sn_preview_"+BackgroundImage);
            if (File.Exists(preview)) return $"{project.BaseUri}/Form8sn_preview_{BackgroundImage}";
            return $"{project.BaseUri}/{BackgroundImage}";
        }

        /// <summary>
        /// Get the expected path to read or write page previews
        /// </summary>
        public string? GetPreviewPath(FileSystemProject project)
        {
            return BackgroundImage == null
                ? null
                : Path.Combine(project.BasePath, "Form8sn_preview_"+BackgroundImage);
        }
        
        /// <summary>
        /// Update all 'DependsOn' values where a box key name has changed
        /// </summary>
        /// <param name="oldKey">The original box name</param>
        /// <param name="newKey">The new box name</param>
        public void FixReferences(string oldKey, string newKey)
        {
            // scan all 'dependsOn' on this page and fix
            foreach (var box in Boxes.Values)
            {
                if (box.DependsOn == oldKey) box.DependsOn = newKey;
            }
        }

    }
}