using System.Collections;
using System.Collections.Generic;
using Form8snCore.FileFormats;

namespace Form8snCore.DataExtraction
{
    public class DataMapper
    {
        private readonly Project _project;
        private readonly object _data;
        private readonly Dictionary<string,string> _emptyParams;
        private object? _repeatData;
        private string[]? _originalPath;

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
        /// <param name="runningTotals"></param>
        /// <returns>String to render, or null if not applicable</returns>
        public string? FindBoxData(TemplateBox box, int pageIndex, Dictionary<string, decimal> runningTotals)
        {
            if (box.MappingPath == null) return null;
            
            var filters = JoinProjectAndPageFilters(pageIndex);

            var obj = MappingActions.ApplyFilter(MappingType.None, _emptyParams, box.MappingPath, _originalPath, filters, _data, _repeatData, runningTotals);

            return obj?.ToString();
        }

        private Dictionary<string, MappingInfo> JoinProjectAndPageFilters(int pageIndex)
        {
            var filters = _project.Index.DataFilters;
            if (_project.Pages[pageIndex].PageDataFilters.Count <= 0) return filters;
            
            var pageFilters = _project.Pages[pageIndex].PageDataFilters;
            filters = new Dictionary<string, MappingInfo>(filters);
            foreach (var filter in pageFilters)
            {
                if (filters.ContainsKey(filter.Key)) filters[filter.Key] = filter.Value; // page specific over-rides general
                else filters.Add(filter.Key, filter.Value);
            }

            return filters;
        }

        /// <summary>
        /// Get data for a repeating page. Will return empty list if there is no data
        /// </summary>
        public List<object> GetRepeatData(string[]? dataPath)
        {
            var outp = new List<object>();
            if (dataPath == null) return outp;
            
            var list = MappingActions.ApplyFilter(MappingType.None, _emptyParams, dataPath, null, _project.Index.DataFilters, _data, null, null) as ArrayList;
            if (list == null) return outp;

            foreach (var item in list)
            {
                if (item != null) outp.Add(item);
            }
            return outp;
        }

        /// <summary>
        /// Set data for a repeater page.
        /// </summary>
        public void SetRepeater(object repeatData, string[]? originalPath)
        {
            // chase to the real data here
            if (originalPath != null && originalPath.Length > 1 && originalPath[0] == "#")
            {
                originalPath = MappingActions.FindSourcePath(originalPath[1], _project.Index.DataFilters);
            }

            _originalPath = originalPath;
            _repeatData = repeatData;
        }

        /// <summary>
        /// Remove repeater data
        /// </summary>
        public void ClearRepeater()
        {
            _repeatData = null;
            _originalPath = null;
        }
    }
}