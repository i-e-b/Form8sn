using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Form8snCore.DataExtraction;
using Form8snCore.FileFormats;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace Form8snCore.Tests
{
    [TestFixture]
    public class MappingTests_SplitIntoN
    {
        [Test]
        public void Splitting_large_data_into_small_groups()
        {
            var splitParams = new Dictionary<string, string>{
                { nameof(MaxCountMappingParams.MaxCount), "2" }
            };
            var sourcePath = new[]{"","Reclaims"};
            var emptyFilterSet = new Dictionary<string, MappingInfo>();
            
            var obj = MappingActions.ApplyFilter(MappingType.SplitIntoN, splitParams, sourcePath, null, emptyFilterSet, SampleData.Standard, null, null);
            
            var actual = obj as IEnumerable;
            Assert.That(actual is null, Is.False, "Return type was not enumerable");
            var list = actual!.ToObjectList();
            
            Assert.That(list.Count, Is.EqualTo(3), "Wrong number of groups"); // 6 reclaims into groups of 2
        }
        
        
        [Test]
        public void Splitting_small_data_into_large_groups()
        {
            var splitParams = new Dictionary<string, string>{
                { nameof(MaxCountMappingParams.MaxCount), "100" }
            };
            var sourcePath = new[]{"","Reclaims"};
            var emptyFilterSet = new Dictionary<string, MappingInfo>();
            
            var obj = MappingActions.ApplyFilter(MappingType.SplitIntoN, splitParams, sourcePath, null, emptyFilterSet, SampleData.Standard, null, null);
            
            var actual = obj as IEnumerable;
            Assert.That(actual is null, Is.False, "Return type was not enumerable");
            var list = actual!.ToObjectList();
            
            Assert.That(list.Count, Is.EqualTo(1), "Wrong number of groups"); // 6 reclaims into groups of 100 (0 full, remainder 6)
            Assert.That(list[0] is IEnumerable, Is.True, "first result was not enumerable");
        }
    }
}