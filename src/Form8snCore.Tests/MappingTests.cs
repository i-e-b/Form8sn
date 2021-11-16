using System.Collections;
using System.Collections.Generic;
using Form8snCore.DataExtraction;
using Form8snCore.FileFormats;
using Form8snCore.Tests.Helpers;
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

        [Test]
        public void TakeWords__if_supplied_a_string_chunks_on_whitespace()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>
            {
                { nameof(TakeMappingParams.Count), "3" },
            };

            var sourcePath = new[] { "", "Claimant", "FiscalAddress", "Country", "OfficialName" };
            var obj = MappingActions.ApplyFilter(MappingType.TakeWords, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo("United Kingdom of"), "Incorrect data");
        }

        [Test]
        public void TakeWords__with_string_if_asked_for_too_many_items_gives_all_data()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>
            {
                { nameof(TakeMappingParams.Count), "1000" },
            };

            var sourcePath = new[] { "", "Claimant", "FiscalAddress", "Country", "Name" };
            var obj = MappingActions.ApplyFilter(MappingType.TakeWords, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo("United Kingdom"), "Incorrect data");
        }

        [Test]
        public void TakeWords__if_supplied_an_array_chunks_on_items()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>
            {
                { nameof(TakeMappingParams.Count), "2" },
            };

            var sourcePath = new[] { "", "Reclaims" };
            var obj = MappingActions.ApplyFilter(MappingType.TakeWords, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null)
                as IList;

            Assert.That(obj?.Count, Is.EqualTo(2), "Wrong number of items");

            var stockId = ((obj?[0] as Dictionary<string, object>)?["Stock"] as Dictionary<string, object>)?["ID"] as string;
            Assert.That(stockId, Is.EqualTo("Stock 1"), "Couldn't find stock id in returned data");

            stockId = ((obj?[1] as Dictionary<string, object>)?["Stock"] as Dictionary<string, object>)?["ID"] as string;
            Assert.That(stockId, Is.EqualTo("Stock 2"), "Couldn't find stock id in returned data");
        }

        [Test]
        public void SkipWords__if_supplied_a_string_chunks_on_whitespace()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>
            {
                { nameof(SkipMappingParams.Count), "3" },
            };

            var sourcePath = new[] { "", "Claimant", "FiscalAddress", "Country", "OfficialName" };
            var obj = MappingActions.ApplyFilter(MappingType.SkipWords, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo("Great Britain and Northern Ireland (the)"), "Incorrect data");
        }

        [Test]
        public void SkipWords__with_string_if_asked_to_skip_too_many_items_gives_empty()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>
            {
                { nameof(SkipMappingParams.Count), "1000" },
            };

            var sourcePath = new[] { "", "Claimant", "FiscalAddress", "Country", "Name" };
            var obj = MappingActions.ApplyFilter(MappingType.SkipWords, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo(""), "Incorrect data");
        }

        [Test]
        public void SkipWords__if_supplied_an_array_chunks_on_items()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>
            {
                { nameof(SkipMappingParams.Count), "4" },
            };

            var sourcePath = new[] { "", "Reclaims" };
            var obj = MappingActions.ApplyFilter(MappingType.SkipWords, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null)
                as IList;

            Assert.That(obj?.Count, Is.EqualTo(2), "Wrong number of items");

            var stockId = ((obj?[0] as Dictionary<string, object>)?["Stock"] as Dictionary<string, object>)?["ID"] as string;
            Assert.That(stockId, Is.EqualTo("Stock 5"), "Couldn't find stock id in returned data");

            stockId = ((obj?[1] as Dictionary<string, object>)?["Stock"] as Dictionary<string, object>)?["ID"] as string;
            Assert.That(stockId, Is.EqualTo("Stock 6"), "Couldn't find stock id in returned data");
        }

        [Test]
        public void TakeAllValues__finds_all_items_in_sibling_paths_and_outputs_them_as_one__flat_list()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>();

            var sourcePath = new[] { "", "Reclaims", "[0]", "PaymentCurrency" };
            var obj = MappingActions.ApplyFilter(MappingType.TakeAllValues, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null)
                as IList;

            Assert.That(obj?.Count, Is.EqualTo(6), "Wrong item count");

            var valuesAsString = string.Join(",", obj.ToStringList());
            Assert.That(valuesAsString, Is.EqualTo("EUR,EUR,EUR,EUR,EUR,EUR"), "Wrong item data");
        }

        [Test]
        public void TakeAllValues__finds_all_items_deeper_in_paths()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>();

            var sourcePath = new[] { "", "Reclaims", "[0]", "Stock", "ID" }; //Note: we pick a specific item, and take-all-values looks for all `[n]` and iterates over them
            var obj = MappingActions.ApplyFilter(MappingType.TakeAllValues, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null)
                as IList;

            Assert.That(obj?.Count, Is.EqualTo(6), "Wrong item count");

            var valuesAsString = string.Join(",", obj.ToStringList());
            Assert.That(valuesAsString, Is.EqualTo("Stock 1,Stock 2,Stock 3,Stock 4,Stock 5,Stock 6"), "Wrong item data");
        }

        [Test]
        public void Concatenate__joins_data_directly_from_arrays()
        {
            var otherFilters = new Dictionary<string, MappingInfo>
            {
                {
                    "arrData", new MappingInfo
                    {
                        DataPath = new[] { "", "Reclaims", "[0]", "Stock", "ID" },
                        MappingType = MappingType.TakeAllValues
                    }
                }
            };

            var filterParams = new Dictionary<string, string>
            {
                { nameof(JoinMappingParams.Prefix), "<[" },
                { nameof(JoinMappingParams.Infix), " | " },
                { nameof(JoinMappingParams.Postfix), "]>" },
            };

            var sourcePath = new[] { "#", "arrData" };
            var obj = MappingActions.ApplyFilter(MappingType.Concatenate, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj, Is.EqualTo("<[Stock 1 | Stock 2 | Stock 3 | Stock 4 | Stock 5 | Stock 6]>"), "Incorrect data");
        }

        [Test]
        public void Concatenate__turns_any_data_into_strings()
        {
            var otherFilters = new Dictionary<string, MappingInfo>
            {
                {
                    "arrData", new MappingInfo
                    {
                        DataPath = new[] { "", "Reclaims", "[0]", "NumberOfShares" },
                        MappingType = MappingType.TakeAllValues
                    }
                }
            };

            var filterParams = new Dictionary<string, string>
            {
                { nameof(JoinMappingParams.Prefix), "< " },
                { nameof(JoinMappingParams.Infix), " | " },
                { nameof(JoinMappingParams.Postfix), " >" },
            };

            var sourcePath = new[] { "#", "arrData" };
            var obj = MappingActions.ApplyFilter(MappingType.Concatenate, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj, Is.EqualTo("< 55 | 55 | 55 | 55 | 55 | 55 >"), "Incorrect data");
        }

        [Test]
        public void Concatenate__as_single_item_is_output_as_a_string()
        {
            var otherFilters = new Dictionary<string, MappingInfo>
            {
                {
                    "arrData", new MappingInfo
                    {
                        DataPath = new[] { "", "Reclaims", "[0]", "NumberOfShares" },
                        MappingType = MappingType.TakeAllValues
                    }
                },
                {
                    "oneItem", new MappingInfo
                    {
                        DataPath = new[] { "#", "arrData" },
                        MappingParameters = new Dictionary<string, string>
                        {
                            { nameof(TakeMappingParams.Count), "1" }
                        },
                        MappingType = MappingType.TakeWords
                    }
                }
            };

            var filterParams = new Dictionary<string, string>
            {
                { nameof(JoinMappingParams.Prefix), "< " },
                { nameof(JoinMappingParams.Infix), " | " },
                { nameof(JoinMappingParams.Postfix), " >" },
            };

            var sourcePath = new[] { "#", "oneItem" };
            var obj = MappingActions.ApplyFilter(MappingType.Concatenate, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj, Is.EqualTo("< 55 >"), "Incorrect data");
        }

        [Test]
        public void Distinct__picks_all_items_on_path_and_returns_only_unique_values()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>();

            var sourcePath = new[] { "", "Reclaims", "[0]", "PaymentCurrency" };
            var obj = MappingActions.ApplyFilter(MappingType.Distinct, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null)
                as IList;

            Assert.That(obj?.Count, Is.EqualTo(1), "Wrong item count");

            var valuesAsString = string.Join(",", obj.ToStringList());
            Assert.That(valuesAsString, Is.EqualTo("EUR"), "Wrong item data");
        }

        [Test]
        public void Distinct__can_pick_items_in_deep_paths()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>();

            var sourcePath = new[] { "", "Reclaims", "[0]", "Stock", "ID" };
            var obj = MappingActions.ApplyFilter(MappingType.Distinct, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null)
                as IList;

            Assert.That(obj?.Count, Is.EqualTo(6), "Wrong item count");

            var valuesAsString = string.Join(",", obj.ToStringList());
            Assert.That(valuesAsString, Is.EqualTo("Stock 1,Stock 2,Stock 3,Stock 4,Stock 5,Stock 6"), "Wrong item data");
        }

        [Test]
        public void FormatAllAsDate__reformats_a_parsable_date()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string> { 
                {nameof(DateFormatMappingParams.FormatString), "ddd d MMM yyyy"}
            };

            var sourcePath = new[] { "", "Reclaims", "[0]", "PayDate" };
            var obj = MappingActions.ApplyFilter(MappingType.FormatAllAsDate, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null)
                as IList;

            Assert.That(obj?.Count, Is.EqualTo(6), "Should have selected all paths");
            
            Assert.That(obj?[0]?.ToString(), Is.EqualTo("Sat 1 Feb 2003"), "Bad date format");
        }
        
        [Test]
        public void FormatAllAsDate__returns_null_for_non_parsable_input()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string> { 
                {nameof(DateFormatMappingParams.FormatString), "ddd d MMM yyyy"}
            };

            var sourcePath = new[] { "", "Reclaims", "[0]", "PaymentCurrency" };
            var obj = MappingActions.ApplyFilter(MappingType.FormatAllAsDate, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj, Is.Null, "Should have rejected all paths");
        }
        
        [Test]
        public void FormatAllAsNumber__reformats_a_parsable_number_string()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string> { 
                {nameof(NumberMappingParams.Postfix), " =ONLY= "},
                {nameof(NumberMappingParams.Prefix), "£ "},
                {nameof(NumberMappingParams.DecimalPlaces), "2"},
                {nameof(NumberMappingParams.DecimalSeparator), "."},
                {nameof(NumberMappingParams.ThousandsSeparator), "'"},
            };

            var sourcePath = new[] { "", "Reclaims", "[0]", "Depot", "AccountNumber" };
            var obj = MappingActions.ApplyFilter(MappingType.FormatAllAsNumber, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null)
                as IList;

            Assert.That(obj?.Count, Is.EqualTo(6), "Should have selected all paths");
            
            Assert.That(obj?[0]?.ToString(), Is.EqualTo("£ 123'456'789.00 =ONLY= "), "Incorrect format");
        }
        [Test]
        public void FormatAllAsNumber__reformats_numeric_data()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string> { 
                {nameof(NumberMappingParams.Postfix), " =ONLY= "},
                {nameof(NumberMappingParams.Prefix), "£ "},
                {nameof(NumberMappingParams.DecimalPlaces), "2"},
                {nameof(NumberMappingParams.DecimalSeparator), "."},
                {nameof(NumberMappingParams.ThousandsSeparator), "'"},
            };

            var sourcePath = new[] { "", "Reclaims", "[0]", "DividendRate" };
            var obj = MappingActions.ApplyFilter(MappingType.FormatAllAsNumber, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null)
                as IList;

            Assert.That(obj?.Count, Is.EqualTo(6), "Should have selected all paths");
            
            Assert.That(obj?[0]?.ToString(), Is.EqualTo("£ 12.34 =ONLY= "), "Incorrect format");
        }
        [Test]
        public void FormatAllAsNumber__allows_zero_decimal_places()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string> { 
                {nameof(NumberMappingParams.DecimalPlaces), "0"},
            };

            var sourcePath = new[] { "", "Reclaims", "[0]", "DividendRate" };
            var obj = MappingActions.ApplyFilter(MappingType.FormatAllAsNumber, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null)
                as IList;

            Assert.That(obj?.Count, Is.EqualTo(6), "Should have selected all paths");
            
            Assert.That(obj?[0]?.ToString(), Is.EqualTo("12"), "Incorrect format");
        }
        [Test]
        public void FormatAllAsNumber__returns_null_for_non_parsable_input()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string> { 
                {nameof(NumberMappingParams.DecimalPlaces), "0"},
            };

            var sourcePath = new[] { "", "Reclaims", "[0]", "PaymentCurrency" };
            var obj = MappingActions.ApplyFilter(MappingType.FormatAllAsNumber, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj, Is.Null, "Should have rejected all paths");
        }

        [Test]
        public void IfElse__picks_item_based_on_a_string_comparison()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string> { 
                {nameof(IfElseMappingParams.ExpectedValue), "My GTRS Branch"},
                {nameof(IfElseMappingParams.Different), ".ForeignTaxAuthority.AuthorityName"},
                {nameof(IfElseMappingParams.Same), ".AuthorisedRepresentative.CompanyName"},
            };

            var sourcePath = new[] { "", "Branch" };
            var obj = MappingActions.ApplyFilter(MappingType.IfElse, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo("DefaultCompany"), "Wrong value selected");
            
            filterParams = new Dictionary<string, string> { 
                {nameof(IfElseMappingParams.ExpectedValue), "Something else?"},
                {nameof(IfElseMappingParams.Different), ".ForeignTaxAuthority.AuthorityName"},
                {nameof(IfElseMappingParams.Same), ".AuthorisedRepresentative.CompanyName"},
            };

            obj = MappingActions.ApplyFilter(MappingType.IfElse, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj?.ToString(), Is.EqualTo("ForeignTaxAuth"), "Wrong value selected");
        }

        [Test]
        public void Count__returns_number_of_items_found()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>();

            var sourcePath = new[] { "", "Reclaims" };
            var obj = MappingActions.ApplyFilter(MappingType.Count, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj, Is.EqualTo(6), "Wrong count");
        }

        [Test]
        public void Total__returns_sum_of_items_found()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>();

            var sourcePath = new[] { "", "Reclaims", "[0]", "Amounts", "ReclaimAmount"};
            var obj = MappingActions.ApplyFilter(MappingType.Total, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, null);

            Assert.That(obj, Is.EqualTo(794.079m), "Wrong sum");
        }

        [Test]// Note - the mapping context sums all values it sees as it builds pages.
        public void RunningTotal__returns_sum_of_count_in_mapping_context()
        {
            var otherFilters = new Dictionary<string, MappingInfo>();

            var filterParams = new Dictionary<string, string>();

            var sourcePath = new[] { "", "Reclaims", "[0]", "Amounts", "ReclaimAmount"};
            var totals = new Dictionary<string, decimal>{
                {".Reclaims.Amounts.ReclaimAmount", 123.00m}
            };
            var obj = MappingActions.ApplyFilter(MappingType.RunningTotal, filterParams, sourcePath, null, otherFilters, SampleData.Standard, null, totals);

            Assert.That(obj, Is.EqualTo(123.00m), "Wrong sum");
        }
    }
}