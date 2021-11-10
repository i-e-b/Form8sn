using System.IO;

namespace Form8snCore
{
    public interface IFileSource
    {
        Stream Load(string? fileName);
    }
}