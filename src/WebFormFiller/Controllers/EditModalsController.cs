﻿using System;
using Form8snCore.FileFormats;
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
        /// <param name="target">The HTML input tag that should be populated when a data item is picked</param>
        /// <param name="multiplesCanBePicked">Default false: If true, multiple-value nodes will be selectable</param>
        [HttpGet]
        public IActionResult DataPicker([FromQuery] int docId, [FromQuery] int? pageIndex,
            [FromQuery] string? oldPath, [FromQuery] string target, [FromQuery] bool multiplesCanBePicked)
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

        /// <summary>
        /// View and edit the details of a single template box.
        /// This does not create new boxes -- that should be done with the BoxEditor view.
        /// </summary>
        [HttpGet]
        public IActionResult TemplateBox(int docId, [FromQuery] int pageIndex, [FromQuery] string boxKey)
        {
            var project = FileDatabaseStub.GetDocumentById(docId);

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
            var existing = FileDatabaseStub.GetDocumentById(model.DocumentId);
            if (existing.Version is not null && model.Version < existing.Version) return Content("OLD")!;

            // Copy new values across
            model.CopyTo(existing);

            // Write back to store
            FileDatabaseStub.SaveDocumentTemplate(existing, model.DocumentId);
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
            var project = FileDatabaseStub.GetDocumentById(docId);
            
            var filterSet = project.PickFilterSet(pageIndex);
            if (filterSet is null) return BadRequest()!;
            if (!filterSet.ContainsKey(filterKey)) return BadRequest()!;
            
            var theFilter = filterSet[filterKey];
            
            var model = FilterEditViewModel.From(project, pageIndex, docId, filterKey, theFilter);
            
            return View(model)!;
        }

        [HttpPost]
        public IActionResult FilterEditor([FromForm]FilterEditViewModel model)
        {
            var existing = FileDatabaseStub.GetDocumentById(model.DocumentId);
            
            var filterSet = existing.PickFilterSet(model.PageIndex);
            if (filterSet is null) return BadRequest()!;
            if (model.FilterKey is null) return BadRequest()!;
            if (!filterSet.ContainsKey(model.FilterKey)) return BadRequest()!;
            
            // Copy new values across
            model.CopyTo(existing);

            // Write back to store
            FileDatabaseStub.SaveDocumentTemplate(existing, model.DocumentId);
            return Content("OK")!;
        }

        /// <summary>
        /// View and edit details that cover the whole document, including filters
        /// </summary>
        [HttpGet]
        public IActionResult EditDocumentDetails(int docId)
        {
            var project = FileDatabaseStub.GetDocumentById(docId);
            
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
            var existing = FileDatabaseStub.GetDocumentById(model.DocumentId);
            if (existing.Version is not null && model.Version < existing.Version) return Content("OLD")!;
            
            // Copy new values across
            model.CopyTo(existing);
            
            // Write back to store
            FileDatabaseStub.SaveDocumentTemplate(existing, model.DocumentId);
            return Content("OK")!;
        }

        /// <summary>
        /// View and edit details of a document template page, including filters and repeaters
        /// </summary>
        [HttpGet]
        public IActionResult EditPageDetails(int docId, int pageIndex)
        {
            var project = FileDatabaseStub.GetDocumentById(docId);
            
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
            var existing = FileDatabaseStub.GetDocumentById(model.DocumentId);
            if (existing.Version is not null && model.Version < existing.Version) return Content("OLD")!;
            
            // Copy new values across
            model.CopyTo(existing);
            
            // Write back to store
            FileDatabaseStub.SaveDocumentTemplate(existing, model.DocumentId);
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
            var project = FileDatabaseStub.GetDocumentById(docId);

            var filterSet = project.PickFilterSet(pageIdx);
            if (filterSet is null) return BadRequest()!;

            for (int i = 1; i < 150; i++)
            {
                var name = $"New filter {i}";
                if (!filterSet.ContainsKey(name))
                {
                    filterSet.Add(name, new MappingInfo());
                    FileDatabaseStub.SaveDocumentTemplate(project, docId);
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
            var project = FileDatabaseStub.GetDocumentById(docId);
            
            var filterSet = project.PickFilterSet(pageIdx);
            if (filterSet is null) return BadRequest()!;
            
            if (filterSet.ContainsKey(name)) filterSet.Remove(name);
            
            FileDatabaseStub.SaveDocumentTemplate(project, docId);
            return Ok()!;
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