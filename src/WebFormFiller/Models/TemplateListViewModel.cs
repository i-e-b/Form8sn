using System;
using System.Collections.Generic;

namespace WebFormFiller.Models
{
    public class TemplateListViewModel
    {
        public IEnumerable<string> Templates { get; set; } = Array.Empty<string>();
    }
}