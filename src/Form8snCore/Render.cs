using System;
using System.IO;
using Form8snCore.FileFormats;
using Form8snCore.Rendering;

namespace Form8snCore
{
    public static class Render
    {
        public static RenderResultInfo ProjectToFile(string outputFilePath, string dataFilePath, IndexFile project)
        {
            var fileSource = new FileSource();
            return new RenderProject(fileSource).ToFile(outputFilePath, dataFilePath, project);
        }
    }

    public class FileSource : IFileSource
    {
        public Stream Load(string? fileName)
        {
            if (fileName is null) throw new Exception("Invalid file path");
            return File.OpenRead(fileName);
        }
    }
}