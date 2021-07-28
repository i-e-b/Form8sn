using System;
using System.Diagnostics;
using NUnit.Framework;

using Real = System.Drawing;
using Port = Portable.Drawing;

namespace PortableGdiTests
{
    [TestFixture]
    public class BasicDrawingTests
    {
        [Test]
        public void portable_gdi_can_draw_to_an_image_like_real_gdi()
        {
            // We get fonts outside of the timing, as the OS has pre-cached font descriptors
            var portFont = new Port.Font("Consolas", 24.5f);
            var realFont = new Real.Font("Consolas", 24.5f);
            var sw = new Stopwatch();
            
            // Normal GDI
            sw.Restart();
            using (var bmp = new Real.Bitmap(800, 600, Real.Imaging.PixelFormat.Format24bppRgb))
            {
                using var g = Real.Graphics.FromImage(bmp);
                
                g.InterpolationMode = Real.Drawing2D.InterpolationMode.High;
                g.SmoothingMode = Real.Drawing2D.SmoothingMode.HighQuality;
                g.TextRenderingHint = Real.Text.TextRenderingHint.AntiAlias;
                
                g.Clear(Real.Color.Lavender);
                g.DrawLine(Real.Pens.Chocolate, 10,10,100,100);
                g.FillPolygon(Real.Brushes.Black, new Real.PointF[]{new (150, 10), new (250, 10), new (200, 100)});
                g.DrawString("Hello, Graphics!", realFont, Real.Brushes.Brown, 10, 250);
                
                bmp.Save("Z_simple_real.png", Real.Imaging.ImageFormat.Png);
            }
            sw.Stop();
            Console.WriteLine($"Real GDI took {sw.ElapsedMilliseconds}ms");
            
            // Portable GDI
            sw.Restart();
            using (var bmp = new Port.Bitmap(800, 600, Port.Imaging.PixelFormat.Format24bppRgb))
            {
                using var g = Port.Graphics.FromImage(bmp);
                
                g.InterpolationMode = Port.Drawing2D.InterpolationMode.High;
                g.SmoothingMode = Port.Drawing2D.SmoothingMode.HighQuality;
                g.TextRenderingHint = Port.Text.TextRenderingHint.AntiAlias;
                
                g.Clear(Port.Color.Lavender);
                g.DrawLine(Port.Pens.Chocolate, 10,10,100,100);
                g.FillPolygon(Port.Brushes.Black, new Port.PointF[]{new (150, 10), new (250, 10), new (200, 100)});
                g.DrawString("Hello, Graphics!", portFont, Port.Brushes.Brown, 10, 250);
                
                bmp.Save("Z_simple_portable.png", Port.Imaging.ImageFormat.Png);
            }
            sw.Stop();
            Console.WriteLine($"Portable GDI took {sw.ElapsedMilliseconds}ms");
            
            Console.WriteLine("Files generated. Compare output");
        }
    }
}