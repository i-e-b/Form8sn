﻿using System;
using System.Linq;
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
            
            UpdateDisplayFormatInfo();
            
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
            var container = _project!.Pages[_pageIndex].Boxes;

            var box = _project!.Pages[_pageIndex].Boxes[_boxKey];
            box.WrapText = wrapTextCheckbox!.Checked;
            box.ShrinkToFit = shrinkToFitCheckbox!.Checked;

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
            
            _project?.Save();
            Close();
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            _project!.Pages[_pageIndex].Boxes.Remove(_boxKey);
            _project?.Save();
            Close();
        }

        private void setMappingButton_Click(object sender, EventArgs e)
        {
            if (_project == null) return;
            var box = _project!.Pages[_pageIndex].Boxes[_boxKey];
            
            var pageRepeat = _project!.Pages[_pageIndex].RepeatMode.DataPath;
            var mapEdit = new PickDataSource(_project, "Pick source for box", box.MappingPath, pageRepeat);
            mapEdit.ShowDialog();

            if (mapEdit.SelectedPath != null && mapEdit.SelectedPath.Length > 0)
            {
                box.MappingPath = mapEdit.SelectedPath;
                dataPathLabel!.Text = string.Join(".", box.MappingPath);
                _project.Save();
            }
        }

        private void setDataFormatButton_Click(object sender, EventArgs e)
        {
            if (_project == null) return;
            // A display format edit form, like the filter editor
            var dfe = new DisplayFormatEditor(_project, _pageIndex, _boxKey);
            dfe.ShowDialog();
            UpdateDisplayFormatInfo();
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
    }
}