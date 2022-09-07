using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Form8snCore.FileFormats;
using SkinnyJson;

namespace WebFormFiller.ServiceStubs
{
    /// <summary>
    /// This class should be replaced with calls to your database or other storage solution.
    /// This stub doesn't even try to be efficient.
    /// </summary>
    public class FileDatabaseStub : IFileDatabaseStub
    {
        public const string StorageDirectory = @"C:\Temp\WebFormFiller";
        private static readonly HttpClient _client = new();

        
        /// <summary>
        /// Load file for rendering process
        /// </summary>
        public Stream Load(string? fileName)
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }
            if (string.IsNullOrWhiteSpace(fileName)) throw new Exception("File name is required");
            
            return File.OpenRead(Path.Combine(StorageDirectory, fileName));
        }

        /// <summary>
        /// Load the file at a given URL as a stream, or null if it can't be loaded.
        /// This is used to load embedded images into files.
        /// </summary>
        public Stream? LoadUrl(string? targetUrl)
        {
            try
            {
                using var src = Sync.Run(() => _client.GetStreamAsync(targetUrl));
                
                var ms = new MemoryStream();
                // ReSharper disable once AccessToDisposedClosure
                Sync.Run(()=>src.CopyToAsync(ms));
                
                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// List all the document template projects available
        /// </summary>
        public IDictionary<int, string> ListDocumentTemplates()
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }
            
            var files = Directory.EnumerateFiles(StorageDirectory, "*.json", SearchOption.TopDirectoryOnly);
            var names = files.Select(Path.GetFileNameWithoutExtension).ToList();
            
            var result = new Dictionary<int, string>();
            foreach (var name in names)
            {
                if (name is null) continue;
                var id = IdFromFileName(name, out var displayName);
                if (id > 0 && !result.ContainsKey(id)) result.Add(id, displayName);
            }
            return result;
        }

        /// <summary>
        /// Store a document template file.
        /// If id is provided, this acts as Update.
        /// If is is null, this is an Insert
        /// </summary>
        public int SaveDocumentTemplate(TemplateProject file, int? id)
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }
            
            if (id == null) id = GetNextId();
            else
            {
                var old = GetDocumentById(id.Value);
                if (old.Name != file.Name) File.Delete(Path.Combine(StorageDirectory, id+"_"+old.Name + ".json"));
            }

            File.WriteAllText(Path.Combine(StorageDirectory, id+"_"+file.Name + ".json"), Json.Freeze(file));
            return id.Value;
        }
        
        /// <summary>
        /// Store a file for later recovery using the [GET]Load(name) endpoint.
        /// In your implementation, you might want to supply files from a CDN, S3, or similar. 
        /// </summary>
        public void Store(string name, Stream stream)
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }

            using var file = File.OpenWrite(Path.Combine(StorageDirectory, name));
            // ReSharper disable once AccessToDisposedClosure
            Sync.Run(() => stream.CopyToAsync(file));
            file.Flush(true);
        }
        
        /// <summary>
        /// Read a document template file by ID
        /// </summary>
        public TemplateProject GetDocumentById(int docId)
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }
            
            var files = Directory.EnumerateFiles(StorageDirectory, docId+"_*.json", SearchOption.TopDirectoryOnly).ToList();
            if (files.Count > 1) throw new Exception("Ambiguous file");
            if (files.Count < 1) throw new Exception("File not found: docId="+docId);
            
            return Json.Defrost<TemplateProject>(File.ReadAllText(files[0]));
        }

        /// <summary>
        /// This method should return a complete example data set, in the same format as will be
        /// used to create final output documents from templates.
        /// It is used by the UI to provide sample data and guidance to the user, so values in the
        /// sample dataset should be as realistic as possible.
        /// Where items are repeated, enough examples should be given to trigger splits and repeats
        /// in normal documents.
        /// </summary>
        public object GetSampleData(int docId)
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }
            
            var files = Directory.EnumerateFiles(StorageDirectory, docId+"sm_*.json", SearchOption.TopDirectoryOnly).ToList();
            if (files.Count > 1) throw new Exception("Ambiguous file");
            if (files.Count < 1) return Json.Defrost(File.ReadAllText(@"C:\Temp\SampleData.json")); // If nothing found, fall back to a global default
            
            return Json.Defrost(File.ReadAllText(files[0]));
        }

        #region Support methods (your app doesn't need to supply these)
        private int IdFromFileName(string name, out string restOfName)
        {
            restOfName = "";
            if (string.IsNullOrEmpty(name)) return 0;
            var bits = name.Split('_');
            
            if (bits.Length < 2) return 0;
            if (!int.TryParse(bits[0], out var result)) return 0;
            
            restOfName = string.Join("_", bits.Skip(1));
            return result;
        }

        private int GetNextId()
        {
            var usedIds =ListDocumentTemplates().Keys.ToList();
            if (usedIds.Count < 1) return 1;
            return ListDocumentTemplates().Keys.Max() + 1; 
        }
        #endregion
    }
}