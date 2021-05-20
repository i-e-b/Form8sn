using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BasicImageFormFiller.FileFormats;
using Form8snCore;

namespace BasicImageFormFiller.EditForms
{
    public partial class PickDataSource : Form
    {
        private const char Separator = '\x1F';
        private const string FilterMarker = "#";

        public string[]? SelectedPath { get; set; }
        public string? SelectedTag { get; set; }
        
        public PickDataSource() { InitializeComponent(); }
        public PickDataSource(Project project, string prompt)
        {
            InitializeComponent();
            Text = prompt;
            
            // load tree from sample data
            var data = project.LoadSampleData();
            if (data == null) return;
            
            treeView!.Nodes.AddRange( ReadObjectRecursive(data, "", "Data").ToArray() );
            AddDataFilters(project, data);
        }

        private void AddDataFilters(Project project, object data)
        {
            var filters = new TreeNode {Text = "Filters", Tag = FilterMarker, ForeColor = Color.DimGray};
            foreach (var filter in project.Index.DataFilters)
            {
                var path = FilterMarker + Separator + filter.Key;
                var sample = FilterData.ApplyFilter(filter.Value.MappingType, filter.Value.MappingParameters, filter.Value.DataPath, data);

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

        public sealed override string Text { get => base.Text ?? ""; set => base.Text = value; }

        private static List<TreeNode> ReadObjectRecursive(object o, string path, string node)
        {
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
                        ForeColor = Color.Purple,
                    });
                }

                outp.Add(new TreeNode(node + " (multiple)", collection.ToArray()){
                    Tag = path,
                    ForeColor = Color.Purple,
                });
            }
            else
            {
                outp.Add(new TreeNode{
                    Tag = path,
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