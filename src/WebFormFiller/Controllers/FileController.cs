using System;
using System.IO;
using Form8snCore;
using Microsoft.AspNetCore.Mvc;
using WebFormFiller.ServiceStubs;

namespace WebFormFiller.Controllers
{
    /// <summary>
    /// Provides access to files for the boxEditor.js code
    /// </summary>
    public class FileController : Controller
    {
        private readonly IFileDatabaseStub _fileDatabase;

        public FileController()
        {
            _fileDatabase = new FileDatabaseStub(); // replace with your own implementation
        }
        
        /// <summary>
        /// Reads a file out of storage. In your implementation, you might want to
        /// supply files from a CDN, S3, or similar. 
        /// </summary>
        [HttpGet]
        public IActionResult Load(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("File name is required")!;
            var stream = _fileDatabase.Load(name);
            
            return File(stream, GuessContentType(name))!;
        }

        private static string GuessContentType(string name)
        {
            var extension = Path.GetExtension(name).ToLowerInvariant();
            switch (extension)
            {
                case ".png":
                    return "image/png";
                
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                
                case ".pdf":
                    return "application/pdf";
                
                case ".txt":
                    return "text/plain";
                
                case ".json":
                    return "application/json";
                
                default:
                    Console.WriteLine($"Unknown '{extension}'");
                    return "application/octet-stream";
            }

        }

        /// <summary>
        /// Load the given document template from storage. Generate and return a sample PDF.
        /// </summary>
        [HttpGet]
        public IActionResult GenerateSamplePdf(int docId)
        {
            var document = _fileDatabase.GetDocumentById(docId);
            var sampleData = _fileDatabase.GetSampleData(docId);
            var ms = new MemoryStream();

            var info = RenderPdf.ToStream(_fileDatabase, sampleData, document, ms);
            Console.WriteLine($"Render success: {info.Success}; Overall time: {info.OverallTime}; Time loading artefacts: {info.LoadingTime}; Time in custom renderers: {info.CustomRenderTime}; Message: {(info.ErrorMessage ?? "<none>")}.");
            return File(ms, "application/pdf")!;
        }
    }
}