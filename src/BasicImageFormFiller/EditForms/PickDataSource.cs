using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Form8snCore;
using Form8snCore.DataExtraction;
using Form8snCore.FileFormats;

namespace BasicImageFormFiller.EditForms
{
    public partial class PickDataSource : Form
    {
        private const char Separator = '\x1F';
        private const string FilterMarker = "#";

        public string[]? SelectedPath { get; set; }
        public string? SelectedTag { get; set; }
        
        public PickDataSource() { InitializeComponent(); }
        public PickDataSource(Project project, string prompt, string[]? previous, string[]? repeaterPath)
        {
            InitializeComponent();
            Text = prompt;
            
            // load tree from sample data
            var data = project.LoadSampleData();
            if (data == null) return;
            
            AddSampleData(data);
            AddDataFilters(project, data);
            if (repeaterPath != null) AddRepeaterPath(project, data, repeaterPath);
            
            SelectPath(previous); // pick previous source if it exists
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
            
            var sample = FilterData.ApplyFilter(
                MappingType.None,
                new Dictionary<string, string>(),
                repeaterPath,
                project.Index.DataFilters,
                sampleData,
                null
            );
            
            // sample should be an ArrayList.
            if (sample is ArrayList list)
            {
                var page1 = list[0] as ArrayList;

                if (page1 == null)
                {
                    pageNode.Nodes.Add(new TreeNode {Text = "Invalid result", ForeColor = Color.Red, BackColor = Color.Pink});
                }
                else
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

        private void AddDataFilters(Project project, object data)
        {
            var filters = new TreeNode {Text = "Filters", Name = "#", Tag = FilterMarker, ForeColor = Color.DimGray};
            foreach (var filter in project.Index.DataFilters)
            {
                var path = FilterMarker + Separator + filter.Key;
                var sample = FilterData.ApplyFilter(
                    filter.Value.MappingType,
                    filter.Value.MappingParameters,
                    filter.Value.DataPath,
                    project.Index.DataFilters,
                    data,
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
                    collection.AddRange(ReadObjectRecursive(kvp.Value, path + Separator + kvp.Key, kvp.Key));
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
                        collection.AddRange(ReadObjectRecursive(kvp, path + Separator + idxStr, idxStr));
                    } else collection.Add(new TreeNode{
                        Text = idxStr + " = <null>",
                        Tag = path + Separator + idxStr,
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
            pickButton!.Enabled = !string.IsNullOrWhiteSpace(SelectedTag);
            tweakPathButton!.Enabled = !string.IsNullOrWhiteSpace(SelectedTag);
            pathPreview!.Text = SelectedTag?.Replace(Separator, '.') ?? "< invalid path >";
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void pickButton_Click(object sender, EventArgs e)
        {
            SelectedPath = SelectedTag?.Split(Separator);
            Close();
        }

        private void tweakPathButton_Click(object sender, EventArgs e)
        {
            // Allow the user to adjust the path. This is needed if we
            // expect inputs longer than the sample data provides
            if (SelectedTag == null) return;
            var pe = new EditPathForm(null, SelectedTag!.Split(Separator));
            pe.ShowDialog();
            if (pe.EditedPath == null) return; // cancelled or closed

            SelectedTag = string.Join(Separator, pe.EditedPath);
            pathPreview!.Text = SelectedTag?.Replace(Separator, '.') ?? "< invalid path >";
        }
    }
}