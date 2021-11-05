using System;
using System.Windows.Forms;
using BasicImageFormFiller.Interfaces;
using Form8snCore.FileFormats;

namespace BasicImageFormFiller.EditForms
{
    public partial class RepeatModePicker : Form
    {
        private readonly IScreenModule? _returnModule;
        private readonly FileSystemProject? _project;
        private readonly int _pageIndex;
        private string[]? _selectedPath;

        public RepeatModePicker()
        {
            InitializeComponent();
        }

        public RepeatModePicker(IScreenModule returnModule, FileSystemProject project, int pageIndex)
        {
            InitializeComponent();
            _returnModule = returnModule;
            _project = project;
            _pageIndex = pageIndex;
            
            var page = _project.Pages[_pageIndex];
            
            repeatsCheckbox!.Checked = page.RepeatMode.Repeats;
            pickDataButton!.Enabled = page.RepeatMode.Repeats;
            if (page.RepeatMode.DataPath != null)
                dataPathLabel!.Text = string.Join(".", page.RepeatMode.DataPath);
        }

        private void pickDataButton_Click(object sender, EventArgs e)
        {
            if (_project == null) return;
            
            var rm = new PickDataSource(_project, "Pick repeat source", _project.Pages[_pageIndex].RepeatMode.DataPath, null, null, allowEmpty: false);
            rm.ShowDialog();
            if (rm.SelectedPath == null) return;
            
            _selectedPath = rm.SelectedPath;
            dataPathLabel!.Text = string.Join(".", rm.SelectedPath);
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (_project == null) return;
            
            var mode = _project.Pages[_pageIndex].RepeatMode;
            mode.Repeats = repeatsCheckbox!.Checked;
            mode.DataPath = _selectedPath;
            
            _project.Save();
            
            Close();
        }

        private void repeatsCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            pickDataButton!.Enabled = repeatsCheckbox!.Checked;
        }

        private void RepeatModePicker_FormClosing(object sender, FormClosingEventArgs e)
        {
            _returnModule?.Activate();
        }
    }
}