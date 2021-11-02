using System.Collections;
using System.Collections.Generic;
using Form8snCore.FileFormats;

namespace Form8snCore.UiHelpers
{
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
                    ForeColor = ColorGrey
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
                            ForeColor = ColorPurple
                        });
                }

                outp.Add(new DataNode(node + " (multiple)", collection.ToArray())
                {
                    Depth = depth,
                    IsRepeated = true,
                    DataPath = path,
                    Name = name,
                    ForeColor = ColorPurple
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
                    ForeColor = ColorGreen
                });
            }

            return outp;

        }

        private const string ColorGrey = "#777";
        private const string ColorPurple = "#808";
        private const string ColorGreen = "#080";

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