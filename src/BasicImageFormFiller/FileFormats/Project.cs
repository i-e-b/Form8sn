using System;
using System.Collections.Generic;
using System.IO;
using SkinnyJson;

namespace BasicImageFormFiller.FileFormats
{
    public class Project
    {
        private readonly string _indexPath;
        private readonly string _baseUri;

        public Project(string path)
        {
            _indexPath = path;
            BasePath = Path.GetDirectoryName(_indexPath)!;
            _baseUri = "file:///"+BasePath.Replace("\\","/");
            Index = Json.Defrost<IndexFile>(File.ReadAllText(_indexPath)!)!;
        }

        public IndexFile Index { get; private set; }
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
            Index = Json.Defrost<IndexFile>(File.ReadAllText(_indexPath)!)!;
        }
    }
}