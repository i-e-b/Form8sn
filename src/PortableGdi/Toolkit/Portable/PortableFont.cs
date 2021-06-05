using System;
using Portable.Drawing.Drawing2D;

namespace Portable.Drawing.Toolkit.Portable
{
    internal class PortableFont : IToolkitFont
    {
        public PortableFont(Font fontSpec, float dpi)
        {
            // TODO: load a font from filesystem. Try local path or special path if available. Maybe guess special paths?
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts, Environment.SpecialFolderOption.DoNotVerify);
        }

        public void Dispose() { }

        public void Select(IToolkitGraphics graphics)
        {
            if (!(graphics is PortableGraphics pg)) return;
            pg.CurrentFont = this;
        }

        public IntPtr GetHfont() => IntPtr.Zero;

        public void ToLogFont(object lf, IToolkitGraphics graphics) { }
    }
}