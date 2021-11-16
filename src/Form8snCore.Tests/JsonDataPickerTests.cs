using System.Linq;
using Form8snCore.FileFormats;
using Form8snCore.HelpersAndConverters;
using Form8snCore.Tests.Helpers;
using NUnit.Framework;

namespace Form8snCore.Tests
{
    [TestFixture]
    public class JsonDataPickerTests
    {
        [Test(Description = "Building a tree from sample data, with multi-select off. Checking data paths")]
        public void data_paths_should_be_built_into_a_tree_structure()
        {
            var lastPath = new string[] { };
            var p = Strings.Separator;
            
            var nodes = JsonDataReader.BuildDataSourcePicker(SampleIndexFiles.BasicFile, SampleData.Standard, lastPath, null, 0, false);
            
            Assert.That(nodes, Is.Not.Null, "Failed to extract picker data");
            
            // Check pickable paths are present
            var claimantMailCode = nodes.X("").X("Claimant").X("MailingAddress").X("Country").N("Code");
            Assert.That(claimantMailCode?.CanBePicked, Is.True, "pickable data is not valid");
            Assert.That(claimantMailCode?.DataPath, Is.EqualTo($"{p}Claimant{p}MailingAddress{p}Country{p}Code"), "data path doesn't match node path");
            Assert.That(claimantMailCode?.Depth, Is.EqualTo(4), "incorrect depth in data path");
            
            // Check object paths are not pickable
            var claimantMailCountry = nodes.X("").X("Claimant").X("MailingAddress").N("Country");
            Assert.That(claimantMailCountry?.CanBePicked, Is.False, "non-pickable data is not valid");
            
            // With multiple-mode set to false, the reclaims should not be pickable
            var reclaims = nodes.X("").N("Reclaims");
            Assert.That(reclaims?.CanBePicked, Is.False, "multiple data was pickable, but should not have been");
            
            
            // Should be able to flatten nodes
            var flattened = JsonDataReader.FlattenTree(nodes).ToList();
            
            // Nothing should be pre-selected as we gave a null path
            var selected = flattened.Where(n=>n.Selected).ToList();
            Assert.That(selected, Is.Empty, "There were selected items with no selection path given");
            
            // Only level-1 nodes should be expanded
            var wrongExpansions = flattened.Where(n=>n.Expanded != (n.Depth == 0)).ToList();
            Assert.That(wrongExpansions, Is.Empty, "Some expansions were wrong");
        }

        [Test(Description = "Building a tree from sample data, with multi-select ON. Checking data paths")]
        public void array_data_should_be_selectable_when_multi_mode_is_on()
        {
            var lastPath = new string[] { };
            var p = Strings.Separator;
            
            var nodes = JsonDataReader.BuildDataSourcePicker(SampleIndexFiles.BasicFile, SampleData.Standard, lastPath, null, 0, true);
            
            Assert.That(nodes, Is.Not.Null, "Failed to extract picker data");
            
            // Check pickable paths are present
            var claimantMailCode = nodes.X("").X("Claimant").X("MailingAddress").X("Country").N("Code");
            Assert.That(claimantMailCode?.CanBePicked, Is.True, "pickable data is not valid");
            Assert.That(claimantMailCode?.DataPath, Is.EqualTo($"{p}Claimant{p}MailingAddress{p}Country{p}Code"), "data path doesn't match node path");
            Assert.That(claimantMailCode?.Depth, Is.EqualTo(4), "incorrect depth in data path");
            
            // Check object paths are not pickable
            var claimantMailCountry = nodes.X("").X("Claimant").X("MailingAddress").N("Country");
            Assert.That(claimantMailCountry?.CanBePicked, Is.False, "non-pickable data is not valid");
            
            // With multiple-mode set to true, the reclaims should be pickable
            var reclaims = nodes.X("").N("Reclaims");
            Assert.That(reclaims?.CanBePicked, Is.True, "multiple data was not pickable, but should have been");
            
            
            // Should be able to flatten nodes
            var flattened = JsonDataReader.FlattenTree(nodes).ToList();
            
            // Nothing should be pre-selected as we gave a null path
            var selected = flattened.Where(n=>n.Selected).ToList();
            Assert.That(selected, Is.Empty, "There were selected items with no selection path given");
            
            // Only level-1 nodes should be expanded
            var wrongExpansions = flattened.Where(n=>n.Expanded != (n.Depth == 0)).ToList();
            Assert.That(wrongExpansions, Is.Empty, "Some expansions were wrong");
        }

        [Test(Description="When a path is pre selected, we should get a flag on the selected item and each node in the path should be expanded")]
        public void existing_selection_should_be_expanded()
        {
            var lastPath = new[] { "", "Claimant", "MailingAddress", "Country", "Code"};
            
            var nodes = JsonDataReader.BuildDataSourcePicker(SampleIndexFiles.BasicFile, SampleData.Standard, lastPath, null, 0, true);
            
            Assert.That(nodes, Is.Not.Null, "Failed to extract picker data");
            
            var n1 = nodes.N("");
            Assert.That(n1?.Expanded, Is.True, "Data root should have been expanded");
            var n2 = n1?.Nodes.N("Claimant");
            Assert.That(n2?.Expanded, Is.True, "Claimant node should have been expanded");
            var n3 = n2?.Nodes.N("MailingAddress");
            Assert.That(n3?.Expanded, Is.True, "MailingAddress node should have been expanded");
            var n4 = n3?.Nodes.N("Country");
            Assert.That(n4?.Expanded, Is.True, "Country node should have been expanded");
            var n5 = n4?.Nodes.N("Code");
            Assert.That(n5?.Expanded, Is.True, "Code node should have been expanded");
            Assert.That(n5?.Selected, Is.True, "Code node should have been selected");
            
        }

        [Test(Description = "If we provide a repeater path, and that points at valid data, there should be a page repeated node in the resulting tree")]
        public void when_repeater_path_is_supplied_it_should_be_added_to_tree()
        {
            var lastPath = new string[] {};
            var repeatPath = new[] { "", "Reclaims"};
            
            var nodes = JsonDataReader.BuildDataSourcePicker(SampleIndexFiles.BasicFile, SampleData.Standard, lastPath, repeatPath, 0, true);
            
            Assert.That(nodes, Is.Not.Null, "Failed to extract picker data");
            
            var pageRepeatNode = nodes.N("D");
            Assert.That(pageRepeatNode, Is.Not.Null, "No repeat node in place");
            Assert.That(pageRepeatNode?.Text, Is.EqualTo("Page Repeat Data"), "Bad node text");
        }

        [Test(Description = "When a repeater path is pointing at an array with only one element, we still get a valid picker tree")]
        public void repeats_with_single_items_are_still_valid()
        {
            var lastPath = new string[] {};
            var repeatPath = new[] { "#", "HugeGroup"};
            
            var nodes = JsonDataReader.BuildDataSourcePicker(SampleIndexFiles.BasicFile, SampleData.Standard, lastPath, repeatPath, 0, true);
            
            Assert.That(nodes, Is.Not.Null, "Failed to extract picker data");
            
            var node = nodes.N("D");
            Assert.That(node?.Text, Is.EqualTo("Page Repeat Data"), "Bad node text");
            Assert.That(node?.Nodes.Count, Is.EqualTo(6), "Wrong child-node count"); // all the reclaims should be on page 1
            
            var subNodeData = nodes.X("D").X("[0]").N("NumberOfShares");
            Assert.That(subNodeData, Is.Not.Null, "Lost path to repeater data");
        }
    }
}