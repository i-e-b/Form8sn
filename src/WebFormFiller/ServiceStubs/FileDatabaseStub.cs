using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Form8snCore.FileFormats;
using SkinnyJson;

namespace WebFormFiller.ServiceStubs
{
    /// <summary>
    /// This class should be replaced with your database or other storage solution.
    /// This stub doesn't even try to be efficient.
    /// </summary>
    public class FileDatabaseStub
    {
        public const string StorageDirectory = @"C:\Temp\WebFormFiller";

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

        public static int SaveDocumentTemplate(IndexFile file, int? id)
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }
            
            if (id == null) id = GetNextId();
            
            File.WriteAllText(Path.Combine(StorageDirectory, id+"_"+file.Name + ".json"), Json.Freeze(file));
            return id.Value;
        }
        
        public static IndexFile GetDocumentById(int docId)
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }
            
            var files = Directory.EnumerateFiles(StorageDirectory, docId+"_*.json", SearchOption.TopDirectoryOnly).ToList();
            if (files.Count > 1) throw new Exception("Ambiguous file");
            if (files.Count < 1) throw new Exception("File not found");
            
            return Json.Defrost<IndexFile>(File.ReadAllText(files[0]));
        }

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

    }
}