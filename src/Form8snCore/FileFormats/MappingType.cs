using System.ComponentModel;

namespace Form8snCore.FileFormats
{
    /// <summary>
    /// How do we map from data path to a displayable value.
    /// This is either none (just use the data), or the output of a filter.
    /// <para></para>
    /// These have an explicit order that we will process the boxes in. This is overriden if a box has an explicit order
    /// </summary>
    public enum MappingType
    {
        [Description("Pass data directly along")]
        [UsesType(typeof(EmptyMappingParams))]
        None = 0,
        
        [Description("Supply an unchanging value")]
        [UsesType(typeof(TextMappingParams))]
        FixedValue,
        
        [Description("Split a list into a list of sub-lists, each up to 'count' long")]
        [UsesType(typeof(MaxCountMappingParams))]
        SplitIntoN,
        
        [Description("Return the first 'count' words, discarding others")]
        [UsesType(typeof(TakeMappingParams))]
        TakeWords,
        
        [Description("Discard the first 'count' words, returning others")]
        [UsesType(typeof(SkipMappingParams))]
        SkipWords,
        
        [Description("Join all elements of a list (treating them as strings)")]
        [UsesType(typeof(JoinMappingParams))]
        Concatenate,
        
        [Description("Select all the values on a path as a list")]
        [UsesType(typeof(EmptyMappingParams))]
        TakeAllValues,
        
        [Description("Select all the different values on a path")]
        [UsesType(typeof(EmptyMappingParams))]
        Distinct,
        
        [Description("Select all values on a path, and try to format them as date strings. Any values that can't be converted will be excluded")]
        [UsesType(typeof(DateFormatMappingParams))]
        FormatAllAsDate,
        
        [Description("Select all values on a path, and try to format them as numeric strings. Any values that can't be converted will be excluded")]
        [UsesType(typeof(NumberMappingParams))]
        FormatAllAsNumber,
        
        [Description("Sum up all numeric values on the path")]
        [UsesType(typeof(EmptyMappingParams))]
        Total = 1000,
        
        [Description("Sum up numeric values on the path that have been used so far")]
        [UsesType(typeof(EmptyMappingParams))]
        RunningTotal = 1001
    }

    public class JoinMappingParams {
        [Description("Text to place at the start of the list")]
        public string Prefix { get; set; } = "";
        [Description("Text to place between each item")]
        public string Infix { get; set; } = "";
        [Description("Text to place at the end of the list")]
        public string Postfix { get; set; } = "";
    }

    public class TakeMappingParams {
        [Description("Number of items to use before discarding the rest")]
        public int Count { get; set; }
    }
    
    public class SkipMappingParams {
        [Description("Number of items to skip before using the rest")]
        public int Count { get; set; }
    }

    public class DateFormatMappingParams {
        [Description("Format for date output\r\nSee https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings for details")]
        public string FormatString { get; set; } = "";
    }
    
    public class NumberMappingParams {
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

    public class TextMappingParams {
        [Description("Text to supply as data")]
        public string Text { get; set; } = "";
    }

    public class MaxCountMappingParams {
        [Description("The largest number of items in each set.")]
        public int MaxCount { get; set; }
    }

    public class EmptyMappingParams { }
}