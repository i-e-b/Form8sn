using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BasicImageFormFiller.FileFormats;

namespace BasicImageFormFiller.EditForms
{
    public partial class PickDataSource : Form
    {
        private const char Separator = '\x1F';
        private const string FilterMarker = "#";

        public string[]? SelectedPath { get; set; }
        public string? SelectedTag { get; set; }
        
        public PickDataSource() { InitializeComponent(); }
        public PickDataSource(Project project)
        {
            InitializeComponent();
            
            // load tree from sample data
            var data = project.LoadSampleData();
            if (data == null) return;
            
            treeView!.Nodes.AddRange( ReadObjectRecursive(data, "", "Data").ToArray() );
            
            var filters = new TreeNode{Text = "Filters", Tag = FilterMarker, ForeColor = Color.DimGray};
            foreach (var filterName in project.Index.DataFilters.Keys)
            {
                filters.Nodes.Add(new TreeNode{
                    Text = filterName,
                    Tag = FilterMarker + Separator + filterName,
                    ForeColor = Color.Blue
                });
            }
            treeView.Nodes.Add(filters);
        }

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
    }
}