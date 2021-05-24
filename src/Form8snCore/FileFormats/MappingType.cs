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
        FixedValue = 2,
        
        [Description("Split a list into a list of sub-lists, each up to 'count' long")]
        [UsesType(typeof(MaxCountMappingParams))]
        SplitIntoN = 3,
        
        [Description("Return the first 'count' words, discarding others")]
        [UsesType(typeof(TakeMappingParams))]
        TakeWords = 4,
        
        [Description("Discard the first 'count' words, returning others")]
        [UsesType(typeof(SkipMappingParams))]
        SkipWords = 5,
        
        [Description("Join all elements of a list (treating them as strings)")]
        [UsesType(typeof(JoinMappingParams))]
        Concatenate = 6,
        
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