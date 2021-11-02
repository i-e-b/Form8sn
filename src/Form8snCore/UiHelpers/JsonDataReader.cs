using System.Collections;
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
        /// Set to true if this is a valid item to be selected by the user
        /// </summary>
        public bool CanBePicked { get; set; }

        public DataNode()
        {
        }

        public DataNode(string text, IEnumerable<DataNode> children)
        {
            Text = text;
            Nodes.AddRange(children);
        }

    }

    /// <summary>
    /// JsonDataReader provides routines to read sample data into formats
    /// appropriate for UI display
    /// </summary>
    public class JsonDataReader
    {

        public static List<DataNode> ReadObjectIntoNodeTree(object o, string path, string node)
        {
            return ReadObjectRecursive(o, path, node, 0);
        }

        private static List<DataNode> ReadObjectRecursive(object o, string path, string node, int depth)
        {
            var name = string.IsNullOrWhiteSpace(path) ? "" : node;
            var outp = new List<DataNode>();
            if (o is Dictionary<string, object> dict)
            {
                var collection = new List<DataNode>();
                foreach (var kvp in dict)
                {
                    collection.AddRange(ReadObjectRecursive(kvp.Value, path + Strings.Separator + kvp.Key, kvp.Key, depth + 1));
                }

                outp.Add(new DataNode(node, collection.ToArray())
                {
                    Depth = depth,
                    DataPath = path,
                    Name = name,
                    ForeColor = "#777" //Color.DimGray,
                });
            }
            else if (o is ArrayList array)
            {
                var collection = new List<DataNode>();
                for (var index = 0; index < array.Count; index++)
                {
                    var kvp = array[index];
                    var idxStr = $"[{index}]";
                    if (kvp != null)
                    {
                        collection.AddRange(ReadObjectRecursive(kvp, path + Strings.Separator + idxStr, idxStr, depth + 1));
                    }
                    else
                        collection.Add(new DataNode
                        {
                            Depth = depth + 1,
                            Text = idxStr + " = <null>",
                            DataPath = path + Strings.Separator + idxStr,
                            Name = name,
                            ForeColor = "#808" //Color.Purple,
                        });
                }

                outp.Add(new DataNode(node + " (multiple)", collection.ToArray())
                {
                    Depth = depth,
                    DataPath = path,
                    Name = name,
                    ForeColor = "#808" //Color.Purple,
                });
            }
            else
            {
                outp.Add(new DataNode
                {
                    Depth = depth,
                    DataPath = path,
                    Name = name,
                    Text = node + " = " + o,
                    CanBePicked = true,
                    ForeColor = "#080" //Color.Green
                });
            }

            return outp;

        }

        public static List<DataNode> FlattenTree(List<DataNode> tree)
        {
            var result = new List<DataNode>();
            FlattenListRecursive(result, tree);
            return result;
        }

        private static void FlattenListRecursive(List<DataNode> result, List<DataNode> tree)
        {
            foreach (var node in tree)
            {
                result.Add(node);
                FlattenListRecursive(result, node.Nodes);
            }
        }
    }
}