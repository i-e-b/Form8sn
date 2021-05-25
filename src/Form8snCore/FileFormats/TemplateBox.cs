// ReSharper disable RedundantDefaultMemberInitializer
namespace Form8snCore.FileFormats
{
    public class TemplateBox
    {
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
        /// Top edge of the box.
        /// Position and size are in pixels, and are relative to the background image
        /// </summary>
        public double Top { get; set; }
        /// <summary>
        /// Left edge of the box,
        /// Position and size are in pixels, and are relative to the background image
        /// </summary>
        public double Left { get; set; }
        /// <summary>
        /// Width of the box.
        /// Position and size are in pixels, and are relative to the background image
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        /// Height of the box.
        /// Position and size are in pixels, and are relative to the background image
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
    }
}