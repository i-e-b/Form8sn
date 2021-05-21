using System;
using System.Drawing;
using System.IO;
using Form8snCore.FileFormats;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SkinnyJson;

namespace Form8snCore
{
    public class RenderProject
    {
        public static bool ToFile(string outputFilePath, string dataFilePath, Project project)
        {
            // TODO: implement this
            if (!File.Exists(dataFilePath)) return false;
            
            var data = Json.Defrost(File.ReadAllText(dataFilePath)!);
            
            
            var document = new PdfDocument();
            document.Info.Author = "Generated from data by Form8sn";
            document.Info.Title = project.Index.Name;

            var font = new XFont("Courier New", 16, XFontStyle.Bold);
            
            foreach (var pageDef in project.Index.Pages)
            {
                if (pageDef.RepeatMode.Repeats)
                {
                    // TODO: pull the repeat source
                    
                    var page = document.AddPage();
                    page.Width = XUnit.FromMillimeter(pageDef.WidthMillimetres); // TODO: deal with invalid page sizes
                    page.Height = XUnit.FromMillimeter(pageDef.HeightMillimetres);
                    using (var gfx = XGraphics.FromPdfPage(page))
                    {

                        gfx.DrawString($"This is page {pageDef.Name}, which is a repeater", font, XBrushes.Black,
                            new XRect(0, 0, page.Width, page.Height),
                            XStringFormats.Center);
                    }
                }
                else
                {
                    var page = document.AddPage();
                    page.Width = XUnit.FromMillimeter(pageDef.WidthMillimetres); // TODO: deal with invalid page sizes
                    page.Height = XUnit.FromMillimeter(pageDef.HeightMillimetres);

                    using (var gfx = XGraphics.FromPdfPage(page))
                    {
                        // Draw background at full page size
                        using var image = XImage.FromFile(pageDef.GetBackgroundPath(project));
                        var destRect = new RectangleF(0, 0, (float) page.Width.Point, (float) page.Height.Point);
                        gfx.DrawImage(image, destRect);
                        
                        // Work out the bitmap -> page adjustment fraction
                        var fx = page.Width.Point / image.PixelWidth;
                        var fy = page.Height.Point / image.PixelHeight;

                        foreach (var boxDef in pageDef.Boxes)
                        {
                            var box = boxDef.Value;
                            // boxes are defined in terms of the background image pixels, so we need to convert
                            var space = new XRect(box.Left * fx, box.Top * fy, box.Width * fx, box.Height * fy);
                            var align = MapAlignments(box);
                            gfx.DrawString(boxDef.Key, font, XBrushes.Black,space,align);
                        }
                        // TODO: data mapping
                    }
                }
            }

            document.Save(outputFilePath);
            return true;
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
}