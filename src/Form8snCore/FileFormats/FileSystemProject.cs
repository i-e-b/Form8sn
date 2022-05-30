using System;
using System.Collections.Generic;
using System.IO;
using SkinnyJson;

namespace Form8snCore.FileFormats
{
    /// <summary>
    /// A helper for loading/reading/storing project information
    /// </summary>
    public class FileSystemProject
    {
        private readonly string _indexPath;
        private readonly string _baseUri;

        public FileSystemProject(string path)
        {
            _indexPath = path;
            BasePath = Path.GetDirectoryName(_indexPath)!;
            _baseUri = "file:///"+BasePath.Replace("\\","/");
            Index = Json.Defrost<TemplateProject>(File.ReadAllText(_indexPath)!)!;
        }

        public TemplateProject Index { get; private set; }
        public string BasePath { get; }
        public List<TemplatePage> Pages => Index.Pages;
        public object BaseUri => _baseUri;

        public void Save()
        {
            var json = Json.Freeze(Index);
            if (string.IsNullOrWhiteSpace(json)) throw new Exception("Json serialiser returned an invalid result");
            File.WriteAllText(_indexPath, json);
        }

        public void Reload()
        {
            Index = Json.Defrost<TemplateProject>(File.ReadAllText(_indexPath)!)!;
        }

        public object? LoadSampleData()
        {
            if (string.IsNullOrWhiteSpace(Index.SampleFileName)) return null;
            var path = Path.Combine(BasePath, Index.SampleFileName);
            if (! File.Exists(path)) return null;
            
            return Json.Defrost(File.ReadAllText(path)!);
        }
    }
}