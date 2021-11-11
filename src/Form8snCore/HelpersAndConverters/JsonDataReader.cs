using System;
using System.Collections;
using System.Collections.Generic;
using Form8snCore.FileFormats;
using Form8snCore.DataExtraction;
using Form8snCore.Rendering;

namespace Form8snCore.HelpersAndConverters
{
    /// <summary>
    /// JsonDataReader provides routines to read sample data into formats
    /// appropriate for UI display
    /// </summary>
    public static class JsonDataReader
    {
        // TODO: test these with data sets that don't fill filters (like split reclaims in 4s with only 2 reclaims)
        
        /// <summary>
        /// Build the tree used to select source data for boxes and filters
        /// </summary>
        /// <param name="index">The template's index file</param>
        /// <param name="sampleData">Sample data to pick from. Also used to populate filter output</param>
        /// <param name="previous">The already selected path, if any</param>
        /// <param name="repeaterPath">Path used by the page repeater, if any</param>
        /// <param name="pageIndex">The page definition being targeted (in the index file), if any</param>
        public static List<DataNode> BuildDataSourcePicker(IndexFile index, object sampleData, string[]? previous, string[]? repeaterPath, int? pageIndex)
        {
            var result = new List<DataNode>();
            
            AddSampleData(result, sampleData);
            AddDataFilters(result, index, sampleData);
            if (repeaterPath != null) AddRepeaterPath(result, index, sampleData, repeaterPath);
            if (pageIndex != null) AddPageDataFilters(result, index, sampleData, pageIndex);
            AddPageNumbers(result, repeaterPath);
            
            SelectPath(result, previous); // Expand and highlight previous selection
            
            return result;
        }

        /// <summary>
        /// Flatten a data tree into a depth-first list.
        /// This is used to display the hierarchy in table views
        /// </summary>
        public static IEnumerable<DataNode> FlattenTree(List<DataNode> tree)
        {
            var result = new List<DataNode>();
            FlattenListRecursive(result, tree);
            return result;
        }
        
        #region Display colors
        // From System.Drawing.KnownColorTable
        private const string ColorGrey = "#777";
        private const string ColorBlue = "#00F";
        private const string ColorRed = "#F00";
        private const string ColorPink = "#FFC0CB";
        private const string ColorPurple = "#808";
        private const string ColorGreen = "#080";
        private const string ColorLinen = "#FAF0E6";
        private const string ColorBrown = "#A52A2A";
        private const string ColorSteelBlue = "#4682B4";
        #endregion
        
        #region Inner workings
        private static void SelectPath(ICollection<DataNode> dataNodes, string[]? path)
        {
            if (path == null || path.Length < 1) return;
            
            var node = FindByName(dataNodes, path[0]);
            if (node is null) return;
            
            for (var i = 1; i < path.Length; i++)
            {
                node.Expand();
                node = FindByName(node.Nodes, path[i]);
                if (node == null) return;
            }

            node.Expand();
            node.Select();
        }
        
        private static DataNode? FindByName(ICollection<DataNode>? nodes, string tag)
        {
            if (nodes == null) return null;
            foreach (var node in nodes)
            {
                if (node.Name == tag) return node;
            }
            return null;
        }

        private static void AddPageNumbers(ICollection<DataNode> dataNodes, string[]? repeaterPath)
        {
            const string root = "page-num";
            var pagesNode = new DataNode { Text = "Page info", Root = root, Depth = 0, ForeColor = ColorSteelBlue };
            
            
            pagesNode.Nodes.Add(new DataNode
            {
                Text = "Generation Date and Time",
                Root = root, Depth = 1,
                DataPath = $"{Strings.PageDataMarker}{Strings.Separator}{nameof(DocumentBoxType.PageGenerationDate)}",
                Name = nameof(DocumentBoxType.PageGenerationDate),
                CanBePicked = true
            });

            pagesNode.Nodes.Add(new DataNode
            {
                Text = "Current Page Number",
                Root = root, Depth = 1,
                DataPath = $"{Strings.PageDataMarker}{Strings.Separator}{nameof(DocumentBoxType.CurrentPageNumber)}",
                Name = nameof(DocumentBoxType.CurrentPageNumber),
                CanBePicked = true
            });

            pagesNode.Nodes.Add(new DataNode
            {
                Text = "Total Page Count",
                Root = root, Depth = 1,
                DataPath = $"{Strings.PageDataMarker}{Strings.Separator}{nameof(DocumentBoxType.TotalPageCount)}",
                Name = nameof(DocumentBoxType.TotalPageCount),
                CanBePicked = true
            });

            if (repeaterPath != null)
            {
                pagesNode.Nodes.Add(new DataNode
                {
                    Text = "Repeating Page Number",
                    Root = root, Depth = 1,
                    DataPath = $"{Strings.PageDataMarker}{Strings.Separator}{nameof(DocumentBoxType.RepeatingPageNumber)}",
                    Name = nameof(DocumentBoxType.RepeatingPageNumber),
                    CanBePicked = true
                });

                pagesNode.Nodes.Add(new DataNode
                {
                    Text = "Repeating Page Total Count",
                    Root = root, Depth = 1,
                    DataPath = $"{Strings.PageDataMarker}{Strings.Separator}{nameof(DocumentBoxType.RepeatingPageTotalCount)}",
                    Name = nameof(DocumentBoxType.RepeatingPageTotalCount),
                    CanBePicked = true
                });
            }

            dataNodes.Add(pagesNode);
        }

        private static void AddSampleData(List<DataNode> dataNodes, object data)
        {
            var nodes = ReadObjectRecursive(data, "", "Data", "page-data", 0);
            if (nodes.Count > 0) nodes[0]!.Expand(); // expand first node by default
            dataNodes.AddRange(nodes.ToArray());
        }
        
        // BUG: this might not be working if not enough data to repeat. Check with unit test.
        private static void AddRepeaterPath(ICollection<DataNode> dataNodes, IndexFile index, object sampleData, string[] repeaterPath)
        {
            const string root = "repeater";
            // Get a "sample" from the data.
            // If it's an ArrayList, take the first item and make nodes from it.
            // If it's not an ArrayList, just make nodes from it
            // Either way, add under "Page Repeat Data" tagged:'D'
            var pageNode = new DataNode{
                Root = root,
                Name = "D",
                Text = "Page Repeat Data",
                DataPath = "D",
                BackColor = ColorLinen,
                ForeColor = ColorBrown
            };
            
            var sample = MappingActions.ApplyFilter(
                MappingType.None,
                new Dictionary<string, string>(),
                repeaterPath,
                null,
                index.DataFilters,
                sampleData,
                null,
                null
            );
            
            // sample should be an ArrayList.
            if (sample is ArrayList list && list.Count > 0)
            {
                switch (list[0])
                {
                    // invalid list
                    case null:
                        pageNode.Nodes.Add(new DataNode {Text = "Invalid result", ForeColor = ColorRed, BackColor = ColorPink});
                        break;
                    // each page has multiple rows
                    case ArrayList page1:
                    {
                        var sampleNodes = ReadObjectRecursive(page1, "D", "XXX", root, 0).ToArray();
                        if (sampleNodes.Length < 1)
                        {
                            pageNode.Nodes.Add(new DataNode {Text = "Sample data can't fill this repeater", ForeColor = ColorRed, BackColor = ColorPink});
                        }
                        else
                        {
                            // Should be one node, with possible multiple children
                            foreach (var node in sampleNodes[0].Nodes)
                            {
                                pageNode.Nodes.Add(node);
                            }

                            pageNode.Expand(); // expand first node by default
                        }

                        break;
                    }
                    // each page has a single compound object
                    case Dictionary<string, object> dict:
                    {
                        var sampleNodes = ReadObjectRecursive(dict, "D", "XXX", root, 0);
                        if (sampleNodes.Count != 1) throw new Exception("Unexpected object result in page data ReadObjectRecursive");
                    
                        foreach (var node in sampleNodes[0]!.Nodes)
                        {
                            pageNode.Nodes.Add(node);
                        }

                        pageNode.Expand(); // expand first node by default
                        break;
                    }
                    // single value
                    default:
                        pageNode.Nodes.Add(new DataNode {Text = "Invalid result", ForeColor = ColorRed, BackColor = ColorPink});
                        break;
                }
            }
            else
            {
                pageNode.Nodes.Add(new DataNode {
                    Name = "invalid",
                    Text = $"No result (repeater path - {(sample?.GetType().ToString())??"<null>"})",
                    DataPath = $"D{Strings.Separator}invalid",
                    Depth = 1,
                    CanBePicked = false,
                    Root = root, ForeColor = ColorRed, BackColor = ColorPink});
            }


            dataNodes.Add(pageNode);
        }
        
        private static void AddPageDataFilters(ICollection<DataNode> dataNodes, IndexFile index, object data, int? pageIndex)
        {
            if (pageIndex is null) return;
            if (pageIndex.Value < 0 || pageIndex.Value >= index.Pages.Count) return;
            
            var thePage = index.Pages[pageIndex.Value];
            if (thePage is null) return;
            
            const string root = "page-data";
            var repeatData = MappingActions.ApplyFilter(
                MappingType.None,
                new Dictionary<string, string>(),
                thePage.RepeatMode.DataPath,
                null,
                index.DataFilters,
                data,
                null,
                null
            );
            if (repeatData is ArrayList list) repeatData = list[0];
            
            var filters = new DataNode {Text = "Page Filters", Root=root, Name = "P", DataPath = Strings.FilterMarker, ForeColor = ColorGrey, CanBePicked = false};
            foreach (var filter in thePage.PageDataFilters)
            {
                if (filter.Value is null || filter.Key is null) continue;
                
                var path = Strings.FilterMarker + Strings.Separator + filter.Key;
                var sample = MappingActions.ApplyFilter(
                    filter.Value.MappingType,
                    filter.Value.MappingParameters,
                    filter.Value.DataPath,
                    null,
                    index.DataFilters,
                    data,
                    repeatData,
                    null
                );

                if (sample == null)
                {
                    var node = new DataNode { Text = filter.Key, DataPath = path, ForeColor = ColorBlue, CanBePicked = false};
                    node.Nodes.Add(new DataNode {Text = "No result", ForeColor = ColorRed, BackColor = ColorPink, CanBePicked = false});
                    filters.Nodes.Add(node);
                }
                else
                {
                    var sampleNodes = ReadObjectRecursive(sample, path, filter.Key, root, 1).ToArray();
                    filters.Nodes.AddRange(sampleNodes);
                }
            }

            dataNodes.Add(filters);
        }
        
        private static void AddDataFilters(ICollection<DataNode> dataNodes, IndexFile index, object data)
        {
            const string root = "page-filters";
            var filters = new DataNode {
                Text = "Filters", Root = root, Name = "#", DataPath = Strings.FilterMarker, ForeColor = ColorGrey, CanBePicked = false
            };
            foreach (var filter in index.DataFilters)
            {
                if (filter.Value is null || filter.Key is null) continue;
                
                var path = Strings.FilterMarker + Strings.Separator + filter.Key;
                var sample = MappingActions.ApplyFilter(
                    filter.Value.MappingType,
                    filter.Value.MappingParameters,
                    filter.Value.DataPath,
                    null,
                    index.DataFilters,
                    data,
                    null,
                    null
                );

                if (sample == null)
                {
                    var node = new DataNode { Text = filter.Key, DataPath = path, ForeColor = ColorBlue, CanBePicked = false };
                    node.Nodes.Add(new DataNode {Text = "No result", ForeColor = ColorRed, BackColor = ColorPink, CanBePicked = false});
                    filters.Nodes.Add(node);
                }
                else
                {
                    var sampleNodes = ReadObjectRecursive(sample, path, filter.Key, root, 1).ToArray();
                    filters.Nodes.AddRange(sampleNodes);
                }
            }

            dataNodes.Add(filters);
        }

        private static List<DataNode> ReadObjectRecursive(object o, string path, string node, string root, int depth)
        {
            var name = string.IsNullOrWhiteSpace(path) ? "" : node;
            var outp = new List<DataNode>();
            if (o is Dictionary<string, object> dict)
            {
                var collection = new List<DataNode>();
                foreach (var kvp in dict)
                {
                    if (kvp.Key is null || kvp.Value is null) continue;
                    collection.AddRange(ReadObjectRecursive(kvp.Value, path + Strings.Separator + kvp.Key, kvp.Key, root, depth + 1));
                }

                outp.Add(new DataNode(node, collection.ToArray())
                {
                    Root = root,
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
                        collection.AddRange(ReadObjectRecursive(kvp, path + Strings.Separator + idxStr, idxStr, root, depth + 1));
                    }
                    else
                        collection.Add(new DataNode
                        {
                            Root = root,
                            Depth = depth + 1,
                            Text = idxStr + " = <null>",
                            DataPath = path + Strings.Separator + idxStr,
                            Name = name,
                            ForeColor = ColorPurple
                        });
                }

                outp.Add(new DataNode(node + " (multiple)", collection.ToArray())
                {
                    Root = root,
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
                    Root = root,
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

        private static void FlattenListRecursive(ICollection<DataNode> result, List<DataNode> tree)
        {
            foreach (var node in tree)
            {
                result.Add(node);
                FlattenListRecursive(result, node.Nodes);
            }
        }
        #endregion
    }
}