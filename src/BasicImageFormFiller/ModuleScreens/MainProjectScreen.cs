using System;
using System.Collections.Generic;
using System.IO;
using BasicImageFormFiller.EditForms;
using BasicImageFormFiller.FileFormats;
using BasicImageFormFiller.Interfaces;
using SkinnyJson;
using Tag;

namespace BasicImageFormFiller.ModuleScreens
{
    internal class MainProjectScreen: IScreenModule
    {
        private readonly Project _project;
        private StateChangePermission _stateChange = StateChangePermission.Allowed;
        private const string EditProjectNotesCommand = "/edit-project-notes";
        private const string SetDataFiltersCommand = "/set-data-filters";
        private const string MovePageUpCommand = "/move-up";
        private const string MovePageDownCommand = "/move-dn";
        private const string SetSampleFileCommand= "/set-sample-file";
        private const string AddPageAtEndCommand = "/add-at-end";
        private const string EditPageCommand = "/edit-page";
        private const string InsertPageCommand = "/insert-page";

        public MainProjectScreen(Project project)
        {
            _project = project;
        }

        public TagContent StartScreen()
        {
            var clearLine = T.g("hr/", "style","clear:both");
            
            var content = T.g()[
                T.g("p")[_project.BasePath],
                T.g("h1")[_project.Index.Name],
                T.g("p")[_project.Index.Notes],
                T.g("a", "href",EditProjectNotesCommand)["Edit project name & notes"]
            ];

            if (string.IsNullOrWhiteSpace(_project.Index.SampleFileName))
            {
                content.Add(T.g("p")[
                    "No sample file loaded. ",
                    T.g("a", "href",SetSampleFileCommand)["Add a sample file"]
                ]);
            }
            else
            {
                content.Add(T.g("p")[
                    $"Sample file '{_project.Index.SampleFileName}'. ",
                    T.g("a", "href",SetSampleFileCommand)["Replace sample file"]
                ]);
            }
            content.Add(T.g("p")[
                T.g("a", "href",SetDataFiltersCommand)["Setup data filters and splits"]
            ]);

            if (_project.Index.Pages.Count > 0)
            {
                for (var index = 0; index < _project.Index.Pages.Count; index++)
                {
                    var templatePage = _project.Index.Pages[index];
                    
                    var bg = string.IsNullOrWhiteSpace(templatePage.BackgroundImage)
                        ? T.g("div",  "style","float:left;width:60%")["no background"]
                        : T.g("div",  "style","float:left;width:60%;height:50%;overflow-y:scroll")[
                            templatePage.BackgroundImage,
                            T.g("br/"),
                            T.g("img",  "src",templatePage.GetBackgroundUrl(_project),  "width","100%")
                        ];
                    
                    content.Add(
                        T.g("a", "href", $"{InsertPageCommand}?index={index}")["Insert new page here"],
                        clearLine,
                        T.g("div", "style", "float:left;width:30%")[
                            T.g("h3")[$"Page {index + 1}: ", templatePage.Name ?? "Untitled"],
                            T.g()[
                                T.g("a", "href", $"{EditPageCommand}?index={index}")[$"Edit page {index + 1} "],
                                " | Move ",
                                T.g("a", "href", $"{MovePageUpCommand}?index={index}")["Up"], " ",
                                T.g("a", "href", $"{MovePageDownCommand}?index={index}")["Down"]
                            ]
                        ],
                        bg,
                        clearLine
                    );
                }
            }
            else { content.Add(T.g("p",  "style","font-style:italic;")["Project is empty"]); }
            
            content.Add(
                T.g("br/"),
                T.g("a",  "href",AddPageAtEndCommand)["Insert new page here"]
                );

            return content;
        }

        public StateChangePermission StateChangeRequest() => _stateChange;

        public void Activate() { _stateChange = StateChangePermission.Allowed; }

        public void InterpretCommand(ITagModuleScreen moduleScreen, string command)
        {
            var prefix = command.Contains('?') ? command[..command.IndexOf('?')] :command;
            
            switch (prefix)
            {
                case AddPageAtEndCommand:
                {
                    _project.Pages.Add(new TemplatePage{Name = "Untitled"});
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

                case InsertPageCommand:
                {
                    var idx = GetIndexFromQuery(command);
                    _project.Pages.Insert(idx, new TemplatePage{Name = "Untitled"});
                    _project.Save();
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditPageCommand:
                {
                    var idx = GetIndexFromQuery(command);
                    if (idx >= _project.Pages.Count) throw new Exception("Page index out of range");
                    moduleScreen.SwitchToModule(new PageEditScreen(_project, idx));
                    break;
                }

                case MovePageUpCommand:
                {
                    var idx = GetIndexFromQuery(command);
                    if (idx <= 0) return; // ignore up past top
                    var tmp = _project.Pages[idx];
                    _project.Pages[idx] = _project.Pages[idx-1];
                    _project.Pages[idx-1] = tmp;
                    _project.Save();
                    moduleScreen.ShowPage(StartScreen());
                    return;
                }

                case MovePageDownCommand:
                {
                    var idx = GetIndexFromQuery(command);
                    if (idx >= _project.Pages.Count - 1) return; // ignore down past bottom
                    var tmp = _project.Pages[idx];
                    _project.Pages[idx] = _project.Pages[idx+1];
                    _project.Pages[idx+1] = tmp;
                    _project.Save();
                    moduleScreen.ShowPage(StartScreen());
                    return;
                }

                default:
                    throw new Exception($"Unexpected command: {command}");
            }
        }

        private static int GetIndexFromQuery(string command)
        {
            if (!GetQueryBits(command).TryGetValue("index", out var idxStr)) idxStr = "0";
            if (!int.TryParse(idxStr, out var idx)) idx = 0;
            return idx;
        }

        private static Dictionary<string,string> GetQueryBits(string command)
        {
            var result = new Dictionary<string,string>();
            var idx = command.IndexOf('?')+1;
            if (idx < 0) return result;
            var pairs = command[idx..]?.Split('&');
            if (pairs == null) return result;
            foreach (var pair in pairs)
            {
                var kvp = pair.Split("=", 2);
                if (result.ContainsKey(kvp[0])) continue;
                result.Add(kvp[0], kvp[1]);
            }
            return result;
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