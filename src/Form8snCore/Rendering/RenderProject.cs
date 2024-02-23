using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Containers;
using Form8snCore.DataExtraction;
using Form8snCore.FileFormats;
using Form8snCore.HelpersAndConverters;
using Form8snCore.Rendering.CustomRendering;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SkinnyJson;

namespace Form8snCore.Rendering
{
    public class RenderProject
    {

        /// <summary>
        /// Create a renderer with a source of files
        /// </summary>
        public RenderProject(IFileSource files)
        {
            _files = files;
        }
        
        /// <summary>
        /// Render a PDF to a writable stream.
        /// </summary>
        /// <param name="target">Output stream. Must be open and writable</param>
        /// <param name="data">Data to use for page generation</param>
        /// <param name="project">Project file that defines the PDF to generate</param>
        public RenderResultInfo ToStream(Stream target, object data, TemplateProject project)
        {
            try
            {
                var info = RenderToDocument(data, project, out var document);
                document.Save(target);
                document.Dispose();

                if (target.CanSeek) target.Seek(0, SeekOrigin.Begin);
                return info;
            }
            catch (Exception ex)
            {
                return new RenderResultInfo
                {
                    Success = false,
                    ErrorMessage = ex.GetType().Name +": " + ex.Message,
                    LoadingTime = TimeSpan.Zero,
                    OverallTime = TimeSpan.Zero,
                    CustomRenderTime = TimeSpan.Zero
                };
            }
        }

        /// <summary>
        /// Render a PDF to a local file path
        /// </summary>
        /// <param name="outputFilePath">Local file path for the output PDF</param>
        /// <param name="dataFilePath">File that contains the data to use for page generation</param>
        /// <param name="project">Project file that defines the PDF to generate</param>
        public RenderResultInfo ToFile(string outputFilePath, string dataFilePath, TemplateProject project)
        {
            if (!File.Exists(dataFilePath))
            {
                return new RenderResultInfo
                {
                    Success = false,
                    ErrorMessage = "Could not find input data file"
                };
            }

            var data = Json.Defrost(File.ReadAllText(dataFilePath));
            var info = RenderToDocument(data, project, out var document);
            document.Save(outputFilePath);
            document.Dispose();
            return info;
        }
        

        /// <summary>
        /// Follow all data paths and page repeaters in the project. Results in either a set of pages that
        /// can be rendered to PDF, or null if the document can't be rendered
        /// </summary>
        /// <param name="project">Project file to generate a document for</param>
        /// <param name="mapper">Data mapper holding the final data for a single document</param>
        /// <param name="result">Rendering result, including any error messages</param>
        /// <returns>List of pages to render, or null</returns>
        public static List<DocumentPage>? PrepareAllPages(TemplateProject project, DataMapper mapper, RenderResultInfo result)
        {
            var runningTotals = new Dictionary<string, decimal>(); // path -> total

            // First, we run through the definitions, gathering data
            // This lets us count pages, and defers fitting content into boxes until
            // we've decided to show it (e.g. if it's dependent on an empty box)
            var pageList = new List<DocumentPage>();
            for (var pageIndex = 0; pageIndex < project.Pages.Count; pageIndex++)
            {
                var pageDef = project.Pages[pageIndex]!;
                if (pageDef.RepeatMode.Repeats)
                {
                    var dataSets = mapper.GetRepeatData(pageDef.RepeatMode.DataPath);

                    for (var repeatIndex = 0; repeatIndex < dataSets.Count; repeatIndex++)
                    {
                        var repeatData = dataSets[repeatIndex];
                        mapper.SetRepeater(repeatData, pageDef.RepeatMode.DataPath);
                        var pageResult = PreparePage(pageDef, mapper, runningTotals, pageIndex);
                        mapper.ClearRepeater();
                        ClearPageTotals(runningTotals);

                        if (pageResult.IsFailure)
                        {
                            result.ErrorMessage = pageResult.FailureMessage;
                            return null;
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
                        return null;
                    }

                    pageList.Add(pageResult.ResultData);
                }
            }

            // remove boxes that have null/empty dependencies
            PruneBoxes(pageList);
            return pageList;
        }

        
        private const string FallbackFontFamily = "Courier New";
        private const XFontStyle BaseFontStyle = XFontStyle.Bold;
        
        private readonly Stopwatch _loadingTimer = new();
        private readonly Stopwatch _totalTimer = new();
        private readonly Stopwatch _customTimer = new();
        private readonly Dictionary<int, XFont> _fontCache = new(); // per-size cache. Only uses one font.
        private string? _baseFontFamily;
        
        private readonly IFileSource _files;
        private PdfDocument? _basePdf;
        
        /// <summary>
        /// This is the main process of rendering a project to a pdf
        /// </summary>
        private RenderResultInfo RenderToDocument(object data, TemplateProject project, out PdfDocument document){
            _loadingTimer.Reset();
            _customTimer.Reset();
            _totalTimer.Restart();

            _loadingTimer.Start();
            _baseFontFamily = project.FontName;
            var result = new RenderResultInfo {Success = false};
            _loadingTimer.Stop();

            var mapper = new DataMapper(project, data);
            document = new PdfDocument();
            document.Info.Author = "Generated from data by Form8sn";
            document.Info.Title = project.Name;
            document.Info.CreationDate = DateTime.UtcNow;

            // Go through the project file and data, generate all the pages, and the
            // template boxes with appropriately formatted data
            var pageList = PrepareAllPages(project, mapper, result);
            if (pageList is null)
            {
                result.Success = false;
                return result;
            }

            // Next, go through the prepared page and render them to PDF
            var ok = RenderPagesToPdfDocument(project, document, pageList, result);
            if (!ok)
            {
                result.Success = false;
                return result;
            }

            _totalTimer.Stop();

            result.Success = true;

            result.OverallTime = _totalTimer.Elapsed;
            result.LoadingTime = _loadingTimer.Elapsed;
            result.CustomRenderTime = _customTimer.Elapsed;

            return result;
        }
        
        
        /// <summary>
        /// Render prepared pages into a PDF document
        /// </summary>
        /// <param name="project">Project file to generate a document for</param>
        /// <param name="document">Open and writable PDF file to add pages to</param>
        /// <param name="pageList">A set of pages from PrepareAllPages()</param>
        /// <param name="result">Rendering result, including any error messages</param>
        /// <returns>True for success, false for failure</returns>
        public bool RenderPagesToPdfDocument(TemplateProject project, PdfDocument document, List<DocumentPage> pageList, RenderResultInfo result)
        {
            var totalPageCount = pageList.Count; // this is output pages, including individual repetitions of repeated template pages.
            for (var pageIndex = 0; pageIndex < totalPageCount; pageIndex++)
            {
                if (pageIndex >= pageList.Count) break;
                _loadingTimer.Start();
                var page = pageList[pageIndex]!;
                var pageDef = page.Definition;
                // If dimensions are silly, reset to A4
                if (pageDef.WidthMillimetres < 10 || pageDef.WidthMillimetres > 3000) pageDef.WidthMillimetres = 210;
                if (pageDef.HeightMillimetres < 10 || pageDef.HeightMillimetres > 3000) pageDef.HeightMillimetres = 297;

                using var background = LoadBackground(project, page.SourcePageIndex, pageDef);
                var font = GetFont(pageDef.PageFontSize ?? project.BaseFontSize ?? 16);
                _loadingTimer.Stop();

                // If we're in normal mode, we will make a new page per template page
                // If in 'roll' mode, only make a new page when we run out of space
                if (pageDef.MergeToRoll)
                {
                    var margin = XUnit.FromMillimeter(pageDef.RollMarginMillimetres??0.0);
                    var offset = new XPoint(margin,margin);
                    var pdfPage = document.AddPage( /*blank*/);

                    // Set the PDF page size (in points under the hood)
                    pdfPage.Width = XUnit.FromMillimeter(pageDef.RollWidthMillimetres??pageDef.WidthMillimetres);
                    pdfPage.Height = XUnit.FromMillimeter(pageDef.RollHeightMillimetres??pageDef.HeightMillimetres);
                    
                    var subPageWidth = XUnit.FromMillimeter(pageDef.WidthMillimetres);
                    var subPageHeight = XUnit.FromMillimeter(pageDef.HeightMillimetres);
                    
                    // Keep trying to add sub-pages
                    for (int rollIndex = 0; rollIndex < page.RepeatCount; rollIndex++)
                    {
                        if (pageIndex >= pageList.Count) break;
                        page = pageList[pageIndex]!; // get the page again, so we don't repeat the first one a bunch of times
                        var pageResult = OutputOntoPage(pdfPage, page, offset, font, background, pageIndex, totalPageCount);
                        if (pageResult.IsFailure)
                        {
                            result.ErrorMessage = pageResult.FailureMessage;
                            return false;
                        }
                        pageIndex++;
                        
                        offset.X += subPageWidth + margin.Point;
                        if (offset.X + subPageWidth > pdfPage.Width) // carriage return
                        {
                            offset.X = margin;
                            offset.Y += subPageHeight + margin.Point;
                        }
                        
                        if (offset.Y + subPageHeight > pdfPage.Height) break; // can't fit any more in
                    }
                }
                else
                {
                    // If we have a source PDF, and we aren't trying to render a blank page, then import the page
                    var shouldCopyPdfPage = background.ExistingPage != null && pageDef.RenderBackground;
                    var pdfPage = shouldCopyPdfPage ? document.AddPage(background.ExistingPage!) : document.AddPage( /*blank*/);

                    // Set the PDF page size (in points under the hood)
                    pdfPage.Width = XUnit.FromMillimeter(pageDef.WidthMillimetres);
                    pdfPage.Height = XUnit.FromMillimeter(pageDef.HeightMillimetres);

                    var pageResult = OutputOntoPage(pdfPage, page, XPoint.Zero, font, background, pageIndex, pageList.Count);
                    if (pageResult.IsFailure)
                    {
                        result.ErrorMessage = pageResult.FailureMessage;
                        return false;
                    }
                }
            }
            return true;
        }
        
        /// <summary>
        /// Render a prepared page into a PDF document.
        /// Returns the maximum point rendered onto the page (ignoring offset)
        /// </summary>
        private Result<XPoint> OutputOntoPage(PdfPage page, DocumentPage pageToRender, XPoint offset, XFont font, PageBacking background, int pageIndex, int pageTotal)
        {
            var pageDef = pageToRender.Definition;
            var max = new XPoint(offset.X, offset.Y);

            var shouldCopyPdfPage = background.ExistingPage != null && pageDef.RenderBackground && !pageDef.MergeToRoll;
            using var gfx = XGraphics.FromPdfPage(page);
            
            gfx.Save();
            
            // Match visual-rotations on page, but only if we're not merging.
            if (pageDef.MergeToRoll == false && shouldCopyPdfPage && (background.ExistingPage?.Rotate??0) != 0)
            {
                var centre = new XPoint(0,0);
                var visualRotate = background.ExistingPage!.Rotate % 360; // degrees, spec says it should be a multiple of 90.
                var angle = 360.0 - visualRotate;
                gfx.RotateAtTransform(angle, centre);
                
                switch (visualRotate)
                {
                    case 90:
                        gfx.TranslateTransform(-page.Height.Point, 0);
                        break;
                    case 180:
                        gfx.TranslateTransform(-page.Width.Point, -page.Height.Point);
                        break;
                    case 270:
                        gfx.TranslateTransform(-page.Height.Point / 2.0, -page.Height.Point); // this one is a guess, as I don't have an example
                        break;
                    default:
                        throw new Exception("Unhandled visual rotation case");
                }

                if (background.ExistingPage.Orientation == PageOrientation.Landscape)
                {
                    // ReSharper disable once SwapViaDeconstruction // not supported on net std 2.0
                    var tmp = page.Height; page.Height = page.Width; page.Width = tmp;
                }
            }

            // Draw background at full page size
            _loadingTimer.Start();
            if (pageDef.RenderBackground && pageDef.BackgroundImage != null)
            {
                try
                {
                    var width = new XUnit(pageDef.WidthMillimetres, XGraphicsUnit.Millimeter);
                    var height = new XUnit(pageDef.HeightMillimetres, XGraphicsUnit.Millimeter);
                    var destRect = new XRect(offset.X, offset.Y, width.Point, height.Point);
                    gfx.DrawImage(background.BackgroundImage, destRect);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to draw background image: {ex}");
                }
            }
            _loadingTimer.Stop();

            // Calculate scaling parameter
            var fx = page.Width.Point / page.Width.Millimeter;
            var fy = page.Height.Point / page.Height.Millimeter;
            
            gfx.TranslateTransform(offset.X, offset.Y);

            // Draw each box.
            foreach (var boxDef in pageToRender.DocumentBoxes)
            {
                var result = RenderBox(font, boxDef, fx, fy, gfx, pageToRender, pageIndex, pageTotal);
                if (result.IsFailure) return result;
                max.ExtendTo(result.ResultData);
            }
            
            gfx.Restore();

            return Result.Success(max);
        }
        

        private PageBacking LoadBackground(TemplateProject project, int sourcePageIndex, TemplatePage pageDef)
        {
            var backing = new PageBacking();
            if (_basePdf is null && !string.IsNullOrWhiteSpace(project.BasePdfFile!))
            {
                using var fileStream = _files.Load(project.BasePdfFile);
                _basePdf = PdfReader.Open(fileStream, PdfDocumentOpenMode.Import); // Must use import mode to copy pages across
            }

            if (_basePdf != null)
            {
                backing.ExistingPage = _basePdf.Pages[sourcePageIndex];
            }

            if (!string.IsNullOrWhiteSpace(pageDef.BackgroundImage!))
            {
                var fileStream = _files.Load(pageDef.BackgroundImage); // This might be a file reference, or a stamp that is resolved to a file
                backing.BackgroundImage = XImage.FromStream(fileStream);
            }
            
            return backing;
        }
        
        private XFont GetFont(int size)
        {
            if (_fontCache.ContainsKey(size)) return _fontCache[size]!;
            _fontCache.Add(size, new XFont(_baseFontFamily ?? FallbackFontFamily, size, BaseFontStyle));
            return _fontCache[size]!;
        }
        
        private static void PruneBoxes(List<DocumentPage> pageList)
        {
            foreach (var page in pageList)
            {
                var boxesToKill = new List<string>(); //key list
                foreach (var box in page.DocumentBoxes)
                {
                    var definition = box.Value!.Definition;
                    if (string.IsNullOrWhiteSpace(definition.DependsOn!)) continue; // not dependent
                    
                    // Follow the depends chain until we hit one that's either empty, missing, or we hit the end
                    var nextBox = box.Value;
                    while (nextBox?.Definition.DependsOn != null)
                    {
                        if (!page.DocumentBoxes.ContainsKey(nextBox.Definition.DependsOn)) { boxesToKill.Add(box.Key); break; } // has been early-culled
                        nextBox = page.DocumentBoxes[nextBox.Definition.DependsOn];
                        if (string.IsNullOrEmpty(nextBox?.RenderContent?.StringValue!)) { boxesToKill.Add(box.Key); break; } // dependent is empty
                        if (string.IsNullOrEmpty(nextBox?.Definition.DependsOn!)) break; // end of the chain, has a value
                        if (nextBox?.Definition.DependsOn == box.Key) { boxesToKill.Add(box.Key); break; } // loop in chain. Drop this box
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

        /// <summary>
        /// Gather all the data for a single output page
        /// </summary>
        private static Result<DocumentPage> PreparePage(TemplatePage pageDef, DataMapper mapper, Dictionary<string, decimal> runningTotals, int pageIndex)
        {
            var docPage = new DocumentPage(pageDef, pageIndex);

            if (pageDef.BackgroundImage is not null)
            {
                var result = mapper.TryResolvePath(pageDef.BackgroundImage!, pageIndex);
                if (result is ImageStampRenderer stamp)
                {
                    pageDef.BackgroundImage = stamp.GetName() + ".jpg";
                } else if (result is not null)
                {
                    pageDef.BackgroundImage = result.ToString();
                }
            }

            // Draw each box. We sort the boxes (currently by filter type) so running totals can work as expected
            foreach (var boxDef in pageDef.Boxes.Where(HasAValue).OrderBy(OrderBoxes))
            {
                var result = PrepareBox(mapper, boxDef.Value!, runningTotals, pageIndex);
                if (result.IsFailure && boxDef.Value!.IsRequired) return Result.Failure<DocumentPage>(result.FailureCause);
                if (result.IsSuccess && result.ResultData != null) docPage.DocumentBoxes.Add(boxDef.Key, result.ResultData);
            }
            
            return Result.Success(docPage);
        }

        /// <summary>
        /// Gather the data available for a single template box.
        /// </summary>
        private static Result<DocumentBox?> PrepareBox(DataMapper mapper, TemplateBox box, Dictionary<string,decimal> runningTotals, int pageIndex)
        {
            try
            {
                if (box.MappingPath is null) return Result.Failure<DocumentBox?>("Box has no mapping path"); 
                var boxData = mapper.TryFindBoxData(box, pageIndex, runningTotals);

                // If no data, either ignore or fail.
                if (boxData == null)
                {
                    if (box.IsRequired) return Result.Failure<DocumentBox?>($"Required data was not found at [{string.Join(".",box.MappingPath)}]");
                    return Result.Success<DocumentBox?>(null); // empty data is considered OK
                }

                // Handle fully custom boxes
                if (boxData.IsSpecial)
                {
                    return Result.Success<DocumentBox?>(new DocumentBox(box) { BoxType = DocumentBoxType.CustomRenderer, RenderContent = boxData});
                }

                // Handle special renderers
                if (DataMapper.IsSpecialBoxType(box, out var type))
                {
                    return Result.Success<DocumentBox?>(new DocumentBox(box) { BoxType = type, RenderContent = boxData});
                }
                
                // Not a weird box, just text.
                var strResult = boxData.StringValue;
                if (box.DisplayFormat != null)
                {
                    strResult = DisplayFormatter.ApplyFormat(box.DisplayFormat, boxData.StringValue);
                    if (strResult == null) return Result.Failure<DocumentBox?>($"Formatter failed: {box.DisplayFormat.Type} applied on {string.Join(".", box.MappingPath!)}");
                }

                return Result.Success<DocumentBox?>(new DocumentBox(box, strResult));
            }
            catch (Exception ex)
            {
                return Result.Failure<DocumentBox>(ex)!;
            }
        }


        private static bool HasAValue(KeyValuePair<string, TemplateBox> boxDef)
        {
            return boxDef.Value?.MappingPath != null;
        }

        private static int OrderBoxes(KeyValuePair<string, TemplateBox> boxDef)
        {
            const int explicitOrderGap = 10000;
            
            // if we have an explicit order, use that (explicits always go first)
            if (boxDef.Value?.BoxOrder != null) return boxDef.Value.BoxOrder.Value;
            
            // otherwise, apply a default ordering.
            var path = boxDef.Value!.MappingPath;
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

        private static string? TryApplyDisplayFormat(DocumentBox box, string? str)
        {
            if (box.Definition.DisplayFormat == null) return str;
            var transformed = DisplayFormatter.ApplyFormat(box.Definition.DisplayFormat, str);
            return transformed ?? str;
        }

        /// <summary>
        /// Draw the prepared box to a PDF page.
        /// Returns maximum x and y coords of the box.
        /// </summary>
        private Result<XPoint> RenderBox(XFont baseFont, KeyValuePair<string, DocumentBox> boxDef, double fx, double fy, XGraphics gfx, DocumentPage pageToRender, int pageIndex, int pageTotal)
        {
            var box = boxDef.Value!;
            var font = (box.Definition.BoxFontSize != null) ? GetFont(box.Definition.BoxFontSize.Value) : baseFont;
            
            
            // boxes are defined in terms of the background image pixels or existing PDF page, so we need to convert
            var boxWidth = box.Definition.Width * fx;
            var boxHeight = box.Definition.Height * fy;
            var space = new XRect(box.Definition.Left * fx, box.Definition.Top * fy, boxWidth, boxHeight);
            
            // Handle the super-special cases
            // TODO: replace these with custom renderers out of this already big class
            switch (box.BoxType)
            {
                case DocumentBoxType.EmbedJpegImage:
                    EmbedJpegImage(gfx, box, space);
                    return Result.Success(space.BottomRight);
                
                case DocumentBoxType.ColorBox:
                    DrawColorBox(gfx, box, space);
                    return Result.Success(space.BottomRight);
                
                case DocumentBoxType.QrCode:
                    DrawQrCode(gfx, box, space);
                    return Result.Success(space.BottomRight);
                
                case DocumentBoxType.CustomRenderer:
                    _customTimer.Start();
                    box.RenderContent?.RenderToPdf(_files, gfx, box, space);
                    _customTimer.Stop();
                    return Result.Success(space.BottomRight);
            }

            // Read data, applying custom formatting if required 
            var str = box.BoxType switch
            {
                DocumentBoxType.Normal => box.RenderContent?.StringValue,

                // For the special values, we might want to re-apply the display format
                DocumentBoxType.PageGenerationDate => TryApplyDisplayFormat(box, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                DocumentBoxType.CurrentPageNumber => TryApplyDisplayFormat(box, (pageIndex + 1).ToString()),
                DocumentBoxType.TotalPageCount => TryApplyDisplayFormat(box, pageTotal.ToString()),
                DocumentBoxType.RepeatingPageNumber => TryApplyDisplayFormat(box, (pageToRender.RepeatIndex + 1).ToString()),
                DocumentBoxType.RepeatingPageTotalCount => TryApplyDisplayFormat(box, pageToRender.RepeatCount.ToString()),

                _ => box.RenderContent?.StringValue
            };

            if (str == null) return Result.Success(space.BottomRight); // empty data is considered OK

            var align = MapAlignments(box.Definition);

            RenderTextInBox(font, gfx, box.Definition, fx, fy, str, space, align);
            return Result.Success(space.BottomRight);
        }

        /// <summary>
        /// Try to interpret the box content as a RGB color string, and
        /// fill the box with that color. If interpretation fails, then
        /// quietly do nothing
        /// </summary>
        private static void DrawColorBox(XGraphics gfx, DocumentBox box, XRect space)
        {
            var colorStr = box.RenderContent?.StringValue ?? "";
            var result = CssColorParser.ParseCssColor(colorStr);
            
            var brush = new XSolidBrush(XColor.FromArgb(result[0], result[1], result[2], result[3]));
            gfx.DrawRectangle(brush, space);
        }
        
        /// <summary>
        /// Fill the box with a QR code based on the box's content.
        /// This is always black-and-white, and always overwrites the
        /// PDF underneath (including quiet space)
        /// </summary>
        private void DrawQrCode(XGraphics gfx, DocumentBox box, XRect space)
        {
            var encoder = new QrEncoder{ErrorCorrectionLevel = ErrorCorrection.Q};
            var matrix = encoder.Encode(box.RenderContent?.StringValue ?? "");
            
            var black = new XSolidBrush(XColor.FromArgb(255, 0, 0, 0));
            var white = new XSolidBrush(XColor.FromArgb(255, 255, 255, 255));
            
            // Should now subdivide the space rect by the matrix dimensions, and call DrawRectangle a bunch.
            // We also should have all but the bottom & right edges slightly over-sized to prevent black-to-black
            // edge problems.
            var width = matrix.GetLength(0);
            var height = matrix.GetLength(1);


            var modSize = Math.Min(space.Width / width, space.Height / height);
            var bleedSize = modSize * 1.01; // draw boxes slightly oversize

            var dx = space.X;
            var dy = space.Y;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var px = dx + (x * modSize);
                    var py = dy + (y * modSize);
                    var color = matrix[y,x] ? black : white; // fun fact: if you get the X & Y the wrong way around, the QR will look perfect, but not scan at all.
                    gfx.DrawRectangle(color, new XRect(px, py, bleedSize, bleedSize));
                }
            }
        }

        /// <summary>
        /// Try to load an image from a URL.
        /// If successful, draw the image into the box.
        /// If failure, fill the box with black
        /// </summary>
        private void EmbedJpegImage(XGraphics gfx, DocumentBox box, XRect space)
        {
            if (string.IsNullOrWhiteSpace(box.RenderContent?.StringValue!)) return;

            try
            {
                _loadingTimer.Start();
                var jpegStream = _files.LoadUrl(box.RenderContent?.StringValue);
                if (jpegStream is null) return; // Embedded images are always considered optional
                var img = XImage.FromStream(jpegStream);
                gfx.DrawImage(img, space);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // Nothing. Embedded images are always considered optional
            }
            finally
            {
                _loadingTimer.Stop();
            }
        }

        private void RenderTextInBox(XFont font, XGraphics gfx, TemplateBox box, double fx, double fy,
            string str, XRect space, XStringFormat align)
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
            if (lines.Count < 1) return;
            
            var top = CalculateTop(box, fy, lines);
            var start = lines[0]!.Text.Length < 1 ? 1 : 0; // Never draw a blank first line
            
            for (var index = start; index < lines.Count; index++)
            {
                var line = lines[index]!;
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
                var bitSize = gfx.MeasureString(words[i]!, font);
                lineHeight = Math.Max(lineHeight, bitSize.Height);
                var wordWidth = bitSize.Width;

                if (wordWidth <= widthRemains) // next word fits on the line
                {
                    sb.Append(words[i]!);
                    widthRemains -= wordWidth;
                    continue;
                }

                // need to wrap
                // output the line, and start another
                lines.Add(new MeasuredLine(sb.ToString(), lineHeight));
                sb.Clear();
                sb.Append(words[i]!);
                lineHeight = bitSize.Height;
                widthRemains = boxWidth - bitSize.Width;
            }

            if (sb.Length > 0) // last line
                lines.Add(new MeasuredLine(sb.ToString(), lineHeight));
        }

        private XFont ShrinkFontAndWrapStringToFit(string str, double boxWidth, double boxHeight, XGraphics gfx, XFont font, out List<MeasuredLine> lines)
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
                    var bitSize = gfx.MeasureString(bits[i]!, altFont);
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

        private XFont ShrinkFontToFit(string str, double boxWidth, double boxHeight, XGraphics gfx, XFont font, XSize size)
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