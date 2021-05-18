namespace BasicImageFormFiller.FileFormats
{
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
}