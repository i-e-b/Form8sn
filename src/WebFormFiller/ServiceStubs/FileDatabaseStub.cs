using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Form8snCore.FileFormats;
using SkinnyJson;

namespace WebFormFiller.ServiceStubs
{
    /// <summary>
    /// This class should be replaced with calls to your database or other storage solution.
    /// This stub doesn't even try to be efficient.
    /// </summary>
    public class FileDatabaseStub
    {
        public const string StorageDirectory = @"C:\Temp\WebFormFiller";

        /// <summary>
        /// List all the document template projects available
        /// </summary>
        public static IDictionary<int, string> ListDocumentTemplates()
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
        public static int SaveDocumentTemplate(IndexFile file, int? id)
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }
            
            if (id == null) id = GetNextId();
            
            File.WriteAllText(Path.Combine(StorageDirectory, id+"_"+file.Name + ".json"), Json.Freeze(file));
            return id.Value;
        }
        
        /// <summary>
        /// Read a document template file by ID
        /// </summary>
        public static IndexFile GetDocumentById(int docId)
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }
            
            var files = Directory.EnumerateFiles(StorageDirectory, docId+"_*.json", SearchOption.TopDirectoryOnly).ToList();
            if (files.Count > 1) throw new Exception("Ambiguous file");
            if (files.Count < 1) throw new Exception("File not found");
            
            return Json.Defrost<IndexFile>(File.ReadAllText(files[0]));
        }

        /// <summary>
        /// This method should return a complete example data set, in the same format as will be
        /// used to create final output documents from templates.
        /// It is used by the UI to provide sample data and guidance to the user, so values in the
        /// sample dataset should be as realistic as possible.
        /// Where items are repeated, enough examples should be given to trigger splits and repeats
        /// in normal documents.
        /// </summary>
        public static object GetSampleData()
        {
            return Json.Defrost(File.ReadAllText(@"C:\Temp\SampleData.json")); 
        }

        #region Support methods (your app doesn't need to supply these)
        private static int IdFromFileName(string name, out string restOfName)
        {
            restOfName = "";
            if (string.IsNullOrEmpty(name)) return 0;
            var bits = name.Split('_');
            
            if (bits.Length < 2) return 0;
            if (!int.TryParse(bits[0], out var result)) return 0;
            
            restOfName = string.Join("_", bits.Skip(1));
            return result;
        }

        private static int GetNextId()
        {
            var usedIds =ListDocumentTemplates().Keys.ToList();
            if (usedIds.Count < 1) return 1;
            return ListDocumentTemplates().Keys.Max() + 1; 
        }
        #endregion
    }
}