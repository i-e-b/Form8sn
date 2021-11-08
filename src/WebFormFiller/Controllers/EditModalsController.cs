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
        /// <param name="documentTemplateId">Document ID, used to read the filters in the document template</param>
        /// <param name="pageIndex">Optional: if supplied, the page-specific filters will be available</param>
        /// <param name="oldPath">Optional: if supplied, this path will be highlighted in the UI as the 'current selection'</param>
        [HttpGet]
        public IActionResult DataPicker(int documentTemplateId, int? pageIndex, string? oldPath)
        {
            var sampleData = FileDatabaseStub.GetSampleData();
            var project = FileDatabaseStub.GetDocumentById(documentTemplateId);

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
        public IActionResult TemplateBox(int documentTemplateId, int pageIndex, string boxKey)
        {
            var project = FileDatabaseStub.GetDocumentById(documentTemplateId);
            
            var model = TemplateBoxModalViewModel.From(project, documentTemplateId, pageIndex, boxKey);
            model.LoadDataPickerUrl = Url!.Action("DataPicker", "EditModals", new{documentTemplateId, pageIndex, oldPath = model.DataPath})!;
            
            return PartialView("EditTemplateBox", model)!;
        }

        /// <summary>
        /// Store the details of a single template box, and re-display the edit controls.
        /// </summary>
        [HttpPost]
        public IActionResult TemplateBox([FromForm]TemplateBoxModalViewModel model)
        {
            // TODO: save changes against the document, then re-load (same as the GET method)
            Console.WriteLine($"Got changes for {model.BoxKey} -> {model.BoxName}");
            return PartialView("EditTemplateBox", model)!;
        }
    }

}