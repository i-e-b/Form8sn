using System;
using System.Collections.Generic;

namespace Portable.Drawing.Toolkit.Fonts.Rendering
{

    /// <summary>
    /// Render a single glyph using a direction and scan-line rule.
    /// </summary>
    public class BresenhamEdgeRasteriser
    {
        public const byte Inside    = 0x01; // pixel is inside the glyph (for filling)

        public const byte DirUp    = 0x02; // pixel Y direction is 'up' (+1)
        public const byte DirDown  = 0x04; // pixel Y direction is 'down' (-1)

        public const byte DirRight = 0x08; // pixel X direction is 'right' (+1)
        public const byte DirLeft  = 0x10; // pixel X direction is 'left' (-1)

        public const byte Touched   = 0x20; // pixel has been processed
        public const byte Dropout   = 0x40; // pixel *might* be a small feature drop-out

        /// <summary>
        /// Render a glyph at the given scale. Result is a grid of flag values.
        /// </summary>
        /// <param name="glyph">Glyph to render</param>
        /// <param name="xScale">Scale factor</param>
        /// <param name="yScale">Scale factor</param>
        public static EdgeWorkspace Rasterise(Glyph? glyph, float xScale, float yScale)
        {
            if (glyph == null) return EdgeWorkspace.Empty();
            if (glyph.GlyphType != GlyphTypes.Simple) return EdgeWorkspace.Empty();

            // glyph sizes are not reliable for this.
            glyph.GetPointBounds(out var xmin, out var xmax, out var ymin, out var ymax);

            var baseline = (float) (glyph.yMin * yScale);
            var leftShift = (float)(-glyph.xMin * xScale * 0.5); // I guess the 0.5 fudge-factor is due to lack of kerning support

            var width = (int)((xmax - xmin) * xScale) + 8;
            var height = (int)((ymax - ymin) * yScale) + 8;

            var workspace = PrepareEdgeWorkspace(width, height);

            // 1. Grid fit / adjust the contours
            var contours = GridFitContours(glyph, xScale, yScale, out var yAdjust);

            // 2. Walk around all the contours, setting scan-line winding data.
            WalkContours(contours, workspace); // also adds extra headroom for super sampling

            // 3. Run each scan line, filling where sum of winding is != 0
            FillScans(workspace);
            //DiagnosticFillScans(workspace);

            // adjust the baseline here, to control 'jitter' caused by pixel-fitting
            if (baseline < 0) yAdjust += 0.4f; // adjust for rounding
            baseline += yAdjust;

            workspace.Baseline = baseline;
            workspace.Shift = leftShift;

            return workspace;
        }

        private static EdgeWorkspace PrepareEdgeWorkspace(int width, int height)
        {
            var requiredBufferSize = width * height;

            var workspace = new EdgeWorkspace
            {
                Height = height,
                Width = width,
                Data = new byte[requiredBufferSize]
            };
            return workspace;
        }

        private static List<GlyphPoint[]> GridFitContours(Glyph? glyph, float xScale, float yScale, out float yAdj)
        {
            yAdj = 0;
            if (glyph == null) return new List<GlyphPoint[]>();
            if (glyph.Points == null || glyph.Points.Length < 1) return new List<GlyphPoint[]>();
            if (glyph.ContourEnds == null || glyph.ContourEnds.Length < 1) return new List<GlyphPoint[]>();

            var input = glyph.NormalisedContours();

            var adjY = double.MaxValue;
            
            var output = new List<GlyphPoint[]>();
            foreach (var contour in input)
            {
                var outContour = new List<GlyphPoint>(contour.Length);
                var prevX = int.MaxValue;
                var prevY = int.MaxValue;
                for (int i = 0; i < contour.Length; i++)
                {
                    int grid = 2;
                    var point = new GlyphPoint{
                        X = contour[i].X * xScale,
                        Y = Math.Round(contour[i].Y * yScale * grid)
                    };


                    // pixel-fit the contour points
                    point.X = Math.Round(point.X);
                    int intfitY = (((int)point.Y) / grid);
                    point.Y = intfitY;

                    if ((int)point.X != prevX || (int)point.Y != prevY) { // ignore segments less than a pixel long
                        outContour.Add(point);

                        // Maybe move this adjustment to the bresenham part, so the baselines are nicer?
                        adjY = Math.Min(adjY, point.Y - (contour[i].Y * yScale)); // calculate how 'wrong' the pixel fit was

                        prevX = (int)point.X;
                        prevY = (int)point.Y;
                    }

                }
                output.Add(outContour.ToArray());
            }

            yAdj = (float)adjY;
            return output;
        }

        // ReSharper disable once UnusedMember.Local
        private static void DiagnosticFillScans(EdgeWorkspace? workspace)
        {
            if (workspace?.Data == null) return;
            var ymax = workspace.Height;
            var xmax = workspace.Width - 1; // space to look ahead

            var data = workspace.Data;

            for (int y = 0; y < ymax; y++)
            {
                var ypos = y * workspace.Width;
                for (int x = 0; x < xmax; x++)
                {
                    if (data[ypos + x] != 0) data[ypos + x] |= Inside;
                }
            }
        }

        private static void FillScans(EdgeWorkspace? workspace)
        {
            if (workspace?.Data == null) return;
            var ymax = workspace.Height;
            var xmax = workspace.Width - 1; // space to look ahead

            var data = workspace.Data;
            var w = workspace.Width;

            for (int y = 0; y < ymax; y++)
            {
                int inside = 0;
                var ypos = y * w;

                for (int x = 0; x < xmax; x++)
                {
                    var v = data[ypos + x];
                    var up = (v & DirUp) > 0;
                    var dn = (v & DirDown) > 0;

                    if (up && dn) {
                        data[ypos + x] |= Inside;
                        continue;
                    }

                    if (up) {inside=1; }
                    if (dn) {inside=0; }

                    if (!up && !dn && inside > 0) data[ypos + x] |= Inside;
                }
            }
        }

        private static void WalkContours(List<GlyphPoint[]>? contours, EdgeWorkspace workspace)
        {
            if (contours == null) return;
            foreach (var contour in contours)
            {
                RenderContour(workspace, contour);
            }
        }

        private static void RenderContour(EdgeWorkspace? workspace, GlyphPoint[]? contour)
        {
            if (contour == null) return;
            if (workspace == null) return;

            var len = contour.Length;
            for (int i = 0; i < len; i++)
            {
                var ptThis = contour[ i      % len];
                var ptNext = contour[(i + 1) % len];
                DirectionalBresenham(workspace, ptThis, ptNext);
            } 
        }

        /// <summary>
        /// Write directions between two points into the workspace.
        /// </summary>
        private static void DirectionalBresenham(EdgeWorkspace? workspace, GlyphPoint start, GlyphPoint end)
        {
            if (workspace?.Data == null) return;

            var fdx = end.X - start.X;
            var fdy = end.Y - start.Y;

            var x0 = (int)start.X;
            var x1 = (int)end.X;
            var y0 = (int)start.Y + 1;
            var y1 = (int)end.Y + 1;

            int dx = x1-x0, sx = x0<x1 ? 1 : -1;
            int dy = y1-y0, sy = y0<y1 ? 1 : -1;
            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;

            byte xWindFlag = fdx < 0 ? DirLeft : DirRight;
            byte yWindFlag = fdy < 0 ? DirDown : DirUp;
            if (dy == 0) yWindFlag = 0;
            if (dx == 0) xWindFlag = 0;

            int pxFlag = yWindFlag | xWindFlag | Touched; // assume first pixel makes a full movement

            if (dy == 0 && dx == 0)
                pxFlag |= Dropout; // a single pixel. We mark for drop-out protection

            int err = (dx>dy ? dx : -dy) / 2;
            int w = workspace.Width;
            var data = workspace.Data;

            for(;;){ // for each point, bit-OR our decided direction onto the pixel

                // set pixel
                data[(y0 * w) + x0] |= (byte)pxFlag;

                // end of line check
                if (x0==x1 && y0==y1) break;

                pxFlag = Touched;
                var e2 = err;
                if (e2 >-dx) { err -= dy; x0 += sx; pxFlag |= xWindFlag; }
                if (e2 < dy) { err += dx; y0 += sy; pxFlag |= yWindFlag; }
            }
        }


    }
}