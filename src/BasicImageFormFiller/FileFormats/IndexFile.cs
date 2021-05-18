using System.Collections.Generic;

namespace BasicImageFormFiller.FileFormats
{
    public class IndexFile
    {
        public IndexFile(string name)
        {
            Name = name;
            Notes = "Add your notes here";
            Pages = new List<TemplatePage>();
        }

        public IndexFile() { Name = "Untitled"; Notes=""; Pages=new List<TemplatePage>(); }

        public string? SampleFileName { get; set; }
        public string Notes { get; set; }
        public string Name { get; set; }
        public List<TemplatePage> Pages { get; set; }
    }

    public class TemplatePage
    {
        /// <summary>
        /// Name is just for the user's use
        /// </summary>
        public string? Name { get; set; }
        
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
    }

    public class TemplateBox
    {
        public bool WrapText { get; set; } = false;
        public bool ShrinkToFit { get; set; } = true;

        public double FontScale { get; set; } = 1.0;
        public TextAlignment Alignment { get; set; }
        
        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public enum TextAlignment
    {
        TopLeft    , TopCentre    , TopRight,
        MidlineLeft, MidlineCentre, MidlineRight,
        BottomLeft , BottomCentre , BottomRight
    }
}