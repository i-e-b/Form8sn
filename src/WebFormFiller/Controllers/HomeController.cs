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
            // TODO: list out documents, option to create/delete
            return View(new TemplateListViewModel{
                Templates = FileDatabaseStub.ListDocumentTemplates()
            })!;
        }

        // TEMPORARY
        [HttpGet]
        public IActionResult TreeTableSample()
        {
            // TEMP: this should be supplied by caller, or is part of the template?
            var sampleData = SkinnyJson.Json.Defrost(System.IO.File.ReadAllText(@"C:\Temp\OldSystem\New Template\ExpectedResponse.json")); 
            var sampleProject = new FileSystemProject(@"C:\Temp\OldSystem\New Template\Index.json");
            // END TEMP
            
            var prev = new string[]{ };
            var repeat = new[]{"#", "Reclaims in sets of 4"};
            var tree = JsonDataReader.BuildDataSourcePicker(sampleProject.Index, sampleData, prev, repeat, 4);
            var list = JsonDataReader.FlattenTree(tree);
            
            var model = new DataSourceViewModel{
                Nodes = list
            };
            
            return View(model)!;
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
            FileController.Store(fileName, ms);
            var pdfUrl = Url!.Action("Load","File", new{name = fileName})!;
            ms.Seek(0,  SeekOrigin.Begin);
            
            var template = ImportToProject.FromPdf(ms, pdfUrl, model.Title);
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
                ProjectLoadUrl = Url!.Action("ReadProject", "Home")!,
                DocumentId = docId.ToString(),
                PdfUrl = document.BasePdfFile!
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