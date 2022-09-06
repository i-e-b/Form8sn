using System;
using System.Collections.Generic;
using Form8snCore.HelpersAndConverters;

namespace WebFormFiller.Models
{
    public class DataSourceViewModel
    {
        public DataSourceViewModel() { }
        public DataSourceViewModel(string warnings) { Warnings = warnings; }
        
        
        public string Warnings { get; set; } = "";
        public IEnumerable<DataNode> Nodes { get; set; } = Array.Empty<DataNode>();
        public string Target { get; set; } = "";
    }
}