using System;
using System.IO;
using Form8snCore;
using Form8snCore.Rendering;
using Microsoft.AspNetCore.Mvc;
using WebFormFiller.ServiceStubs;

namespace WebFormFiller.Controllers
{
    public class FileController : Controller
    {
        /// <summary>
        /// Reads a file out of storage. In your implementation, you might want to
        /// supply files from a CDN, S3, or similar. 
        /// </summary>
        [HttpGet]
        public IActionResult Load(string? name)
        {
            if (!Directory.Exists(FileDatabaseStub.StorageDirectory))
            {
                Directory.CreateDirectory(FileDatabaseStub.StorageDirectory);
            }

            if (string.IsNullOrWhiteSpace(name)) throw new Exception("File name is required");

            var bytes = System.IO.File.ReadAllBytes(Path.Combine(FileDatabaseStub.StorageDirectory, name));
            return File(bytes, "application/pdf")!;
        }

        /// <summary>
        /// Load the given document template from storage. Generate and return a sample PDF.
        /// </summary>
        [HttpGet]
        public IActionResult GenerateSamplePdf(int docId)
        {
            var document = FileDatabaseStub.GetDocumentById(docId);
            var sampleData = FileDatabaseStub.GetSampleData();
            var ms = new MemoryStream();
            var fileSource = new FileDatabaseStub();

            var info = RenderPdf.ToStream(fileSource, sampleData, document, ms);
            Console.WriteLine($"Render success: {info.Success}; Overall time: {info.OverallTime}; Time loading artefacts: {info.LoadingTime}; Message: {(info.ErrorMessage ?? "<none>")}.");
            return File(ms, "application/pdf")!;
        }
    }
}