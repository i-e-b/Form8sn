using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Form8snCore.FileFormats;
using String_Extensions;

namespace Form8snCore.HelpersAndConverters
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
        /// The 'root' name for this node. This is used to ensure unique HTML element IDs.
        /// </summary>
        public string Root { get; set; } = "undefined";
        
        /// <summary>
        /// Name of the node
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// Standard color of this node (green for leaf nodes, grey for parent nodes, purple for arrays)
        /// </summary>
        public string ForeColor { get; set; } = "#000";
        
        /// <summary>
        /// Background color for this node
        /// </summary>
        public string BackColor { get; set; } = "#FFF";
        
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

        private string? _idString;

        /// <summary>
        /// A unique ID for this node that can be used for HTML elements.
        /// </summary>
        public string HtmlId => _idString ?? GenerateIdString();

        /// <summary>
        /// A comma separated list of HtmlId for each direct child of this node
        /// </summary>
        public string ChildIds => "#"+string.Join(",#", Nodes.Select(n=>n.HtmlId));

        /// <summary>
        /// Set to true if this is a valid item to be selected by the user.
        /// By default, this is set to all leaf nodes
        /// </summary>
        public bool CanBePicked { get; set; }

        /// <summary>
        /// This is set to true if the source is an array type
        /// </summary>
        public bool IsRepeated { get; set; }

        /// <summary>
        /// True if this row and its immediate children should be visible in the UI
        /// </summary>
        public bool Expanded { get; set; }

        /// <summary>
        /// True if this node is part of the currently selected path
        /// </summary>
        public bool Selected { get; set; }
        
        public DataNode()
        {
        }

        public DataNode(string text, IEnumerable<DataNode> children)
        {
            Text = text;
            Nodes.AddRange(children);
        }

        public void Expand() => Expanded = true;

        public void Select() => Selected = true;


        public string InitialDisplayClass {
            get {
                if (Depth <= 0) return "";
                if (Expanded) return ""; // ???
                return "hidden";
            }
        }

        public override string ToString() => Text;

        private string GenerateIdString()
        {
            if (!(_idString is null)) return _idString;
            var sb = new StringBuilder();
            
            var bits = DataPath.Split(Strings.Separator) ?? Array.Empty<string>();
            if (bits.Length < 1) { _idString = Name; return _idString; }
            
            sb.Append(Root);
            for (var i = 1; i < bits.Length; i++)
            {
                sb.Append('-');
                sb.Append(HtmlIdSafe(bits[i]));
            }

            _idString = sb.ToString();
            return _idString;
        }

        private string HtmlIdSafe(string input)
        {
            /*
             HTML 4 requires --
             ID and NAME tokens must begin with a letter ([A-Za-z]) and may be followed by any number of letters,
             digits ([0-9]), hyphens ("-"), underscores ("_"), colons (":"), and periods (".").
             
             HTML 5 only requires no spaces. But using query selector syntax and comma-separated lists
             can be broken by these less strict requirements.
             
             We restrict ourselves to a subset of the HTML4, with no periods.
             */
            var sb = new StringBuilder();
            var src = input.ReplaceAsciiCompatible();
            var init = src[0];

            if (! IsAlpha(init)) { sb.Append("i"); }

            foreach (var c in src)
            {
                if (IsAcceptable(c)) sb.Append(c);
            }
            
            return sb.ToString();
        }

        private static bool IsAcceptable(char c)
        {
            if (c >= 'A' && c <= 'Z') return true;
            if (c >= 'a' && c <= 'z') return true;
            if (c >= '0' && c <= '9') return true;
            if (c == '-') return true;
            if (c == ':') return true;
            
            return false;
        }

        private static bool IsAlpha(char c)
        {
            if (c >= 'A' && c <= 'Z') return true;
            if (c >= 'a' && c <= 'z') return true;
            return false;
        }
    }
}