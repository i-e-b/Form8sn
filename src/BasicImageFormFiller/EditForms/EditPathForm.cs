using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BasicImageFormFiller.Interfaces;

namespace BasicImageFormFiller.EditForms
{
    public partial class EditPathForm : Form
    {
        private readonly IScreenModule? _returnModule;
        private readonly string[] _originalValue;
        private bool _updating;
        
        public string[]? EditedPath { get; set; }
        
        public EditPathForm()
        {
            InitializeComponent();
            _originalValue = new string[0];
        }

        public EditPathForm(IScreenModule? returnModule, IEnumerable<string> path)
        {
            _returnModule = returnModule;
            InitializeComponent();

            _updating = false;
            _originalValue = path.ToArray();
            pathElementView!.DataSource = _originalValue.Select(element => new PathElementItem(element)).ToList();
            pathElementView.Columns[0]!.Width = pathElementView.Width - 70; // hack to fill the view.
        }

        private void pathElementView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_updating)
            {
                _updating = false;
                return;
            }

            var x = pathElementView!.DataSource as List<PathElementItem> ?? throw new Exception("Failure in DataGridView");
            
            var i = e.RowIndex;
            
            var original = _originalValue[i];
            var updated = x[i].PathElement;

            if (original.StartsWith('[') && original.EndsWith(']'))
            {
                _updating = true;
                var final = $"[{updated.Trim('[', ']')}]";
                x[i].PathElement = final;
                pathElementView.UpdateCellValue(0, i);
            }
        }

        private void EditPathForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _returnModule?.Activate();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            EditedPath = null;
            Close();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            var result = pathElementView!.DataSource as List<PathElementItem>;
            EditedPath = result?.Select(r=>r.PathElement).ToArray();
            
            Close();
        }
    }

    public class PathElementItem
    {
        public PathElementItem(string s)
        {
            PathElement = s;
        }
        public string PathElement { get; set; }
    }
}