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
    public class MappingTests
    {
        [Test]
        public void SplitIntoN__Splitting_large_data_into_small_groups()
        {
            var splitParams = new Dictionary<string, string>
            {
                { nameof(MaxCountMappingParams.MaxCount), "2" }
            };
            var sourcePath = new[] { "", "Reclaims" };
            var emptyFilterSet = new Dictionary<string, MappingInfo>();

            var obj = MappingActions.ApplyFilter(MappingType.SplitIntoN, splitParams, sourcePath, null, emptyFilterSet, SampleData.Standard, null, null);

            var actual = obj as IEnumerable;
            Assert.That(actual is null, Is.False, "Return type was not enumerable");
            var list = actual!.ToObjectList();

            Assert.That(list.Count, Is.EqualTo(3), "Wrong number of groups"); // 6 reclaims into groups of 2
        }

        [Test]
        public void SplitIntoN__Splitting_small_data_into_large_groups()
        {
            var splitParams = new Dictionary<string, string>
            {
                { nameof(MaxCountMappingParams.MaxCount), "100" }
            };
            var sourcePath = new[] { "", "Reclaims" };
            var emptyFilterSet = new Dictionary<string, MappingInfo>();

            var obj = MappingActions.ApplyFilter(MappingType.SplitIntoN, splitParams, sourcePath, null, emptyFilterSet, SampleData.Standard, null, null);

            var actual = obj as IEnumerable;
            Assert.That(actual is null, Is.False, "Return type was not enumerable");
            var list = actual!.ToObjectList();

            Assert.That(list.Count, Is.EqualTo(1), "Wrong number of groups"); // 6 reclaims into groups of 100 (0 full, remainder 6)
            Assert.That(list[0] is IEnumerable, Is.True, "first result was not enumerable");
        }

        [Test]
        public void None__Passthrough_filter()
        {
            var filterParams = new Dictionary<string, string>();
            var sourcePath = new[] { "", "Claimant", "Name" };
            var emptyFilterSet = new Dictionary<string, MappingInfo>();

            var obj = MappingActions.ApplyFilter(MappingType.None, filterParams, sourcePath, null, emptyFilterSet, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo("FirstName Surname"), "Incorrect data");
        }

        [Test]
        public void FixedValue__return_a_string_regardless_of_source_data()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();
            var filterParams = new Dictionary<string, string>
            {
                { nameof(TextMappingParams.Text), "Hello, world" }
            };

            // Should work with no path selected
            var sourcePath = new string[] { };
            var obj = MappingActions.ApplyFilter(MappingType.FixedValue, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo("Hello, world"), "Incorrect data");

            // Should get the same result, even if a path is provided
            sourcePath = new[] { "", "Claimant", "Name" };
            obj = MappingActions.ApplyFilter(MappingType.FixedValue, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo("Hello, world"), "Incorrect data");
        }

        [Test]
        public void Join__can_combine_two_paths()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();
            var filterParams = new Dictionary<string, string>
            {
                { nameof(JoinPathsMappingParams.Infix), " - " },
                { nameof(JoinPathsMappingParams.ExtraData), ".Branch" }
            };

            var sourcePath = new[] { "", "Claimant", "Name" };
            var obj = MappingActions.ApplyFilter(MappingType.Join, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo("FirstName Surname - My GTRS Branch"), "Incorrect data");
        }

        [Test]
        public void Join__if_extra_data_is_missing_no_infix_is_added()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();
            var filterParams = new Dictionary<string, string>
            {
                { nameof(JoinPathsMappingParams.Infix), "!!!" },
                { nameof(JoinPathsMappingParams.ExtraData), ".not.a.real.path" }
            };

            var sourcePath = new[] { "", "Claimant", "Name" };
            var obj = MappingActions.ApplyFilter(MappingType.Join, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo("FirstName Surname"), "Incorrect data");
        }

        [Test]
        public void Join__if_extra_data_is_present_but_empty_no_infix_is_added()
        {
            var otherFilters = new Dictionary<string, MappingInfo>
            {
                {
                    "BlankData", new MappingInfo
                    {
                        MappingType = MappingType.FixedValue,
                        MappingParameters = new Dictionary<string, string> { { nameof(TextMappingParams.Text), "" } }
                    }
                }
            };

            var filterParams = new Dictionary<string, string>
            {
                { nameof(JoinPathsMappingParams.Infix), "!!!" },
                { nameof(JoinPathsMappingParams.ExtraData), "#.BlankData" }
            };

            var sourcePath = new[] { "", "Claimant", "Name" };
            var obj = MappingActions.ApplyFilter(MappingType.Join, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo("FirstName Surname"), "Incorrect data");
        }
    }
}