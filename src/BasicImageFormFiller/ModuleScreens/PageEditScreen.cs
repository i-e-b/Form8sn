using System;
using System.IO;
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
        private const string ChangePageRepeatCommand = "/change-repeat";
        private const string EditMappingCommand = "/edit-mapping";
        private const string EditBoxesCommand = "/edit-boxes";
        private const string EditMetaDataCommand = "/edit-meta";
        private const string BackToTemplateCommand = "/back-to-template";
        private const string EditBackgroundCommand = "/pick-background";

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
                ? T.g("p")[$"Page repeats over '{page.RepeatMode.DataPath ?? "<invalid reference>"}' ", changeRpt]
                : T.g("p")["Single page ", changeRpt];
            content.Add(repeats);
            

            var background = string.IsNullOrWhiteSpace(page.BackgroundImage)
                ? T.g()["no background"]
                : T.g()[T.g("h3")["Preview of ", page.BackgroundImage], T.g("br/"), T.g("img",  "src",page.GetBackgroundUrl(_project),  "width","100%")];
            
            content.Add(
                T.g("ul")[
                T.g("li").Repeat(
                    T.g("a", "href",EditMetaDataCommand)["Edit page info & notes"],
                    T.g("a", "href",EditBoxesCommand)["Place template boxes on background"],
                    T.g("a", "href",EditMappingCommand)["Edit data to box mapping"],
                    T.g("a", "href",EditBackgroundCommand)["Pick background image"]
                    )
                ]);

            content.Add(
                T.g("hr/"),
                background
            );
            
            return content;
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
                    var screen = new BoxPlacer(this, _project, _pageIndex);
                    screen.ShowDialog();
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditMappingCommand:
                {
                    
                    //_stateChange = StateChangePermission.NotAllowed;
                    // TODO: show edit screen, with link back to self for unlock
                    throw new Exception("Not yet implemented");
                    break;
                }

                case ChangePageRepeatCommand:
                {
                    // TODO: show screen to edit
                    
                    // This is just the data picker:
                    var pds = new PickDataSource(_project);
                    pds.ShowDialog();
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