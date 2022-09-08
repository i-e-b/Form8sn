using Microsoft.AspNetCore.Http;

namespace WebFormFiller.Models
{
    public class NewImageStampModel
    {
        public string Name { get; set; } = "";
        public IFormFile? Upload { get; set; }
    }
}