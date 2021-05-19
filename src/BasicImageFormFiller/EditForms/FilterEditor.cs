using System;
using System.Collections;
using System.Linq;
using System.Windows.Forms;
using BasicImageFormFiller.FileFormats;
using BasicImageFormFiller.Interfaces;

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
            // TODO: save values, change key name if required.

            Close();
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
    }
}