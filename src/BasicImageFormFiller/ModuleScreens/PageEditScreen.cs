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
        private const string EditMappingCommand = "/edit-mapping";
        private const string EditBoxesCommand = "/edit-boxes";
        private const string EditMetaDataCommand = "/edit-meta";
        private const string BackToTemplateCommand = "/back-to-template";

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
                T.g("p")[T.g("a", "href", BackToTemplateCommand)[$"Back to '{_project.Index.Name}' overview"]]
            ]; 
            
            content.Add(
                T.g("p").Repeat(
                    T.g("a", "href",EditMetaDataCommand)["Edit page info & notes"],
                    T.g("a", "href",EditBoxesCommand)["Place template boxes on background"],
                    T.g("a", "href",EditMappingCommand)["Edit data to box mapping"]
                    )
                );
            
            return content;
        }
        
        public void InterpretCommand(ITagModuleScreen moduleScreen, string command)
        {
            switch (command)
            {
                case BackToTemplateCommand:
                {
                    moduleScreen.SwitchToModule(new TemplateProject(_project));
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
                    // TODO: show edit screen, with link back to self for unlock
                    break;
                }

                case EditMappingCommand:
                {
                    
                    _stateChange = StateChangePermission.NotAllowed;
                    // TODO: show edit screen, with link back to self for unlock
                    break;
                }
            }
        }


        public StateChangePermission StateChangeRequest() => _stateChange;
        public void Activate()
        {
            _stateChange = StateChangePermission.Allowed;
            _project.Reload();
        }
    }
}