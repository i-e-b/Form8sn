using Form8snCore.Rendering;
using Form8snCore.Rendering.CustomRendering;
using PdfSharp.Drawing;

namespace Form8snCore.DataExtraction;

/// <summary>
/// Encapsulates the data for a template box. This is
/// usually a string unless using a special data type
/// </summary>
public class BoxDataWrapper
{
    private readonly ICustomRenderedBox? _customRendered;
    public bool IsSpecial { get; }
    public string? StringValue { get; }
        
    /// <summary>
    /// Define data for a box that uses a custom renderer
    /// </summary>
    public BoxDataWrapper(ICustomRenderedBox customRendered)
    {
        _customRendered = customRendered;
        IsSpecial = true;
        StringValue = customRendered.GetName();
    }

    /// <summary>
    /// Define data for a box that uses a standard renderer
    /// </summary>
    public BoxDataWrapper(string? value)
    {
        IsSpecial = true;
        StringValue = value;
    }

    /// <summary>
    /// Run the custom rendering function
    /// </summary>
    public void RenderToPdf(IFileSource files, XGraphics gfx, DocumentBox box, XRect space)
    {
        _customRendered?.RenderToPdf(files, gfx, box, space);
    }
}