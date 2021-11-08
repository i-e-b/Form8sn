using Form8snCore.FileFormats;
using Microsoft.AspNetCore.Mvc;
using WebFormFiller.Models;
using WebFormFiller.ServiceStubs;

namespace WebFormFiller.Controllers
{
    public class EditModalsController : Controller
    {
        /// <summary>
        /// View and edit the details of a single template box.
        /// This does not create new boxes -- that should be done with the BoxEditor view.
        /// </summary>
        [HttpGet]
        public IActionResult TemplateBox(int documentTemplateId, int pageIndex, string boxKey)
        {
            var project = FileDatabaseStub.GetDocumentById(documentTemplateId);
            
            var model = TemplateBoxModalViewModel.From(project, documentTemplateId, pageIndex, boxKey);
            
            
            return View(model)!;
        }

        /// <summary>
        /// Store the details of a single template box.
        /// </summary>
        [HttpPost]
        public IActionResult TemplateBox([FromForm]TemplateBoxModalViewModel model)
        {
            return View(model)!;
        }
    }

}