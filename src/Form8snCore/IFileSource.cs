using System.IO;

namespace Form8snCore
{
    public interface IFileSource
    {
        /// <summary>
        /// Load a file from the server's storage by name or ID.
        /// This is used to load background images for PDF pages.
        /// This should throw an exception if the file is not found.
        /// If your implementation doesn't support background images, throw a NotImplementedException.
        /// </summary>
        Stream Load(string? fileName);
        
        /// <summary>
        /// Load the file at a given URL as a stream, or null if it can't be loaded.
        /// This is used to load embedded images into files.
        /// </summary>
        Stream? LoadUrl(string? targetUrl);
    }
}