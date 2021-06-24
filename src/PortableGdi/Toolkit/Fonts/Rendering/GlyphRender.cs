using System;
using System.Collections.Generic;
using Portable.Drawing.Imaging;

namespace Portable.Drawing.Toolkit.Fonts.Rendering
{
    
    /// <summary>
    /// Image renderers.
    /// These use the Scanline rasteriser internally.
    /// See https://developer.apple.com/fonts/TrueType-Reference-Manual/RM02/Chap2.html
    /// </summary>
    public class GlyphRenderer
    {

        /// <summary>
        /// Draw a single glyph, picking a renderer based on size
        /// </summary>
        public static void DrawGlyph(IBitmapProxy img, float dx, float dy, float scale, Glyph glyph, int color)
        {
            try
            {
                if (scale <= 0.03f) // Optimised for smaller sizes
                {
                    RenderSubPixel_RGB_Super3(img, dx, dy, scale, glyph, color);
                }
                else // Optimised for larger sizes
                {
                    RenderSuperSampled(img, dx, dy, scale, glyph, color);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // tried to write off the end of the img
            }
        }


        private static readonly Dictionary<string, RenderCacheEntry> _renderCache = new Dictionary<string, RenderCacheEntry>();

        /// <summary>
        /// Render small font sizes with a super-sampling sub-pixel algorithm. Super-samples only in the x direction.
        /// This renderer may cache its output
        /// </summary>
        public static void RenderSubPixel_RGB_Super3(IBitmapProxy img, float dx, float dy, float scale, Glyph glyph, int color)
        {
            if (glyph == null) throw new ArgumentNullException(nameof(glyph));
            const int bright = 17;

            float leftShift;
            float baseline;
            int[] table;
            int height;
            int width;

            var cacheKey = glyph.SourceCharacter + scale + glyph.SourceFont;
            if (_renderCache.ContainsKey(cacheKey)) {
                var ce = _renderCache[cacheKey];
                table = ce.Table;
                width = ce.Width;
                height = ce.Height;
                leftShift = ce.LeftShift;
                baseline = ce.Baseline;
            }
            else
            {
                var workspace = BresenhamEdgeRasteriser.Rasterise(glyph, scale * 3, scale);
                height = workspace.Height;
                width = workspace.Width / 3;
                baseline = workspace.Baseline;
                leftShift = workspace.Shift;

                if (baseline*baseline < 1) baseline = 0;

                var topsFlag = BresenhamEdgeRasteriser.DirRight | BresenhamEdgeRasteriser.DirLeft | BresenhamEdgeRasteriser.Dropout;

                var data = workspace.Data;
                var w = workspace.Width;
                var tableSize = width * height;

                // Not cached: Build the RGB table for the char
                table = new int[tableSize];
                for (int y = 0; y < height; y++)
                {
                    var yBase = y * w;
                    var yout = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        var r = 0;
                        var g = 0;
                        var b = 0;
                        // ReSharper disable JoinDeclarationAndInitializer
                        int tops, ins, left, right;
                        // ReSharper restore JoinDeclarationAndInitializer

                        var _1 = data[yBase + x * 3];
                        var _2 = data[yBase + x * 3 + 1];
                        var _3 = data[yBase + x * 3 + 2];

                        // first try the simple case of all pixels in:
                        if (
                               (_1 & BresenhamEdgeRasteriser.Inside) > 0
                            && (_2 & BresenhamEdgeRasteriser.Inside) > 0
                            && (_3 & BresenhamEdgeRasteriser.Inside) > 0
                            )
                        {
                            table[yout + x] = 0xffffff;
                            continue;
                        }
                        var topS = 3;
                        var insS = 5;
                        var sideS = 3;

                        var flag = _1;
                        tops = (flag & topsFlag) > 0 ? topS : 0;
                        ins = (flag & BresenhamEdgeRasteriser.Inside) > 0 ? insS : 0;
                        left = (flag & BresenhamEdgeRasteriser.DirUp) > 0 ? sideS : 0;
                        right = (flag & BresenhamEdgeRasteriser.DirDown) > 0 ? sideS : 0;
                        if (ins > 0 || left > 0 || right > 0) tops = 0;

                        b += tops + ins + (left * 2);
                        g += tops + ins + (left) + (right);
                        r += tops + ins + (right * 2);

                        flag = _2;
                        tops = (flag & topsFlag) > 0 ? topS : 0;
                        ins = (flag & BresenhamEdgeRasteriser.Inside) > 0 ? insS : 0;
                        left = (flag & BresenhamEdgeRasteriser.DirUp) > 0 ? sideS : 0;
                        right = (flag & BresenhamEdgeRasteriser.DirDown) > 0 ? sideS : 0;
                        if (ins > 0 || left > 0 || right > 0) tops = 0;

                        b += tops + ins + (left * 2);
                        g += tops + ins + (left) + (right);
                        r += tops + ins + (right * 2);

                        flag = _3;
                        tops = (flag & topsFlag) > 0 ? topS : 0;
                        ins = (flag & BresenhamEdgeRasteriser.Inside) > 0 ? insS : 0;
                        left = (flag & BresenhamEdgeRasteriser.DirUp) > 0 ? sideS : 0;
                        right = (flag & BresenhamEdgeRasteriser.DirDown) > 0 ? sideS : 0;
                        if (ins > 0 || left > 0 || right > 0) tops = 0;

                        b += tops + ins + (left * 2);
                        g += tops + ins + left + right;
                        r += tops + ins + (right * 2);

                        r *= bright;
                        g *= bright;
                        b *= bright;

                        Saturate(ref r, ref g, ref b);

                        table[yout + x] = (r << 16) + (g << 8) + b;
                    }
                }

                // add to cache
                var entry = new RenderCacheEntry{
                    Width = width,
                    Height = height,
                    Baseline = baseline,
                    LeftShift = leftShift,
                    Table = table
                };
                if (_renderCache.ContainsKey(cacheKey)) _renderCache[cacheKey] = entry;
                else _renderCache.Add(cacheKey, entry);
            }

            // Stupid way to keep the cache smallish:
            if (_renderCache.Count > 100)
                _renderCache.Clear();

            // now render the table to an image
            for (int y = 0; y < height; y++)
            {
                var yBase = y * width;
                for (int x = 0; x < width; x++) {
                    int c = table[yBase + x];

                    if (c == 0) continue;

                    var r = (byte)((c & 0xff0000) >> 16);
                    var g = (byte)((c & 0x00ff00) >> 8);
                    var b = (byte)((c & 0x0000ff));

                    img.BlendSubPixel((int)(dx + x - leftShift), (int)(dy - y - baseline), r, g, b, color);
                }
            }
        }

        /// <summary>
        /// Smoothing renderer for larger sizes. Does not gurantee sharp pixel edges, loses edges on small sizes
        /// This renderer will not cache its output
        /// </summary>
        public static void RenderSuperSampled(IBitmapProxy img, float dx, float dy, float scale, Glyph glyph, int color)
        {
            // Over-scale sizes. More y precision works well for latin glyphs
            const int xos = 2;
            const int yos = 3;

            // Render over-sized, then average back down
            var workspace = BresenhamEdgeRasteriser.Rasterise(glyph, scale * xos, scale * yos);
            var height = workspace.Height / yos;
            var width = workspace.Width / xos;
            var baseline = workspace.Baseline;
            var leftShift = workspace.Shift;
            baseline /= yos;
            baseline += 1; // account for Y overrun in our sampling

            var data = workspace.Data;
            var w = workspace.Width;
            var w2 = 2*workspace.Width;
            var w3 = 3*workspace.Width;
            var w4 = 4*workspace.Width;

            for (int y = 0; y < height - 1; y++)
            {
                var sy = y * yos * w;

                for (int x = 0; x < width - 1; x++)
                {
                    var sx = x*xos;
                    int v;
                    v  = data[sy   + sx  ] & BresenhamEdgeRasteriser.Inside; // based on `INSIDE` == 1
                    v += data[sy   + sx+1] & BresenhamEdgeRasteriser.Inside;
                    v += data[sy+w + sx  ] & BresenhamEdgeRasteriser.Inside;
                    v += data[sy+w + sx+1] & BresenhamEdgeRasteriser.Inside;
                    v += data[sy+w2+ sx  ] & BresenhamEdgeRasteriser.Inside;
                    v += data[sy+w2+ sx+1] & BresenhamEdgeRasteriser.Inside;

                    // slightly over-run in Y to smooth slopes further. The `ScanlineRasteriser` adds some buffer space for this
                    v += data[sy+w3+ sx  ] & BresenhamEdgeRasteriser.Inside;
                    v += data[sy+w3+ sx+1] & BresenhamEdgeRasteriser.Inside;
                    v += data[sy+w4+ sx  ] & BresenhamEdgeRasteriser.Inside;
                    v += data[sy+w4+ sx+1] & BresenhamEdgeRasteriser.Inside;

                    if (v == 0) continue;
                    v *= 255 / 10;

                    img.BlendPixel((int)(dx + x - leftShift), (int)(dy - y - baseline), color, (byte)v);
                }
            }
        }

        private static void Saturate(ref int r, ref int g, ref int b)
        {
            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (b > 255) b = 255;
            if (r < 0) r = 0;
            if (g < 0) g = 0;
            if (b < 0) b = 0;
        }
        
    }
    
    internal class RenderCacheEntry
    {
        public int[] Table;
        public float LeftShift;
        public float Baseline;
        public int Width;
        public int Height;
    }
}