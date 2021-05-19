using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BasicImageFormFiller.FileFormats;
using BasicImageFormFiller.Helpers;
using BasicImageFormFiller.Interfaces;
using SkinnyJson;

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
            dataPathLabel!.Text = string.Join(".", filter.DataPath ?? new string[0]);
            FillWithEnum(filterTypeComboBox!.Items, typeof(MappingType));
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
            
            MapProperties(filter.MappingParameters, filterPropertyGrid!.SelectedObject);
            
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

        private void MapProperties(Dictionary<string,string> dict, object obj)
        {
            var props = obj.GetType().GetProperties().Where(p=>p.CanRead);
            foreach (var prop in props)
            {
                dict.Add(prop.Name, prop.GetValue(obj)?.ToString() ?? "");
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void dataButton_Click(object sender, EventArgs e)
        {
            if (_project == null) return;
            var rm = new PickDataSource(_project);
            rm.ShowDialog();
            if (rm.SelectedPath == null) return;

            _selectedPath = rm.SelectedPath;
            dataPathLabel!.Text = string.Join(".", rm.SelectedPath);
        }

        private void filterTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            filterPropertyGrid!.SelectedObject = new {};
            
            var selectedName = filterTypeComboBox!.SelectedItem?.ToString();
            var typeInfo = typeof(MappingType).GetFields().SingleOrDefault(f=> f.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
            if (typeInfo == null) return;
            
            var attr = typeInfo.GetCustomAttributes(true).SingleOrDefault(a=>a is UsesTypeAttribute) as UsesTypeAttribute;
            if (attr == null) return;
            
            filterPropertyGrid!.SelectedObject = Activator.CreateInstance(attr.Type);
        }
    }
}