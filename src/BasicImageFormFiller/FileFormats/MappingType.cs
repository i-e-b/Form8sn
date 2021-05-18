namespace BasicImageFormFiller.FileFormats
{
    public enum MappingType
    {
        /// <summary> Pass data directly along </summary>
        None,
        
        /// <summary>
        /// Split a list into a list of sub-lists, each up to `N` long.
        /// <para>"Count":int -- max length of sub-lists</para>
        /// </summary>
        SplitIntoN,
        
        /// <summary>
        /// Sum up all numeric values on the path
        /// </summary>
        Total,
        
        /// <summary>
        /// Sum up numeric values on the path that have been used so far
        /// </summary>
        RunningTotal,
    }
}