using System;
using System.Windows.Forms;
using BasicImageFormFiller.FileFormats;
using BasicImageFormFiller.Interfaces;

namespace BasicImageFormFiller.EditForms
{
    public partial class EditPageMeta : Form
    {
        private readonly IScreenModule? _returnModule;
        private readonly Project? _project;
        private readonly int _pageIndex;

        public EditPageMeta() { InitializeComponent(); }

        public EditPageMeta(IScreenModule returnModule, Project project, int pageIndex)
        {
            _returnModule = returnModule;
            _project = project;
            _pageIndex = pageIndex;
            InitializeComponent();
            
            nameTextBox!.Text = _project.Pages[pageIndex].Name;
            renderBackgroundCheckbox!.Checked = _project.Pages[pageIndex].RenderBackground;
        }

        private void EditPageMeta_FormClosing(object sender, FormClosingEventArgs e)
        {
            _returnModule?.Activate();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (_project != null)
            {
                _project.Pages[_pageIndex].Name = nameTextBox!.Text;
                _project.Pages[_pageIndex].RenderBackground = renderBackgroundCheckbox!.Checked;
                _project.Save();
            }
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}