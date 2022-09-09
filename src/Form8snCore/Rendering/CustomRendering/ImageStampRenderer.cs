using System;
using PdfSharp.Drawing;

namespace Form8snCore.Rendering.CustomRendering;

internal class ImageStampRenderer:ICustomRenderedBox
{
    private readonly string _fileName;

    public ImageStampRenderer(string fileName)
    {
        _fileName = fileName;
    }

    public string GetName() => _fileName;

    public void RenderToPdf(IFileSource files, XGraphics gfx, DocumentBox box, XRect space)
    {
        // TODO: load from files, draw.
        var pen = new XPen(XColor.FromArgb(255,255,0,0), 3.0);
        gfx.DrawLine(pen, space.TopLeft, space.BottomRight);
        gfx.DrawLine(pen, space.TopRight, space.BottomLeft);
    }
}