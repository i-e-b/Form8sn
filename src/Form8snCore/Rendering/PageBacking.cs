using System;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Form8snCore.Rendering
{
    internal class PageBacking : IDisposable
    {
        public PdfPage? ExistingPage;
        public XImage? BackgroundImage;

        public void Dispose() => BackgroundImage?.Dispose();
    }
}