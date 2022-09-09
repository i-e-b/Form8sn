using PdfSharp.Drawing;

namespace Form8snCore.Rendering.CustomRendering;

/// <summary>
/// Marker interface for box data that requires
/// special treatment in the rendering pipeline
/// </summary>
public interface ICustomRenderedBox
{
    /// <summary>
    /// Produce string data, or a display name for this box.
    /// This should not be null or empty.
    /// </summary>
    string GetName();

    /// <summary>
    /// Render special box into the PDF being generated.
    /// </summary>
    void RenderToPdf(IFileSource files, XGraphics gfx, DocumentBox box, XRect space);
}