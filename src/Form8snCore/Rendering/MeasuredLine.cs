namespace Form8snCore.Rendering
{
    internal class MeasuredLine
    {
        public MeasuredLine(string text, double height)
        {
            Text = text;
            Height = height;
        }

        public string Text { get; }
        public double Height { get; }
    }
}