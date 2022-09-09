using System;
using System.IO;
using System.Threading.Tasks;
using Form8snCore.FileFormats;
using Form8snCore.HelpersAndConverters;
using Microsoft.AspNetCore.Mvc;
using Portable.Drawing.Imaging;
using Portable.Drawing.Imaging.ImageFormats;
using WebFormFiller.Models;
using WebFormFiller.ServiceStubs;

namespace WebFormFiller.Controllers
{
    public class TemplateController : Controller
    {
        private readonly IFileDatabaseStub _fileDatabase;

        public TemplateController()
        {
            _fileDatabase = new FileDatabaseStub(); // replace with your own implementation
        }
        
        /// <summary>
        /// List existing document template projects with edit links.
        /// Show a link to create a new project
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View(new TemplateListViewModel{
                Templates = _fileDatabase.ListDocumentTemplates(),
                ImageStamps = _fileDatabase.ListImageStamps()
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
            _fileDatabase.Store(fileName, ms);
            ms.Seek(0, SeekOrigin.Begin);
            
            var template = ImportToProject.FromPdf(ms, fileName, model.Title);
            var id = _fileDatabase.SaveDocumentTemplate(template, null);

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
            var document = _fileDatabase.GetDocumentById(docId);
            
            var model = new TemplateEditViewModel{
                Document = document,
                DocumentId = docId.ToString(),
                BasePdfFile = document.BasePdfFile!,
                
                FileLoadUrl = Url!.Action("Load","File")!,
                ProjectLoadUrl = Url!.Action("ReadProject", "Template")!,
                ProjectStoreUrl = Url!.Action("WriteProject","Template")!,
                
                BoxSampleUrl = Url!.Action("BoxSampleData", "EditModals")!,
                BoxMoveUrl = Url!.Action("MoveBox", "EditModals")!,
                AddFilterUrl = Url!.Action("AddFilter","EditModals")!,
                DeleteFilterUrl = Url!.Action("DeleteFilter","EditModals")!,
                
                BoxEditPartialUrl = Url!.Action("TemplateBox","EditModals")!,
                DataPathPartialUrl = Url!.Action("DataPicker", "EditModals")!,
                DisplayFormatPartialUrl = Url!.Action("DisplayFormat","EditModals")!,
                DocumentInfoPartialUrl = Url!.Action("EditDocumentDetails", "EditModals")!,
                PageInfoPartialUrl = Url!.Action("EditPageDetails","EditModals")!,
                FilterEditPartialUrl = Url!.Action("FilterEditor","EditModals")!
            };
            
            return View(model)!;
        }

        /// <summary>
        /// Returns JSON of the given document's project file
        /// </summary>
        [HttpGet]
        public IActionResult ReadProject([FromQuery]int docId)
        {
            var document = _fileDatabase.GetDocumentById(docId);
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
            var document = SkinnyJson.Json.Defrost<TemplateProject>(ms);
            
            var original = _fileDatabase.GetDocumentById(docId);
            if (document.Version < (original.Version ?? 0)) return BadRequest("Out of Order")!;
            
            _fileDatabase.SaveDocumentTemplate(document, docId);
            return Content("OK")!;
        }

        /// <summary>
        /// Store a project file against a document template ID
        /// </summary>
        /// <param name="project">Details of the document template</param>
        /// <param name="docId">The document template ID</param>
        [HttpPost]
        public IActionResult StoreProject([FromBody]TemplateProject project, [FromQuery]int? docId)
        {
            var newId = _fileDatabase.SaveDocumentTemplate(project, docId);
            return Json(new{id = newId})!;
        }

        /// <summary>
        /// Show a page to accept an image upload, which will be
        /// stored to use as a data source for template boxes.
        /// </summary>
        [HttpGet]
        public IActionResult UploadNewImageStamp()
        {
            return View(new NewImageStampModel())!;
        }

        /// <summary>
        /// Creates a new image stamp from the result of [GET]UploadNewImageStamp()
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadNewImageStamp([FromForm] NewImageStampModel model)
        {
            if (model.Upload == null) { throw new Exception("No file!"); }
            if (string.IsNullOrWhiteSpace(model.Name))
                model.Name = Path.GetFileNameWithoutExtension(model.Upload.Name);
            
            var safeName = model.Name.Safe() + ".jpg";
            
            // It's way more efficient to embed JPEGs into PDFs compared to 
            // other formats, so we convert before storing.
            
            await using var stream = model.Upload.OpenReadStream();
            
            using var image = Portable.Drawing.Image.FromStream(stream);

            if (image.LoadFormat == PortableImage.Jpeg) // no translation needed
            {
                stream.Seek(0, SeekOrigin.Begin);
                _fileDatabase.Store(safeName, stream);
                return RedirectToAction(nameof(Index))!;
            }
            

            var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Jpeg);
            ms.Seek(0, SeekOrigin.Begin);
            
            _fileDatabase.Store(safeName, ms);
            
            return RedirectToAction(nameof(Index))!;
        }

        /// <summary>
        /// Delete an image stamp by name
        /// </summary>
        public IActionResult DeleteImageStamp([FromQuery] string? name)
        {
            _fileDatabase.DeleteStoredFile(name);
            return RedirectToAction(nameof(Index))!;
        }
    }
}