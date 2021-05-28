using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Form8snCore.DataExtraction;
using Form8snCore.FileFormats;
using Form8snCore.Rendering;

namespace BasicImageFormFiller.EditForms
{
    public partial class PickDataSource : Form
    {
        private readonly bool _allowEmpty;
        public string[]? SelectedPath { get; set; }
        public string? SelectedTag { get; set; }
        
        /// <summary> Don't use this one </summary>
        public PickDataSource() { InitializeComponent(); }

        /// <summary>
        /// Create a form to pick a data source item or group
        /// </summary>
        /// <param name="project">Project holding sample data and filters</param>
        /// <param name="prompt">Text to show user</param>
        /// <param name="previous">Optional: The path that was previously selected. This will be shown as expanded and selected if it's still valid</param>
        /// <param name="repeaterPath">Optional: Page-specific repeater data path. This will show the data for page 1, but the path selection will be correct for all pages</param>
        /// <param name="pageIndex">Optional: If given, page-specific filters will be shown</param>
        /// <param name="allowEmpty">If true, the pick button will be available when no item is selected</param>
        public PickDataSource(Project project, string prompt, string[]? previous, string[]? repeaterPath, int? pageIndex, bool allowEmpty)
        {
            _allowEmpty = allowEmpty;
            InitializeComponent();
            Text = prompt;
            
            // load tree from sample data
            var data = project.LoadSampleData();
            if (data == null) return;
            
            AddSampleData(data);
            AddDataFilters(project, data);
            if (repeaterPath != null) AddRepeaterPath(project, data, repeaterPath);
            if (pageIndex != null) AddPageDataFilters(project, data, pageIndex!.Value);
            AddPageNumbers(repeaterPath);
            
            if (allowEmpty) pickButton!.Enabled = true;
            SelectPath(previous); // pick previous source if it exists
        }

        private void AddPageNumbers(string[]? repeaterPath)
        {
            var pagesNode = new TreeNode{Text="Page numbers", ForeColor = Color.SteelBlue};
            
            pagesNode.Nodes.Add(new TreeNode{
                Text = "Current Page Number",
                Tag = $"{Strings.PageDataMarker}{Strings.Separator}{nameof(DocumentBoxType.CurrentPageNumber)}",
                Name = nameof(DocumentBoxType.CurrentPageNumber),
            });
            
            pagesNode.Nodes.Add(new TreeNode{
                Text = "Total Page Count",
                Tag = $"{Strings.PageDataMarker}{Strings.Separator}{nameof(DocumentBoxType.TotalPageCount)}",
                Name = nameof(DocumentBoxType.TotalPageCount),
            });

            if (repeaterPath != null)
            {
                pagesNode.Nodes.Add(new TreeNode
                {
                    Text = "Repeating Page Number",
                    Tag = $"{Strings.PageDataMarker}{Strings.Separator}{nameof(DocumentBoxType.RepeatingPageNumber)}",
                    Name = nameof(DocumentBoxType.RepeatingPageNumber),
                });

                pagesNode.Nodes.Add(new TreeNode
                {
                    Text = "Repeating Page Total Count",
                    Tag = $"{Strings.PageDataMarker}{Strings.Separator}{nameof(DocumentBoxType.RepeatingPageTotalCount)}",
                    Name = nameof(DocumentBoxType.RepeatingPageTotalCount),
                });
            }

            treeView!.Nodes.Add(pagesNode);
        }

        private void AddRepeaterPath(Project project, object sampleData, string[] repeaterPath)
        {
            // Get a "sample" from the data.
            // If it's an ArrayList, take the first item and make nodes from it.
            // If it's not an ArrayList, just make nodes from it
            // Either way, add under "Page Repeat Data" tagged:'D'
            var pageNode = new TreeNode{
                Name = "D",
                Text = "Page Repeat Data",
                Tag = "D",
                BackColor = Color.Linen,
                ForeColor = Color.Brown
            };
            
            var sample = MappingActions.ApplyFilter(
                MappingType.None,
                new Dictionary<string, string>(),
                repeaterPath,
                null,
                project.Index.DataFilters,
                sampleData,
                null,
                null
            );
            
            // sample should be an ArrayList.
            if (sample is ArrayList list && list.Count > 0)
            {
                if (list[0] == null) // invalid list
                {
                    pageNode.Nodes.Add(new TreeNode {Text = "Invalid result", ForeColor = Color.Red, BackColor = Color.Pink});
                }
                else if (list[0] is ArrayList page1) // each page has multiple rows
                {
                    var sampleNodes = ReadObjectRecursive(page1, "D", "XXX").ToArray();
                    if (sampleNodes.Length < 1)
                    {
                        pageNode.Nodes.Add(new TreeNode {Text = "Sample data insufficient?", ForeColor = Color.Red, BackColor = Color.Pink});
                    }
                    else
                    {
                        // Should be one node, with possible multiple children
                        foreach (TreeNode? node in sampleNodes[0].Nodes)
                        {
                            if (node != null) pageNode.Nodes.Add(node);
                        }

                        pageNode.Expand(); // expand first node by default
                    }
                }
                else if (list[0] is Dictionary<string, object> dict) // each page has a single compound object
                {
                    var sampleNodes = ReadObjectRecursive(dict, "D", "XXX");
                    if (sampleNodes.Count != 1) throw new Exception("Unexpected object result in page data ReadObjectRecursive");
                    
                    foreach (TreeNode? node in sampleNodes[0].Nodes)
                    {
                        if (node != null) pageNode.Nodes.Add(node);
                    }

                    pageNode.Expand(); // expand first node by default
                }
                else // single value
                {
                    pageNode.Nodes.Add(new TreeNode {Text = "Invalid result", ForeColor = Color.Red, BackColor = Color.Pink}); // TODO: can I handle single values here?
                }
            }
            else
            {
                pageNode.Nodes.Add(new TreeNode {Text = "No result", ForeColor = Color.Red, BackColor = Color.Pink});
            }


            treeView!.Nodes.Add(pageNode);
        }

        private void AddSampleData(object data)
        {
            var nodes = ReadObjectRecursive(data, "", "Data");
            if (nodes.Count > 0) nodes[0].Expand(); // expand first node by default
            treeView!.Nodes.AddRange(nodes.ToArray());
        }

        private void AddPageDataFilters(Project project, object data, int pageIndex)
        {
            var repeatData = MappingActions.ApplyFilter(
                MappingType.None,
                new Dictionary<string, string>(),
                project.Pages[pageIndex].RepeatMode.DataPath,
                null,
                project.Index.DataFilters,
                data,
                null,
                null
            );
            if (repeatData is ArrayList list) repeatData = list[0];
            
            var filters = new TreeNode {Text = "Page Filters", Name = "P", Tag = Strings.FilterMarker, ForeColor = Color.DimGray};
            foreach (var filter in project.Pages[pageIndex].PageDataFilters)
            {
                var path = Strings.FilterMarker + Strings.Separator + filter.Key;
                var sample = MappingActions.ApplyFilter(
                    filter.Value.MappingType,
                    filter.Value.MappingParameters,
                    filter.Value.DataPath,
                    null,
                    project.Index.DataFilters,
                    data,
                    repeatData,
                    null
                );

                if (sample == null)
                {
                    var node = new TreeNode { Text = filter.Key, Tag = path, ForeColor = Color.Blue };
                    node.Nodes.Add(new TreeNode {Text = "No result", ForeColor = Color.Red, BackColor = Color.Pink});
                    filters.Nodes.Add(node);
                }
                else
                {
                    var sampleNodes = ReadObjectRecursive(sample, path, filter.Key).ToArray();
                    filters.Nodes.AddRange(sampleNodes);
                }
            }

            treeView!.Nodes.Add(filters);
        }

        private void AddDataFilters(Project project, object data)
        {
            var filters = new TreeNode {Text = "Filters", Name = "#", Tag = Strings.FilterMarker, ForeColor = Color.DimGray};
            foreach (var filter in project.Index.DataFilters)
            {
                var path = Strings.FilterMarker + Strings.Separator + filter.Key;
                var sample = MappingActions.ApplyFilter(
                    filter.Value.MappingType,
                    filter.Value.MappingParameters,
                    filter.Value.DataPath,
                    null,
                    project.Index.DataFilters,
                    data,
                    null,
                    null
                    );

                if (sample == null)
                {
                    var node = new TreeNode { Text = filter.Key, Tag = path, ForeColor = Color.Blue };
                    node.Nodes.Add(new TreeNode {Text = "No result", ForeColor = Color.Red, BackColor = Color.Pink});
                    filters.Nodes.Add(node);
                }
                else
                {
                    var sampleNodes = ReadObjectRecursive(sample, path, filter.Key).ToArray();
                    filters.Nodes.AddRange(sampleNodes);
                }
            }

            treeView!.Nodes.Add(filters);
        }
        
        private void SelectPath(string[]? previous)
        {
            if (previous == null || previous.Length < 1) return;
            
            var node = FindByTag(treeView!.Nodes, previous[0]);
            for (int i = 1; i < previous.Length; i++)
            {
                node = FindByTag(node?.Nodes, previous[i]);
                if (node == null) return;
            }

            if (node != null) treeView.SelectedNode = node;
        }

        private TreeNode? FindByTag(TreeNodeCollection? nodes, string tag)
        {
            if (nodes == null) return null;
            foreach (TreeNode? node in nodes)
            {
                if (node != null && node.Name == tag) return node;
            }
            return null;
        }

        public sealed override string Text { get => base.Text ?? ""; set => base.Text = value; }

        private static List<TreeNode> ReadObjectRecursive(object o, string path, string node)
        {
            var name = string.IsNullOrWhiteSpace(path) ? "" : node;
            var outp = new List<TreeNode>();
            if (o is Dictionary<string, object> dict)
            {
                var collection = new List<TreeNode>();
                foreach (var kvp in dict)
                {
                    collection.AddRange(ReadObjectRecursive(kvp.Value, path + Strings.Separator + kvp.Key, kvp.Key));
                }
                outp.Add(new TreeNode(node, collection.ToArray()){
                    Tag = path,
                    Name = name,
                    ForeColor = Color.DimGray,
                });
            }
            else if (o is ArrayList array)
            {
                var collection = new List<TreeNode>();
                for (var index = 0; index < array.Count; index++)
                {
                    var kvp = array[index];
                    var idxStr = $"[{index}]";
                    if (kvp != null)
                    {
                        collection.AddRange(ReadObjectRecursive(kvp, path + Strings.Separator + idxStr, idxStr));
                    } else collection.Add(new TreeNode{
                        Text = idxStr + " = <null>",
                        Tag = path + Strings.Separator + idxStr,
                        Name = name,
                        ForeColor = Color.Purple,
                    });
                }

                outp.Add(new TreeNode(node + " (multiple)", collection.ToArray()){
                    Tag = path,
                    Name = name,
                    ForeColor = Color.Purple,
                });
            }
            else
            {
                outp.Add(new TreeNode{
                    Tag = path,
                    Name = name,
                    Text = node + " = " + o,
                    ForeColor = Color.Green
                });
            }

            return outp;
        }

        private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectedTag = e.Node?.Tag?.ToString();
            pickButton!.Enabled = _allowEmpty || !string.IsNullOrWhiteSpace(SelectedTag);
            tweakPathButton!.Enabled = !string.IsNullOrWhiteSpace(SelectedTag);
            pathPreview!.Text = SelectedTag?.Replace(Strings.Separator, '.') ?? "< invalid path >";
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void pickButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            SelectedPath = SelectedTag?.Split(Strings.Separator);
            Close();
        }

        private void tweakPathButton_Click(object sender, EventArgs e)
        {
            // Allow the user to adjust the path. This is needed if we
            // expect inputs longer than the sample data provides
            if (SelectedTag == null) return;
            var pe = new EditPathForm(null, SelectedTag!.Split(Strings.Separator));
            pe.ShowDialog();
            if (pe.EditedPath == null) return; // cancelled or closed

            SelectedTag = string.Join(Strings.Separator, pe.EditedPath);
            pathPreview!.Text = SelectedTag?.Replace(Strings.Separator, '.') ?? "< invalid path >";
        }
    }
}