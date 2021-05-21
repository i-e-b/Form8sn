namespace Form8snCore.FileFormats
{
    public class TemplateBox
    {
        public bool WrapText { get; set; } = false;
        public bool ShrinkToFit { get; set; } = true;

        public TextAlignment Alignment { get; set; }
        
        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        
        public string[]? MappingPath { get; set; }
        public DisplayFormatFilter? DisplayFormat { get; set; }
    }
}