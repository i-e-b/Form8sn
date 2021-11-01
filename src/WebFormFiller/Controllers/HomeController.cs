using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebFormFiller.Models;

namespace WebFormFiller.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // TODO: list out documents, option to create/delete
            return View();
        }

        public IActionResult BoxEditor(int docId)
        {
            return View();
        }
    }
}