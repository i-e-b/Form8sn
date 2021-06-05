// ReSharper disable InconsistentNaming
namespace Portable.Drawing.Toolkit.Fonts
{
    public class PanoseClassification
    {
        public int bFamilyType { get; set; }
        public int bSerifStyle { get; set; }
        public int bWeight { get; set; }
        public int bProportion { get; set; }
        public int bContrast { get; set; }
        public int bStrokeVersion { get; set; }
        public int bArmStyle { get; set; }
        public int bLetterform { get; set; }
        public int bMidline { get; set; }
        public int bXHeight { get; set; }

        public override string ToString()
        {
            return $"Weight: {bWeight}, Midline: {bMidline}, x-height: {bXHeight}";
        }
    }
}