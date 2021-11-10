using System;
using System.IO;
using Form8snCore;
using Form8snCore.Rendering;

namespace BasicImageFormFiller.ModuleScreens
{
    public class FileSource : IFileSource
    {
        private readonly string _baseDir;

        public FileSource(string baseDir)
        {
            _baseDir = baseDir;
        }
        
        public Stream Load(string? fileName)
        {
            if (fileName is null) throw new Exception("Invalid file path");
            return File.OpenRead(Path.Combine(_baseDir, fileName));
        }
    }
}