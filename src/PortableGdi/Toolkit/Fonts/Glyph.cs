using System;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace Portable.Drawing.Toolkit.Fonts
{
    public class Glyph {
        /// <summary>
        /// Glyph type. Used to interpret the other parts of a glyph structure
        /// </summary>
        public GlyphTypes GlyphType;

        /// <summary>
        /// Number of connected point sets in the glyph
        /// </summary>
        public int NumberOfContours;

        // Container box size
        public double xMin, xMax, yMin, yMax;

        /// <summary>
        /// Components if this is a compound glyph (made of transformed copies of other glyphs)
        /// Null if this is a simple glyph
        /// </summary>
        public CompoundComponent[]? Components;

        /// <summary>
        /// character that was used to find this glyph
        /// </summary>
        public char SourceCharacter;

        /// <summary>
        /// The font from which this glyph was loaded
        /// </summary>
        public string? SourceFont;

        /// <summary>
        /// All glyph points (as loaded from font file)
        /// </summary>
        public GlyphPoint[]? Points;

        /// <summary>
        /// Indexes of points where contours end
        /// </summary>
        public int[]? ContourEnds;

        /// <summary>
        /// Cache of normalised contour points
        /// </summary>
        public List<GlyphPoint[]>? ContourCache;

        /// <summary>
        /// Reduce the glyph to a set of simple point contours.
        /// Curves will be re-drawn as segments.
        /// This list will be cached, so do NOT directly edit the output
        /// </summary>
        public List<GlyphPoint[]> NormalisedContours() {
            if (ContourCache != null) return ContourCache;

            if (Points == null || Points.Length < 1) return new List<GlyphPoint[]>();
            if (ContourEnds == null || ContourEnds.Length < 1) return new List<GlyphPoint[]>();

            var outp = new List<GlyphPoint[]>();
            var p = 0;
            var c = 0;
            var contour = new List<GlyphPoint>();
            var pLen = Points.Length;

            while (p < pLen)
            {
                var point = Points[p];

                var xpos = point.X - xMin;
                var ypos = point.Y - yMin;

                if (xpos < 0) xpos = 0;
                if (ypos < 0) ypos = 0;

                contour.Add(new GlyphPoint { X = xpos, Y = ypos, OnCurve = point.OnCurve });

                if (p == ContourEnds[c])
                {
                    outp.Add(Contour.NormaliseContour(contour));
                    contour.Clear();
                    c++;
                }

                p++;
            }
            ContourCache = outp;
            return outp;
        }

        /// <summary>
        /// Return the boundaries of the points on this glyph.
        /// This ignored the stated min/max bounds for positioning
        /// </summary>
        public void GetPointBounds(out double xmin, out double xmax, out double ymin, out double ymax)
        {
            xmin = 0d;
            xmax = 4d;
            ymin = 0d;
            ymax = 4d;
            if (Points == null) return;
            for (int i = 0; i < Points.Length; i++)
            {
                var p = Points[i];
                xmin = Math.Min(p.X, xmin);
                xmax = Math.Max(p.X, xmax);
                ymin = Math.Min(p.Y, ymin);
                ymax = Math.Max(p.Y, ymax);
            }
        }

    }
}