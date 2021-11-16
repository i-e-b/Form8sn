using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Form8snCore.HelpersAndConverters;

namespace Form8snCore.Tests.Helpers
{
    public static class TestHelperExtensions
    {
        public static List<DataNode>? X(this List<DataNode>? from, string name)
        {
            return from?.FirstOrDefault(n=>n.Name == name)?.Nodes;
        }
        public static DataNode? N(this List<DataNode>? from, string name)
        {
            return from?.FirstOrDefault(n=>n.Name == name);
        }

        public static List<object?> ToObjectList(this IEnumerable? src)
        {
            var result = new List<object?>();
            if (src is null) return result;
            
            var e = src.GetEnumerator();
            while (e.MoveNext())
            {
                result.Add(e.Current);
            }

            return result;
        }
        
        public static List<string?> ToStringList(this IEnumerable? src)
        {
            var result = new List<string?>();
            if (src is null) return result;
            
            var e = src.GetEnumerator();
            while (e.MoveNext())
            {
                result.Add(e.Current?.ToString());
            }

            return result;
        }
    }
}