using System;
using System.IO;
using System.Windows.Forms;
using BasicImageFormFiller.EditForms;
using BasicImageFormFiller.FileFormats;
using BasicImageFormFiller.Interfaces;
using Tag;

namespace BasicImageFormFiller.ModuleScreens
{
    internal class PageEditScreen : IScreenModule
    {
        private readonly Project _project;
        private readonly int _pageIndex;
        private StateChangePermission _stateChange;
        private const string BackToTemplateCommand = "/back-to-template";
        private const string ChangePageRepeatCommand = "/change-repeat";
        private const string EditMappingCommand = "/edit-mapping";
        private const string EditBoxesCommand = "/edit-boxes";
        private const string EditMetaDataCommand = "/edit-meta";
        private const string EditBackgroundCommand = "/pick-background";
        private const string DeletePageCommand = "/delete-this-page";

        public PageEditScreen(Project project, int pageIndex)
        {
            _stateChange = StateChangePermission.Allowed;
            _project = project;
            _pageIndex = pageIndex;
        }

        public TagContent StartScreen()
        {
            var page = _project.Pages[_pageIndex];
            var content = T.g()[
                T.g("h2")[$"Page {_pageIndex+1}: '{page.Name}'"],
                T.g("p")[T.g("a", "href", BackToTemplateCommand)[$"Back to '{_project.Index.Name}' overview"]],
                T.g("p")[page.Notes ?? ""],
                T.g("hr/")
            ];

            var changeRpt = T.g("a", "href",ChangePageRepeatCommand)["Change repeat mode"];
            var repeats = page.RepeatMode.Repeats
                ? T.g("p")[$"Page repeats over '{RepeatPath(page)}' ", changeRpt]
                : T.g("p")["Single page ", changeRpt];
            content.Add(repeats);
            

            var background = string.IsNullOrWhiteSpace(page.BackgroundImage)
                ? T.g()["no background"]
                : T.g()[T.g("h3")["Preview of ", page.BackgroundImage], T.g("br/"), T.g("img",  "src",page.GetBackgroundPreviewUrl(_project),  "width","100%")];
            
            content.Add(
                T.g("ul")[
                T.g("li").Repeat(
                    T.g("a", "href",EditBoxesCommand)["PLACE - Layout template boxes on background"],
                    T.g("a", "href",EditMappingCommand)["MAP - Edit source data to box mapping"],
                    T.g("a", "href",EditMetaDataCommand)["Edit page info & notes"],
                    T.g("a", "href",EditBackgroundCommand)["Pick background image"]
                    )
                ]);
            
            content.Add(
                T.g("a", "href",DeletePageCommand,  "style","color:#f00")["Delete this page"]
                );

            content.Add(
                T.g("hr/"),
                background
            );
            
            return content;
        }

        private static string RepeatPath(TemplatePage page)
        {
            if (page.RepeatMode.DataPath == null) return "<invalid reference>";
            return string.Join(".", page.RepeatMode.DataPath);
        }

        public void InterpretCommand(ITagModuleScreen moduleScreen, string command)
        {
            switch (command)
            {
                case BackToTemplateCommand:
                {
                    moduleScreen.SwitchToModule(new MainProjectScreen(_project));
                    break;
                }

                case DeletePageCommand:
                {
                    var result = MessageBox.Show("Are you sure", "Delete", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        _project.Index.Pages.RemoveAt(_pageIndex);
                        _project.Save();
                        moduleScreen.SwitchToModule(new MainProjectScreen(_project));
                        return;
                    }
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditBackgroundCommand:
                {
                    ChooseBackgroundFile(moduleScreen);
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditMetaDataCommand:
                {
                    _stateChange = StateChangePermission.NotAllowed;
                    var screen = new EditPageMeta(this, _project, _pageIndex);
                    screen.ShowDialog();
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditBoxesCommand:
                {
                    _stateChange = StateChangePermission.NotAllowed;
                    new BoxPlacer(this, _project, _pageIndex).ShowDialog();
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditMappingCommand:
                {
                    moduleScreen.SwitchToModule(new PageMapEditScreen(_project, _pageIndex));
                    break;
                }

                case ChangePageRepeatCommand:
                {
                    if (string.IsNullOrWhiteSpace(_project.Index.SampleFileName))
                    {
                        MessageBox.Show("Please add a sample file before setting repeat mappings");
                        moduleScreen.ShowPage(StartScreen());
                        return;
                    }

                    _stateChange = StateChangePermission.NotAllowed;
                    new RepeatModePicker(this, _project, _pageIndex).ShowDialog();
                    _project.Reload();
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                default:
                    throw new Exception($"Unhandled command: {command}");
            }
        }

        private void ChooseBackgroundFile(ITagModuleScreen module)
        {
            // If cancel, do nothing
            // If not in the project directory, copy it in and continue with that as the new path
            // Check that it's json
            //  - if not, delete the file and go back to the old one (if present)
            //  - if OK, set the sample value and save
            
            var ok = module.PickAFile(out var filePath);
            if (!ok || filePath == null) return;

            if (Path.GetDirectoryName(filePath) != _project.BasePath)
            {
                var newPath = Path.Combine(_project.BasePath, Path.GetFileName(filePath));
                File.Copy(filePath, newPath);
                filePath = newPath;
            }

            _project.Pages[_pageIndex].BackgroundImage = Path.GetFileName(filePath);
            _project.Save();
        }


        public StateChangePermission StateChangeRequest() => _stateChange;
        public void Activate()
        {
            _stateChange = StateChangePermission.Allowed;
            _project.Reload();
        }
    }
}