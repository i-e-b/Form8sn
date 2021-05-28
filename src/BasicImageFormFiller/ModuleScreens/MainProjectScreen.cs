using System;
using System.IO;
using System.Windows.Forms;
using BasicImageFormFiller.EditForms;
using BasicImageFormFiller.Helpers;
using BasicImageFormFiller.Interfaces;
using Form8snCore;
using Form8snCore.FileFormats;
using SkinnyJson;
using Tag;

namespace BasicImageFormFiller.ModuleScreens
{
    internal class MainProjectScreen: IScreenModule
    {
        private readonly Project _project;
        private StateChangePermission _stateChange = StateChangePermission.Allowed;
        private int _lastPageIndex;
       
        #region Action links
        // Project wide
        private const string EditProjectNotesCommand = "/edit-project-notes";
        private const string SetDataFiltersCommand = "/set-data-filters";
        private const string MovePageUpCommand = "/move-up";
        private const string MovePageDownCommand = "/move-dn";
        private const string SetSampleFileCommand = "/set-sample-file";
        private const string RenderSampleCommand = "/render-sample";
        private const string AddPageAtEndCommand = "/add-at-end";
        private const string InsertPageCommand = "/insert-page";

        // per-page
        private const string ChangePageRepeatCommand = "/change-repeat";
        private const string EditPageFiltersCommand  = "/edit-filters";
        private const string EditBoxesCommand        = "/edit-boxes";
        private const string EditMetaDataCommand     = "/edit-meta";
        private const string EditBackgroundCommand   = "/pick-background";
        private const string DeletePageCommand       = "/delete-this-page";
        #endregion
        
        public MainProjectScreen(Project project, int pageIndex)
        {
            _lastPageIndex = pageIndex;
            _project = project;
        }

        public TagContent StartScreen()
        {
            var content = T.g();
            
            RenderProjectHeader(content);
            RenderSampleDataSection(content);

            if (_project.Index.Pages.Count > 0)
            {
                for (var index = 0; index < _project.Index.Pages.Count; index++)
                {
                    var templatePage = _project.Index.Pages[index];
                    RenderPageInfo(templatePage, content, index);
                }
            }
            else { content.Add(T.g("p",  "style","font-style:italic;")["Project is empty"]); }
            
            content.Add(
                T.g("br/"),
                T.g("a",  "href",AddPageAtEndCommand)["Insert new page here"]
                );

            content.Add(T.g("script")[$"var d=document.getElementById('page{_lastPageIndex}'); if(d)d.scrollIntoView();"]);
            return content;
        }

        private void RenderSampleDataSection(TagContent content)
        {
            if (string.IsNullOrWhiteSpace(_project.Index.SampleFileName))
            {
                content.Add(T.g("p")[
                    "No sample file loaded. ",
                    T.g("a", "href", SetSampleFileCommand)["Add a sample file"]
                ]);
            }
            else
            {
                content.Add(T.g("p")[
                    $"Sample file '{_project.Index.SampleFileName}'. ",
                    T.g("a", "href", SetSampleFileCommand)["Replace sample file"]
                ]);
            }

            content.Add(T.g("p")[
                T.g("a", "href", SetDataFiltersCommand)["Setup data filters and splits"]
            ]);
            
            content.Add(T.g("p")[
                T.g("a", "href", RenderSampleCommand)["Render PDF from sample data"]
            ]);
        }

        private void RenderProjectHeader(TagContent content)
        {
            content.Add(
                T.g("p")[_project.BasePath],
                T.g("h1")[_project.Index.Name],
                T.g("p")[_project.Index.Notes],
                T.g("a", "href", EditProjectNotesCommand)["Edit project name & notes"]
            );
        }

        private void RenderPageInfo(TemplatePage templatePage, TagContent content, int index)
        {
            var clearLine = T.g("hr/", "style","clear:both");

            content.Add(
                T.g("a", "href", $"{InsertPageCommand}?index={index}")["Insert new page here"],
                clearLine,
                T.g("div", "id",$"page{index}", "style","float:left;width:40%;margin-right:5%;")[
                    RenderPageInfoAndCommands(templatePage, index)
                ],
                T.g("div", "style", "float:left;width:50%;height:60%;overflow-y:scroll")[
                    RenderBackgroundPreview(templatePage)
                ],
                clearLine
            );
        }

        private TagContent RenderBackgroundPreview(TemplatePage templatePage)
        {
            var bg = string.IsNullOrWhiteSpace(templatePage.BackgroundImage)
                ? T.g()[
                    "no background | ",
                    T.g("a", "href", EditBackgroundCommand)["Pick background image"]
                ]
                : T.g()[
                    templatePage.BackgroundImage, " | ",
                    T.g("a", "href", EditBackgroundCommand)["Pick background image"],
                    T.g("br/"),
                    T.g("img", "src", templatePage.GetBackgroundPreviewUrl(_project), "width", "100%")
                ];
            return bg;
        }

        private TagContent RenderPageInfoAndCommands(TemplatePage page, int index)
        {
            // Title, description and delete link
            var content = T.g()[
                T.g("h3")[$"Page {index + 1}: ", page.Name ?? "Untitled"],
                T.g("p")[
                    $"Size: {page.WidthMillimetres}mm x {page.HeightMillimetres}mm",
                    " | Move page ",
                    T.g("a", "href", $"{MovePageUpCommand}?index={index}")["Up"], " ",
                    T.g("a", "href", $"{MovePageDownCommand}?index={index}")["Down"]
                ],
                T.g("p",  "style","font-style:italic;")[page.Notes??""],
                T.g("a", "href",$"{DeletePageCommand}?index={index}",  "style","color:#f00")["Delete this page"]
            ];
            
            // Repeater mode and edit link
            var changeRepeatModeLink = T.g("a", "href",$"{ChangePageRepeatCommand}?index={index}")["Change repeat mode"];
            content.Add(
                page.RepeatMode.Repeats
                    ? T.g("p")[$"Page repeats over '{RepeatPath(page)}' ", T.g("br/"), changeRepeatModeLink]
                    : T.g("p")["Single page ", changeRepeatModeLink]
            );
            
            // Links to other edit screens
            content.Add(
                T.g("ul")[
                    T.g("li").Repeat(
                        T.g("a", "href", $"{EditBoxesCommand}?index={index}")[" [ Boxes ] "],
                        T.g("a", "href", $"{EditPageFiltersCommand}?index={index}")["Page specific filters"],
                        T.g("a", "href", $"{EditMetaDataCommand}?index={index}")["Edit page info & notes"]
                    )
                ]);
            
            return content;
        }
        
        private static string RepeatPath(TemplatePage page)
        {
            if (page.RepeatMode.DataPath == null) return "<invalid reference>";
            return string.Join(".", page.RepeatMode.DataPath);
        }

        public StateChangePermission StateChangeRequest() => _stateChange;

        public void Activate() { _stateChange = StateChangePermission.Allowed; }

        public void InterpretCommand(ITagModuleScreen moduleScreen, string command)
        {
            var prefix = Url.Prefix(command);
            _lastPageIndex = -1; // by default, go back to the top of the screen
            
            switch (prefix)
            {
                #region Project commands

                case AddPageAtEndCommand:
                {
                    _project.Pages.Add(new TemplatePage {Name = "Untitled"});
                    _project.Save();
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditProjectNotesCommand:
                {
                    // show a mini-edit screen for name and notes
                    _stateChange = StateChangePermission.NotAllowed;
                    var screen = new EditProjectNotes(this, _project);
                    screen.ShowDialog();
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case SetSampleFileCommand:
                {
                    ChooseSampleFile(moduleScreen);
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case RenderSampleCommand:
                {
                    if (string.IsNullOrWhiteSpace(_project.Index.SampleFileName))
                    {
                        moduleScreen.ShowPage(StartScreen());
                        return;
                    }

                    var file = moduleScreen.PickNewFile();
                    if (!string.IsNullOrWhiteSpace(file))
                    {
                        var result = Render.ProjectToFile(file, Path.Combine(_project.BasePath, _project.Index.SampleFileName), _project);
                        if (result.Success)
                        {
                            MessageBox.Show($"Render complete\r\n\r\nloading images: {result.LoadingTime}\r\ntotal time: {result.OverallTime}");
                        }
                        else
                        {
                            MessageBox.Show($"Render failed: {result.ErrorMessage ?? "<unknown>"}");
                        }
                    }

                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case SetDataFiltersCommand:
                {
                    moduleScreen.SwitchToModule(new ProjectFiltersScreen(_project));
                    break;
                }

                #endregion

                #region Page commands

                case DeletePageCommand:
                {
                    var pageIndex = Url.GetIndexFromQuery(command);
                    _lastPageIndex = pageIndex;
                    var result = MessageBox.Show("Are you sure", "Delete", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        _project.Index.Pages.RemoveAt(pageIndex);
                        _project.Save();
                        _lastPageIndex = Math.Max(0, pageIndex-1);
                    }
                    
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditBackgroundCommand:
                {
                    var pageIndex = Url.GetIndexFromQuery(command);
                    ChooseBackgroundFile(moduleScreen, pageIndex);
                    _lastPageIndex = pageIndex;
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditMetaDataCommand:
                {
                    var pageIndex = Url.GetIndexFromQuery(command);
                    _stateChange = StateChangePermission.NotAllowed;
                    
                    new EditPageMeta(this, _project, pageIndex).ShowDialog();
                    
                    _lastPageIndex = pageIndex;
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditBoxesCommand:
                {
                    var pageIndex = Url.GetIndexFromQuery(command);
                    _stateChange = StateChangePermission.NotAllowed;
                    
                    new BoxPlacer(this, _project, pageIndex).ShowDialog();
                    
                    _lastPageIndex = pageIndex;
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditPageFiltersCommand:
                {
                    var pageIndex = Url.GetIndexFromQuery(command);
                    moduleScreen.SwitchToModule(new PageFiltersScreen(_project, pageIndex));
                    break;
                }

                case ChangePageRepeatCommand:
                {
                    var pageIndex = Url.GetIndexFromQuery(command);
                    if (string.IsNullOrWhiteSpace(_project.Index.SampleFileName))
                    {
                        MessageBox.Show("Please add a sample file before setting repeat mappings");
                        moduleScreen.ShowPage(StartScreen());
                        return;
                    }
                    _stateChange = StateChangePermission.NotAllowed;
                    
                    new RepeatModePicker(this, _project, pageIndex).ShowDialog();
                    
                    _project.Reload();
                    _lastPageIndex = pageIndex;
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case InsertPageCommand:
                {
                    var idx = Url.GetIndexFromQuery(command);
                    _project.Pages.Insert(idx, new TemplatePage {Name = "Untitled"});
                    _project.Save();
                    _lastPageIndex = idx;
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case MovePageUpCommand:
                {
                    var idx = Url.GetIndexFromQuery(command);
                    if (idx <= 0) return; // ignore up past top
                    var tmp = _project.Pages[idx];
                    _project.Pages[idx] = _project.Pages[idx - 1];
                    _project.Pages[idx - 1] = tmp;
                    _project.Save();
                    _lastPageIndex = idx - 1;
                    moduleScreen.ShowPage(StartScreen());
                    return;
                }

                case MovePageDownCommand:
                {
                    var idx = Url.GetIndexFromQuery(command);
                    if (idx >= _project.Pages.Count - 1) return; // ignore down past bottom
                    var tmp = _project.Pages[idx];
                    _project.Pages[idx] = _project.Pages[idx + 1];
                    _project.Pages[idx + 1] = tmp;
                    _project.Save();
                    _lastPageIndex = idx + 1;
                    moduleScreen.ShowPage(StartScreen());
                    return;
                }

                #endregion
                
                default:
                    throw new Exception($"Unexpected command: {command}");
            }
        }

        private void ChooseBackgroundFile(ITagModuleScreen module, int pageIndex)
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

            _project.Pages[pageIndex].BackgroundImage = Path.GetFileName(filePath);
            _project.Save();
        }

        private void ChooseSampleFile(ITagModuleScreen module)
        {
            // If cancel, do nothing
            // If not in the project directory, copy it in and continue with that as the new path
            // Check that it's json
            //  - if not, delete the file and go back to the old one (if present)
            //  - if OK, set the sample value and save
            
            var ok = module.PickAFile(out var filePath);
            if (!ok || filePath == null) return;
            string? newPath = null;

            if (Path.GetDirectoryName(filePath) != _project.BasePath)
            {
                newPath = Path.Combine(_project.BasePath, Path.GetFileName(filePath));
                File.Copy(filePath, newPath);
                filePath = newPath;
            }

            try
            {
                var test = Json.DefrostDynamic(File.ReadAllText(filePath)!);
                if (test == null) throw new Exception("Serialiser returned unexpected empty result");
            }
            catch (Exception ex)
            {
                if (newPath != null && File.Exists(newPath)) File.Delete(newPath);
                throw new Exception($"Could not read sample data: {ex.Message}", ex);
            }
            
            _project.Index.SampleFileName = Path.GetFileName(filePath);
            _project.Save();
        }
    }
}