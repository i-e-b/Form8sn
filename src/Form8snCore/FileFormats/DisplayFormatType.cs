using System.ComponentModel;

namespace Form8snCore.FileFormats
{
    public enum DisplayFormatType
    {
        [Description("Pass data directly along")]
        [UsesType(typeof(EmptyDisplayParams))]
        None,
        
        [Description("Format as a date")]
        [UsesType(typeof(DateDisplayParams))]
        DateFormat,
        
        [Description("Format as a number")]
        [UsesType(typeof(NumberDisplayParams))]
        NumberFormat,
        
        [Description("Format as a number, displaying only whole number part")]
        [UsesType(typeof(EmptyDisplayParams))]
        Integral,
        
        [Description("Format as a number, displaying only the decimal fraction")]
        [UsesType(typeof(FractionalDisplayParams))]
        Fractional,
        
        [Description("Data URL to a JPEG image")]
        [UsesType(typeof(EmptyDisplayParams))]
        RenderImage,
        
        [Description("Fill box with a CSS colour. Does not redact data from PDF file")]
        [UsesType(typeof(EmptyDisplayParams))]
        ColorBox,
        
        [Description("Render a QR code of the data")]
        [UsesType(typeof(EmptyDisplayParams))]
        QrCode,
    }


    public class FractionalDisplayParams
    {
        [Description("Decimal places")]
        public int DecimalPlaces { get; set; } = 2;
    }

    public class NumberDisplayParams {
        [Description("Decimal places")]
        public int DecimalPlaces { get; set; } = 2;
        
        [Description("Thousands separator (blank for none)")]
        public string ThousandsSeparator { get; set; } = "";
        
        [Description("Decimal place (defaults to '.')")]
        public string DecimalSeparator { get; set; } = "";
        
        [Description("Prefix (like £ or $, placed on left, blank for none)")]
        public string Prefix { get; set; } = "";
        
        [Description("Postfix (like ¥ or units of measure, placed on right, blank for none)")]
        public string Postfix { get; set; } = "";
    }
    
    public class DateDisplayParams {
        [Description("Format for date output\r\nSee https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings for details")]
        public string FormatString { get; set; } = "";
    }
    
    public class EmptyDisplayParams { }
}