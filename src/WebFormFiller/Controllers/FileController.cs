﻿using System.IO;
using System.Threading.Tasks;
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
        public IActionResult Load(string? name)
        {
            if (!Directory.Exists(FileDatabaseStub.StorageDirectory)) { Directory.CreateDirectory(FileDatabaseStub.StorageDirectory); }
            
            var bytes = System.IO.File.ReadAllBytes(Path.Combine(FileDatabaseStub.StorageDirectory, name + ".pdf"));
            return File(bytes, "application/pdf")!;
        }

        public static void Store(string name, Stream stream)
        {
            if (!Directory.Exists(FileDatabaseStub.StorageDirectory)) { Directory.CreateDirectory(FileDatabaseStub.StorageDirectory); }

            using var file = System.IO.File.OpenWrite(Path.Combine(FileDatabaseStub.StorageDirectory, name + ".pdf"));
            // ReSharper disable once AccessToDisposedClosure
            Sync.Run(() => stream.CopyToAsync(file));
            file.Flush(true);
        }
    }
}