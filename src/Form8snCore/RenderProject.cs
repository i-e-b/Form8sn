using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Containers;
using Containers.Types;
using Form8snCore.DataExtraction;
using Form8snCore.FileFormats;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SkinnyJson;

namespace Form8snCore
{
    public class RenderProject
    {
        private const string FallbackFontFamily = "Courier New";
        private const XFontStyle BaseFontStyle = XFontStyle.Bold;
        private static readonly Stopwatch _layoutTimer = new Stopwatch();
        private static readonly Stopwatch _loadingTimer = new Stopwatch();
        private static readonly Stopwatch _filterTimer = new Stopwatch();
        private static readonly Stopwatch _renderTimer = new Stopwatch();
        private static readonly Stopwatch _totalTimer = new Stopwatch();
        
        private static readonly Dictionary<int, XFont> _fontCache = new Dictionary<int, XFont>();
        private static string? _baseFontFamily;

        private static XFont GetFont(int size)
        {
            if (_fontCache.ContainsKey(size)) return _fontCache[size];
            _fontCache.Add(size, new XFont(_baseFontFamily ?? FallbackFontFamily, size, BaseFontStyle));
            return _fontCache[size];
        }

        public static RenderResultInfo ToFile(string outputFilePath, string dataFilePath, Project project)
        {
            _layoutTimer.Reset();
            _loadingTimer.Reset();
            _filterTimer.Reset();
            _renderTimer.Reset();
            _totalTimer.Restart();

            _loadingTimer.Start();
            _baseFontFamily = project.Index.FontName;
            var result = new RenderResultInfo {Success = false};
            if (!File.Exists(dataFilePath))
            {
                result.ErrorMessage = "Could not find input data file";
                return result;
            }

            var data = Json.Defrost(File.ReadAllText(dataFilePath)!);
            _loadingTimer.Stop();

            var mapper = new DataMapper(project, data);
            _renderTimer.Start();
            var document = new PdfDocument();
            document.Info.Author = "Generated from data by Form8sn";
            document.Info.Title = project.Index.Name;
            document.Info.CreationDate = DateTime.UtcNow;
            _renderTimer.Stop();

            var runningTotals = new Dictionary<string, decimal>(); // path -> total

            // TODO: if we have any 'page total count' filters, we will need to count pages. Either before, or go back and edit afterwards
            for (var pageIndex = 0; pageIndex < project.Index.Pages.Count; pageIndex++)
            {
                _loadingTimer.Start();
                var pageDef = project.Index.Pages[pageIndex];
                using var image = XImage.FromFile(pageDef.GetBackgroundPath(project)); // we need to load the image regardless, to get the pixel size
                var font = GetFont(pageDef.PageFontSize ?? project.Index.BaseFontSize ?? 16);
                _loadingTimer.Stop();

                if (pageDef.RepeatMode.Repeats)
                {
                    _filterTimer.Start();
                    var dataSets = mapper.GetRepeatData(pageDef.RepeatMode.DataPath);
                    _filterTimer.Stop();
                    for (int repeatIndex = 0; repeatIndex < dataSets.Count; repeatIndex++)
                    {
                        var repeatData = dataSets[repeatIndex];
                        mapper.SetRepeater(repeatData, pageDef.RepeatMode.DataPath);
                        var pageResult = OutputStandardPage(document, pageDef, mapper, pageIndex, font, image, runningTotals);
                        ClearPageTotals(runningTotals);
                        if (pageResult.IsFailure)
                        {
                            result.ErrorMessage = pageResult.FailureMessage;
                            return result;
                        }

                        mapper.ClearRepeater();
                    }
                }
                else
                {
                    var pageResult = OutputStandardPage(document, pageDef, mapper, pageIndex, font, image, runningTotals);
                    ClearPageTotals(runningTotals);
                    if (pageResult.IsFailure)
                    {
                        result.ErrorMessage = pageResult.FailureMessage;
                        return result;
                    }
                }
            }

            _renderTimer.Start();
            document.Save(outputFilePath);
            _renderTimer.Stop();
            _totalTimer.Stop();

            result.Success = true;

            result.OverallTime = _totalTimer.Elapsed;
            result.LayoutTime = _layoutTimer.Elapsed;
            result.LoadingTime = _loadingTimer.Elapsed;
            result.FilterApplicationTime = _filterTimer.Elapsed;
            result.FinalRenderTime = _renderTimer.Elapsed;

            return result;
        }

        private static void ClearPageTotals(Dictionary<string,decimal> runningTotals)
        {
            var keys = runningTotals.Keys.Where(key => key.StartsWith("D.")).ToArray();
            foreach (var key in keys)
            {
                runningTotals[key] = 0.0m;
            }
        }

        private static Result<Nothing> OutputStandardPage(PdfDocument document, TemplatePage pageDef, DataMapper mapper,
            int pageIndex, XFont font, XImage image, Dictionary<string, decimal> runningTotals)
        {
            _renderTimer.Start();
            var page = document.AddPage();
            _renderTimer.Stop();
            // If dimensions are silly, reset to A4
            if (pageDef.WidthMillimetres < 10 || pageDef.WidthMillimetres > 1000) pageDef.WidthMillimetres = 210;
            if (pageDef.HeightMillimetres < 10 || pageDef.HeightMillimetres > 1000) pageDef.HeightMillimetres = 297;


            // Set the PDF page size (in points under the hood)
            page.Width = XUnit.FromMillimeter(pageDef.WidthMillimetres);
            page.Height = XUnit.FromMillimeter(pageDef.HeightMillimetres);

            _renderTimer.Start();
            using var gfx = XGraphics.FromPdfPage(page);
            _renderTimer.Stop();

            // Draw background at full page size
            _loadingTimer.Start();
            if (pageDef.RenderBackground)
            {
                var destRect = new RectangleF(0, 0, (float) page.Width.Point, (float) page.Height.Point);
                gfx.DrawImage(image, destRect);
            }
            _loadingTimer.Stop();

            // Work out the bitmap -> page adjustment fraction
            var fx = page.Width.Point / image.PixelWidth;
            var fy = page.Height.Point / image.PixelHeight;

            // Draw each box. We sort the boxes (currently by filter type) so running totals can work as expected
            foreach (var boxDef in pageDef.Boxes.Where(HasAValue).OrderBy(OrderBoxes))
            {
                var result = RenderBox(mapper, pageIndex, font, boxDef, fx, fy, gfx, runningTotals);
                if (result.IsFailure) return result;
            }

            return Result.Success();
        }

        private static bool HasAValue(KeyValuePair<string, TemplateBox> boxDef)
        {
            return boxDef.Value.MappingPath != null;
        }

        private static int OrderBoxes(KeyValuePair<string, TemplateBox> boxDef)
        {
            const int explicitOrderGap = 10000;
            
            // if we have an explicit order, use that (explicits always go first)
            if (boxDef.Value.BoxOrder != null) return boxDef.Value.BoxOrder.Value;
            
            // otherwise, apply a default ordering.
            var path = boxDef.Value.MappingPath;
            if (path == null || path.Length < 1) return explicitOrderGap - 3; // unmapped first (they will get ignored anyway)
            if (path[0] == "") return explicitOrderGap - 2; // raw data paths next
            if (path[0] == "D") return explicitOrderGap - 1; // next, page data
            if (path[0] == "#") // finally filters
            {
                if (!Enum.TryParse<MappingType>(path[1], out var mappingType)) return explicitOrderGap; // unknown filters
                return explicitOrderGap + (int)mappingType;
            }
            return explicitOrderGap; // shouldn't be hit
        }

        private static Result<Nothing> RenderBox(DataMapper mapper, int pageIndex, XFont baseFont, KeyValuePair<string, TemplateBox> boxDef,
            double fx, double fy, XGraphics gfx, Dictionary<string, decimal> runningTotals)
        {
            var box = boxDef.Value;
            var font = (box.BoxFontSize != null) ? GetFont(box.BoxFontSize.Value) : baseFont;

            // Read data, apply filters, apply display formatting
            _filterTimer.Start();
            var str = mapper.FindBoxData(box, pageIndex, runningTotals);
            if (str == null) return Result.Success(); // empty data is considered OK

            if (box.DisplayFormat != null)
            {
                str = DisplayFormatter.ApplyFormat(box, str);
                if (str == null) return Result.Failure($"Formatter failed: {box.DisplayFormat.Type} applied on {string.Join(".", box.MappingPath!)}");
            }

            _filterTimer.Stop();

            _layoutTimer.Start();
            // boxes are defined in terms of the background image pixels, so we need to convert
            var boxWidth = box.Width * fx;
            var boxHeight = box.Height * fy;
            var space = new XRect(box.Left * fx, box.Top * fy, boxWidth, boxHeight);
            var align = MapAlignments(box);

            RenderTextInBox(font, gfx, box, fx, fy, str, space, align);
            _layoutTimer.Stop();
            return Result.Success();
        }

        private static void RenderTextInBox(XFont font, XGraphics gfx, TemplateBox box, double fx, double fy, string str, XRect space, XStringFormat align)
        {
            var boxLeft = box.Left * fx;
            var boxWidth = box.Width * fx;

            var boxHeight = box.Height * fy;


            var size = gfx.MeasureString(str, font);
            if (box.ShrinkToFit && box.WrapText && (size.Width > boxWidth || size.Height > boxHeight)) // try to change the string to match box aspect
            {
                var altFont = ShrinkFontAndWrapStringToFit(str, boxWidth, boxHeight, gfx, font, out var lines);

                RenderWrappedLines(altFont, gfx, box, fy, align, lines, boxLeft, boxWidth);
            }
            else if (box.WrapText && size.Width > boxWidth)
            {
                WrapStringToFit(SplitByWhitespace(str), boxWidth, gfx, font, out var lines);

                RenderWrappedLines(font, gfx, box, fy, align, lines, boxLeft, boxWidth);
            }
            else if (box.ShrinkToFit && (size.Width > boxWidth || size.Height > boxHeight))
            {
                var altFont = ShrinkFontToFit(str, boxWidth, boxHeight, gfx, font, size);

                gfx.DrawString(str, altFont, XBrushes.Black, space, align);
            }
            else
            {
                gfx.DrawString(str, font, XBrushes.Black, space, align);
            }
        }

        private static void RenderWrappedLines(XFont font, XGraphics gfx, TemplateBox box, double fy, XStringFormat align, List<MeasuredLine> lines, double boxLeft, double boxWidth)
        {
            var top = CalculateTop(box, fy, lines);
            foreach (var line in lines)
            {
                var lineRect = new XRect(boxLeft, top, boxWidth, line.Height);
                gfx.DrawString(line.Text, font, XBrushes.Black, lineRect, align);
                top += line.Height;
            }
        }

        private static double CalculateTop(TemplateBox box, double fy, List<MeasuredLine> lines)
        {
            var textHeight = lines.Sum(l => l.Height);
            var top = box.Top * fy;
            var bottom = top + (box.Height * fy);
            var middle = top + (box.Height * fy * 0.5);

            switch (box.Alignment)
            {
                case TextAlignment.TopLeft:
                case TextAlignment.TopCentre:
                case TextAlignment.TopRight:
                    return top;

                case TextAlignment.MidlineLeft:
                case TextAlignment.MidlineCentre:
                case TextAlignment.MidlineRight:
                    return middle - (textHeight * 0.5);

                case TextAlignment.BottomLeft:
                case TextAlignment.BottomCentre:
                case TextAlignment.BottomRight:
                    return bottom - textHeight;

                default:
                    return top;
            }
        }

        private static void WrapStringToFit(List<string> words, double boxWidth, XGraphics gfx, XFont font, out List<MeasuredLine> lines)
        {
            lines = new List<MeasuredLine>();
            var widthRemains = boxWidth;
            var lineHeight = 0.0;

            // Keep adding chunks until one doesn't fit, then break line
            var sb = new StringBuilder();
            for (int i = 0; i < words.Count; i++)
            {
                var bitSize = gfx.MeasureString(words[i], font);
                lineHeight = Math.Max(lineHeight, bitSize.Height);
                var wordWidth = bitSize.Width;

                if (wordWidth <= widthRemains) // next word fits on the line
                {
                    sb.Append(words[i]);
                    widthRemains -= wordWidth;
                    continue;
                }

                // need to wrap
                // output the line, and start another
                lines.Add(new MeasuredLine(sb.ToString(), lineHeight));
                sb.Clear();
                sb.Append(words[i]);
                lineHeight = bitSize.Height;
                widthRemains = boxWidth - bitSize.Width;
            }

            if (sb.Length > 0) // last line
                lines.Add(new MeasuredLine(sb.ToString(), lineHeight));
        }

        private static XFont ShrinkFontAndWrapStringToFit(string str, double boxWidth, double boxHeight, XGraphics gfx, XFont font, out List<MeasuredLine> lines)
        {
            var bits = SplitByWhitespace(str);
            // Not going to bother with justification, just go ragged edge. Let the normal alignment do its thing.

            var baseSize = font.Size;
            XFont altFont;
            while (true)
            {
                baseSize -= baseSize / 10;
                altFont = GetFont((int)baseSize);

                var widthRemains = boxWidth;
                var heightAccumulated = 0.0;
                var lineMaxHeight = 0.0;
                var sizeIsAcceptable = true;

                // Keep adding chunks until one doesn't fit, then break line
                for (int i = 0; i < bits.Count; i++)
                {
                    var bitSize = gfx.MeasureString(bits[i], altFont);
                    lineMaxHeight = Math.Max(bitSize.Height, lineMaxHeight);
                    var wordWidth = bitSize.Width;

                    if (wordWidth <= widthRemains)
                    {
                        widthRemains -= wordWidth;
                        continue; // not run out of line space
                    }

                    // need to wrap
                    widthRemains = boxWidth - bitSize.Width;
                    heightAccumulated += lineMaxHeight;
                    lineMaxHeight = 0.0;

                    if (heightAccumulated <= boxHeight) continue; // not run out of vertical space

                    sizeIsAcceptable = false;
                    break; // out of the for(...bits...)
                }

                // Handle last line dangle
                if (lineMaxHeight > 0 && heightAccumulated + lineMaxHeight > boxHeight) sizeIsAcceptable = false;

                if (sizeIsAcceptable) break; // out of while(true)
                if (baseSize < 4) break; // font size is getting too small. Abandon.
            }

            // Now do the real wrapping with the font size we found...
            WrapStringToFit(bits, boxWidth, gfx, altFont, out lines);

            return altFont;
        }

        private static List<string> SplitByWhitespace(string str)
        {
            var outp = new List<string>();
            var sb = new StringBuilder();
            var firstWhitespace = true;

            foreach (var c in str)
            {
                if (!char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                    firstWhitespace = true;
                    continue;
                }

                if (firstWhitespace) sb.Append(' ');
                firstWhitespace = false;

                outp.Add(sb.ToString());
                sb.Clear();
            }

            if (sb.Length > 0) outp.Add(sb.ToString());

            return outp;
        }

        private static XFont ShrinkFontToFit(string str, double boxWidth, double boxHeight, XGraphics gfx, XFont font, XSize size)
        {
            var baseSize = font.Size;
            var altFont = GetFont((int)baseSize);
            while (size.Width > boxWidth || size.Height > boxHeight)
            {
                baseSize -= baseSize / 10;
                altFont = GetFont((int)baseSize);
                size = gfx.MeasureString(str, altFont);
                if (baseSize < 4) break;
            }

            return altFont;
        }

        private static XStringFormat MapAlignments(TemplateBox box)
        {
            switch (box.Alignment)
            {
                case TextAlignment.TopLeft: return XStringFormats.TopLeft;
                case TextAlignment.TopCentre: return XStringFormats.TopCenter;
                case TextAlignment.TopRight: return XStringFormats.TopRight;
                case TextAlignment.MidlineLeft: return XStringFormats.CenterLeft;
                case TextAlignment.MidlineCentre: return XStringFormats.Center;
                case TextAlignment.MidlineRight: return XStringFormats.CenterRight;
                case TextAlignment.BottomLeft: return XStringFormats.BottomLeft;
                case TextAlignment.BottomCentre: return XStringFormats.BottomCenter;
                case TextAlignment.BottomRight: return XStringFormats.BottomRight;
                default: return XStringFormats.Default;
            }
        }
    }

    public class RenderResultInfo
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public TimeSpan LoadingTime { get; set; }
        public TimeSpan LayoutTime { get; set; }
        public TimeSpan FilterApplicationTime { get; set; }
        public TimeSpan FinalRenderTime { get; set; }
        public TimeSpan OverallTime { get; set; }
    }

    internal class MeasuredLine
    {
        public MeasuredLine(string text, double height)
        {
            Text = text;
            Height = height;
        }

        public string Text { get; }
        public double Height { get; }
    }
}