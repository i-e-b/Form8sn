using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Form8snCore.FileFormats;

namespace BasicImageFormFiller.EditForms
{
    public partial class BoxEdit : Form
    {
        private readonly Project? _project;
        private readonly int _pageIndex;
        private readonly string _boxKey;

        public BoxEdit()
        {
            _boxKey = "";
            InitializeComponent();
        }

        public BoxEdit (Project project, int pageIndex, string boxKey)
        {
            InitializeComponent();
            _project = project;
            _pageIndex = pageIndex;
            _boxKey = boxKey;
            
            var box = project.Pages[pageIndex].Boxes[boxKey];

            dataPathLabel!.Text = box.MappingPath == null
                ? ""
                : string.Join(".", box.MappingPath);
            
            SetupDependentCombo(project, pageIndex, boxKey, box);

            UpdateDisplayFormatInfo();
            
            processOrderTextBox!.Text = box.BoxOrder?.ToString() ?? "";
            fontSizeTextBox!.Text = box.BoxFontSize?.ToString() ?? "";
            
            wrapTextCheckbox!.Checked = box.WrapText;
            shrinkToFitCheckbox!.Checked = box.ShrinkToFit;
            boxKeyTextbox!.Text = boxKey;

            topLeft!.Checked = box.Alignment == TextAlignment.TopLeft;
            topCentre!.Checked = box.Alignment == TextAlignment.TopCentre;
            topRight!.Checked = box.Alignment == TextAlignment.TopRight;
            
            midLeft!.Checked = box.Alignment == TextAlignment.MidlineLeft;
            midCentre!.Checked = box.Alignment == TextAlignment.MidlineCentre;
            midRight!.Checked = box.Alignment == TextAlignment.MidlineRight;
            
            bottomLeft!.Checked = box.Alignment == TextAlignment.BottomLeft;
            bottomCentre!.Checked = box.Alignment == TextAlignment.BottomCentre;
            bottomRight!.Checked = box.Alignment == TextAlignment.BottomRight;
        }
        
        private void BoxEdit_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            UpdateBoxDefinition();
            _project?.Save();
            Close();
        }

        private void copyBoxButton_Click(object sender, EventArgs e)
        {
            // Write a new box with a copy name and an offset, then save & close
            UpdateBoxDefinition();
            CopyBox();
            _project?.Save();
            Close();
        }

        private void setDataFormatButton_Click(object sender, EventArgs e)
        {
            if (_project == null) return;
            // A display format edit form, like the filter editor
            var dfe = new DisplayFormatEditor(_project, _pageIndex, _boxKey);
            dfe.ShowDialog();
            UpdateDisplayFormatInfo();
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            var boxDict = _project!.Pages[_pageIndex].Boxes;
            boxDict.Remove(_boxKey);
            
            // delete any dependencies
            foreach (var box in boxDict.Values) { if (box.DependsOn == _boxKey) box.DependsOn = null; }
            
            _project?.Save();
            Close();
        }

        private void setMappingButton_Click(object sender, EventArgs e)
        {
            if (_project == null) return;
            var box = _project!.Pages[_pageIndex].Boxes[_boxKey];
            
            var pageRepeat = _project!.Pages[_pageIndex].RepeatMode.DataPath;
            var mapEdit = new PickDataSource(_project, "Pick source for box", box.MappingPath, pageRepeat, _pageIndex, allowEmpty:false);
            mapEdit.ShowDialog();

            if (mapEdit.SelectedPath != null && mapEdit.SelectedPath.Length > 0)
            {
                box.MappingPath = mapEdit.SelectedPath;
                dataPathLabel!.Text = string.Join(".", box.MappingPath);

                if (IsUsingDefaultName(boxKeyTextbox?.Text)) // auto-name the box based on the data path
                {
                    boxKeyTextbox!.Text = AutoNameBox(box) ??  boxKeyTextbox!.Text;
                }

                _project.Save();
            }
        }

        
        private void UpdateBoxDefinition()
        {
            var container = _project!.Pages[_pageIndex].Boxes;

            var box = _project!.Pages[_pageIndex].Boxes[_boxKey];
            box.WrapText = wrapTextCheckbox!.Checked;
            box.ShrinkToFit = shrinkToFitCheckbox!.Checked;

            if (string.IsNullOrWhiteSpace(fontSizeTextBox!.Text)) box.BoxFontSize = null;
            else if (int.TryParse(fontSizeTextBox!.Text, out var fontSize)) box.BoxFontSize = fontSize;
            else box.BoxFontSize = null;

            if (string.IsNullOrWhiteSpace(processOrderTextBox!.Text)) box.BoxOrder = null;
            else if (int.TryParse(processOrderTextBox!.Text, out var order)) box.BoxOrder = order;
            else box.BoxOrder = null;

            if (string.IsNullOrWhiteSpace(dependsOnComboBox!.Text)) box.DependsOn = null;
            else if (NotCircular(dependsOnComboBox!.Text, _boxKey)) box.DependsOn = dependsOnComboBox!.Text;

            if (topLeft!.Checked) box.Alignment = TextAlignment.TopLeft;
            else if (topCentre!.Checked) box.Alignment = TextAlignment.TopCentre;
            else if (topRight!.Checked) box.Alignment = TextAlignment.TopRight;

            else if (midLeft!.Checked) box.Alignment = TextAlignment.MidlineLeft;
            else if (midCentre!.Checked) box.Alignment = TextAlignment.MidlineCentre;
            else if (midRight!.Checked) box.Alignment = TextAlignment.MidlineRight;

            else if (bottomLeft!.Checked) box.Alignment = TextAlignment.BottomLeft;
            else if (bottomCentre!.Checked) box.Alignment = TextAlignment.BottomCentre;
            else if (bottomRight!.Checked) box.Alignment = TextAlignment.BottomRight;
            else box.Alignment = TextAlignment.MidlineLeft;


            // If key has changed, try updating
            var newKey = boxKeyTextbox!.Text;
            // if key has changed and it's not already in use:
            if (newKey != _boxKey && !string.IsNullOrWhiteSpace(newKey) && !container.ContainsKey(newKey))
            {
                var tmp = container[_boxKey];
                container.Remove(_boxKey);
                container.Add(newKey, tmp);
            }
        }

        private bool NotCircular(string boxDependsOn, string boxKey)
        {
            const bool circular = false;
            const bool ok = true;
            
            var page = _project?.Pages[_pageIndex];
            if (page == null) return circular;
            if (boxDependsOn == boxKey) return circular;
            
            var currentBox = page.Boxes[boxDependsOn];
            for (int i = 0; i < 255; i++) // safety limit
            {
                if (currentBox.DependsOn == null) return ok;
                if (currentBox.DependsOn == boxKey) return circular;
                
                if (!page.Boxes.ContainsKey(currentBox.DependsOn)) return ok; // the chain is broken, but no circular
                currentBox = page.Boxes[currentBox.DependsOn!];
            }
            return circular;
        }

        private static string? AutoNameBox(TemplateBox box)
        {
            if (box.MappingPath == null) return null;
            var sb = new StringBuilder();
            
            if (box.MappingPath[0] == "#") sb.Append("filter ");
            sb.Append(string.Join(" ", box.MappingPath.Skip(1)));
            if (box.MappingPath[0] == "D") sb.Append(" from page");
            
            return sb.ToString();
        }

        private bool IsUsingDefaultName(string? name)
        {
            if (name == null) return false;
            if (!name.StartsWith("Box_")) return false;
            if (!int.TryParse(name.Substring(4), out _)) return false;
            return true;
        }

        private void SetupDependentCombo(Project project, int pageIndex, string boxKey, TemplateBox box)
        {
            dependsOnComboBox!.Items.Clear();
            dependsOnComboBox!.Items.Add(""); // Blank is no dependency
            foreach (var key in project.Pages[pageIndex].Boxes.Keys.OrderBy(s=>s))
            {
                if (key != boxKey) dependsOnComboBox!.Items.Add(key);
            }

            dependsOnComboBox.SelectedItem = box.DependsOn ?? "";
        }

        private void UpdateDisplayFormatInfo()
        {
            var format = _project?.Pages[_pageIndex].Boxes[_boxKey].DisplayFormat;
            if (format == null)
            {
                displayFormatInfo!.Text = "None";
                return;
            }
            
            var paramText = string.Join(", ",
                format.FormatParameters
                    .Where(kvp=> !string.IsNullOrEmpty(kvp.Value))
                    .Select(kvp => $"{kvp.Key} = '{kvp.Value}'")
            );
            var split = paramText.Length > 0 ? ":\r\n" : "";
            displayFormatInfo!.Text = $"{format.Type}{split}{paramText}";
        }

        private void CopyBox()
        {
            var parentKey = boxKeyTextbox!.Text;
            if (_project == null || string.IsNullOrEmpty(parentKey) || !_project.Pages[_pageIndex].Boxes.ContainsKey(parentKey)) return;
            var parent = _project.Pages[_pageIndex].Boxes[parentKey];
            
            string? name = null;
            for (int i = 0; i < 255; i++)
            {
                var potential = $"copy {i+1} of {parentKey}";
                if (_project!.Pages[_pageIndex].Boxes.ContainsKey(potential)) continue; // already used
                name = potential;
                break;
            }
            if (name == null) return;
            
            _project!.Pages[_pageIndex].Boxes.Add(
                name, new TemplateBox(parent)
            );
        }
    }
}