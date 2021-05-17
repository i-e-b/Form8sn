using System;
using System.IO;
using BasicImageFormFiller.FileFormats;
using SkinnyJson;
using Tag;

namespace BasicImageFormFiller
{
    internal class TemplateProject: IScreenModule
    {
        private const string SetSampleFileCommand= "/set-sample-file";
        private const string AddPageAtEndCommand = "/add-at-end";
        private readonly string _indexPath;
        private readonly string _basePath;
        private readonly IndexFile _index;

        public TemplateProject(string indexPath)
        {
            _indexPath = indexPath;
            _basePath = Path.GetDirectoryName(indexPath)!;
            _index = Json.Defrost<IndexFile>(File.ReadAllText(indexPath)!)!;
        }

        public TagContent StartScreen()
        {
            var page = T.g()[
                T.g("h1")[_index.Name],
                T.g("p")[_indexPath],
                T.g("p")[_index.Notes]
            ];

            if (string.IsNullOrWhiteSpace(_index.SampleFileName))
            {
                page.Add(T.g("p")[
                    "No sample file loaded.",
                    T.g("a", "href",SetSampleFileCommand)["Add a sample file"]
                ]);
            }
            else
            {
                page.Add(T.g("p")[
                    $"Sample file '{_index.SampleFileName}'. ",
                    T.g("a", "href",SetSampleFileCommand)["Replace sample file"]
                ]);
            }

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

        public StateChangePermission StateChangeRequest()
        {
            return StateChangePermission.Allowed;
        }

        public void InterpretCommand(ITagModuleScreen moduleScreen, string command)
        {
            switch (command)
            {
                case AddPageAtEndCommand:
                {
                    // TODO: add a blank page, and refresh screen
                    _index.Pages.Add(new TemplatePage());
                    SaveChanges();
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                case SetSampleFileCommand:
                {
                    ChooseSampleFile(moduleScreen);
                    moduleScreen.ShowPage(StartScreen());
                    break;
                }

                default:
                    throw new Exception($"Unexpected command: {command}");
            }
        }

        private void ChooseSampleFile(ITagModuleScreen module)
        {
            // TODO: Pick a file
            // If cancel, do nothing
            // If not in the project directory, copy it in and continue with that as the new path
            // Check that it's json
            //  - if not, delete the file and go back to the old one (if present)
            //  - if OK, set the sample value and save
            
            var ok = module.PickAFile(out var filePath);
            if (!ok || filePath == null) return;
            string? newPath = null;

            if (Path.GetDirectoryName(filePath) != _basePath)
            {
                newPath = Path.Combine(_basePath, Path.GetFileName(filePath));
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
            
            _index.SampleFileName = Path.GetFileName(filePath);
            SaveChanges();
        }

        private void SaveChanges()
        {
            var json = Json.Freeze(_index);
            if (string.IsNullOrWhiteSpace(json)) throw new Exception("Json serialiser returned an invalid result");
            File.WriteAllText(_indexPath, json);
        }
    }
}