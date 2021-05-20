using System;
using System.Collections.Generic;
using BasicImageFormFiller.EditForms;
using BasicImageFormFiller.Helpers;
using BasicImageFormFiller.Interfaces;
using Form8snCore.FileFormats;
using Tag;

namespace BasicImageFormFiller.ModuleScreens
{
    internal class PageMapEditScreen : IScreenModule
    {
        private const string BackToPageCommand = "/back-to-page";
        private const string MapKeyCommand = "/map-key";
        private const string UnmapKeyCommand = "/unmap-key";
        
        private const string QueryKey = "key";
    
        private readonly Project _project;
        private readonly int _pageIndex;

        public PageMapEditScreen(Project project, int pageIndex)
        {
            _project = project;
            _pageIndex = pageIndex;
        }

        public TagContent StartScreen()
        {
            var page = _project.Pages[_pageIndex];
            var content = T.g()[
                T.g("h2")[$"Mappings for page {_pageIndex+1}: '{page.Name}'"],
                T.g("p")[T.g("a", "href", BackToPageCommand)["Back to page overview"]],
                T.g("p")[page.Notes ?? ""],
                T.g("hr/")
            ];

            var list = T.g("dl");
            foreach (var box in page.Boxes)
            {
                list.Add(RenderBoxMapping(box, list));
            }
            content.Add(list);
            
            return content;
        }

        private static TagContent RenderBoxMapping(KeyValuePair<string, TemplateBox> box, TagContent list)
        {
            var (key, value) = box;
            var path = value.MappingPath;
            list.Add(T.g("dt")[key]);

            var info = T.g("dd");
            if (path == null || path.Length < 1) info.Add(T.g("span", "style", "color:#f77")["&lt;unmapped&gt;"]);
            else info.Add(T.g("span", "style", "color:#070")[string.Join(".", path)]);

            info.Add(
                " | ",
                T.g("a", "href", $"{MapKeyCommand}?{QueryKey}={key}")["Map to..."], " | ",
                T.g("a", "href", $"{UnmapKeyCommand}?{QueryKey}={key}")["Unmap"],
                T.g("br/"), "&nbsp;"
            );
            return info;
        }

        public void InterpretCommand(ITagModuleScreen moduleScreen, string command)
        {
            var prefix = Url.Prefix(command);
            switch (prefix)
            {
                case BackToPageCommand:
                {
                    moduleScreen.SwitchToModule(new PageEditScreen(_project, _pageIndex));
                    break;
                }

                case MapKeyCommand:
                {
                    var key = Url.GetValueFromQuery(command, QueryKey);
                    var pick = new PickDataSource(_project, $"Map source for '{key}'", _project.Pages[_pageIndex].Boxes[key].MappingPath, null);
                    pick.ShowDialog();
                    if (pick.SelectedPath != null)
                    {
                        _project.Pages[_pageIndex].Boxes[key].MappingPath = pick.SelectedPath;
                        _project.Save();
                    }
                    
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case UnmapKeyCommand:
                {
                    var key = Url.GetValueFromQuery(command, QueryKey);
                    _project.Pages[_pageIndex].Boxes[key].MappingPath = null;
                    _project.Save();
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }
                
                default: throw new Exception($"Unknown command: '{command}'");
            }
        }

        public StateChangePermission StateChangeRequest()
        {
            throw new NotImplementedException();
        }

        public void Activate()
        {
            throw new NotImplementedException();
        }
    }
}