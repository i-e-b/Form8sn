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
            
            var portFontSmall = new Port.Font("Calibri", 6);
            var realFontSmall = new Real.Font("Calibri", 6);
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
                
                var sz = g.MeasureString("Hello, Graphics!", realFont);
                g.FillRectangle(Real.Brushes.Ivory, 10, 250, sz.Width, sz.Height);
                g.DrawString("Hello, Graphics!", realFont, Real.Brushes.Brown, 10, 250);
                g.DrawString("This should be tricky to render %$&@ 日本人 (にほんじん) ", realFontSmall, Real.Brushes.Maroon, 10, 250+sz.Height);
                
                var tf = g.Transform;
                tf.Translate(sz.Width + 50, 250);
                tf.Rotate(10);
                tf.Shear(0.1f, 0);
                g.Transform = tf;
                g.DrawString("transforms", realFont, Real.Brushes.Sienna, 0, 0);
                g.ResetTransform();
                
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
                
                var sz = g.MeasureString("Hello, Graphics!", portFont);
                g.FillRectangle(Port.Brushes.Ivory, 10, 250, sz.Width, sz.Height);
                g.DrawString("Hello, Graphics!", portFont, Port.Brushes.Brown, 10, 250);
                g.DrawString("This should be tricky to render %$&@ 日本人 (にほんじん) ", portFontSmall, Port.Brushes.Maroon, 10, 250+sz.Height);
                
                var tf = g.Transform;
                tf.Translate(sz.Width + 50, 250);
                tf.Rotate(10);
                tf.Shear(0.1f, 0);
                g.Transform = tf;
                g.DrawString("transforms", portFont, Port.Brushes.Sienna, 0, 0);
                g.ResetTransform();
                bmp.Save("Z_simple_portable.png", Port.Imaging.ImageFormat.Png);
            }
            sw.Stop();
            Console.WriteLine($"Portable GDI took {sw.ElapsedMilliseconds}ms");
            
            Console.WriteLine("Files generated. Compare output");
        }
    }
}