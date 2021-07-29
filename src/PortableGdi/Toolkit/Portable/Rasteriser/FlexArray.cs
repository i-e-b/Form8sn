using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Portable.Drawing.Toolkit.Portable.Rasteriser
{
    internal class FlexArray<T>
    {
        private readonly List<T> _store = new();
        
        public T this[int idx]
        {
            get => _store[idx];
            set {
                EnsureRange(idx); 
                _store[idx] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureRange(int idx)
        {
            if (_store.Count > idx) return;
            var difference = (idx - _store.Count + 1) * 2; // extend further to prevent too many calls
            _store.AddRange(Enumerable.Repeat(default(T)!, difference));
        }
    }
}