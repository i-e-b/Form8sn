using System.Collections.Generic;
using System.IO;

namespace BasicImageFormFiller.FileFormats
{
    public class TemplatePage
    {
        public TemplatePage()
        {
            Boxes = new Dictionary<string, TemplateBox>();
            RepeatMode = new RepeatMode();
        }
        
        /// <summary>
        /// Name for the page (not used in PDF)
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// Notes for the user (not used in PDF)
        /// </summary>
        public string? Notes { get; set; }
        
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
        /// If true: background is drawn when setting up the template, AND when creating PDFs.
        /// Use this for printing on blank paper.
        /// <para></para>
        /// If false: background is only shown when setting up template, NOT when creating PDFs.
        /// Use this for printing on pre-printed forms.
        /// </summary>
        public bool RenderBackground { get; set; } = true;

        public string GetBackgroundUrl(Project project)
        {
            return $"{project.BaseUri}/{BackgroundImage}";
        }

        public string GetBackgroundPath(Project project)
        {
            return Path.Combine(project.BasePath, BackgroundImage ?? "");
        }

        /// <summary>
        /// If a 'boxes' preview has been written, return a URL to that.
        /// Otherwise if there is a background, return the URL to that.
        /// Otherwise, return empty string.
        /// </summary>
        public string GetBackgroundPreviewUrl(Project project)
        {
            if (BackgroundImage == null) return "";
            
            var preview = Path.Combine(project.BasePath, "Form8sn_preview_"+BackgroundImage);
            if (File.Exists(preview)) return $"{project.BaseUri}/Form8sn_preview_{BackgroundImage}";
            return $"{project.BaseUri}/{BackgroundImage}";
        }

        /// <summary>
        /// Get the expected path to read or write page previews
        /// </summary>
        public string? GetPreviewPath(Project project)
        {
            return BackgroundImage == null
                ? null
                : Path.Combine(project.BasePath, "Form8sn_preview_"+BackgroundImage);
        }
    }

    public class RepeatMode
    {
        /// <summary>
        /// If true, the data path must be set.
        /// </summary>
        public bool Repeats { get; set; }

        /// <summary>
        /// Path into data, or name of filter.
        /// If the path points to a single item or object, you will get only one page.
        /// If the path doesn't exist in the data set, this page will be skipped.
        /// </summary>
        public string? DataPath { get; set; }
    }
}