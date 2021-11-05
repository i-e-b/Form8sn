using System;
using System.Collections.Generic;
using Form8snCore.HelpersAndConverters;

namespace WebFormFiller.Models
{
    public class DataSourceViewModel
    {
        public IEnumerable<DataNode> Nodes { get; set; } = Array.Empty<DataNode>();
    }
}