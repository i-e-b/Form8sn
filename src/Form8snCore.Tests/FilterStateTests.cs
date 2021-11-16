using System.Collections.Generic;
using Form8snCore.DataExtraction;
using Form8snCore.FileFormats;
using NUnit.Framework;

namespace Form8snCore.Tests
{
    [TestFixture]
    public class FilterStateTests
    {
        [Test]
        public void filter_redirects_prevent_recursion_overflow()
        {
            var subject = new FilterState();
            subject.FilterSet.Add("NewName", new MappingInfo());
            subject.FilterSet.Add("AnotherName", new MappingInfo());
            
            subject = subject.RedirectFilter("NewName");
            Assert.That(subject, Is.Not.Null, "state lost after valid redirect (1)");
            
            subject = subject!.RedirectFilter("AnotherName");
            Assert.That(subject, Is.Not.Null, "state lost after valid redirect (2)");
            
            subject = subject!.RedirectFilter("NewName"); // repeat of first redirect
            Assert.That(subject, Is.Null, "redirect should have been rejected, but was not");
        }

        [Test]
        public void filter_should_reject_unknown_redirects()
        {
            var subject = new FilterState();
            subject.FilterSet.Add("AnotherName", new MappingInfo());
            
            subject = subject.RedirectFilter("UnknownName");
            Assert.That(subject, Is.Null, "redirect should have been rejected, but was not");
        }

        [Test]
        public void redirect_should_copy_state_and_use_new_filter_params()
        {
            var rdObj = new object();
            var dataObj = new object();
            var subject = new FilterState
            {
                Type = MappingType.Total,
                Params = new Dictionary<string, string>{{"original","filter-settings"}},
                RepeaterData = rdObj,
                Data = dataObj,
                RunningTotals = new Dictionary<string, decimal>{
                    {"sample", 100m}
                },
                OriginalPath = new[] { "orig", "path" },
                SourcePath = new[] { "orig", "path" },
            };
            subject.FilterSet.Add("DifferentFilter", new MappingInfo{
                MappingType = MappingType.Count,
                DataPath = new[]{"new","data"},
                MappingParameters = new Dictionary<string, string>{{"changed","params"}}
            });
            
            var redirected = subject.RedirectFilter("DifferentFilter");
            Assert.That(redirected, Is.Not.Null, "redirect failed");
            
            Assert.That(redirected!.Type, Is.EqualTo(MappingType.Count), "did not redirect type");
            Assert.That(redirected.Params["changed"], Is.EqualTo("params"), "did not redirect params");
            Assert.That(redirected.SourcePath?[0], Is.EqualTo("new"), "did not redirect data path");
            
            Assert.That(redirected.Data, Is.EqualTo(dataObj), "reference to data was broken");
            Assert.That(redirected.RepeaterData, Is.EqualTo(rdObj), "reference to repeater-data was broken");
            Assert.That(redirected.OriginalPath?[0], Is.EqualTo("orig"), "original path was not retained");
            Assert.That(redirected.RunningTotals?["sample"], Is.EqualTo(100m), "running totals were damaged");
        }
        
        
        [Test]
        public void changing_path_should_clear_filter_and_update_path()
        {
            var rdObj = new object();
            var dataObj = new object();
            var subject = new FilterState
            {
                Type = MappingType.Total,
                Params = new Dictionary<string, string>{{"original","filter-settings"}},
                RepeaterData = rdObj,
                Data = dataObj,
                RunningTotals = new Dictionary<string, decimal>{
                    {"sample", 100m}
                },
                OriginalPath = new[] { "orig", "path" },
                SourcePath = new[] { "orig", "path" },
            };
            subject.FilterSet.Add("DifferentFilter", new MappingInfo{
                MappingType = MappingType.Count,
                DataPath = new[]{"new","data"},
                MappingParameters = new Dictionary<string, string>{{"changed","params"}}
            });
            
            var changed = subject.NewPath(new []{"different","path"});
            Assert.That(changed, Is.Not.Null, "re-path failed");
            
            Assert.That(changed!.Type, Is.EqualTo(MappingType.None), "did not clear type");
            Assert.That(changed.SourcePath?[0], Is.EqualTo("different"), "did not change data path");
            
            Assert.That(changed.Params["original"], Is.EqualTo("filter-settings"), "did not copy params");
            Assert.That(changed.Data, Is.EqualTo(dataObj), "reference to data was broken");
            Assert.That(changed.RepeaterData, Is.EqualTo(rdObj), "reference to repeater-data was broken");
            Assert.That(changed.OriginalPath?[0], Is.EqualTo("orig"), "original path was not retained");
            Assert.That(changed.RunningTotals?["sample"], Is.EqualTo(100m), "running totals were damaged");
        }
    }
}