using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Form8snCore.FileFormats;

namespace BasicImageFormFiller.EditForms
{
    public partial class DisplayFormatEditor : Form
    {
        private readonly FileSystemProject? _project;
        private readonly int _pageIndex;
        private readonly string? _boxKey;

        public DisplayFormatEditor() { InitializeComponent(); }

        public DisplayFormatEditor(FileSystemProject project, int pageIndex, string boxKey)
        {
            _project = project;
            _pageIndex = pageIndex;
            _boxKey = boxKey;
            InitializeComponent();

            var box = _project.Pages[pageIndex].Boxes[boxKey];
            var parameters = box.DisplayFormat?.FormatParameters ?? new Dictionary<string, string>();
            
            FillWithEnum(formatTypeComboBox!.Items, typeof(DisplayFormatType));
            var mappingType = box.DisplayFormat?.Type ?? DisplayFormatType.None;
            
            formatTypeComboBox.SelectedItem = mappingType.ToString();
            
            var propObj = CreateTypedContainerForParams(mappingType.ToString());
            MapDictionaryToProperties(parameters, propObj);
            formatPropertyGrid!.SelectedObject = propObj;
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

        private void FilterEditor_FormClosing(object sender, FormClosingEventArgs e) { }
        private void FilterEditor_Load(object sender, EventArgs e) { }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (_project == null || _boxKey == null) return;

            var box = _project.Pages[_pageIndex].Boxes[_boxKey];
            
            box.DisplayFormat ??= new DisplayFormatFilter();
            
            var formatType = formatTypeComboBox?.SelectedItem?.ToString() ?? "None";
            box.DisplayFormat.Type = Enum.Parse<DisplayFormatType>(formatType);
            
            MapPropertiesToDictionary(box.DisplayFormat.FormatParameters, formatPropertyGrid!.SelectedObject);
            
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

        private object? CreateTypedContainerForParams(string? selectedName)
        {
            var typeInfo = typeof(DisplayFormatType).GetFields().SingleOrDefault(f=> f.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
            if (typeInfo == null) return null;
            
            var attr = typeInfo.GetCustomAttributes(true).SingleOrDefault(a=>a is UsesTypeAttribute) as UsesTypeAttribute;
            if (attr == null) return null;
            
            return Activator.CreateInstance(attr.Type);
        }

        private void filterTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            formatPropertyGrid!.SelectedObject = CreateTypedContainerForParams(formatTypeComboBox!.SelectedItem?.ToString()) ?? new {};
        }
    }
}