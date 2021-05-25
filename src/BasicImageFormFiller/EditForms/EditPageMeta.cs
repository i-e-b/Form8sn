using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using BasicImageFormFiller.Interfaces;
using Form8snCore.FileFormats;

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
            
            var page = _project.Pages[pageIndex];
            
            nameTextBox!.Text = page.Name;
            renderBackgroundCheckbox!.Checked = page.RenderBackground;
            notesTextBox!.Text = page.Notes;
            
            fontSizeTextBox!.Text = page.PageFontSize?.ToString() ?? "16";
            
            widthTextBox!.Text = page.WidthMillimetres.ToString(CultureInfo.InvariantCulture);
            heightTextBox!.Text = page.HeightMillimetres.ToString(CultureInfo.InvariantCulture);
        }

        private void EditPageMeta_FormClosing(object sender, FormClosingEventArgs e)
        {
            _returnModule?.Activate();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (_project != null)
            {
                var page = _project.Pages[_pageIndex];
                page.Name = nameTextBox!.Text;
                page.RenderBackground = renderBackgroundCheckbox!.Checked;
                page.Notes = notesTextBox!.Text;
                page.PageFontSize = int.TryParse(fontSizeTextBox!.Text, out var size) ? size : (int?) null;
                
                if (int.TryParse(widthTextBox!.Text, out var w)) page.WidthMillimetres = w;
                if (int.TryParse(heightTextBox!.Text, out var h)) page.HeightMillimetres = h;
                
                _project.Save();
            }
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private bool InRange(double aspect, double target)
        {
            var diff = Math.Abs(aspect - target);
            return diff < 0.05;
        }

        private void guessSizeButton_Click(object sender, EventArgs e)
        {
            // guess size from background. Default to A4.
            var path = _project?.Pages[_pageIndex].GetBackgroundPath(_project);
            if (path == null || !File.Exists(path)) return;
            
            using var img = Image.FromFile(path);
            if (img.Height < 1) return;
            
            var aspect = img.Width / (double)img.Height;

            if (InRange(aspect, 0.707)) // A4 Portrait
            {
                widthTextBox!.Text = "210";
                heightTextBox!.Text = "297";
                return;
            }

            if (InRange(aspect, 0.774)) // US letter Portrait
            {
                widthTextBox!.Text = "216";
                heightTextBox!.Text = "279";
                return;
            }
            
            if (InRange(aspect, 0.607)) // US legal Portrait
            {
                widthTextBox!.Text = "216";
                heightTextBox!.Text = "356";
                return;
            }
            
            
            if (InRange(aspect, 1.414)) // A4 Landscape
            {
                widthTextBox!.Text = "297";
                heightTextBox!.Text = "210";
                return;
            }

            if (InRange(aspect, 1.292)) // US letter Landscape
            {
                widthTextBox!.Text = "279";
                heightTextBox!.Text = "216";
                return;
            }
            
            if (InRange(aspect, 1.648)) // US legal Landscape
            {
                widthTextBox!.Text = "356";
                heightTextBox!.Text = "216";
                return;
            }
            
            
            // No idea. Use A4
            if (aspect > 1)
            {
                widthTextBox!.Text = "297";
                heightTextBox!.Text = "210";
            }
            else
            {
                widthTextBox!.Text = "210";
                heightTextBox!.Text = "297";
            }
        }
    }
}