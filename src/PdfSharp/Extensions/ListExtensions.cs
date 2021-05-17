using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PdfSharp.Extensions
{
    public static class ListExtensions
    {
        public static List<T> ToListOrEmpty<T>(this IEnumerable<T>? source)
        {
            if (source == null) return new List<T>();
            return source.ToList();
        }
        
        public static List<T> CastToListOrEmpty<T>(this IEnumerable? source)
        {
            if (source == null) return new List<T>();
            return source.Cast<T>().ToList();
        }
    }
}