using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Form8snCore.DataExtraction;
using Form8snCore.FileFormats;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SkinnyJson;

namespace Form8snCore
{
    public class RenderProject
    {
        private const string BaseFontFamily = "Courier New";
        private const XFontStyle BaseFontStyle = XFontStyle.Bold;
        private static readonly Stopwatch _layoutTimer = new Stopwatch();
        private static readonly Stopwatch _loadingTimer = new Stopwatch();
        private static readonly Stopwatch _filterTimer = new Stopwatch();
        private static readonly Stopwatch _renderTimer = new Stopwatch();
        private static readonly Stopwatch _totalTimer = new Stopwatch();

        public static RenderResultInfo ToFile(string outputFilePath, string dataFilePath, Project project)
        {
            _layoutTimer.Reset();
            _loadingTimer.Reset();
            _filterTimer.Reset();
            _renderTimer.Reset();
            _totalTimer.Restart();
            
            _loadingTimer.Start();
            var result = new RenderResultInfo{Success = false};
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
            
            _loadingTimer.Start();
            var font = new XFont(BaseFontFamily, 16, BaseFontStyle);
            _loadingTimer.Stop();

            for (var pageIndex = 0; pageIndex < project.Index.Pages.Count; pageIndex++)
            {
                _loadingTimer.Start();
                var pageDef = project.Index.Pages[pageIndex];
                using var image = XImage.FromFile(pageDef.GetBackgroundPath(project));
                _loadingTimer.Stop();
                
                if (pageDef.RepeatMode.Repeats)
                {
                    _filterTimer.Start();
                    var dataSets = mapper.GetRepeatData(pageDef.RepeatMode.DataPath);
                    _filterTimer.Stop();
                    for (int repeatIndex = 0; repeatIndex < dataSets.Count; repeatIndex++)
                    {
                        var repeatData = dataSets[repeatIndex];
                        mapper.SetRepeater(repeatData);
                        OutputStandardPage(document, pageDef, mapper, pageIndex, font, image);
                        mapper.ClearRepeater();
                    }
                }
                else
                {
                    OutputStandardPage(document, pageDef, mapper, pageIndex, font, image);
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

        private static void OutputStandardPage(PdfDocument document, TemplatePage pageDef, DataMapper mapper, int pageIndex, XFont font, XImage image)
        {
            _renderTimer.Start();
            var page = document.AddPage();
            _renderTimer.Stop();
            // If dimensions are silly, reset to A4
            if (pageDef.WidthMillimetres < 10 || pageDef.WidthMillimetres > 1000) pageDef.WidthMillimetres = 210;
            if (pageDef.HeightMillimetres < 10 || pageDef.HeightMillimetres > 1000) pageDef.HeightMillimetres = 297;
            
            
            // Set the PDF page size (in points under the hood)
            page.Width = XUnit.FromMillimeter(pageDef.WidthMillimetres); // TODO: deal with invalid page sizes
            page.Height = XUnit.FromMillimeter(pageDef.HeightMillimetres);

            _renderTimer.Start();
            using var gfx = XGraphics.FromPdfPage(page);
            _renderTimer.Stop();
            
            // Draw background at full page size
            _loadingTimer.Start();
            var destRect = new RectangleF(0, 0, (float) page.Width.Point, (float) page.Height.Point);
            gfx.DrawImage(image, destRect);
            _loadingTimer.Stop();

            // Work out the bitmap -> page adjustment fraction
            var fx = page.Width.Point / image.PixelWidth;
            var fy = page.Height.Point / image.PixelHeight;

            // Draw each box
            foreach (var boxDef in pageDef.Boxes)
            {
                RenderBox(mapper, pageIndex, font, boxDef, fx, fy, gfx);
            }
        }

        private static void RenderBox(DataMapper mapper, int pageIndex, XFont font, KeyValuePair<string, TemplateBox> boxDef, double fx, double fy, XGraphics gfx)
        {
            var box = boxDef.Value;

            _filterTimer.Start();
            var str = mapper.FindBoxData(box, pageIndex);
            _filterTimer.Stop();
            if (str == null) return;

            _layoutTimer.Start();
            // boxes are defined in terms of the background image pixels, so we need to convert
            var boxWidth = box.Width * fx;
            var boxHeight = box.Height * fy;
            var space = new XRect(box.Left * fx, box.Top * fy, boxWidth, boxHeight);
            var align = MapAlignments(box);

            RenderTextInBox(font, gfx, box, fx, fy, str, space, align);
            _layoutTimer.Stop();
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
                WrapStringToFit(SplitByNonLetter(str), boxWidth, gfx, font, out var lines);

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
            var textHeight = lines.Sum(l=>l.Height);
            var top = box.Top * fy;
            var bottom = top + (box.Height*fy);
            var middle = top + (box.Height*fy*0.5);

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
            var spaceWidth = gfx.MeasureString(" ", font).Width;
            var widthRemains = boxWidth;
            var firstOnLine = true;
            var lineHeight = 0.0;
                
            // Keep adding chunks until one doesn't fit, then break line
            var sb = new StringBuilder();
            for (int i = 0; i < words.Count; i++)
            {
                var bitSize = gfx.MeasureString(words[i], font);
                lineHeight = Math.Max(lineHeight, bitSize.Height);
                var wordWidth = bitSize.Width;
                
                if (!firstOnLine) wordWidth += spaceWidth;

                if (wordWidth <= widthRemains) // next word fits on the line
                {
                    if (!firstOnLine) sb.Append(' ');
                    sb.Append(words[i]);
                    widthRemains -= wordWidth;
                    firstOnLine = false;
                    continue;
                }

                // need to wrap
                // output the line, and start another
                lines.Add(new MeasuredLine(sb.ToString(), lineHeight));
                sb.Clear();
                sb.Append(words[i]);
                lineHeight = bitSize.Height;
                widthRemains = boxWidth - bitSize.Width;
                firstOnLine = false;
            }
            
            if (sb.Length > 0) // last line
                lines.Add(new MeasuredLine(sb.ToString(), lineHeight));
        }

        private static XFont ShrinkFontAndWrapStringToFit(string str, double boxWidth, double boxHeight, XGraphics gfx, XFont font, out List<MeasuredLine> lines)
        {
            var bits = SplitByNonLetter(str);
            // Not going to bother with justification, just go ragged edge. Let the normal alignment do its thing.
            
            var baseSize = font.Size;
            XFont altFont;
            while (true)
            {
                baseSize -= baseSize / 10;
                altFont = new XFont(BaseFontFamily, baseSize, BaseFontStyle);
                
                var spaceWidth = gfx.MeasureString(" ", altFont).Width;
                var widthRemains = boxWidth;
                var heightAccumulated = 0.0;
                var firstOnLine = true;
                var lineMaxHeight = 0.0;
                var sizeIsAcceptable = true;
                
                // Keep adding chunks until one doesn't fit, then break line
                for (int i = 0; i < bits.Count; i++)
                {
                    var bitSize = gfx.MeasureString(bits[i], altFont);
                    lineMaxHeight = Math.Max(bitSize.Height, lineMaxHeight);
                    var wordWidth = bitSize.Width;
                    if (firstOnLine) firstOnLine = false;
                    else wordWidth += spaceWidth;

                    if (wordWidth <= widthRemains)
                    {
                        widthRemains -= wordWidth;
                        continue; // not run out of line space
                    }

                    // need to wrap
                    widthRemains = boxWidth - bitSize.Width;
                    heightAccumulated += lineMaxHeight;
                    lineMaxHeight = 0.0;
                    firstOnLine = true;

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

        private static List<string> SplitByNonLetter(string str)
        {
            var outp = new List<string>();
            var sb = new StringBuilder();
            foreach (var c in str)
            {
                if (!char.IsWhiteSpace(c)) sb.Append(c); // TODO: reduce to single whitespace, and handle in wrapping
                if (char.IsLetterOrDigit(c)) continue;

                outp.Add(sb.ToString()); // we specifically leave the first non-letter char attached (like ["hello," "world!"]). This simplifies common wrapping cases
                sb.Clear();
            }

            if (sb.Length > 0) outp.Add(sb.ToString());

            return outp;
        }

        private static XFont ShrinkFontToFit(string str, double boxWidth, double boxHeight, XGraphics gfx, XFont font, XSize size)
        {
            var baseSize = font.Size;
            var altFont = new XFont(BaseFontFamily, baseSize, BaseFontStyle);
            while (size.Width > boxWidth || size.Height > boxHeight)
            {
                baseSize -= baseSize / 10;
                altFont = new XFont(BaseFontFamily, baseSize, BaseFontStyle);
                size = gfx.MeasureString(str, altFont);
                if (baseSize < 4) break;
            }

            return altFont;
        }

        private static XStringFormat MapAlignments(TemplateBox box)
        {
            switch (box.Alignment)
            {
                case TextAlignment.TopLeft      : return XStringFormats.TopLeft;
                case TextAlignment.TopCentre    : return XStringFormats.TopCenter;
                case TextAlignment.TopRight     : return XStringFormats.TopRight;
                case TextAlignment.MidlineLeft  : return XStringFormats.CenterLeft;
                case TextAlignment.MidlineCentre: return XStringFormats.Center;
                case TextAlignment.MidlineRight : return XStringFormats.CenterRight;
                case TextAlignment.BottomLeft   : return XStringFormats.BottomLeft;
                case TextAlignment.BottomCentre : return XStringFormats.BottomCenter;
                case TextAlignment.BottomRight  : return XStringFormats.BottomRight;
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