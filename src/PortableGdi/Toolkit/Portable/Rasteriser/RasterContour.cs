using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Portable.Drawing.Drawing2D;

namespace Portable.Drawing.Toolkit.Portable.Rasteriser
{
    
    /// <summary>
    /// Represents a glyph that has been scaled and positioned for rendering
    /// </summary>
    public class RenderableGlyph: ICollection<RasterContour>
    {
        private readonly List<RasterContour> _store = new();
        
        /// <summary>
        /// Adjust all glyph points based on a transform matrix
        /// </summary>
        public void ApplyTransform(Matrix? transform)
        {
            if (transform == null) return;
            if (transform.IsIdentity) return;
            
            foreach (var contour in _store)
            {
                contour.Transform(transform);
            }
        }

        #region ICollection
        public IEnumerator<RasterContour> GetEnumerator() => _store.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(RasterContour item) => _store.Add(item);
        public void Clear() => _store.Clear();
        public bool Contains(RasterContour item) => _store.Contains(item);
        public void CopyTo(RasterContour[] array, int arrayIndex) => _store.CopyTo(array, arrayIndex);
        public bool Remove(RasterContour item) => _store.Remove(item);
        public int Count => _store.Count;
        public bool IsReadOnly => false;
        #endregion
    }
    
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

        /// <summary>
        /// Apply a transform matrix to this contour
        /// </summary>
        public void Transform(Matrix transform)
        {
            transform.TransformPoints(_points);
        }

        /// <summary>
        /// Return the Axis-aligned bounding box of the contour in its current position
        /// </summary>
        public bool Bounds(out double minX, out double maxX, out double minY, out double maxY)
        {
            minX = double.MaxValue;
            minY = double.MaxValue;
            maxX = double.MinValue;
            maxY = double.MinValue;
            if (_points.Length < 1) return false;
            foreach (var point in _points)
            {
                minX = Math.Min(point.X, minX);
                minY = Math.Min(point.Y, minY);
                
                maxX = Math.Max(point.X, maxX);
                maxY = Math.Max(point.Y, maxY);
            }
            return true;
        }
    }
}