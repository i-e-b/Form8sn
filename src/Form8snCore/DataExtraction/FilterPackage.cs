using System.Collections.Generic;
using Form8snCore.FileFormats;

namespace Form8snCore.DataExtraction
{
    internal class FilterPackage
    {
        public FilterPackage() { Params = new Dictionary<string, string>(); FilterSet = new Dictionary<string, MappingInfo>(); Redirects = new HashSet<string>(); }
        
        public MappingType Type { get; set; }
        public Dictionary<string, string> Params { get; set; }
        public string[]? SourcePath { get; set; }
        public Dictionary<string, MappingInfo> FilterSet { get; set; }
        public object? Data { get; set; }
        public object? RepeaterData { get; set; }
        public HashSet<string> Redirects { get; set; }
        
        public FilterPackage? RedirectFilter(string name)
        {
            if (!FilterSet.ContainsKey(name)) return null;
            
            if (Redirects.Contains(name)) return null; // Recursion in filters!
            Redirects.Add(name);
            
            var newFilterDef = FilterSet[name];
            
            return new FilterPackage{
                Type = newFilterDef.MappingType,
                SourcePath = newFilterDef.DataPath,
                Params = newFilterDef.MappingParameters,
                
                Data = Data,
                FilterSet = FilterSet,
                Redirects = Redirects
            };
        }
    }
}