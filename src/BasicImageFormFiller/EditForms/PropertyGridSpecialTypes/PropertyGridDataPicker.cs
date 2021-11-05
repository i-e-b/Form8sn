using System.ComponentModel;
using Form8snCore.FileFormats;

namespace BasicImageFormFiller.EditForms.PropertyGridSpecialTypes
{
    /// <summary>
    /// Property grid value to pick a Data Source
    /// </summary>
    [Editor(typeof(PropertyGridDataPickEditor), typeof(System.Drawing.Design.UITypeEditor))]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class PropertyGridDataPicker: ISpecialString
    {
        public PropertyGridDataPicker() { }
        public PropertyGridDataPicker(string? parsedValue, FileSystemProject? project, int? pageIndex)
        {
            Path = string.IsNullOrWhiteSpace(parsedValue) ? null : parsedValue.Split('.');
            BaseProject = project;
            PageDefinitionIndex = pageIndex;
        }

        internal string []? Path { get; set; }
        internal FileSystemProject? BaseProject { get; set; }
        internal int? PageDefinitionIndex { get; set; }

        public override string ToString()
        {
            if (Path == null || Path.Length < 1) return "< no data >";
            return string.Join(".", Path);
        }

        public string? StringValue => Path == null ? null : string.Join(".", Path);
    }
}