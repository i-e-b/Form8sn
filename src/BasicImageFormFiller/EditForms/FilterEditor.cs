using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BasicImageFormFiller.FileFormats;
using BasicImageFormFiller.Helpers;
using BasicImageFormFiller.Interfaces;
using Form8snCore;

namespace BasicImageFormFiller.EditForms
{
    public partial class FilterEditor : Form
    {
        private readonly IScreenModule? _returnModule;
        private readonly Project? _project;
        private readonly string? _filterName;

        private string[]? _selectedPath;

        public FilterEditor()
        {
            InitializeComponent();
        }

        public FilterEditor(IScreenModule returnModule, Project project, string filterName)
        {
            _returnModule = returnModule;
            _project = project;
            _filterName = filterName;
            InitializeComponent();

            var filter = _project.Index.DataFilters[_filterName];

            filterNameTextbox!.Text = _filterName;
            
            _selectedPath = filter.DataPath;
            dataPathLabel!.Text = string.Join(".", filter.DataPath ?? new string[0]);
            
            FillWithEnum(filterTypeComboBox!.Items, typeof(MappingType));
            filterTypeComboBox.SelectedItem = filter.MappingType.ToString();
            
            var propObj = CreateTypedContainerForParams(filter.MappingType.ToString());
            MapDictionaryToProperties(filter.MappingParameters, propObj);
            filterPropertyGrid!.SelectedObject = propObj;
        }

        private void FillWithEnum(IList items, Type type)
        {
            items.Clear();
            var enumValues = type.GetFields().Where(f=> !f.IsSpecialName);
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
            // TODO: save values, change key name if required.

            var container = _project.Index.DataFilters;
            
            var filter = container[_filterName];
            
            filter.DataPath = _selectedPath;
            
            var filterName = filterTypeComboBox?.SelectedItem?.ToString() ?? "None";
            filter.MappingType = Enum.Parse<MappingType>(filterName);
            
            MapPropertiesToDictionary(filter.MappingParameters, filterPropertyGrid!.SelectedObject);
            
            // If key has changed, try updating
            var newKey = filterNameTextbox!.Text;
            // if key has changed and it's not already in use:
            if (newKey != _filterName && !string.IsNullOrWhiteSpace(newKey) && !container.ContainsKey(newKey)) 
            {
                var tmp = container[_filterName];
                container.Remove(_filterName);
                container.Add(newKey, tmp);
            }
            
            _project.Save();
            
            Close();
        }

        private void MapPropertiesToDictionary(Dictionary<string,string> dict, object? obj)
        {
            if (obj == null) return;
            
            var props = obj.GetType().GetProperties().Where(p=>p.CanRead);
            foreach (var prop in props)
            {
                if (dict.ContainsKey(prop.Name))
                {
                    dict[prop.Name] = prop.GetValue(obj)?.ToString() ?? "";
                }
                else
                {
                    dict.Add(prop.Name, prop.GetValue(obj)?.ToString() ?? "");
                }
            }
        }
        
        private void MapDictionaryToProperties(Dictionary<string,string> dict, object? obj)
        {
            if (obj == null) return;
            
            var props = obj.GetType().GetProperties().Where(p=>p.CanWrite);
            foreach (var prop in props)
            {
                if (!dict.ContainsKey(prop.Name)) continue;
                if (prop.PropertyType == typeof(int)) prop.SetValue(obj, int.Parse(dict[prop.Name]));
                else prop.SetValue(obj, dict[prop.Name]);
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void dataButton_Click(object sender, EventArgs e)
        {
            if (_project == null) return;
            var rm = new PickDataSource(_project, "Pick filter data source", _selectedPath);
            rm.ShowDialog();
            if (rm.SelectedPath == null) return;

            _selectedPath = rm.SelectedPath;
            dataPathLabel!.Text = string.Join(".", rm.SelectedPath);
        }

        private object? CreateTypedContainerForParams(string? selectedName)
        {
            var typeInfo = typeof(MappingType).GetFields().SingleOrDefault(f=> f.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
            if (typeInfo == null) return null;
            
            var attr = typeInfo.GetCustomAttributes(true).SingleOrDefault(a=>a is UsesTypeAttribute) as UsesTypeAttribute;
            if (attr == null) return null;
            
            return Activator.CreateInstance(attr.Type);
        }

        private void filterTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            filterPropertyGrid!.SelectedObject = CreateTypedContainerForParams(filterTypeComboBox!.SelectedItem?.ToString()) ?? new {};
        }
    }
}