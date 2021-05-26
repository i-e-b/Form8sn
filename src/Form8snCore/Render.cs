using Form8snCore.FileFormats;
using Form8snCore.Rendering;

namespace Form8snCore
{
    public static class Render
    {
        public static RenderResultInfo ProjectToFile(string outputFilePath, string dataFilePath, Project project)
        {
            return RenderProject.ToFile(outputFilePath, dataFilePath, project);
        }
    }
}