using System.Collections.Generic;
using Form8snCore.DataExtraction;
using Form8snCore.FileFormats;
using Form8snCore.Rendering;
using Form8snCore.Tests.Helpers;
using NUnit.Framework;

namespace Form8snCore.Tests
{
    [TestFixture]
    public class DataMapperTests
    {
        [Test]
        public void finding_normal_data_for_a_page_box()
        {
            var totals = new Dictionary<string, decimal>();
            
            var subject = new DataMapper(SampleProjectFiles.BasicFile, SampleData.Standard);

            var box = SampleProjectFiles.Basic_Page_Boxes["Box 1"];
            
            var result = subject.TryFindBoxData(box, 0, totals);
            
            Assert.That(result, Is.EqualTo("FirstName Surname"), "data did not map as expected");
        }
        
        [Test]
        public void finding_document_filter_data_for_a_page_box()
        {
            var totals = new Dictionary<string, decimal>();
            
            var subject = new DataMapper(SampleProjectFiles.BasicFile, SampleData.Standard);

            var box = SampleProjectFiles.Basic_Page_Boxes["BoxWithDocRef"];
            
            var result = subject.TryFindBoxData(box, 0, totals);
            
            Assert.That(result, Is.EqualTo("794.0790"), "data did not map as expected");
            Assert.That(totals["#.Doc Filter to be referenced"], Is.EqualTo(794.0790m), "mapped did not increment totals");
        }
        
        [Test]
        public void finding_page_filter_data_for_a_page_box()
        {
            var totals = new Dictionary<string, decimal>();
            
            var subject = new DataMapper(SampleProjectFiles.BasicFile, SampleData.Standard);

            var box = SampleProjectFiles.Basic_Page_Boxes["BoxWithPageRef"];
            
            var result = subject.TryFindBoxData(box, 0, totals);
            
            Assert.That(result, Is.EqualTo("794.0790-DefaultCompany"), "data did not map as expected");
        }
        
        [Test]
        public void should_not_find_a_filter_for_a_different_page()
        {
            var totals = new Dictionary<string, decimal>();
            
            var subject = new DataMapper(SampleProjectFiles.BasicFile, SampleData.Standard);

            var box = SampleProjectFiles.Basic_Page_Boxes["BoxWithPageRef"]; // references page index 0
            
            var result = subject.TryFindBoxData(box, 1, totals);             // uses page index 1
            
            Assert.That(result, Is.Null, "data did not map as expected");
        }

        [Test]
        public void reading_page_repeat_data()
        {
            var subject = new DataMapper(SampleProjectFiles.BasicFile, SampleData.Standard);
            
            var result = subject.GetRepeatData(new []{"","Reclaims"});
            
            Assert.That(result, Is.Not.Null, "data did not map");
            Assert.That(result.Count, Is.EqualTo(6), "data did not map as expected");
        }

        [Test]
        public void render_image_should_trigger_a_special_type()
        {
            var box = new TemplateBox{
                MappingPath = new []{"", "Item"},
                DisplayFormat = new DisplayFormatFilter{
                    Type = DisplayFormatType.RenderImage
                }
            };
            var result = DataMapper.IsSpecialValue(box, out var type);
            Assert.That(result, Is.True, "did not trigger");
            Assert.That(type, Is.EqualTo(DocumentBoxType.EmbedJpegImage), "wrong special type");
        }
        
        
        [Test]
        [TestCase(nameof(DocumentBoxType.CurrentPageNumber))]
        [TestCase(nameof(DocumentBoxType.PageGenerationDate))]
        [TestCase(nameof(DocumentBoxType.RepeatingPageNumber))]
        [TestCase(nameof(DocumentBoxType.TotalPageCount))]
        [TestCase(nameof(DocumentBoxType.RepeatingPageTotalCount))]
        public void page_info_types_should_trigger_a_special_type(string name)
        {
            var box = new TemplateBox{
                MappingPath = new []{"P", name}
            };
            var result = DataMapper.IsSpecialValue(box, out var type);
            Assert.That(result, Is.True, "did not trigger");
            Assert.That(type.ToString(), Is.EqualTo(name), "wrong special type");
        }
        
        
        [Test]
        public void normal_paths_should_not_be_treated_as_special_types()
        {
            var box = new TemplateBox{
                MappingPath = new []{"", "Anything"}
            };
            var result = DataMapper.IsSpecialValue(box, out var type);
            Assert.That(result, Is.False, "was marked as special, but should not have been");
            Assert.That(type, Is.EqualTo(DocumentBoxType.Normal), "wrong type");
        }
    }
}