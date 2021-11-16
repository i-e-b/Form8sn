using System.Collections;
using System.Collections.Generic;
using System.IO;
using Form8snCore.FileFormats;
using Form8snCore.Rendering;
using SkinnyJson;

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
            var standardisedData = Standardise(data);
            return new RenderProject(fileSource).ToStream(target, standardisedData, document);
        }

        /// <summary>
        /// Make sure the incoming data is a hierarchy of dictionaries and arrays
        /// </summary>
        private static object Standardise(object data)
        {
            if (data is Dictionary<string, object>) return data;
            if (data is ArrayList) return data;
            return Json.Defrost(Json.Freeze(data));
        }
    }
}