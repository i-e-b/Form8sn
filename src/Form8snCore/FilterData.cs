using System;
using System.Collections.Generic;

namespace Form8snCore
{
    public static class FilterData
    {
        public static object? ApplyFilter(MappingType type,
            Dictionary<string, string> parameters,
            string[]? sourcePath,
            dynamic? sourceData)
        {
            // return one of ArrayList, string, int, Dictionary<string,object>; For the dict, 'object' must also be one of these.
            
            
            return "sample data here";
        }
    }
}