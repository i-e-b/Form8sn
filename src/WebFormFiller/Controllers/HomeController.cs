using System;
using System.IO;
using System.Threading.Tasks;
using Form8snCore.FileFormats;
using Form8snCore.HelpersAndConverters;
using Microsoft.AspNetCore.Mvc;
using WebFormFiller.Models;
using WebFormFiller.ServiceStubs;

namespace WebFormFiller.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(new TemplateListViewModel{
                Templates = FileDatabaseStub.ListDocumentTemplates()
            })!;
        }

        /// <summary>
        /// Shows a page to supply a PDF and title to start a new document template
        /// </summary>
        [HttpGet]
        public IActionResult StartNewTemplate()
        {
            return View(new NewTemplateViewModel())!;
        }

        /// <summary>
        /// Creates a new document template from the result of [GET]StartNewTemplate()
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> StartNewTemplate([FromForm]NewTemplateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Title)) { model.Title = "Untitled"; }

            if (model.Upload == null) { throw new Exception("No file!"); }

            await using var ms = new MemoryStream();
            await using (var stream = model.Upload.OpenReadStream()) { await stream.CopyToAsync(ms); }
            
            ms.Seek(0, SeekOrigin.Begin);
            var fileName = model.Title + "_" + Guid.NewGuid() + ".pdf";
            FileDatabaseStub.Store(fileName, ms);
            ms.Seek(0, SeekOrigin.Begin);
            
            var template = ImportToProject.FromPdf(ms, fileName, model.Title);
            var id = FileDatabaseStub.SaveDocumentTemplate(template, null);


            return RedirectToAction(nameof(BoxEditor), new{docId = id})!;
        }

        /// <summary>
        /// Loads the document template visual editor for an existing document template.
        /// Use [GET]StartNewTemplate() to create the initial document template.
        /// This page calls [GET]ReadProject(docId) and [POST]StoreProject(project,docId) via AJAX calls to update state.
        /// </summary>
        [HttpGet]
        public IActionResult BoxEditor([FromQuery]int docId)
        {
            var document = FileDatabaseStub.GetDocumentById(docId);
            
            var model = new TemplateEditViewModel{
                Document = document,
                DocumentId = docId.ToString(),
                BasePdfFile = document.BasePdfFile!,
                
                FileLoadUrl = Url!.Action("Load","File")!,
                ProjectLoadUrl = Url!.Action("ReadProject", "Home")!,
                ProjectStoreUrl = Url!.Action("WriteProject","Home")!,
                BoxMoveUrl = Url!.Action("MoveBox", "Home")!,
                BoxEditPartialUrl = Url!.Action("TemplateBox","EditModals")!,
                DisplayFormatPartialUrl = Url!.Action("DisplayFormat","EditModals")!,
                DocumentInfoPartialUrl = Url!.Action("EditDocumentDetails", "EditModals")!
            };
            
            return View(model)!;
        }

        /// <summary>
        /// Returns JSON of the given document's project file
        /// </summary>
        [HttpGet]
        public IActionResult ReadProject([FromQuery]int docId)
        {
            var document = FileDatabaseStub.GetDocumentById(docId);
            return Content(SkinnyJson.Json.Freeze(document), "application/json")!;
        }

        /// <summary>
        /// Store a new version of a document template's project file.
        /// Changes are rejected if the incoming file is based off an older version than stored.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> WriteProject([FromQuery]int docId)
        {
            // MVC [FromBody] is a bit fussy, and tends to return 'null'
            // So we read directly.
            if (Request?.Body is null) return BadRequest("Invalid Document")!;
            var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            
            ms.Seek(0, SeekOrigin.Begin);
            var document = SkinnyJson.Json.Defrost<IndexFile>(ms);
            
            var original = FileDatabaseStub.GetDocumentById(docId);
            if (document.Version < (original.Version ?? 0)) return BadRequest("Out of Order")!;
            
            FileDatabaseStub.SaveDocumentTemplate(document, docId);
            return Content("OK")!;
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
            
            var document = FileDatabaseStub.GetDocumentById(docId);
            if (document.Version is not null && document.Version > docVersion) return BadRequest("Document Version")!;
            if (pageIndex < 0 || pageIndex >= document.Pages.Count) return BadRequest("Page Index")!;
            
            var thePage = document.Pages[pageIndex];
            if (!thePage.Boxes.ContainsKey(boxKey)) return BadRequest("Box Key")!;
            
            var theBox = thePage.Boxes[boxKey];
            theBox.Width = width;
            theBox.Height = height;
            theBox.Left = left;
            theBox.Top = top;
            
            FileDatabaseStub.SaveDocumentTemplate(document, docId);
            return Content("OK")!;
        }

        /// <summary>
        /// Store a project file against a document template ID
        /// </summary>
        /// <param name="project">Details of the document template</param>
        /// <param name="docId">The document template ID</param>
        [HttpPost]
        public IActionResult StoreProject([FromBody]IndexFile project, [FromQuery]int? docId)
        {
            var newId = FileDatabaseStub.SaveDocumentTemplate(project, docId);
            return Json(new{id = newId})!;
        }
    }
}