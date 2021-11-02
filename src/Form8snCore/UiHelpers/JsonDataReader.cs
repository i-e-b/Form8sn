using System.Collections;
using System.Collections.Generic;
using Form8snCore.FileFormats;

namespace Form8snCore.UiHelpers
{

    /// <summary>
    /// NodeTree holds structured data along with descriptions, tags, and other display information.
    /// </summary>
    public class DataNode
    {
        public List<DataNode> Nodes { get; set; } = new List<DataNode>();
        public string DataPath { get; set; } = "";
        public string Name { get; set; } = "";
        public string ForeColor { get; set; } = "#000";
        public string Text { get; set; } = "";
        public bool HasChildren => Nodes.Count > 0;

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
            return ReadObjectRecursive(o, path, node);
        }

        private static List<DataNode> ReadObjectRecursive(object o, string path, string node)
        {
            var name = string.IsNullOrWhiteSpace(path) ? "" : node;
            var outp = new List<DataNode>();
            if (o is Dictionary<string, object> dict)
            {
                var collection = new List<DataNode>();
                foreach (var kvp in dict)
                {
                    collection.AddRange(ReadObjectRecursive(kvp.Value, path + Strings.Separator + kvp.Key, kvp.Key));
                }

                outp.Add(new DataNode(node, collection.ToArray())
                {
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
                        collection.AddRange(ReadObjectRecursive(kvp, path + Strings.Separator + idxStr, idxStr));
                    }
                    else
                        collection.Add(new DataNode
                        {
                            Text = idxStr + " = <null>",
                            DataPath = path + Strings.Separator + idxStr,
                            Name = name,
                            ForeColor = "#808" //Color.Purple,
                        });
                }

                outp.Add(new DataNode(node + " (multiple)", collection.ToArray())
                {
                    DataPath = path,
                    Name = name,
                    ForeColor = "#808" //Color.Purple,
                });
            }
            else
            {
                outp.Add(new DataNode
                {
                    DataPath = path,
                    Name = name,
                    Text = node + " = " + o,
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