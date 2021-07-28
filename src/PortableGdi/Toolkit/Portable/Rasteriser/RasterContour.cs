using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Portable.Drawing.Toolkit.Portable.Rasteriser
{
    /// <summary>
    /// Represents a closed loop on a 2D plane,
    /// which can be queried as pairs of points
    /// </summary>
    public class RasterContour
    {
        private readonly Vector2[] _points;

        public RasterContour(Vector2[] points)
        {
            _points = points;
        }
        
        public RasterContour(IEnumerable<Vector2> points)
        {
            _points = points.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PairCount() => _points!.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VecSegment2 Pair(int idx)
        {
            return new VecSegment2 {
                A = _points![idx    % _points.Length],
                B = _points[(idx+1) % _points.Length]
            };
        }

        public static RasterContour[] Combine(params PointF[][] polygons)
        {
            var c = polygons!.Length;
            var outp = new RasterContour[c];

            for (int i = 0; i < c; i++) outp[i] = new RasterContour(polygons[i]!.Select(p => new Vector2(p)).ToArray());

            return outp;
        }
    }
}