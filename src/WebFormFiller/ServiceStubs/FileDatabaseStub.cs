using System.Collections.Generic;
using System.IO;
using System.Linq;
using Form8snCore.FileFormats;
using SkinnyJson;

namespace WebFormFiller.ServiceStubs
{
    /// <summary>
    ///  This class should be replaced with your database or other storage solution.
    /// This stub doesn't even try to be efficient.
    /// </summary>
    public class FileDatabaseStub
    {
        public const string StorageDirectory = @"C:\Temp\WebFormFiller";

        public static List<string> ListDocumentTemplates()
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }
            
            var files = Directory.EnumerateFiles(StorageDirectory, "*.json", SearchOption.TopDirectoryOnly);
            return files.Select(Path.GetFileNameWithoutExtension).ToList()!;
        }

        public static int SaveDocumentTemplate(IndexFile file, int? id)
        {
            if (!Directory.Exists(StorageDirectory)) { Directory.CreateDirectory(StorageDirectory); }
            
            if (id == null) id = GetNextId();
            
            File.WriteAllText(Path.Combine(StorageDirectory, file.Name + ".json"), Json.Freeze(file)!);
            return id.Value;
        }

        private static int IdFromFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            var bits = name.Split('_');
            
            if (bits.Length < 2) return 0;
            if (!int.TryParse(bits[0], out var result)) return 0;
            
            return result;
        }

        private static int GetNextId()
        {
           return ListDocumentTemplates().Select(IdFromFileName).Max() + 1; 
        }
    }
}