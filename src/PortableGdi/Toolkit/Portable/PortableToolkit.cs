using System;
using System.Drawing;
using System.Threading;
using Portable.Drawing.Drawing2D;
using Portable.Drawing.Imaging;
using Portable.Drawing.Imaging.ImageFormats;
using Portable.Drawing.Text;

namespace Portable.Drawing.Toolkit.Portable
{
    class PortableToolkit : IToolkit
    {
        private PortableGraphics? _graphics;

        public bool ProcessEvents(bool waitForEvent)
        {
            throw new NotImplementedException();
        }

        public void Quit()
        {
            throw new NotImplementedException();
        }

        public void Wakeup(Thread thread)
        {
            throw new NotImplementedException();
        }

        public int ResolveSystemColor(KnownColor color)
        {
            throw new NotImplementedException();
        }

        public IToolkitGraphics CreateFromHdc(IntPtr hdc, IntPtr hdevice)
        {
            throw new NotImplementedException();
        }

        public IToolkitGraphics CreateFromHwnd(IntPtr hwnd)
        {
            throw new NotImplementedException();
        }

        public IToolkitGraphics CreateFromImage(IToolkitImage image)
        {
            throw new NotImplementedException();
        }

        public IToolkitBrush CreateSolidBrush(Color color)
        {
            throw new NotImplementedException();
        }

        public IToolkitBrush CreateHatchBrush(HatchStyle style, Color foreColor, Color backColor)
        {
            throw new NotImplementedException();
        }

        public IToolkitBrush CreateXorBrush(IToolkitBrush innerBrush)
        {
            throw new NotImplementedException();
        }

        public IToolkitBrush CreateLinearGradientBrush(RectangleF rect, Color color1, Color color2, LinearGradientMode mode)
        {
            throw new NotImplementedException();
        }

        public IToolkitBrush CreateLinearGradientBrush(RectangleF rect, Color color1, Color color2, float angle, bool isAngleScaleable)
        {
            throw new NotImplementedException();
        }

        public IToolkitBrush CreateTextureBrush(TextureBrush properties, IToolkitImage image, RectangleF dstRect, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public IToolkitPen CreatePen(Pen pen)
        {
            throw new NotImplementedException();
        }

        public IToolkitFont CreateFont(Font font, float dpi)
        {
            return new PortableFont(font, dpi);
        }

        public Font CreateDefaultFont()
        {
            throw new NotImplementedException();
        }

        public IToolkitImage CreateImage(PortableImage image, int frame)
        {
            throw new NotImplementedException();
        }

        public IntPtr GetHalftonePalette()
        {
            throw new NotImplementedException();
        }

        public IToolkitTopLevelWindow CreateTopLevelWindow(int width, int height, IToolkitEventSink sink)
        {
            throw new NotImplementedException();
        }

        public IToolkitWindow CreateTopLevelDialog(int width, int height, bool modal, bool resizable, IToolkitWindow dialogParent, IToolkitEventSink sink)
        {
            throw new NotImplementedException();
        }

        public IToolkitWindow CreatePopupWindow(int x, int y, int width, int height, IToolkitEventSink sink)
        {
            throw new NotImplementedException();
        }

        public IToolkitWindow CreateChildWindow(IToolkitWindow parent, int x, int y, int width, int height, IToolkitEventSink sink)
        {
            throw new NotImplementedException();
        }

        public IToolkitMdiClient CreateMdiClient(IToolkitWindow parent, int x, int y, int width, int height, IToolkitEventSink sink)
        {
            throw new NotImplementedException();
        }

        public FontFamily[] GetFontFamilies(IToolkitGraphics graphics)
        {
            throw new NotImplementedException();
        }

        public void GetFontFamilyMetrics(GenericFontFamilies genericFamily, string name, FontStyle style, out int ascent, out int descent, out int emHeight, out int lineSpacing)
        {
            throw new NotImplementedException();
        }

        public IToolkitFont GetFontFromHdc(IntPtr hdc)
        {
            throw new NotImplementedException();
        }

        public IToolkitFont GetFontFromHfont(IntPtr hfont)
        {
            throw new NotImplementedException();
        }

        public IToolkitFont GetFontFromLogFont(object lf, IntPtr hdc)
        {
            throw new NotImplementedException();
        }

        public IToolkitGraphics GetDefaultGraphics()
        {
            return _graphics ??= new PortableGraphics(this);
        }

        public Size GetScreenSize()
        {
            throw new NotImplementedException();
        }

        public Rectangle GetWorkingArea()
        {
            throw new NotImplementedException();
        }

        public void GetWindowAdjust(out int leftAdjust, out int topAdjust, out int rightAdjust, out int bottomAdjust, ToolkitWindowFlags flags)
        {
            throw new NotImplementedException();
        }

        public object RegisterTimer(object owner, int interval, EventHandler expire)
        {
            throw new NotImplementedException();
        }

        public void UnregisterTimer(object cookie)
        {
            throw new NotImplementedException();
        }

        public Point ClientToScreen(IToolkitWindow window, Point point)
        {
            throw new NotImplementedException();
        }

        public Point ScreenToClient(IToolkitWindow window, Point point)
        {
            throw new NotImplementedException();
        }

        public IToolkitClipboard GetClipboard()
        {
            throw new NotImplementedException();
        }

        public IToolkitWindowBuffer CreateWindowBuffer(IToolkitWindow window)
        {
            throw new NotImplementedException();
        }

        public Brush CreateXorBrush(Brush innerBrush)
        {
            throw new NotImplementedException();
        }
    }
}