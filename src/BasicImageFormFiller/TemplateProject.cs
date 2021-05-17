using System;
using System.IO;
using System.Windows.Forms;
using BasicImageFormFiller.FileFormats;
using SkinnyJson;
using Tag;

namespace BasicImageFormFiller
{
    internal class TemplateProject: IScreenModule
    {
        private const string AddPageAtEndCommand = "/add-at-end";
        private readonly string _path;
        private readonly IndexFile _index;

        public TemplateProject(string path)
        {
            _path = path;
            _index = Json.Defrost<IndexFile>(File.ReadAllText(path));
        }

        public TagContent StartScreen()
        {
            var page = T.g()[
                T.g("h1")[_index.Name],
                T.g("p")[_path],
                T.g("p")[_index.Notes]
            ];

            if (_index.Pages.Count > 0)
            {
                foreach (var templatePage in _index.Pages)
                {
                    page.Add(
                        T.g("p")[templatePage.BackgroundImage ?? "no background"] // TODO: list pages with thumbnails
                        );
                }
            }
            else { page.Add(T.g("p",  "style","font-style:italic;")["Project is empty"]); }
            
            page.Add(
                T.g("br/"),
                T.g("a",  "href",AddPageAtEndCommand)["Add new page"]
                );

            return page;
        }

        public void InterpretCommand(ITagModuleScreen moduleScreen, string command)
        {
            switch (command)
            {
                case AddPageAtEndCommand:
                {
                    // TODO: add a blank page, and refresh screen
                    _index.Pages.Add(new TemplatePage());
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }
            }
        }
    }
}