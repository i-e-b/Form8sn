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