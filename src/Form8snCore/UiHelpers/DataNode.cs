using System.Collections.Generic;
using System.Linq;
using Form8snCore.FileFormats;

namespace Form8snCore.UiHelpers
{
    /// <summary>
    /// NodeTree holds structured data along with descriptions, tags, and other display information.
    /// </summary>
    public class DataNode
    {
        /// <summary>
        /// Child nodes of this node (can be empty)
        /// </summary>
        public List<DataNode> Nodes { get; set; } = new List<DataNode>();
        
        /// <summary>
        /// Path through the object to reach this node as a string separated with a special char.
        /// </summary>
        public string DataPath { get; set; } = "";
        
        /// <summary>
        /// Name of the node
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// Standard color of this node (green for leaf nodes, grey for parent nodes, purple for arrays)
        /// </summary>
        public string ForeColor { get; set; } = "#000";
        
        /// <summary>
        /// Description of the data at this node (node name plus sampled value)
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// Depth through the tree to this node
        /// </summary>
        public int Depth { get; set; }
        
        /// <summary>
        /// True if the node has any child nodes. False if this is a leaf node.
        /// </summary>
        public bool HasChildren => Nodes.Count > 0;

        /// <summary>
        /// A unique ID for this node that can be used for HTML elements.
        /// </summary>
        public string HtmlId => DataPath.Replace(Strings.Separator, '-').Replace('[','_').Replace(']','_'); // TODO: optimise this

        /// <summary>
        /// A comma separated list of HtmlId for each direct child of this node
        /// </summary>
        public string ChildIds => "#"+string.Join(",#", Nodes.Select(n=>n.HtmlId)); // TODO: optimise this

        /// <summary>
        /// Set to true if this is a valid item to be selected by the user.
        /// By default, this is set to all leaf nodes
        /// </summary>
        public bool CanBePicked { get; set; }

        /// <summary>
        /// This is set to true if the source is an array type
        /// </summary>
        public bool IsRepeated { get; set; }

        public DataNode()
        {
        }

        public DataNode(string text, IEnumerable<DataNode> children)
        {
            Text = text;
            Nodes.AddRange(children);
        }

    }
}