using System.Collections.Generic;
using Form8snCore.FileFormats;

namespace Form8snCore
{
    public class DataMapper
    {
        // TODO: add caching here?
        private readonly Project _project;
        private readonly object _data;
        private Dictionary<string,string> _emptyParams;

        public DataMapper(Project project, object data)
        {
            _emptyParams = new Dictionary<string, string>();
            _project = project;
            _data = data;
        }

        /// <summary>
        /// Get the data to display in a box for a specific page.
        /// </summary>
        /// <param name="box">Box to be rendered</param>
        /// <param name="pageIndex">Page definition index</param>
        /// <param name="repeaterIndex">Index of repeat. Ignored if not a repeater page</param>
        /// <returns>String to render, or null if not applicable</returns>
        public string? FindBoxData(TemplateBox box, int pageIndex, int repeaterIndex)
        {
            if (box.MappingPath == null) return null;
            
            var obj = FilterData.ApplyFilter(MappingType.None, _emptyParams, box.MappingPath, _project.Index.DataFilters, _data);
            
            return obj?.ToString();
            
        }
    }
}