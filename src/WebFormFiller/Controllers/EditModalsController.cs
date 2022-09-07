using System;
using System.Collections.Generic;
using System.IO;
using Form8snCore.DataExtraction;
using Form8snCore.FileFormats;
using Form8snCore.HelpersAndConverters;
using Microsoft.AspNetCore.Mvc;
using WebFormFiller.Models;
using WebFormFiller.ServiceStubs;

namespace WebFormFiller.Controllers
{
    /// <summary>
    /// Controller for handling async data populating edit modals, and storing the
    /// data returned from modals, and storing the results of interactions on the
    /// box layout page.
    /// </summary>
    public class EditModalsController : Controller
    {
        private readonly IFileDatabaseStub _fileDatabase;

        public EditModalsController()
        {
            _fileDatabase = new FileDatabaseStub(); // replace with your own implementation
        }
        
        /// <summary>
        /// View and select the data available from the sample data set for a specific document template file.
        /// If a page index is given, page-specific options may be provided.
        /// </summary>
        /// <param name="docId">Document ID, used to read the filters in the document template</param>
        /// <param name="pageIndex">Optional: if supplied, the page-specific filters will be available</param>
        /// <param name="oldPath">Optional: if supplied, this path will be highlighted in the UI as the 'current selection'</param>
        /// <param name="target">The HTML input tag that should be populated when a data item is picked</param>
        /// <param name="multiplesCanBePicked">Default false: If true, multiple-value nodes will be selectable</param>
        [HttpGet]
        public IActionResult DataPicker([FromQuery] int docId, [FromQuery] int? pageIndex,
            [FromQuery] string? oldPath, [FromQuery] string target, [FromQuery] bool multiplesCanBePicked)
        {
            try
            {
                var sampleData = _fileDatabase.GetSampleData(docId);
                var project = _fileDatabase.GetDocumentById(docId);

                string[]? repeat = null;
                if (pageIndex is not null)
                {
                    var page = project.Pages[pageIndex.Value];
                    if (page.RepeatMode.Repeats) repeat = page.RepeatMode.DataPath;
                }

                var prev = Array.Empty<string>();
                if (!string.IsNullOrWhiteSpace(oldPath))
                {
                    prev = oldPath.Split('.');
                }

                var tree = JsonDataReader.BuildDataSourcePicker(project, sampleData, prev, repeat, pageIndex, multiplesCanBePicked);
                var list = JsonDataReader.FlattenTree(tree);

                var model = new DataSourceViewModel
                {
                    Nodes = list, Target = target
                };

                return PartialView("DataPathPicker", model)!;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return PartialView("DataPathPicker", new DataSourceViewModel( warnings : "Could not load: "+ex.Message))!;
            }
        }

        /// <summary>
        /// Return sample data to display in the box editor screen.
        /// This can return empty if there is no suitable sample data.
        /// This can return synthetic data if that makes sense.
        /// </summary>
        [HttpGet]
        public IActionResult BoxSampleData([FromQuery] int docId, [FromQuery] int? docVersion,
            [FromQuery] int? pageIndex, [FromQuery] string? boxKey)
        {
            try
            {
                if (pageIndex is null || boxKey is null) return Content(" ")!;
                var sampleData = _fileDatabase.GetSampleData(docId);
                var project = _fileDatabase.GetDocumentById(docId);
                
                var page = project.Pages[pageIndex.Value];
                var box = page.Boxes[boxKey];
                
                var subject = new DataMapper(project, sampleData);
            
                var result = subject.TryFindBoxData(box, 0, new Dictionary<string, decimal>());
                
                return Content(result ?? " ")!;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Content(" ")!; // not critical, so we can return empty
            }
        }

        /// <summary>
        /// Update the size and location of a single template page box.
        /// This does nothing if the docVersion provided is less than the one stored.
        /// </summary>
        [HttpGet]
        public IActionResult MoveBox([FromQuery] int docId, [FromQuery] int docVersion, [FromQuery] int pageIndex, [FromQuery] string boxKey,
            [FromQuery] double left, [FromQuery] double top, [FromQuery] double width, [FromQuery] double height)
        {
            if (width <= 0 ||height <= 0)return BadRequest("Box Dimensions")!;
            
            var document = _fileDatabase.GetDocumentById(docId);
            if (document.Version is not null && document.Version > docVersion) return BadRequest("Document Version")!;
            if (pageIndex < 0 || pageIndex >= document.Pages.Count) return BadRequest("Page Index")!;
            
            var thePage = document.Pages[pageIndex];
            if (!thePage.Boxes.ContainsKey(boxKey)) return BadRequest("Box Key")!;
            
            var theBox = thePage.Boxes[boxKey];
            theBox.Width = width;
            theBox.Height = height;
            theBox.Left = left;
            theBox.Top = top;
            
            _fileDatabase.SaveDocumentTemplate(document, docId);
            return Content("OK")!;
        }

        /// <summary>
        /// View and edit the details of a single template box.
        /// This does not create new boxes -- that should be done with the BoxEditor view.
        /// </summary>
        [HttpGet]
        public IActionResult TemplateBox(int docId, [FromQuery] int pageIndex, [FromQuery] string boxKey)
        {
            var project = _fileDatabase.GetDocumentById(docId);

            var model = TemplateBoxModalViewModel.From(project, docId, pageIndex, boxKey);

            return PartialView("EditTemplateBox", model)!;
        }

        /// <summary>
        /// Store the details of a single template box, and re-display the edit controls.
        /// </summary>
        [HttpPost]
        public IActionResult TemplateBox([FromForm] TemplateBoxModalViewModel model)
        {
            // Check against existing version
            var existing = _fileDatabase.GetDocumentById(model.DocumentId);
            if (existing.Version is not null && model.Version < existing.Version) return Content("OLD")!;

            // Copy new values across
            model.CopyTo(existing);

            // Write back to store
            _fileDatabase.SaveDocumentTemplate(existing, model.DocumentId);
            return Content("OK")!;
        }

        /// <summary>
        /// View and edit the details of a data filter
        /// </summary>
        /// <param name="docId">Document ID</param>
        /// <param name="pageIndex">Optional: page index. If null, document filters will be used</param>
        /// <param name="filterKey">Name of the filter</param>
        [HttpGet]
        public IActionResult FilterEditor(int docId, [FromQuery] int? pageIndex, [FromQuery] string filterKey)
        {
            var project = _fileDatabase.GetDocumentById(docId);
            
            var filterSet = project.PickFilterSet(pageIndex);
            if (filterSet is null) return BadRequest()!;
            if (!filterSet.ContainsKey(filterKey)) return BadRequest()!;
            
            var theFilter = filterSet[filterKey];
            
            var model = FilterEditViewModel.From(project, pageIndex, docId, filterKey, theFilter);
            
            return View(model)!;
        }

        /// <summary>
        /// Save changes to a data filter
        /// </summary>
        [HttpPost]
        public IActionResult FilterEditor([FromForm]FilterEditViewModel model)
        {
            var existing = _fileDatabase.GetDocumentById(model.DocumentId);
            
            var filterSet = existing.PickFilterSet(model.PageIndex);
            if (filterSet is null) return BadRequest()!;
            if (model.FilterKey is null) return BadRequest()!;
            if (!filterSet.ContainsKey(model.FilterKey)) return BadRequest()!;
            
            // Copy new values across
            model.CopyTo(existing);

            // Write back to store
            _fileDatabase.SaveDocumentTemplate(existing, model.DocumentId);
            return Content("OK")!;
        }

        /// <summary>
        /// View and edit details that cover the whole document, including filters
        /// </summary>
        [HttpGet]
        public IActionResult EditDocumentDetails(int docId)
        {
            var project = _fileDatabase.GetDocumentById(docId);
            
            var model = DocumentSettingsViewModel.From(project, docId);
            
            return PartialView("EditDocumentDetails", model)!;
        }

        /// <summary>
        /// Store changes to document settings
        /// </summary>
        [HttpPost]
        public IActionResult EditDocumentDetails([FromForm]DocumentSettingsViewModel model)
        {
            // Check against existing version
            var existing = _fileDatabase.GetDocumentById(model.DocumentId);
            if (existing.Version is not null && model.Version < existing.Version) return Content("OLD")!;
            
            // Copy new values across
            model.CopyTo(existing);
            
            // If we have a new sample file, store it
            if (!string.IsNullOrEmpty(model.FileUpload))
            {
                var originalName = Path.GetFileNameWithoutExtension(model.FileUploadName ?? "sample").Safe();
                var storeName = $"{model.DocumentId}sm_{originalName}.json";
                
                _fileDatabase.Store(storeName, model.FileUpload.ToStream());
                existing.SampleFileName = storeName;
            }
            
            // Write back to store
            _fileDatabase.SaveDocumentTemplate(existing, model.DocumentId);
            
            return Content("OK")!;
        }

        /// <summary>
        /// View and edit details of a document template page, including filters and repeaters
        /// </summary>
        [HttpGet]
        public IActionResult EditPageDetails(int docId, int pageIndex)
        {
            var project = _fileDatabase.GetDocumentById(docId);
            
            var model = PageSettingsViewModel.From(project, docId, pageIndex);
            
            return PartialView("EditPageDetails", model)!;
        }
        
        /// <summary>
        /// Store changes to page settings
        /// </summary>
        [HttpPost]
        public IActionResult EditPageDetails([FromForm]PageSettingsViewModel model)
        {
            // Check against existing version
            var existing = _fileDatabase.GetDocumentById(model.DocumentId);
            if (existing.Version is not null && model.Version < existing.Version) return Content("OLD")!;
            
            // Copy new values across
            model.CopyTo(existing);
            
            // Write back to store
            _fileDatabase.SaveDocumentTemplate(existing, model.DocumentId);
            return Content("OK")!;
        }

        /// <summary>
        /// Add a new filter to page or document
        /// </summary>
        /// <param name="docId"></param>
        /// <param name="pageIdx">Optional: if given, the filter will be page specific. Otherwise it will be a document filter</param>
        [HttpGet]
        public IActionResult AddFilter(int docId, int? pageIdx)
        {
            var project = _fileDatabase.GetDocumentById(docId);

            var filterSet = project.PickFilterSet(pageIdx);
            if (filterSet is null) return BadRequest()!;

            for (int i = 1; i < 150; i++)
            {
                var name = $"New filter {i}";
                if (!filterSet.ContainsKey(name))
                {
                    filterSet.Add(name, new MappingInfo());
                    _fileDatabase.SaveDocumentTemplate(project, docId);
                    return Ok(name)!;
                }
            }
            return BadRequest("Too many unnamed filters")!;
        }

        /// <summary>
        /// Remove a single filter by name from the document or page
        /// </summary>
        [HttpGet]
        public IActionResult DeleteFilter(int docId, string? name, int? pageIdx)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest()!;
            var project = _fileDatabase.GetDocumentById(docId);
            
            var filterSet = project.PickFilterSet(pageIdx);
            if (filterSet is null) return BadRequest()!;
            
            if (filterSet.ContainsKey(name)) filterSet.Remove(name);
            
            _fileDatabase.SaveDocumentTemplate(project, docId);
            return Ok()!;
        }

        /// <summary>
        /// View and edit the display format of a template box.
        /// </summary>
        [HttpGet]
        public IActionResult DisplayFormat(int docId, [FromQuery]int pageIndex, [FromQuery]string boxKey)
        {
            var project = _fileDatabase.GetDocumentById(docId);
            
            var model = DisplayFormatViewModel.From(project, docId, pageIndex, boxKey);
            
            return PartialView("DisplayFormatEditor", model)!;
        }
    }

}