using System;
using Form8snCore.FileFormats;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebFormFiller.Models;
using WebFormFiller.ServiceStubs;

namespace WebFormFiller.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // TODO: list out documents, option to create/delete
            return View(new TemplateListViewModel{Templates = FileDatabaseStub.ListDocumentTemplates()})!;
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
            
            var id = FileDatabaseStub.SaveDocumentTemplate(new IndexFile(model.Title), null);

            using (var stream = model.Upload.OpenReadStream())
            {
                FileController.Store(model.Title, stream);
            }

            return RedirectToAction(nameof(BoxEditor), new{docId = id})!;
        }

        public IActionResult BoxEditor([FromQuery]int docId)
        {
            return View()!;
        }
    }
}