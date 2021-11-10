using System;
using Form8snCore.HelpersAndConverters;
using Microsoft.AspNetCore.Mvc;
using WebFormFiller.Models;
using WebFormFiller.ServiceStubs;

namespace WebFormFiller.Controllers
{
    public class EditModalsController : Controller
    {
        /// <summary>
        /// View and select the data available from the sample data set for a specific document template file.
        /// If a page index is given, page-specific options may be provided.
        /// </summary>
        /// <param name="docId">Document ID, used to read the filters in the document template</param>
        /// <param name="pageIndex">Optional: if supplied, the page-specific filters will be available</param>
        /// <param name="oldPath">Optional: if supplied, this path will be highlighted in the UI as the 'current selection'</param>
        [HttpGet]
        public IActionResult DataPicker(int docId, [FromQuery]int? pageIndex, [FromQuery]string? oldPath)
        {
            var sampleData = FileDatabaseStub.GetSampleData();
            var project = FileDatabaseStub.GetDocumentById(docId);

            string[]? repeat = null;
            if (pageIndex is not null)
            {
                var page = project.Pages[pageIndex.Value];
                if (page.RepeatMode.Repeats) repeat = page.RepeatMode.DataPath;
            }

            var prev = Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(oldPath)) { prev = oldPath.Split('.'); }

            var tree = JsonDataReader.BuildDataSourcePicker(project, sampleData, prev, repeat, 4);
            var list = JsonDataReader.FlattenTree(tree);
            
            var model = new DataSourceViewModel{
                Nodes = list
            };
            
             return PartialView("DataPathPicker", model)!;
        }

        /// <summary>
        /// View and edit the details of a single template box.
        /// This does not create new boxes -- that should be done with the BoxEditor view.
        /// </summary>
        [HttpGet]
        public IActionResult TemplateBox(int docId, [FromQuery]int pageIndex, [FromQuery]string boxKey)
        {
            var project = FileDatabaseStub.GetDocumentById(docId);
            
            var model = TemplateBoxModalViewModel.From(project, docId, pageIndex, boxKey);
            model.LoadDataPickerUrl = Url!.Action("DataPicker", "EditModals", new{ docId = docId, pageIndex, oldPath = model.DataPath})!;
            
            return PartialView("EditTemplateBox", model)!;
        }

        /// <summary>
        /// Store the details of a single template box, and re-display the edit controls.
        /// </summary>
        [HttpPost]
        public IActionResult TemplateBox([FromForm]TemplateBoxModalViewModel model)
        {
            // Check against existing version
            var existing = FileDatabaseStub.GetDocumentById(model.DocumentId);
            if (existing.Version is not null && model.Version < existing.Version) return Content("OLD")!;
            
            // Copy new values across
            model.CopyTo(existing);
            
            // Write back to store
            FileDatabaseStub.SaveDocumentTemplate(existing, model.DocumentId);
            return Content("OK")!;
        }

        
        /// <summary>
        /// View and edit the display format of a template box.
        /// </summary>
        [HttpGet]
        public IActionResult DisplayFormat(int docId, [FromQuery]int pageIndex, [FromQuery]string boxKey)
        {
            var project = FileDatabaseStub.GetDocumentById(docId);
            
            var model = DisplayFormatViewModel.From(project, docId, pageIndex, boxKey);
            
            return PartialView("DisplayFormatEditor", model)!;
        }
    }

}