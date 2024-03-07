using System.IO;
using Form8snCore.FileFormats;
using Form8snCore.HelpersAndConverters;
using Form8snCore.Rendering;

namespace Form8snCore
{
    /// <summary>
    /// Helper class to render a PDF directly from a project file and data
    /// </summary>
    public static class RenderPdf
    {
        /// <summary>
        /// Render a PDF into a writable stream
        /// </summary>
        /// <param name="fileSource">File provider, for loading backing PDFs, images, etc that may be referenced by the project file</param>
        /// <param name="data">The data to be rendered into the PDF</param>
        /// <param name="document">The document template project</param>
        /// <param name="target">Target stream to write a PDF into</param>
        /// <returns>Result information, with any error messages and timings</returns>
        public static RenderResultInfo ToStream(IFileSource fileSource, object data, TemplateProject document, Stream target)
        {
            var standardisedData = DataPickerBuilder.Standardise(data);
            return new RenderProject(fileSource).ToStream(target, standardisedData, document);
        }
    }
}