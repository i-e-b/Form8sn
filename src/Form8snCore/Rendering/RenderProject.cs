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

namespace Form8snCore.Rendering
{
    public class RenderProject
    {
        private const string FallbackFontFamily = "Courier New";
        private const XFontStyle BaseFontStyle = XFontStyle.Bold;
        private static readonly Stopwatch _loadingTimer = new Stopwatch();
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
            _loadingTimer.Reset();
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
            var document = new PdfDocument();
            document.Info.Author = "Generated from data by Form8sn";
            document.Info.Title = project.Index.Name;
            document.Info.CreationDate = DateTime.UtcNow;

            var runningTotals = new Dictionary<string, decimal>(); // path -> total

            // First, we run through the definitions, gathering data
            // This lets us count pages, and defers fitting content into boxes until
            // we've decided to show it (e.g. if it's dependent on an empty box)
            var pageList = new List<DocumentPage>();
            for (var pageIndex = 0; pageIndex < project.Index.Pages.Count; pageIndex++)
            {
                var pageDef = project.Index.Pages[pageIndex];
                if (pageDef.RepeatMode.Repeats)
                {
                    var dataSets = mapper.GetRepeatData(pageDef.RepeatMode.DataPath);

                    for (int repeatIndex = 0; repeatIndex < dataSets.Count; repeatIndex++)
                    {
                        var repeatData = dataSets[repeatIndex];
                        mapper.SetRepeater(repeatData, pageDef.RepeatMode.DataPath);
                        var pageResult = PreparePage(pageDef, mapper, runningTotals, pageIndex);
                        mapper.ClearRepeater();
                        ClearPageTotals(runningTotals);
                        
                        if (pageResult.IsFailure)
                        {
                            result.ErrorMessage = pageResult.FailureMessage;
                            return result;
                        }
                    
                        pageResult.ResultData.RepeatIndex = repeatIndex;
                        pageResult.ResultData.RepeatCount = dataSets.Count;
                        pageList.Add(pageResult.ResultData);
                    }
                }
                else
                {
                    var pageResult = PreparePage(pageDef, mapper, runningTotals, pageIndex);
                    ClearPageTotals(runningTotals);
                        
                    if (pageResult.IsFailure)
                    {
                        result.ErrorMessage = pageResult.FailureMessage;
                        return result;
                    }
                    
                    pageList.Add(pageResult.ResultData);
                }
            }

            // remove boxes that have null/empty dependencies
            PruneBoxes(pageList);

            // Next, go through the prepared page and render them to PDF
            for (var pageIndex = 0; pageIndex < pageList.Count; pageIndex++)
            {
                _loadingTimer.Start();
                var pageDef = pageList[pageIndex].Definition;
                using var image = XImage.FromFile(pageDef.GetBackgroundPath(project)); // we need to load the image regardless, to get the pixel size
                var font = GetFont(pageDef.PageFontSize ?? project.Index.BaseFontSize ?? 16);
                _loadingTimer.Stop();

                var pageResult = OutputPage(document, pageList[pageIndex], font, image, pageIndex, pageList.Count);
                if (pageResult.IsFailure)
                {
                    result.ErrorMessage = pageResult.FailureMessage;
                    return result;
                }
            }

            document.Save(outputFilePath);
            _totalTimer.Stop();

            result.Success = true;

            result.OverallTime = _totalTimer.Elapsed;
            result.LoadingTime = _loadingTimer.Elapsed;

            return result;
        }

        private static void PruneBoxes(List<DocumentPage> pageList)
        {
            foreach (var page in pageList)
            {
                var boxesToKill = new List<string>(); //key list
                foreach (var box in page.DocumentBoxes)
                {
                    var definition = box.Value.Definition;
                    if (string.IsNullOrWhiteSpace(definition.DependsOn)) continue; // not dependent
                    
                    // Follow the depends chain until we hit one that's either empty, missing, or we hit the end
                    var nextBox = box.Value;
                    while (nextBox.Definition.DependsOn != null)
                    {
                        if (!page.DocumentBoxes.ContainsKey(nextBox.Definition.DependsOn)) { boxesToKill.Add(box.Key); break; } // has been early-culled
                        nextBox = page.DocumentBoxes[nextBox.Definition.DependsOn];
                        if (string.IsNullOrEmpty(nextBox.RenderContent)) { boxesToKill.Add(box.Key); break; } // dependent is empty
                        if (string.IsNullOrEmpty(nextBox.Definition.DependsOn)) break; // end of the chain, has a value
                        if (nextBox.Definition.DependsOn == box.Key) { boxesToKill.Add(box.Key); break; } // loop in chain. Drop this box
                    }
                }

                foreach (var key in boxesToKill) { page.DocumentBoxes.Remove(key); }
            }
        }

        private static void ClearPageTotals(Dictionary<string,decimal> runningTotals)
        {
            var keys = runningTotals.Keys.Where(key => key.StartsWith("D.")).ToArray();
            foreach (var key in keys)
            {
                runningTotals[key] = 0.0m;
            }
        }

        private static Result<DocumentPage> PreparePage(TemplatePage pageDef, DataMapper mapper, Dictionary<string, decimal> runningTotals, int pageIndex)
        {
            var docPage = new DocumentPage(pageDef);
            
            // Draw each box. We sort the boxes (currently by filter type) so running totals can work as expected
            foreach (var boxDef in pageDef.Boxes.Where(HasAValue).OrderBy(OrderBoxes))
            {
                var result = PrepareBox(mapper, boxDef.Value, runningTotals, pageIndex);
                if (result.IsFailure) return Result.Failure<DocumentPage>(result.FailureCause);
                if (result.ResultData != null) docPage.DocumentBoxes.Add(boxDef.Key, result.ResultData);
            }
            
            return Result.Success(docPage);
        }

        private static Result<DocumentBox?> PrepareBox(DataMapper mapper, TemplateBox box, Dictionary<string,decimal> runningTotals, int pageIndex)
        {
            if (mapper.IsPageValue(box, out var type))
            {
                return Result.Success<DocumentBox?>(new DocumentBox(box){BoxType = type});
            }

            var str = mapper.FindBoxData(box, pageIndex, runningTotals);
            if (str == null) return Result.Success<DocumentBox?>(null); // empty data is considered OK
            
            if (box.DisplayFormat != null)
            {
                str = DisplayFormatter.ApplyFormat(box, str);
                if (str == null) return Result.Failure<DocumentBox?>($"Formatter failed: {box.DisplayFormat.Type} applied on {string.Join(".", box.MappingPath!)}");
            }
            
            return Result.Success<DocumentBox?>(new DocumentBox(box, str));
        }

        private static Result<Nothing> OutputPage(PdfDocument document, DocumentPage pageToRender, XFont font, XImage image, int pageIndex, int pageTotal)
        {
            var page = document.AddPage();
            var pageDef = pageToRender.Definition;
            // If dimensions are silly, reset to A4
            if (pageDef.WidthMillimetres < 10 || pageDef.WidthMillimetres > 1000) pageDef.WidthMillimetres = 210;
            if (pageDef.HeightMillimetres < 10 || pageDef.HeightMillimetres > 1000) pageDef.HeightMillimetres = 297;


            // Set the PDF page size (in points under the hood)
            page.Width = XUnit.FromMillimeter(pageDef.WidthMillimetres);
            page.Height = XUnit.FromMillimeter(pageDef.HeightMillimetres);

            using var gfx = XGraphics.FromPdfPage(page);

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

            // Draw each box.
            foreach (var boxDef in pageToRender.DocumentBoxes)
            {
                var result = RenderBox(font, boxDef, fx, fy, gfx, pageToRender, pageIndex, pageTotal);
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

        private static Result<Nothing> RenderBox(XFont baseFont, KeyValuePair<string, DocumentBox> boxDef, double fx, double fy, XGraphics gfx, DocumentPage pageToRender, int pageIndex, int pageTotal)
        {
            var box = boxDef.Value;
            var font = (box.Definition.BoxFontSize != null) ? GetFont(box.Definition.BoxFontSize.Value) : baseFont;

            // Read data, or pick a special type
            var str = box.BoxType switch
            {
                DocumentBoxType.Normal => box.RenderContent,
                DocumentBoxType.CurrentPageNumber => (pageIndex + 1).ToString(),
                DocumentBoxType.TotalPageCount => pageTotal.ToString(),
                DocumentBoxType.RepeatingPageNumber => (pageToRender.RepeatIndex+1).ToString(),
                DocumentBoxType.RepeatingPageTotalCount => pageToRender.RepeatCount.ToString(),
                _ =>  box.RenderContent
            };

            if (str == null) return Result.Success(); // empty data is considered OK

            // boxes are defined in terms of the background image pixels, so we need to convert
            var boxWidth = box.Definition.Width * fx;
            var boxHeight = box.Definition.Height * fy;
            var space = new XRect(box.Definition.Left * fx, box.Definition.Top * fy, boxWidth, boxHeight);
            var align = MapAlignments(box.Definition);

            RenderTextInBox(font, gfx, box.Definition, fx, fy, str, space, align);
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
}