using PdfSharp.Drawing;

namespace Form8snCore.Rendering.CustomRendering;

/// <summary>
/// Render locally stored JPEG images to the page
/// </summary>
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
        var jpegStream = files.Load(box.RenderContent?.StringValue+".jpg");
        var img = XImage.FromStream(jpegStream);
        gfx.DrawImage(img, space);
    }
}