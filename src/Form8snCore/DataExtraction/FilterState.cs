using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Form8snCore.FileFormats;

[assembly: InternalsVisibleTo("Form8snCore.Tests")]

namespace Form8snCore.DataExtraction
{
    
    /// <summary>
    /// Contains the state that follows the recursive application of filters to data
    /// </summary>
    internal class FilterState
    {
        public FilterState()
        {
            Params = new Dictionary<string, string>();
            FilterSet = new Dictionary<string, MappingInfo>();
            Redirects = new HashSet<string>();
            RunningTotals = new Dictionary<string, decimal>();
        }

        public override string ToString()
        {
            if (SourcePath == null) return "<null>";
            return string.Join(".", SourcePath);
        }

        public MappingType Type { get; set; }
        public Dictionary<string, string> Params { get; set; }
        public string[]? SourcePath { get; set; }
        public Dictionary<string, MappingInfo> FilterSet { get; set; }
        public object? Data { get; set; }
        public object? RepeaterData { get; set; }
        public HashSet<string> Redirects { get; set; }
        public Dictionary<string, decimal> RunningTotals { get; set; }
        public string[]? OriginalPath { get; set; }

        public FilterState? RedirectFilter(string name)
        {
            if (!FilterSet.ContainsKey(name)) return null;
            
            if (Redirects.Contains(name)) return null; // Recursion in filters!
            Redirects.Add(name);
            
            var newFilterDef = FilterSet[name];
            if (newFilterDef is null) return null;
            
            return new FilterState{
                Type = newFilterDef.MappingType,
                SourcePath = newFilterDef.DataPath,
                OriginalPath = OriginalPath,
                Params = newFilterDef.MappingParameters,
                
                RepeaterData = RepeaterData,
                Data = Data,
                FilterSet = FilterSet,
                Redirects = Redirects,
                RunningTotals = RunningTotals
            };
        }

        public FilterState NewPath(string[] path)
        {
            return new FilterState{
                Type = MappingType.None,
                SourcePath = path,
                OriginalPath = OriginalPath,
                Params = Params,
                
                RepeaterData = RepeaterData,
                Data = Data,
                FilterSet = FilterSet,
                Redirects = Redirects,
                RunningTotals = RunningTotals
            };
        }
    }
}