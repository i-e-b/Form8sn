using System.Collections.Generic;
using Form8snCore.DataExtraction;
using Form8snCore.FileFormats;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Form8snCore.Tests
{
    [TestFixture]
    public class DisplayFormatTests
    {
        [Test]
        public void Null__undefined_format_passes_data_through()
        {
            string input = "my input";
            
            var expected = "my input";
            var actual = DisplayFormatter.ApplyFormat(null, input);
            
            Assert.That(actual, Is.EqualTo(expected));
        }
        
        [Test]
        public void None__default_format_passes_data_through()
        {
            string input = "my input";
            var format = new DisplayFormatFilter{
                Type = DisplayFormatType.None,
                FormatParameters = new Dictionary<string, string>()
            };
            
            var expected = "my input";
            var actual = DisplayFormatter.ApplyFormat(format, input);
            
            Assert.That(actual, Is.EqualTo(expected));
        }
        
        [Test] // Note: render image is used as a flag in the render phase. This format doesn't change the underlying data
        public void RenderImage__passes_data_through()
        {
            string input = "my input";
            var format = new DisplayFormatFilter{
                Type = DisplayFormatType.RenderImage,
                FormatParameters = new Dictionary<string, string>()
            };
            
            var expected = "my input";
            var actual = DisplayFormatter.ApplyFormat(format, input);
            
            Assert.That(actual, Is.EqualTo(expected));
        }
        
        [Test]
        public void DateFormat__reformats_parsable_date_input()
        {
            string input = "2019-04-11";
            var format = new DisplayFormatFilter{
                Type = DisplayFormatType.DateFormat,
                FormatParameters = new Dictionary<string, string>{
                    {nameof(DateDisplayParams.FormatString), "dddd d MMM yyyy"}
                }
            };
            
            var expected = "Thursday 11 Apr 2019";
            var actual = DisplayFormatter.ApplyFormat(format, input);
            
            Assert.That(actual, Is.EqualTo(expected));
        }
        
        [Test]
        public void DateFormat__returns_null_for_invalid_inputs()
        {
            string input = "ceci nest pas un date";
            var format = new DisplayFormatFilter{
                Type = DisplayFormatType.DateFormat,
                FormatParameters = new Dictionary<string, string>{
                    {nameof(DateDisplayParams.FormatString), "dddd d MMM yyyy"}
                }
            };
            
            var actual = DisplayFormatter.ApplyFormat(format, input);
            
            Assert.That(actual, Is.Null);
        }
        
        [Test]
        [TestCase("1.2345","=$1.235=")]
        [TestCase("12.345","=$12.345=")]
        [TestCase("123.45","=$123.450=")]
        [TestCase("1234.5","=$1'234.500=")]
        [TestCase("12345","=$12'345.000=")]
        [TestCase("123450","=$123'450.000=")]
        [TestCase("1234500","=$1'234'500.000=")]
        public void NumberFormat__reformats_numbers(string input, string expected)
        {
            var format = new DisplayFormatFilter{
                Type = DisplayFormatType.NumberFormat,
                FormatParameters = new Dictionary<string, string>{
                    {nameof(NumberDisplayParams.Prefix), "=$"},
                    {nameof(NumberDisplayParams.Postfix), "="},
                    {nameof(NumberDisplayParams.DecimalPlaces), "3"},
                    {nameof(NumberDisplayParams.DecimalSeparator), "."},
                    {nameof(NumberDisplayParams.ThousandsSeparator), "'"},
                }
            };
            
            var actual = DisplayFormatter.ApplyFormat(format, input);
            
            Assert.That(actual, Is.EqualTo(expected));
        }
        
        [Test]
        public void NumberFormat__returns_null_for_invalid_input()
        {
            var input = "Little house on the prairie";
            var format = new DisplayFormatFilter{
                Type = DisplayFormatType.NumberFormat,
                FormatParameters = new Dictionary<string, string>{
                    {nameof(NumberDisplayParams.Prefix), "=$"},
                    {nameof(NumberDisplayParams.Postfix), "="},
                    {nameof(NumberDisplayParams.DecimalPlaces), "3"},
                    {nameof(NumberDisplayParams.DecimalSeparator), "."},
                    {nameof(NumberDisplayParams.ThousandsSeparator), "'"},
                }
            };
            
            var actual = DisplayFormatter.ApplyFormat(format, input);
            
            Assert.That(actual, Is.Null);
        }
        
        [Test]
        [TestCase("0.12345","0")]
        [TestCase("1.2345","1")]
        [TestCase("12.345","12")]
        [TestCase("123.45","123")]
        [TestCase("1234.5","1234")]
        [TestCase("12345.6","12345")]
        [TestCase("123456","123456")]
        [TestCase("1234560","1234560")]
        public void Integral__truncates_numbers_with_no_rounding(string input, string expected)
        {
            var format = new DisplayFormatFilter{
                Type = DisplayFormatType.Integral,
                FormatParameters = new Dictionary<string, string>()
            };
            
            var actual = DisplayFormatter.ApplyFormat(format, input);
            
            Assert.That(actual, Is.EqualTo(expected));
        }
        
        
        [Test]
        [TestCase("0.001","3", "001")]
        [TestCase("0.0","3", "000")]
        [TestCase("10.09","1", "1")]
        [TestCase("12.346","2","35")]
        [TestCase("123.45","2","45")]
        [TestCase("1234.5","2", "50")]
        [TestCase("12345.6","2","60")]
        [TestCase("123456","2","00")]
        public void Fractional__displays_the_fractional_part_with_exact_number_of_places_and_rounding(string input, string places, string expected)
        {
            var format = new DisplayFormatFilter{
                Type = DisplayFormatType.Fractional,
                FormatParameters = new Dictionary<string, string>{
                    {nameof(FractionalDisplayParams.DecimalPlaces), places}
                }
            };
            
            var actual = DisplayFormatter.ApplyFormat(format, input);
            
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}