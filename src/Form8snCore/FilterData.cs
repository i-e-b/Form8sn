using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Form8snCore
{
    public static class FilterData
    {
        public static object? ApplyFilter(MappingType type,
            Dictionary<string, string> parameters,
            string[]? sourcePath,
            object? sourceData)
        {
            // return one of ArrayList, string, int, Dictionary<string,object>; For the dict, 'object' must also be one of these.

            switch (type)
            {
                case MappingType.None: return null;
                case MappingType.FixedValue: return GetFixedValue(parameters);

                case MappingType.SplitIntoN:
                    return SplitIntoMaxCount(parameters, sourcePath, sourceData);
                
                case MappingType.TakeWords:
                    return TakeWords(parameters, sourcePath, sourceData);
                
                case MappingType.SkipWords:
                    return SkipWords(parameters, sourcePath, sourceData);
                
                case MappingType.Total:
                    return SumOfAllOnPath(sourcePath, sourceData);
                
                case MappingType.RunningTotal:
                    // TODO...
                    // This should go through a 'Repeat Source'
                    break;
                default: return "Not yet implemented";
            }
            return null;
        }

        private static object? SumOfAllOnPath(string[]? sourcePath, object? sourceData)
        {
            if (sourceData == null || sourcePath == null) return null;
            
            if (sourcePath[0] == "#") throw new Exception("Filters as data sources is not yet implemented"); // TODO: write filter values back to data and cycle with change-count.
            if (sourcePath[0] != "") throw new Exception($"Unexpected root marker: {sourcePath[0]}");
            
            // Walk the path. Each time we hit an array, recurse down the path
            return SumPathRecursive(sourcePath.Skip(1), sourceData);
        }

        /// <summary>
        /// If target is string, split on white space and do an array take, then join on 'space'
        /// If target is an array, do an array take and return resulting array
        /// Otherwise return null 
        /// </summary>
        private static object? TakeWords(Dictionary<string,string> parameters, string[]? sourcePath, object? sourceData)
        {
            if (sourceData == null || sourcePath == null || sourcePath.Length < 1) return null;
            var mcs = parameters.ContainsKey("Count") ? parameters["Count"] : null;
            if (!int.TryParse(mcs, out var count)) return $"Invalid parameter: Count should be an integer, but is {mcs}";
            
            
            var target = FindDataAtPath(sourceData, sourcePath);

            if (target == null) return null;
            
            if (target is string str) return SkipTakeFromString(str, 0, count);
            
            if (target is ArrayList list) return SkipTakeFromArray(list, 0, count);

            return null;
        }
        
        /// <summary>
        /// If target is string, split on white space and do an array take, then join on 'space'
        /// If target is an array, do an array take and return resulting array
        /// Otherwise return null 
        /// </summary>
        private static object? SkipWords(Dictionary<string,string> parameters, string[]? sourcePath, object? sourceData)
        {
            if (sourceData == null || sourcePath == null || sourcePath.Length < 1) return null;
            var mcs = parameters.ContainsKey("Count") ? parameters["Count"] : null;
            if (!int.TryParse(mcs, out var count)) return $"Invalid parameter: Count should be an integer, but is {mcs}";
            
            
            var target = FindDataAtPath(sourceData, sourcePath);

            if (target == null) return null;
            
            if (target is string str) return SkipTakeFromString(str, count, str.Length);
            
            if (target is ArrayList list) return SkipTakeFromArray(list, count, list.Count);

            return null;
        }

        /// <summary>
        /// If target is array, split into sub-arrays
        /// If target is a string, split into character groups
        /// If target is a number, stringify and split as a string
        /// Otherwise return null
        /// </summary>
        private static object? SplitIntoMaxCount(Dictionary<string,string> parameters, string[]? sourcePath, object? sourceData)
        {
            if (sourceData == null || sourcePath == null || sourcePath.Length < 1) return null;
            var mcs = parameters.ContainsKey("MaxCount") ? parameters["MaxCount"] : null;
            if (!int.TryParse(mcs, out var count)) return $"Invalid parameter: MaxCount should be an integer, but is {mcs}";
            
            var target = FindDataAtPath(sourceData, sourcePath);

            if (target == null) return null;
            
            if (target is ArrayList list) return RepackArray(list, count);

            if (target is string str) return RepackString(str, count);

            return RepackString(target.ToString()!, count);
        }

        private static object? SkipTakeFromString(string str, int skip, int take)
        {
            if (take < 1) return null;
            return string.Join(" ", SplitOnWhiteSpace(str).Skip(skip).Take(take));
        }

        private static object? SkipTakeFromArray(ArrayList list, int skip, int take)
        {
            if (take < 1) return null;
            if (take == 1 && list.Count > skip) return list[skip]; // if we're asking for 'first', don't make a 1-long list
            return new ArrayList(list.ToArray().Skip(skip).Take(take).ToArray());
        }

        private static List<string> SplitOnWhiteSpace(string str)
        {
            // string.Split() doesn't have a char-class based option, so...
            var outp = new List<string>();
            var sb = new StringBuilder();

            foreach (var c in str)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (sb.Length > 0) outp.Add(sb.ToString());
                    sb.Clear();
                } else sb.Append(c);
            }
            
            return outp;
        }

        private static object RepackString(string str, int count)
        {
            if (str.Length <= count) return str;
            var outp = new ArrayList();
            var chars = str.ToArray();
            
            var sb = new StringBuilder();
            foreach (var c in chars)
            {
                sb.Append(c);
                if (sb.Length < count) continue;
                
                outp.Add(sb.ToString());
                sb.Clear();
            }
            if (sb.Length > 0) outp.Add(sb.ToString());
            
            return outp;
        }

        private static object RepackArray(ArrayList list, int count)
        {
            if (list.Count <= count) return list;
            var outp = new ArrayList();
            
            var subList = new ArrayList();
            foreach (var item in list)
            {
                subList.Add(item);
                if (subList.Count < count) continue;
                
                outp.Add(subList);
                subList = new ArrayList();
            }
            if (subList.Count > 0) outp.Add(subList);
            
            return outp;
        }

        private static object? FindDataAtPath(object sourceData, string[] path)
        {
            if (path[0] == "#") throw new Exception("Filters as data sources is not yet implemented"); // TODO: write filter values back to data and cycle with change-count.
            if (path[0] != "") throw new Exception($"Unexpected root marker: {path[0]}");

            var sb = new StringBuilder();
            var target = sourceData;
            for (int i = 1; i < path.Length; i++)
            {
                var name = path[i];
                
                sb.Append(".");sb.Append(name);
                if (name.StartsWith('[') && name.EndsWith(']'))
                { // array reference.
                    if (!(target is ArrayList list)) return $"Invalid path: expected list, but was not at {sb}";
                    if (! int.TryParse(name.Trim('[',']'), out var idx)) return $"Invalid path: malformed index at {sb}";
                    if (idx < 0 || idx >= list.Count) return null;
                    
                    target = list[idx];
                }
                else
                {
                    if (!(target is Dictionary<string, object> dict)) return $"Invalid path: expected object, but was not at {sb}";
                    if (dict.ContainsKey(name))
                    {
                        target = dict[name];
                        continue;
                    }
                    return null;
                }
            }
            return target;
        }

        private static double? SumPathRecursive(IEnumerable<string> path, object sourceData)
        {
            var sum = 0.0;
            var target = sourceData;
            var remainingPath = new Stack<string>(path.Reverse());
            while(remainingPath.Count > 0)
            {
                var name = remainingPath.Pop();
                
                if (name.StartsWith('[') && name.EndsWith(']'))
                { // array reference.
                    if (remainingPath.Count < 1) return null; // didn't find a valid item
                    if (!(target is ArrayList list)) return null; // can't do it
                    
                    // Ignore the actual index, and recurse over every actual item
                    var pathArray = remainingPath.ToArray();
                    foreach (var item in list)
                    {
                        if (item == null) continue;
                        var maybe = SumPathRecursive(pathArray, item);
                        if (maybe != null) sum += maybe.Value;
                    }
                    
                    return sum;
                }

                if (target is Dictionary<string, object> dict)
                {
                    if (!dict.ContainsKey(name)) return null; // data ended before path
                    target = dict[name];
                    if (remainingPath.Count > 0) continue; // otherwise we *require* a single value at target
                }

                // We have a single value. Try to get a number from it
                if (target is double d) return d;
                if (target is int i) return i;
                var lastChance = target.ToString();
                if (double.TryParse(lastChance, out var d2)) return d2;
                
                // Not a number-like value:
                return null;
            }
            return sum;
        }

        private static object? GetFixedValue(Dictionary<string,string> parameters)
        {
            return parameters.ContainsKey("Text") ? parameters["Text"] : null;
        }
    }
}