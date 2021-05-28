using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using BasicImageFormFiller.EditForms;
using BasicImageFormFiller.Helpers;
using BasicImageFormFiller.Interfaces;
using Form8snCore.FileFormats;
using Tag;

namespace BasicImageFormFiller.ModuleScreens
{
    internal class PageFiltersScreen : IScreenModule
    {
        private const string BackToPageCommand = "/back-to-page";
        private const string AddNewFilterCommand = "/add-filter";
        private const string DeleteFilterCommand = "/delete-filter";
        private const string EditFilterCommand = "/edit-filter";
        
        private readonly Project _project;
        private readonly int _pageIndex;
        private StateChangePermission _stateChange;

        public PageFiltersScreen(Project project, int pageIndex)
        {
            _project = project;
            _pageIndex = pageIndex;
            _stateChange = StateChangePermission.Allowed;
        }

        public TagContent StartScreen()
        {
            var content = T.g()[
                T.g("h2")["Page Specific Filters"],
                T.g("p")[T.g("a", "href", BackToPageCommand)["Back to page overview"]],
                T.g("p")["Filters allow you to split, join, and transform the supplied data before writing it to forms."],
                T.g("hr/"),
                T.g("p")[T.g("a", "href", AddNewFilterCommand)["Add a filter"]]
            ];

            var list = T.g("dl");
            foreach (var filter in _project.Pages[_pageIndex].PageDataFilters)
            {
                list.Add(
                    T.g("dt")[filter.Key],
                    T.g("dd")[
                        filter.Value.MappingType.ToString(), DisplayParams(filter.Value), " over ", DisplayPath(filter), " | ",
                        T.g("a", "href", $"{DeleteFilterCommand}?filter={filter.Key}")["Delete"], " ",
                        T.g("a", "href", $"{EditFilterCommand}?filter={filter.Key}")["Edit"], " ",
                        T.g("br/"), T.g()["&nbsp;"]
                    ]
                );
            }
            content.Add(list);


            return content;
        }

        private string DisplayParams(MappingInfo map)
        {
            if (map.MappingParameters.Count < 1) return "";
            var sb = new StringBuilder(" (");
            foreach (var kvp in map.MappingParameters)
            {
                sb.Append(kvp.Key);
                sb.Append("=");
                sb.Append(kvp.Value);
            }
            sb.Append(") ");
            return sb.ToString();
        }

        private static string DisplayPath(KeyValuePair<string, MappingInfo> filter)
        {
            var path = filter.Value.DataPath;
            if (path == null || path.Length < 1) return "&lt;no path set&gt;";
            return string.Join(".", path).Replace("<","&lt;").Replace(">","&gt;");
        }

        public void InterpretCommand(ITagModuleScreen moduleScreen, string command)
        {
            var prefix = Url.Prefix(command);
            switch (prefix)
            {
                case BackToPageCommand:
                {
                    moduleScreen.SwitchToModule(new MainProjectScreen(_project, _pageIndex));
                    break;
                }

                case AddNewFilterCommand:
                {
                    AddNewFilter();
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case DeleteFilterCommand:
                {
                    var name = Url.GetValueFromQuery(command, "filter");
                    DeleteFilter(name);
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case EditFilterCommand:
                {
                    var name = Url.GetValueFromQuery(command, "filter");
                    if (string.IsNullOrWhiteSpace(name)) return;
                    
                    _stateChange = StateChangePermission.NotAllowed;
                    var ed = new FilterEditor(this, _project, name, _pageIndex);
                    ed.ShowDialog();
                    
                    _project.Reload();
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                default:
                    throw new Exception($"Unhandled command: {command}");
            }
        }

        private void DeleteFilter(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            _project.Pages[_pageIndex].PageDataFilters.Remove(name);
            _project.Save();
        }

        private void AddNewFilter()
        {
            var filters = _project.Pages[_pageIndex].PageDataFilters;
            for (int index = 0; index < 256; index++)
            {
                var key = $"Untitled {index+1}";
                if (filters.ContainsKey(key)) continue;
                
                filters.Add(key, new MappingInfo(MappingType.None));
                _project.Save();
                return;
            }
            MessageBox.Show("Too many un-named filters. Please rename some");
        }

        public StateChangePermission StateChangeRequest() => _stateChange;
        public void Activate()
        {
            _stateChange = StateChangePermission.Allowed;
            _project.Reload();
        }
    }
}