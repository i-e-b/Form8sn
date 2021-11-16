using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Form8snCore.FileFormats;

namespace Form8snCore.DataExtraction
{
    public static class MappingActions
    {
        /// <summary>
        /// returns one of ArrayList, string, int, Dictionary&lt;string,object>; For the dict, 'object' must also be one of these.
        /// </summary>
        /// <param name="type">Filter to apply</param>
        /// <param name="parameters">parameters for this filter</param>
        /// <param name="sourcePath">path the filter is pointing to</param>
        /// <param name="originalPath">if filter is being applied over repeater data, this should be the original data source</param>
        /// <param name="otherFilters">Other filters in the project that can be used as sources</param>
        /// <param name="sourceData">complete data set for the template</param>
        /// <param name="repeaterData">if we are mapping data for a repeating page, put the page specific data here</param>
        /// <param name="runningTotals"></param>
        public static object? ApplyFilter(MappingType type,
            Dictionary<string, string> parameters,
            string[]? sourcePath,
            string[]? originalPath,
            Dictionary<string, MappingInfo> otherFilters,
            object? sourceData,
            object? repeaterData,
            Dictionary<string, decimal>? runningTotals)
        {
            var redirects = new HashSet<string>(); // for detecting loops in filter-over-filter
            var filterPackage = new FilterState
            {
                Type = type,
                Params = parameters,
                SourcePath = sourcePath,
                OriginalPath = originalPath,
                FilterSet = otherFilters,
                Data = sourceData,
                RepeaterData = repeaterData,
                Redirects = redirects,
                RunningTotals = runningTotals ?? new Dictionary<string, decimal>()
            };
            var value = ApplyFilterRecursive(filterPackage);

            if (runningTotals != null)
            {
                if ((value is decimal d) ||
                    (value != null && decimal.TryParse(value.ToString(), out d)))
                {
                    // Both the literal path, and the original (in case we are repeating a page)
                    AddRunningTotal(runningTotals, sourcePath, originalPath, d);
                }
            }

            return value;
        }


        /// <summary>
        /// This is the core of filtering. It can be called by other filters to allow sub-filtering
        /// </summary>
        private static object? ApplyFilterRecursive(FilterState pkg)
        {
            switch (pkg.Type)
            {
                case MappingType.None:
                    return GetDataAtPath(pkg);
                
                case MappingType.FixedValue:
                    return GetFixedValue(pkg);

                case MappingType.SplitIntoN:
                    return SplitIntoMaxCount(pkg);

                case MappingType.TakeWords:
                    return TakeWords(pkg);

                case MappingType.SkipWords:
                    return SkipWords(pkg);

                case MappingType.Total:
                    return SumOfAllOnPath(pkg);
                
                case MappingType.Count:
                    return CountOfItem(pkg);
                
                case MappingType.IfElse:
                    return ReturnIfElse(pkg);
                
                case MappingType.Join:
                    return JoinValues(pkg);
                
                case MappingType.Distinct:
                    return ListOfDistinctValues(pkg);
                
                case MappingType.TakeAllValues:
                    return ListOfAllValues(pkg);
                
                case MappingType.Concatenate:
                    return ConcatenateList(pkg);
                
                case MappingType.FormatAllAsDate:
                    return FormatAllValuesAsDates(pkg);
                
                case MappingType.FormatAllAsNumber:
                    return FormatAllValuesAsNumberStrings(pkg);

                case MappingType.RunningTotal:
                    return RunningTotal(pkg);
                
                default: return "Not yet implemented";
            }
        }

        private static object? JoinValues(FilterState pkg)
        {
            if (pkg.Data == null || pkg.SourcePath == null || pkg.SourcePath.Length < 1) return null;
            var param = pkg.Params;

            object? GetMappedData(string s) => string.IsNullOrWhiteSpace(s) ? null : GetDataAtPath(pkg.NewPath(s.Split('.') ?? Array.Empty<string>()));

            // Get the params.
            // Get the data
            // If data == expected return recurse success path, else recurse fail path
            
            var target = GetDataAtPath(pkg);
            
            var infixKey = nameof(JoinPathsMappingParams.Infix);
            var infix = param.ContainsKey(infixKey) ? param[infixKey]??"" : "";
            
            var extraDataKey = nameof(JoinPathsMappingParams.ExtraData);
            var extraDataPath = param.ContainsKey(extraDataKey) ? param[extraDataKey] : "";

            if (string.IsNullOrWhiteSpace(extraDataPath!)) return target;
            
            var extraData =  GetMappedData(extraDataPath)?.ToString();
            
            if (string.IsNullOrWhiteSpace(extraData!)) return target;

            return target + infix + extraData;
        }

        private static object? ReturnIfElse(FilterState pkg)
        {
            if (pkg.Data == null || pkg.SourcePath == null || pkg.SourcePath.Length < 1) return null;
            var param = pkg.Params;

            object? GetMappedData(string s) => string.IsNullOrWhiteSpace(s) ? null : GetDataAtPath(pkg.NewPath(s.Split('.')));

            // Get the params.
            // Get the data
            // If data == expected return recurse success path, else recurse fail path
            
            var target = GetDataAtPath(pkg);
            
            var expectKey = nameof(IfElseMappingParams.ExpectedValue);
            var expected = param.ContainsKey(expectKey) ? param[expectKey] : "";
            
            var sameKey = nameof(IfElseMappingParams.Same);
            var samePath = param.ContainsKey(sameKey) ? param[sameKey] : "";
            
            var differentKey = nameof(IfElseMappingParams.Different);
            var differentPath = param.ContainsKey(differentKey) ? param[differentKey] : "";

            if (target == null) return GetMappedData(string.IsNullOrEmpty(expected) ? samePath : differentPath);

            return GetMappedData(target.ToString() == expected ? samePath : differentPath);
        }

        private static object CountOfItem(FilterState pkg)
        {
            if (pkg.Data == null || pkg.SourcePath == null || pkg.SourcePath.Length < 1) return 0;

            var target = GetDataAtPath(pkg);
            if (target == null) return 0;
            
            if (target is string str) return str.Length;
            if (target is ArrayList list) return list.Count;
            
            return 0;
        }

        private static object? FormatAllValuesAsNumberStrings(FilterState pkg)
        {
            var allValues = FindAllOnPath(pkg);
            var outp = new ArrayList();
            
            var param = pkg.Params;

            var dpKey = nameof(NumberMappingParams.DecimalPlaces);
            var dpStr = param.ContainsKey(dpKey) ? param[dpKey] : "2";
            if (!int.TryParse(dpStr, out var decimalPlaces)) decimalPlaces = 2;
            if (decimalPlaces < 0 || decimalPlaces > 20) decimalPlaces = 2;

            var tsKey = nameof(NumberMappingParams.ThousandsSeparator);
            var thousands = param.ContainsKey(tsKey) ? param[tsKey] : "";

            var dcKey = nameof(NumberMappingParams.DecimalSeparator);
            var decimalSeparator = param.ContainsKey(dcKey) ? param[dcKey] : ".";
            if (string.IsNullOrEmpty(decimalSeparator)) decimalSeparator = ".";

            var preKey = nameof(NumberMappingParams.Prefix);
            var prefix = param.ContainsKey(preKey) ? param[preKey] : "";

            var postKey = nameof(NumberMappingParams.Postfix);
            var postfix = param.ContainsKey(postKey) ? param[postKey] : "";
            
            
            foreach (var value in allValues)
            {
                var dec = Decimalise(value);
                if (dec == null) continue;
                
                outp.Add(prefix + DisplayFormatter.FloatToString(dec.Value, decimalPlaces, decimalPlaces, decimalSeparator, thousands) + postfix);
            }
            return outp.Count < 1 ? null : outp;
        }

        private static decimal? Decimalise(object? o)
        {
            if (o == null) return null;
            if (o is decimal dec) return dec;
            if (decimal.TryParse(o.ToString(), out dec)) return dec;
            return null;
        }

        private static object? FormatAllValuesAsDates(FilterState pkg)
        {
            var allValues = FindAllOnPath(pkg);
            var outp = new ArrayList();
            var param = pkg.Params;

            var key = nameof(DateDisplayParams.FormatString);
            var fmt = param.ContainsKey(key) ? param[key] : null;

            foreach (var value in allValues)
            {
                try
                {
                    var str = value.ToString();
                    
                    // first, try the exact format that *should* be used
                    if (DateTime.TryParseExact(str, "yyyy-MM-dd", null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces, out var dt)) outp.Add(dt.ToString(fmt));

                    // try a more general search
                    else if (DateTime.TryParse(str, out dt)) outp.Add(dt.ToString(fmt));
                }
                catch { /*ignore*/ }
            }
            return outp.Count < 1 ? null : outp;
        }

        private static object? RunningTotal(FilterState pkg)
        {
            if (pkg.SourcePath == null) return null;
            
            var pathKey = JoinPathWithoutArrayIndexes(pkg.SourcePath);
            if (pkg.RunningTotals.ContainsKey(pathKey)) return pkg.RunningTotals[pathKey];
            
            return 0.0m;
        }

        private static object? ConcatenateList(FilterState pkg)
        {
            if (pkg.Data == null || pkg.SourcePath == null || pkg.SourcePath.Length < 1) return null;

            var target = GetDataAtPath(pkg);
            if (target == null) return null;
            
            var preKey = nameof(JoinMappingParams.Prefix);
            var prefix = pkg.Params.ContainsKey(preKey) ? pkg.Params[preKey] : "";
            var inKey = nameof(JoinMappingParams.Infix);
            var infix = pkg.Params.ContainsKey(inKey) ? pkg.Params[inKey] : "";
            var postKey = nameof(JoinMappingParams.Postfix);
            var postfix = pkg.Params.ContainsKey(postKey) ? pkg.Params[postKey] : "";

            if (target is string str) return $"{prefix}{str}{postfix}";

            if (target is ArrayList list) return JoinAsString(list, prefix, infix, postfix);

            return null;
        }

        private static string JoinAsString(ArrayList list, string prefix, string infix, string postfix)
        {
            var sb = new StringBuilder(prefix);

            var first = true;
            foreach (var item in list)
            {
                if (first) first = false;
                else sb.Append(infix);
                sb.Append(item);
            }
            
            sb.Append(postfix);
            return sb.ToString();
        }

        /// <summary>
        /// If target is string, split on white space and do an array take, then join on 'space'
        /// If target is an array, do an array take and return resulting array
        /// Otherwise return null 
        /// </summary>
        private static object? TakeWords(FilterState pkg)
        {
            if (pkg.Data == null || pkg.SourcePath == null || pkg.SourcePath.Length < 1) return null;
            var countKey = nameof(TakeMappingParams.Count);
            var mcs = pkg.Params.ContainsKey(countKey) ? pkg.Params[countKey] : null;
            if (!int.TryParse(mcs, out var count)) return $"Invalid parameter: Count should be an integer, but is {mcs}";


            var target = GetDataAtPath(pkg);

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
        private static object? SkipWords(FilterState pkg)
        {
            if (pkg.Data == null || pkg.SourcePath == null || pkg.SourcePath.Length < 1) return null;
            var countKey = nameof(SkipMappingParams.Count);
            var mcs = pkg.Params.ContainsKey(countKey) ? pkg.Params[countKey] : null;
            if (!int.TryParse(mcs, out var count)) return $"Invalid parameter: Count should be an integer, but is {mcs}";

            var target = GetDataAtPath(pkg);

            if (target == null) return null;

            if (target is string str) return SkipTakeFromString(str, count, int.MaxValue);

            if (target is ArrayList list) return SkipTakeFromArray(list, count, int.MaxValue);

            return null;
        }

        /// <summary>
        /// If target is array, split into sub-arrays
        /// If target is a string, split into character groups
        /// If target is a number, stringify and split as a string
        /// Otherwise return null
        /// </summary>
        private static object? SplitIntoMaxCount(FilterState pkg)
        {
            if (pkg.Data == null || pkg.SourcePath == null || pkg.SourcePath.Length < 1) return null;
            var countKey = nameof(MaxCountMappingParams.MaxCount);
            var mcs = pkg.Params.ContainsKey(countKey) ? pkg.Params[countKey] : null;
            if (!int.TryParse(mcs, out var count)) return $"Invalid parameter: MaxCount should be an integer, but is {mcs}";

            var target = GetDataAtPath(pkg);

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
                }
                else sb.Append(c);
            }

            if (sb.Length > 0) outp.Add(sb.ToString());

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
        
        private static object? GetDataAtPath(FilterState pkg)
        {
            var path = pkg.SourcePath;
            var data = pkg.Data;
            if (path == null || data == null) return null;

            var root = path[0];
            var pathSkip = 1;

            // Empty root means plain data, otherwise, we expect '#', 'D', 'P'
            if (root == "#") // Data filter
            {
                if (path.Length < 2) return null;
                var newFilter = pkg.RedirectFilter(path[1]);
                if (newFilter == null) return null;
                data = ApplyFilterRecursive(newFilter);
                if (data == null) return null;
                pathSkip = 2; // root and filter name
            }
            else if (root == "D") // Page repeat
            {
                if (pkg.RepeaterData == null) throw new Exception("Was asked for repeat page data, but none was supplied");
                if (path.Length < 2) return null;
                data = pkg.RepeaterData;
                pathSkip = 1; // root 'D'
            }
            else if (root == "P")
            {
                return GetPageInfoData(pkg);
            }
            else if (path[0] != "") throw new Exception($"Unexpected root marker: {path[0]}");

            var sb = new StringBuilder();
            var target = data;
            for (int i = pathSkip; i < path.Length; i++)
            {
                var name = path[i];

                sb.Append(".");
                sb.Append(name);
                if (name.StartsWith('[') && name.EndsWith(']'))
                {
                    // array reference.
                    if (!(target is ArrayList list)) return $"Invalid path: expected list, but was not at {sb}";
                    if (!int.TryParse(name.Trim('[', ']'), out var idx)) return $"Invalid path: malformed index at {sb}";
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

        private static object? GetPageInfoData(FilterState pkg)
        {
            if (pkg.SourcePath is null) return null;
            if (pkg.SourcePath.Length < 2) return null;
            switch (pkg.SourcePath[1])
            {
                case "PageGenerationDate":
                    return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                default: return "Unmapped page info data: "+pkg.SourcePath[1];
            }
        }

        private static object? ListOfDistinctValues(FilterState pkg)
        {
            var basic = FindAllOnPath(pkg).ToArray();
            var value = basic.Distinct().ToArray();
            if (value.Length < 1) return null;
            return new ArrayList(value);
        }
        
        private static object? ListOfAllValues(FilterState pkg)
        {
            var value = FindAllOnPath(pkg).ToArray();
            if (value.Length < 1) return null;
            return new ArrayList(value);
        }

        private static object? SumOfAllOnPath(FilterState pkg)
        {
            var found = FindAllOnPath(pkg).ToList();
            if (found.Count < 1) return null;
            
            var sum = 0.0m;
            foreach (object value in found)
            {
                if (value is double d) sum += (decimal) d;
                else if (value is int i) sum += i;
                else if (decimal.TryParse(value.ToString(), out var dec)) sum += dec;
            }
            return sum;
        }

        private static IEnumerable<object> FindAllOnPath(FilterState pkg)
        {
            if (pkg.Data == null || pkg.SourcePath == null || pkg.SourcePath.Length < 1) yield break;

            var root = pkg.SourcePath[0];
            var data = pkg.Data;
            var pathSkip = 1;

            if (root == "#")
            {
                if (pkg.SourcePath.Length < 2) yield break;
                var newFilter = pkg.RedirectFilter(pkg.SourcePath[1]);
                if (newFilter == null) yield break;
                data = ApplyFilterRecursive(newFilter);
                if (data == null) yield break;
                pathSkip = 2; // root and filter name
            }
            else if (root == "D")
            {
                if (pkg.SourcePath.Length < 1)  yield break;
                data = pkg.RepeaterData;
                if (data == null) throw new Exception("Found a repeater filter on a page with no repeater data");
                pathSkip = 1; // root
            }
            else if (root != "") throw new Exception($"Unexpected root marker: {root}");

            // Walk the path. Each time we hit an array, recurse down the path
            var walk = FindPathRecursive(pkg.SourcePath.Skip(pathSkip), data);
            foreach (var value in walk)
            {
                if (value is ArrayList list)
                {
                    foreach (var item in list)
                    {
                        if (item != null) yield return item;
                    }
                }
                else
                {
                    yield return value;
                }
            }
        }

        private static IEnumerable<object> FindPathRecursive(IEnumerable<string> path, object sourceData)
        {
            var target = sourceData;
            var remainingPath = new Queue<string>(path);
            while (remainingPath.Count > 0)
            {
                var name = remainingPath.Dequeue();

                if (name.StartsWith('[') && name.EndsWith(']'))
                {
                    // array reference.
                    if (remainingPath.Count < 1) yield break; // didn't find a valid item
                    if (!(target is ArrayList list)) yield break; // can't do it

                    // Ignore the actual index, and recurse over every actual item
                    var pathArray = remainingPath.ToArray();
                    foreach (var item in list)
                    {
                        if (item == null) continue;
                        var walk = FindPathRecursive(pathArray, item);
                        foreach (var value in walk)
                        {
                            yield return value;
                        }
                    }

                    yield break;
                }

                if (target is Dictionary<string, object> dict)
                {
                    if (!dict.ContainsKey(name))  yield break; // data ended before path
                    target = dict[name];
                    if (remainingPath.Count > 0) continue; // otherwise we *require* a single value at target
                }

                // We have a single value.
                yield return target;
                yield break;
            }
            yield return target;
        }

        private static object? GetFixedValue(FilterState pkg)
        {
            var key = nameof(TextMappingParams.Text);
            return pkg.Params.ContainsKey(key) ? pkg.Params[key] : null;
        }
        
        private static void AddRunningTotal(Dictionary<string,decimal> runningTotals, string[]? sourcePath, string[]? prefixPath, decimal value)
        {
            if (sourcePath == null || sourcePath.Length < 1) return;
            
            var pathStr = JoinPathWithoutArrayIndexes(sourcePath);
            if (runningTotals.ContainsKey(pathStr)) runningTotals[pathStr] += value; 
            else runningTotals.Add(pathStr, value);

            if (prefixPath != null)
            {
                pathStr = JoinPathWithoutArrayIndexes(prefixPath) + pathStr.Substring(1); // clip off the 'D'
                if (runningTotals.ContainsKey(pathStr)) runningTotals[pathStr] += value; 
                else runningTotals.Add(pathStr, value);
            }
        }

        private static string JoinPathWithoutArrayIndexes(string[] sourcePath)
        {
            return string.Join(".", sourcePath.Where(s=> !s.StartsWith('[') && !s.EndsWith(']')));
        }

        public static string[]? FindSourcePath(string filterName, Dictionary<string,MappingInfo> indexDataFilters)
        {
            return indexDataFilters.ContainsKey(filterName) ? indexDataFilters[filterName].DataPath : null;
        }
    }
}