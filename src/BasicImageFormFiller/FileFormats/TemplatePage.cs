using System.Collections.Generic;
using System.IO;

namespace BasicImageFormFiller.FileFormats
{
    public class TemplatePage
    {
        public TemplatePage()
        {
            Boxes = new Dictionary<string, TemplateBox>();
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
    }
}