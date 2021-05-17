using System;
using System.IO;
using System.Windows.Forms;
using BasicImageFormFiller.FileFormats;
using SkinnyJson;
using Tag;

namespace BasicImageFormFiller
{
    public partial class FormEditorWindow : Form, ITagModuleScreen
    {
        private static volatile bool _midNavigation;
        private IScreenModule _currentModule;
        private const string IndexFileName = "Index.json";

        public FormEditorWindow()
        {
            InitializeComponent();
            
            Json.DefaultParameters.EnableAnonymousTypes = true;

            _currentModule = new WelcomeScreen();
            ShowPage(_currentModule.StartScreen());
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DisallowStateChange()) return;
            Environment.Exit(0);
        }

        private bool DisallowStateChange()
        {
            // TODO: unsaved change check, work in progress check.
            // if user doesn't allow change-of-file or exit, or if the program is doing uninterruptible work -- then return true; 
            return false;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DisallowStateChange()) return;
            CreateNewTemplate();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DisallowStateChange()) return;
            LoadExistingTemplate();
        }
        
        

        private void CreateNewTemplate()
        {
            // Show 'new file' screen, write a folder and index there, then load it.
            saveFileDialog1!.FileName = "New Template";
            var result = saveFileDialog1.ShowDialog();
            if (result != DialogResult.OK) return;

            var path = saveFileDialog1.FileName;
            var name = Path.GetFileName(path);
            var directory = Directory.CreateDirectory(path);
            if (!directory.Exists) throw new Exception("Failed to create template path");

            File.WriteAllText(Path.Combine(path, IndexFileName), Json.Freeze(new IndexFile(name)));

            LoadTemplateProject(path);
        }
        
        private void LoadExistingTemplate()
        {
            var result = openFileDialog1!.ShowDialog();
            if (result != DialogResult.OK) return;

            var path = openFileDialog1.FileName;
            if (string.IsNullOrWhiteSpace(path)) return;
            LoadTemplateProject(path);
        }

        private void LoadTemplateProject(string path)
        {
            if (!path.EndsWith(IndexFileName, StringComparison.OrdinalIgnoreCase))
                path = Path.Combine(path, IndexFileName);

            if (!File.Exists(path)) ShowFailure("Index file not accessible. Check permissions?");
            _currentModule = new TemplateProject(path);
            ShowPage(_currentModule.StartScreen());
        }

        public void ShowFailure(string message)
        {
            ShowPage(T.g("pre")[
                "UNEXPECTED FAILURE:\r\n",
                message
            ]);
        }

        public void ShowNewTemplate()
        {
            CreateNewTemplate();
        }

        public void ShowLoadTemplate()
        {
            LoadExistingTemplate();
        }


        #region Page rendering

        public void ShowPage(TagContent bodyContent)
        {
            var page = BasePage(out var body);
            body.Add(bodyContent);
            _midNavigation = true;
            mainViewHtml!.DocumentText = page;
        }

        private TagContent BasePage(out TagContent body)
        {
            body = T.g("body");
            return T.g("html")[
                T.g("head"),
                body
            ];
        }

        #endregion

        #region Link hijack

        private void mainViewHtml_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            // Changing document text is considered 'Navigation', so we have to do this to prevent infinite loops
            if (_midNavigation) return;
            _midNavigation = true;
            
            var info = e.Url?.ToString();
            if (string.IsNullOrWhiteSpace(info)) return;
            if (info.StartsWith("about:"))
            {
                string command = info.Substring(6);
                try
                {
                    _currentModule.InterpretCommand(this, command);
                }
                catch (Exception ex)
                {
                    ShowFailure(ex.ToString());
                }
            }

            _midNavigation = false;
        }

        private void mainViewHtml_Navigated(object sender, WebBrowserNavigatedEventArgs e) => _midNavigation = false;

        #endregion

    }
}