using System;
using System.Collections.Generic;
using Form8snCore.FileFormats;
using Form8snCore.UiHelpers;
using Microsoft.AspNetCore.Http;
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

        [HttpGet]
        public IActionResult TreeTableSample()
        {
            // TEMP: this should be supplied by caller, or is part of the template?
            var sampleData = SkinnyJson.Json.Defrost(System.IO.File.ReadAllText(@"C:\Temp\ExpectedResponse.json")!); 
            // END TEMP
            
            var tree = JsonDataReader.ReadObjectIntoNodeTree(sampleData, "", "Data");
            var list = JsonDataReader.FlattenTree(tree);
            
            var model = new DataSourceViewModel{
                Nodes = list
            };
            
            return View(model)!;
        }

        [HttpGet]
        public IActionResult StartNewTemplate()
        {
            return View(new NewTemplateViewModel())!;
        }

        [HttpPost]
        public IActionResult StartNewTemplate([FromForm]NewTemplateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                model.Title = "Untitled";
            }

            if (model.Upload == null)
            {
                throw new Exception("No file!");
            }
            
            var id = FileDatabaseStub.SaveDocumentTemplate(new IndexFile(model.Title){
                BasePdfFile = model.Title 
            }, null);

            using (var stream = model.Upload.OpenReadStream())
            {
                FileController.Store(model.Title, stream);
            }

            return RedirectToAction(nameof(BoxEditor), new{docId = id})!;
        }

        public IActionResult BoxEditor([FromQuery]int docId)
        {
            var document = FileDatabaseStub.GetDocumentById(docId);
            
            var model = new TemplateEditViewModel{
                Document = document,
                PdfUrl = Url!.Action("Load", "File", new{name = document.BasePdfFile})!
            };
            
            return View(model)!;
        }
    }
}