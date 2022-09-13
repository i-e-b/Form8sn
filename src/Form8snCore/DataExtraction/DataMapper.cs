using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Form8snCore.FileFormats;
using Form8snCore.Rendering;
using Form8snCore.Rendering.CustomRendering;

namespace Form8snCore.DataExtraction
{
    /// <summary>
    /// This is a helper that wraps a template and some data to drive the rendering process
    /// </summary>
    public class DataMapper
    {
        private readonly TemplateProject _project;
        private readonly object _data;
        private readonly Dictionary<string,string> _emptyParams;
        private object? _repeatData;
        private string[]? _originalPath;

        public DataMapper(TemplateProject project, object data)
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
        public BoxDataWrapper? TryFindBoxData(TemplateBox box, int pageIndex, Dictionary<string, decimal> runningTotals)
        {
            try
            {
                if (box.MappingPath == null) return null;

                var filters = JoinProjectAndPageFilters(pageIndex);

                var obj = MappingActions.ApplyFilter(MappingType.None, _emptyParams, box.MappingPath, _originalPath, filters, _data, _repeatData, runningTotals);

                if (obj is ICustomRenderedBox special)
                {
                    return new BoxDataWrapper(special);
                }
                return obj is null ? null : new BoxDataWrapper(obj.ToString());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get data for a repeating page. Will return empty list if there is no data
        /// </summary>
        public List<object> GetRepeatData(string[]? dataPath)
        {
            var outp = new List<object>();
            if (dataPath == null) return outp;
            
            var list = MappingActions.ApplyFilter(MappingType.None, _emptyParams, dataPath, null, _project.DataFilters, _data, null, null) as IList;
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
        public void SetRepeater(object? repeatData, string[]? originalPath)
        {
            // chase to the real data here
            if (originalPath != null && originalPath.Length > 1 && originalPath[0] == "#")
            {
                originalPath = MappingActions.FindSourcePath(originalPath[1], _project.DataFilters);
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

        /// <summary>
        /// Returns true if this box requires special handling -- such as the page count to be known, or image embedding
        /// </summary>
        public static bool IsSpecialBoxType(TemplateBox box, out DocumentBoxType type)
        {
            type = DocumentBoxType.Normal;
            if (box.MappingPath == null) return false;

            if (box.DisplayFormat?.Type == DisplayFormatType.RenderImage)
            {
                type = DocumentBoxType.EmbedJpegImage;
                return true;
            }
            
            if (box.DisplayFormat?.Type == DisplayFormatType.ColorBox)
            {
                type = DocumentBoxType.ColorBox;
                return true;
            }
            
            if (box.DisplayFormat?.Type == DisplayFormatType.QrCode)
            {
                type = DocumentBoxType.QrCode;
                return true;
            }

            var filter = box.MappingPath[0];
            if (filter != "P") return false;
            
            if (!Enum.TryParse(box.MappingPath[1], out type)) return false;
            return true;
        }
        
        
        private Dictionary<string, MappingInfo> JoinProjectAndPageFilters(int pageIndex)
        {
            var filters = _project.DataFilters;
            if (_project.Pages[pageIndex]!.PageDataFilters.Count <= 0) return filters;
            
            var pageFilters = _project.Pages[pageIndex]!.PageDataFilters;
            filters = new Dictionary<string, MappingInfo>(filters);
            foreach (var filter in pageFilters)
            {
                if (filters.ContainsKey(filter.Key)) filters[filter.Key] = filter.Value; // page specific over-rides general
                else filters.Add(filter.Key, filter.Value);
            }

            return filters;
        }

        /// <summary>
        /// Set the mapper's current repeat data set for
        /// a page, plus the index through the repeat set.
        /// <p></p>
        /// If no data matches, the repeat data will set to
        /// an empty object.
        /// </summary>
        public void SetRepeatDataByIndex(TemplatePage page, int repeatItemIndex)
        {
            var repeatData = GetRepeatData(page.RepeatMode.DataPath).Skip(repeatItemIndex).FirstOrDefault() ?? new{};
            SetRepeater(repeatData, page.RepeatMode.DataPath);
        }
    }
}