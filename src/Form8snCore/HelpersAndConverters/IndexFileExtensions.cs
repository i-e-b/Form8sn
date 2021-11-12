using System.Collections.Generic;
using Form8snCore.FileFormats;

namespace Form8snCore.HelpersAndConverters
{
    public static class IndexFileExtensions
    {
        /// <summary>
        /// Return the data filter set for a given page.
        /// If the pageIdx is null, document filters are returned
        /// </summary>
        public static Dictionary<string, MappingInfo>? PickFilterSet(this IndexFile project, int? pageIdx)
        {
            if (pageIdx is null || pageIdx < 0) return project.DataFilters;
            if (pageIdx >= project.Pages.Count) return null;
            return project.Pages[pageIdx.Value]?.PageDataFilters;
        }
    }
}