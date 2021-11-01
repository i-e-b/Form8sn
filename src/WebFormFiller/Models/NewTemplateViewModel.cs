using Microsoft.AspNetCore.Http;

namespace WebFormFiller.Models
{
    public class NewTemplateViewModel
    {
        public string Title { get; set; } = "Untitled";
        public IFormFile? Upload { get; set; }
    }
}