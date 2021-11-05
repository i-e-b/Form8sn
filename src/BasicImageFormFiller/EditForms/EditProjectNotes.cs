using System;
using System.Windows.Forms;
using BasicImageFormFiller.Interfaces;
using Form8snCore.FileFormats;

namespace BasicImageFormFiller.EditForms
{
    public partial class EditProjectNotes : Form
    {
        private readonly IScreenModule? _returnModule;
        private readonly FileSystemProject? _project;

        public EditProjectNotes()
        {
            InitializeComponent();
        }
        public EditProjectNotes(IScreenModule returnModule, FileSystemProject project)
        {
            _returnModule = returnModule;
            _project = project;
            InitializeComponent();
            
            nameTextBox!.Text = _project.Index.Name;
            notesTextBox!.Text = _project.Index.Notes;
            fontFamilyTextBox!.Text = _project.Index.FontName?.ToString() ?? "";
            fontSizeTextBox!.Text = _project.Index.BaseFontSize?.ToString() ?? "";
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (_project != null)
            {
                _project.Index.Name = nameTextBox!.Text ?? "Untitled";
                _project.Index.Notes = notesTextBox!.Text ?? "";
                _project.Index.BaseFontSize = int.TryParse(fontSizeTextBox!.Text, out var size) ? size : (int?) null;
                _project.Index.FontName = string.IsNullOrWhiteSpace(fontFamilyTextBox!.Text) ? null : fontFamilyTextBox!.Text;
                _project.Save();
            }
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void EditProjectNotes_FormClosing(object sender, FormClosingEventArgs e)
        {
            _returnModule?.Activate();
        }
    }
}