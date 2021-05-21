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
        /// <returns>String to render, or null if not applicable</returns>
        public string? FindBoxData(TemplateBox box, int pageIndex)
        {
            if (box.MappingPath == null) return null;
            
            var obj = FilterData.ApplyFilter(MappingType.None, _emptyParams, box.MappingPath, _project.Index.DataFilters, _data, _repeatData);

            return obj?.ToString();
        }

        /// <summary>
        /// Get data for a repeating page. Will return empty list if there is no data
        /// </summary>
        public List<object> GetRepeatData(string[]? dataPath)
        {
            var outp = new List<object>();
            if (dataPath == null) return outp;
            
            var list = FilterData.ApplyFilter(MappingType.None, _emptyParams, dataPath, _project.Index.DataFilters, _data, null) as ArrayList;
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
        public void SetRepeater(object repeatData)
        {
            _repeatData = repeatData;
        }

        /// <summary>
        /// Remove repeater data
        /// </summary>
        public void ClearRepeater()
        {
            _repeatData = null;
        }
    }
}