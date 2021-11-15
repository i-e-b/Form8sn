using System;
using System.Collections.Generic;
using System.Linq;
using Form8snCore.FileFormats;
using Form8snCore.HelpersAndConverters;
using NUnit.Framework;

namespace Form8snCore.Tests
{
    public static class NodeTestHelpers
    {
        public static List<DataNode>? X(this List<DataNode>? from, string name)
        {
            return from?.FirstOrDefault(n=>n.Name == name)?.Nodes;
        }
        public static DataNode? N(this List<DataNode>? from, string name)
        {
            return from?.FirstOrDefault(n=>n.Name == name);
        }
    }

    [TestFixture]
    public class JsonDataPickerTests
    {
        [Test]
        public void covert_test_data_to_picker_tree__data_paths__single_mode()
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
    }
}