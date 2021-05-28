using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BasicImageFormFiller.EditForms.PropertyGridSpecialTypes;
using BasicImageFormFiller.Interfaces;
using Form8snCore.FileFormats;

namespace BasicImageFormFiller.EditForms
{
    public partial class FilterEditor : Form
    {
        private readonly IScreenModule? _returnModule;
        private readonly Project? _project;
        private readonly string? _filterName;
        private readonly int? _pageIndex;

        private string[]? _selectedPath;

        public FilterEditor()
        {
            InitializeComponent();
        }

        public FilterEditor(IScreenModule returnModule, Project project, string filterName, int? pageIndex)
        {
            _returnModule = returnModule;
            _project = project;
            _filterName = filterName;
            _pageIndex = pageIndex;
            InitializeComponent();

            var filter = GetMappingInfo();

            filterNameTextbox!.Text = _filterName;

            _selectedPath = filter.DataPath;
            dataPathLabel!.Text = string.Join(".", filter.DataPath ?? new string[0]);

            FillWithEnum(filterTypeComboBox!.Items, typeof(MappingType));
            filterTypeComboBox.SelectedItem = filter.MappingType.ToString();

            var propObj = CreateTypedContainerForParams(filter.MappingType.ToString());
            MapDictionaryToProperties(filter.MappingParameters, propObj);
            filterPropertyGrid!.SelectedObject = propObj;
        }

        private MappingInfo GetMappingInfo()
        {
            var filter = _pageIndex == null
                ? _project!.Index.DataFilters[_filterName!]
                : _project!.Pages[_pageIndex.Value].PageDataFilters[_filterName!];
            return filter;
        }

        private void FillWithEnum(IList items, Type type)
        {
            items.Clear();
            var enumValues = type.GetFields().Where(f => !f.IsSpecialName);
            foreach (var info in enumValues)
            {
                items.Add(info.Name);
            }
        }

        private void FilterEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            _returnModule?.Activate();
        }

        private void FilterEditor_Load(object sender, EventArgs e)
        {
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (_project == null || _filterName == null) return;

            var container = _pageIndex == null
                ? _project.Index.DataFilters
                : _project.Pages[_pageIndex.Value].PageDataFilters;

            var filter = container[_filterName];

            filter.DataPath = _selectedPath;

            var filterName = filterTypeComboBox?.SelectedItem?.ToString() ?? "None";
            filter.MappingType = Enum.Parse<MappingType>(filterName);

            MapPropertiesToDictionary(filter.MappingParameters, filterPropertyGrid!.SelectedObject);

            // If key has changed, try updating
            var newKey = Strings.CleanKeyName(filterNameTextbox!.Text);
            // if key has changed and it's not already in use:
            if (newKey != _filterName && !string.IsNullOrWhiteSpace(newKey) && !container.ContainsKey(newKey))
            {
                var tmp = container[_filterName];
                container.Remove(_filterName);
                container.Add(newKey, tmp);

                FixFilterReferences(_filterName, newKey);
            }

            _project.Save();

            Close();
        }

        private void FixFilterReferences(string oldKey, string newKey)
        {
            if (_project == null) return;
            var pageSet = _project.Index.Pages;
            
            
            if (_pageIndex != null)  // Do on-page references ONLY
            {
                pageSet = new List<TemplatePage> {_project.Pages[_pageIndex.Value]};
            }
            else // Do global filter search
            {
                foreach (var filter in _project.Index.DataFilters)
                {
                    TryUpdateFilterReference(oldKey, newKey, filter.Value.DataPath);
                    TryToUpdateFilterNameInMappingParameters(oldKey, newKey, filter.Value.MappingParameters);
                }
            }

            foreach (var page in pageSet)
            {
                foreach (var box in page.Boxes)
                {
                    TryUpdateFilterReference(oldKey, newKey, box.Value.MappingPath);
                }

                foreach (var pageFilter in page.PageDataFilters)
                {
                    TryUpdateFilterReference(oldKey, newKey, pageFilter.Value.DataPath);
                    TryToUpdateFilterNameInMappingParameters(oldKey, newKey, pageFilter.Value.MappingParameters);
                }

                if (page.RepeatMode.DataPath != null)
                {
                    var path = page.RepeatMode.DataPath;
                    TryUpdateFilterReference(oldKey, newKey, path);
                }
            }
        }

        private static void TryToUpdateFilterNameInMappingParameters(string oldKey, string newKey, Dictionary<string, string>? map)
        {
            if (map == null) return;
            var mapKeys = map.Keys.ToList(); // ToList() is required
            foreach (var paramKey in mapKeys)
            {
                var value = map[paramKey];
                if (value.Length <= 2 || !value.StartsWith("#.")) continue;

                // value looks like a path
                var path = map[paramKey].Split('.');
                TryUpdateFilterReference(oldKey, newKey, path);
                map[paramKey] = string.Join(".", path);
            }
        }

        private static void TryUpdateFilterReference(string oldKey, string newKey, string[]? path)
        {
            if (path == null || path.Length <= 1 || path[0] != Strings.FilterMarker) return;
            if (path[1] == oldKey) path[1] = newKey;
        }

        private void MapPropertiesToDictionary(Dictionary<string, string> dict, object? obj)
        {
            if (obj == null) return;

            var baseType = obj.GetType();
            var props = baseType.GetProperties().Where(p => p.CanRead).ToList();
            foreach (var prop in props)
            {
                if (prop.DeclaringType != baseType)
                {
                    // Inherited property. If it is hidden by another type, ignore it
                    if (props.Any(p => p.Name == prop.Name && p.DeclaringType == baseType)) continue;
                }

                var propValue = prop.GetValue(obj);
                var value = (propValue is ISpecialString ss) ? ss.StringValue : propValue?.ToString();

                if (dict.ContainsKey(prop.Name)) dict[prop.Name] = value ?? "";
                else dict.Add(prop.Name, value ?? "");
            }
        }

        private void MapDictionaryToProperties(Dictionary<string, string> dict, object? obj)
        {
            if (obj == null) return;

            var props = obj.GetType().GetProperties().Where(p => p.CanWrite);
            foreach (var prop in props)
            {
                var existingValue = dict.ContainsKey(prop.Name) ? dict[prop.Name] : null;

                if (prop.PropertyType == typeof(int)) prop.SetValue(obj, ParseIntOrDefault(existingValue));
                else if (prop.PropertyType == typeof(PropertyGridDataPicker)) prop.SetValue(obj, new PropertyGridDataPicker(existingValue, _project, _pageIndex));
                else prop.SetValue(obj, existingValue);
            }
        }

        private int ParseIntOrDefault(string? str)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            if (int.TryParse(str, out var i)) return i;
            return 0;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void dataButton_Click(object sender, EventArgs e)
        {
            if (_project == null) return;

            string[]? repeatPath = null;
            if (_pageIndex != null) repeatPath = _project.Pages[_pageIndex.Value].RepeatMode.DataPath;

            var rm = new PickDataSource(_project, "Pick filter data source", _selectedPath, repeatPath, _pageIndex, allowEmpty: false);
            rm.ShowDialog();
            if (rm.SelectedPath == null) return;

            _selectedPath = rm.SelectedPath;
            dataPathLabel!.Text = string.Join(".", rm.SelectedPath);
        }

        private object? CreateTypedContainerForParams(string? selectedName)
        {
            var typeInfo = typeof(MappingType).GetFields().SingleOrDefault(f => f.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
            if (typeInfo == null) return null;

            var attr = typeInfo.GetCustomAttributes(true).SingleOrDefault(a => a is UsesTypeAttribute) as UsesTypeAttribute;
            if (attr == null) return null;

            // A bit of type hackery to enable helpful editors
            if (attr.Type == typeof(IfElseMappingParams)) return Activator.CreateInstance(typeof(IfElseMappingParamsUI));

            return Activator.CreateInstance(attr.Type);
        }

        private void filterTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var filter = GetMappingInfo();
            var newParams = CreateTypedContainerForParams(filterTypeComboBox!.SelectedItem?.ToString()) ?? new { };
            MapDictionaryToProperties(filter.MappingParameters, newParams);
            filterPropertyGrid!.SelectedObject = newParams;
        }
    }
}