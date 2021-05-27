using System.ComponentModel;
using Form8snCore.FileFormats;

namespace BasicImageFormFiller.EditForms.PropertyGridSpecialTypes
{
    /// <summary>
    /// Property grid value to pick a Data Source
    /// </summary>
    [Editor(typeof(PropertyGridDataPickEditor), typeof(System.Drawing.Design.UITypeEditor))]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class PropertyGridDataPicker
    {
        public PropertyGridDataPicker() { }
        public PropertyGridDataPicker(string parsedValue, Project? project, int? pageIndex)
        {
            Path = parsedValue.Split(Strings.Separator);
            BaseProject = project;
            PageDefinitionIndex = pageIndex;
        }

        internal string []? Path { get; set; }
        internal Project? BaseProject { get; set; }
        internal int? PageDefinitionIndex { get; set; }

        public override string ToString()
        {
            if (Path == null || Path.Length < 1) return "Choose a data path";
            return string.Join(".", Path);
        }
    }
}