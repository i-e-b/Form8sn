﻿using System.Collections.Generic;
using System.IO;
using Form8snCore;
using Form8snCore.FileFormats;

namespace WebFormFiller.ServiceStubs
{
    public interface IFileDatabaseStub : IFileSource
    {
        /// <summary>
        /// List all the document template projects available
        /// </summary>
        IDictionary<int, string> ListDocumentTemplates();

        /// <summary>
        /// Store a document template file.
        /// If id is provided, this acts as Update.
        /// If is is null, this is an Insert
        /// </summary>
        int SaveDocumentTemplate(TemplateProject file, int? id);

        /// <summary>
        /// Store a file for later recovery using the [GET]Load(name) endpoint.
        /// In your implementation, you might want to supply files from a CDN, S3, or similar. 
        /// </summary>
        void Store(string name, Stream stream);

        /// <summary>
        /// <p>OPTIONAL</p>
        /// Delete a file previously saved with <see cref="Store"/>.
        /// <p></p>
        /// If deleting files is not supported, take no action.
        /// </summary>
        void DeleteStoredFile(string? fileName);

        /// <summary>
        /// Read a document template file by ID
        /// </summary>
        TemplateProject GetDocumentById(int docId);

        /// <summary>
        /// This method should return a complete example data set, in the same format as will be
        /// used to create final output documents from templates.
        /// It is used by the UI to provide sample data and guidance to the user, so values in the
        /// sample dataset should be as realistic as possible.
        /// Where items are repeated, enough examples should be given to trigger splits and repeats
        /// in normal documents.
        /// </summary>
        object GetSampleData(int docId);
        
        /// <summary>
        /// Return a list of stored files that match the sample file pattern for this document
        /// </summary>
        IList<string> GetSampleFiles(int docId);

        /// <summary>
        /// <p>OPTIONAL</p>
        /// List images that are available to use in template boxes via the data picker.
        /// The names should work when passed to <see cref="IFileSource.Load"/>
        /// <p></p>
        /// If images stamps are not supported, return an empty list.
        /// </summary>
        IList<string> ListImageStamps();

    }
}