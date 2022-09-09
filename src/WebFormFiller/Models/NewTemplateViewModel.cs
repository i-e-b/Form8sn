using Microsoft.AspNetCore.Http;

namespace WebFormFiller.Models
{
    public class NewTemplateViewModel
    {
        public string Title { get; set; } = "";
        public IFormFile? Upload { get; set; }
    }
}