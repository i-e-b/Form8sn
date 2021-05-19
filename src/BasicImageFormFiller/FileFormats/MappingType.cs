using System.ComponentModel;

namespace BasicImageFormFiller.FileFormats
{
    public enum MappingType
    {
        /// <summary> Pass data directly along </summary>
        [UsesType(typeof(EmptyMappingParams))]
        None,
        
        /// <summary>
        /// Split a list into a list of sub-lists, each up to `N` long.
        /// <para>"Count":int -- max length of sub-lists</para>
        /// </summary>
        [UsesType(typeof(SplitIntoNMappingParams))]
        SplitIntoN,
        
        /// <summary>
        /// Sum up all numeric values on the path
        /// </summary>
        [UsesType(typeof(TotalMappingParams))]
        Total,
        
        /// <summary>
        /// Sum up numeric values on the path that have been used so far
        /// </summary>
        [UsesType(typeof(RunningTotalMappingParams))]
        RunningTotal,
    }

    public class RunningTotalMappingParams { }

    public class TotalMappingParams { }

    public class SplitIntoNMappingParams {
        [Description("The largest number of items in each set.")]
        public int MaxCount { get; set; }
    }

    public class EmptyMappingParams { }
}