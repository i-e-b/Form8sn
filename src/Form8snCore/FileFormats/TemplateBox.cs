// ReSharper disable RedundantDefaultMemberInitializer
namespace Form8snCore.FileFormats
{
    public class TemplateBox
    {
        /// <summary>
        /// Create a box that is a copy of another
        /// </summary>
        /// <param name="parent"></param>
        public TemplateBox(TemplateBox parent)
        {
            WrapText = parent.WrapText;
            ShrinkToFit = parent.ShrinkToFit;
            BoxFontSize = parent.BoxFontSize;
            Alignment = parent.Alignment;
            DependsOn = parent.DependsOn;
            DisplayFormat = parent.DisplayFormat;
            MappingPath = parent.MappingPath;
            BoxOrder = parent.BoxOrder;
            
            Width = parent.Width;
            Height = parent.Height;
            Top = parent.Top + (Height / 2);
            Left = parent.Left + (Width / 2);
        }

        /// <summary>
        /// Create a new empty box
        /// </summary>
        public TemplateBox() { }

        /// <summary>
        /// Optional: notes for the template editor. These are not used for generation
        /// and are not stored in the PDF output.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Defaults to false.
        /// If true and the data path can't be found in the source data, generation of
        /// the entire document will fail.
        /// If false and the data path can't be found, this box will be blank and the rest
        /// of the document will render as normal.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// If text doesn't fit in the box, break on whitespace
        /// <para>This can be combined with ShrinkToFit</para>
        /// </summary>
        public bool WrapText { get; set; } = false;
        
        /// <summary>
        /// If text doesn't fit in the box, reduce the font size.
        /// The font size will not go lower than 4pt.
        /// <para>This can be combined with WrapText</para>
        /// </summary>
        public bool ShrinkToFit { get; set; } = true;

        /// <summary>
        /// Optional: Font size to use for this box (before shrinking is applied)
        /// </summary>
        public int? BoxFontSize { get; set; }

        /// <summary>
        /// Text position within the box.
        /// Text will flow out of the box if there is a value that can't be displayed inside the box
        /// </summary>
        public TextAlignment Alignment { get; set; }

        /// <summary>
        /// Key for another box on this page.
        /// If the 'DependsOn' box has no content, this box will not show.
        /// This property can be chained.
        /// </summary>
        public string? DependsOn { get; set; }
        
        /// <summary>
        /// Top edge of the box.
        /// If a background image is used, position and size are in pixels, and are relative to the background image
        /// If a source PDF is used, position and size are in millimetres, and are relative to each PDF page
        /// </summary>
        public double Top { get; set; }
        /// <summary>
        /// Left edge of the box,
        /// If a background image is used, position and size are in pixels, and are relative to the background image
        /// If a source PDF is used, position and size are in millimetres, and are relative to each PDF page
        /// </summary>
        public double Left { get; set; }
        /// <summary>
        /// Width of the box.
        /// If a background image is used, position and size are in pixels, and are relative to the background image
        /// If a source PDF is used, position and size are in millimetres, and are relative to each PDF page
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        /// Height of the box.
        /// If a background image is used, position and size are in pixels, and are relative to the background image
        /// If a source PDF is used, position and size are in millimetres, and are relative to each PDF page
        /// </summary>
        public double Height { get; set; }
        
        /// <summary>
        /// Path in data or filters that should be written in this box.
        /// If the mapping path is null, or the resulting data is null -- then the box won't be rendered.
        /// </summary>
        public string[]? MappingPath { get; set; }
        
        /// <summary>
        /// Filter over the value for the box. This is for date formatting, rounding settings, etc.
        /// </summary>
        public DisplayFormatFilter? DisplayFormat { get; set; }
        
        /// <summary>
        /// Optional: order in which the box value should be calculated. This mainly affects running totals
        /// </summary>
        public int? BoxOrder { get; set; }

        /// <summary>
        /// Update the size and position of this box
        /// </summary>
        public void SetSize(double left, double top, double width, double height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }
}